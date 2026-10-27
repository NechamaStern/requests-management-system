import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';

import { RequestPriority, RequestStatus } from '../../models/dashboard.model';
import { RequestListItem, STATUS_TRANSITIONS, User } from '../../models/request.model';
import { RequestsService } from '../../services/api/requests.service';
import { UsersService } from '../../services/api/users.service';
import { CURRENT_USER_ID } from '../../services/current-user';
import { UpdateProblem, toUpdateProblem } from '../../services/utils/api-error.utils';
import { PRIORITY_DISPLAY, STATUS_DISPLAY } from '../../models/request-display';
import { RequestHistory } from './request-history/request-history';

@Component({
  selector: 'app-request-details',
  imports: [DatePipe, ReactiveFormsModule, RequestHistory],
  templateUrl: './request-details.html',
  styleUrl: './request-details.scss',
  host: { '(document:keydown.escape)': 'closed.emit()' },
})
export class RequestDetails {
  private readonly requestsService = inject(RequestsService);
  private readonly usersService = inject(UsersService);
  private readonly destroyRef = inject(DestroyRef);

  readonly request = input.required<RequestListItem>();
  readonly closed = output<void>();
  // Emits the latest server state after a successful save or a refresh.
  readonly updated = output<RequestListItem>();

  protected readonly form = new FormGroup({
    status: new FormControl<RequestStatus>('New', { nonNullable: true }),
    priority: new FormControl<RequestPriority>('Medium', { nonNullable: true }),
    assignedToUserId: new FormControl<number | null>(null),
  });

  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly problem = signal<UpdateProblem | null>(null);
  protected readonly refreshing = signal(false);
  protected readonly users = signal<User[]>([]);
  protected readonly usersFailed = signal(false);

  protected readonly status = computed(() => STATUS_DISPLAY[this.request().status]);

  // Only the current status and the transitions the server allows from it.
  protected readonly statusOptions = computed(() =>
    [this.request().status, ...STATUS_TRANSITIONS[this.request().status]].map((value) => ({
      value,
      label: STATUS_DISPLAY[value].label,
    })),
  );
  protected readonly priorityOptions = Object.entries(PRIORITY_DISPLAY).map(([value, d]) => ({
    value: value as RequestPriority,
    label: d.label,
  }));

  // Keeps the current assignee selectable even if the users list failed to load or the user is inactive.
  protected readonly assigneeOptions = computed(() => {
    const { assignedToUserId, assignedTo } = this.request();
    const users = this.users();
    return assignedToUserId !== null && assignedTo && !users.some((u) => u.id === assignedToUserId)
      ? [...users, { id: assignedToUserId, displayName: assignedTo }]
      : users;
  });

  constructor() {
    this.usersService
      .getUsers()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (users) => this.users.set(users),
        error: () => this.usersFailed.set(true),
      });

    // Whenever the server state changes (open, save, refresh) the form starts from it.
    // emitEvent: false, so the reset is not mistaken for a user edit.
    effect(() => {
      const r = this.request();
      this.form.reset(
        { status: r.status, priority: r.priority, assignedToUserId: r.assignedToUserId },
        { emitEvent: false },
      );
    });

    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.saved.set(false));
  }

  protected save(): void {
    if (this.form.pristine || this.saving()) {
      return;
    }
    const request = this.request();
    this.saving.set(true);
    this.saved.set(false);
    this.problem.set(null);

    this.requestsService
      .updateRequest(request.id, {
        ...this.form.getRawValue(),
        rowVersion: request.rowVersion,
        changedByUserId: CURRENT_USER_ID,
      })
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (item) => {
          this.updated.emit(item);
          this.saved.set(true);
        },
        error: (err) => this.problem.set(toUpdateProblem(err)),
      });
  }

  // After a 409: drop the local edits and load the version someone else saved.
  protected refresh(): void {
    this.refreshing.set(true);
    this.requestsService
      .getRequest(this.request().id)
      .pipe(
        finalize(() => this.refreshing.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (item) => {
          this.problem.set(null);
          this.updated.emit(item);
        },
        error: (err) => this.problem.set(toUpdateProblem(err)),
      });
  }
}
