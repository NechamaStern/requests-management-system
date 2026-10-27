import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BehaviorSubject, catchError, combineLatest, finalize, map, of, switchMap, tap } from 'rxjs';

import { RequestStatus } from '../../models/dashboard.model';
import { RequestFilters } from '../../models/request-filters.model';
import { DEFAULT_SORT, RequestSort } from '../../models/request-sort.model';
import { PagedResult, RequestListItem } from '../../models/request.model';
import { RequestsService } from '../../services/api/requests.service';
import { CURRENT_USER_ID } from '../../services/current-user';
import { ApiError, toUpdateProblem } from '../../services/utils/api-error.utils';
import { fromQueryParams, toQueryParams } from '../../services/utils/request-filters.utils';
import { Pagination } from './pagination/pagination';
import { RequestCard } from './request-card/request-card';
import { BulkActionsBar } from './bulk-actions-bar/bulk-actions-bar';
import { BulkOutcome, BulkResult } from './bulk-result/bulk-result';
import { RequestDetails } from '../request-details/request-details';
import { RequestsSort } from './requests-sort/requests-sort';
import { RequestsFilters } from '../shared/requests-filters/requests-filters';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-requests-list',
  imports: [RouterLink, RequestsFilters, RequestsSort, RequestCard, BulkActionsBar, BulkResult, RequestDetails, Pagination],
  templateUrl: './requests-list.html',
  styleUrl: './requests-list.scss',
})
export class RequestsList {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly requestsService = inject(RequestsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new BehaviorSubject<void>(undefined);
  private detailsChanged = false;

  protected readonly loading = signal(true);
  protected readonly result = signal<PagedResult<RequestListItem> | null>(null);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly sort = signal<RequestSort>(DEFAULT_SORT);
  protected readonly selected = signal<RequestListItem | null>(null);
  protected readonly bulkBusy = signal(false);
  protected readonly bulkOutcome = signal<BulkOutcome | null>(null);
  protected readonly bulkError = signal<string | null>(null);
  protected readonly selectedIds = signal<ReadonlySet<number>>(new Set());
  protected readonly allSelected = computed(() => {
    const items = this.result()?.items ?? [];
    return items.length > 0 && items.every((r) => this.selectedIds().has(r.id));
  });
  protected readonly someSelected = computed(() => this.selectedIds().size > 0 && !this.allSelected());

  // Used only to initialize the filter form; later changes come from the form itself.
  protected readonly initialFilters = fromQueryParams(this.route.snapshot.queryParams).filters;

  constructor() {
    // The URL drives the search. switchMap cancels the in-flight request when the URL changes again.
    combineLatest([this.route.queryParams, this.reload$])
      .pipe(
        map(([params]) => fromQueryParams(params)),
        tap(({ sort }) => {
          // The previous results stay on screen (dimmed) until the new ones arrive.
          this.loading.set(true);
          this.error.set(null);
          this.sort.set(sort);
          window.scrollTo({ top: 0, behavior: 'smooth' });
        }),
        switchMap(({ filters, sort, page }) =>
          this.requestsService.searchRequests(filters, sort, page, PAGE_SIZE).pipe(
            catchError((err: ApiError) => of(err)),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((outcome) => {
        if ('items' in outcome) {
          this.result.set(outcome);
          // Selection is per page: any new result set (page, filter, sort, reload) clears it.
          this.selectedIds.set(new Set());
        } else {
          this.result.set(null);
          this.error.set(outcome);
        }
        this.loading.set(false);
      });
  }

  // New filters or a new sort restart from page 1; changing the page keeps both.
  protected onSearch(filters: RequestFilters): void {
    this.router.navigate([], { queryParams: toQueryParams(filters, 1, this.sort()) });
  }

  protected onSortChange(sort: RequestSort): void {
    const { filters } = fromQueryParams(this.route.snapshot.queryParams);
    this.router.navigate([], { queryParams: toQueryParams(filters, 1, sort) });
  }

  protected onPageChange(page: number): void {
    const { filters, sort } = fromQueryParams(this.route.snapshot.queryParams);
    this.router.navigate([], { queryParams: toQueryParams(filters, page, sort) });
  }

  protected toggleOne(id: number, checked: boolean): void {
    this.selectedIds.update((ids) => {
      const next = new Set(ids);
      if (checked) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  protected toggleAll(checked: boolean): void {
    this.selectedIds.set(new Set(checked ? (this.result()?.items ?? []).map((r) => r.id) : []));
  }

  // Selected requests are sent with the RowVersion shown on screen; the server reports per-request success or failure.
  protected onBulkApply(status: RequestStatus): void {
    const selected = (this.result()?.items ?? []).filter((r) => this.selectedIds().has(r.id));
    if (selected.length === 0 || this.bulkBusy()) {
      return;
    }
    this.bulkBusy.set(true);
    this.bulkOutcome.set(null);
    this.bulkError.set(null);

    this.requestsService
      .bulkUpdateStatus({
        status,
        changedByUserId: CURRENT_USER_ID,
        items: selected.map((r) => ({ id: r.id, rowVersion: r.rowVersion })),
      })
      .pipe(
        finalize(() => this.bulkBusy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.bulkOutcome.set({
            status,
            result,
            titles: Object.fromEntries(selected.map((r) => [r.id, r.title])),
          });
          // Reloading shows the new statuses and clears the selection.
          this.reload$.next();
        },
        error: (err) => this.bulkError.set(toUpdateProblem(err).message),
      });
  }

  protected retry(): void {
    this.reload$.next();
  }

  protected onRequestUpdated(updated: RequestListItem): void {
    this.selected.set(updated);
    this.detailsChanged = true;
  }

  // The edited request may no longer match the filters or sort order, so the list is reloaded after changes.
  protected closeDetails(): void {
    this.selected.set(null);
    if (this.detailsChanged) {
      this.detailsChanged = false;
      this.reload$.next();
    }
  }
}
