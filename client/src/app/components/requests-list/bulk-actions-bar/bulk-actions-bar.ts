import { Component, input, output, signal } from '@angular/core';

import { RequestStatus } from '../../../models/dashboard.model';
import { STATUS_DISPLAY } from '../../../models/request-display';

@Component({
  selector: 'app-bulk-actions-bar',
  templateUrl: './bulk-actions-bar.html',
  styleUrl: './bulk-actions-bar.scss',
})
export class BulkActionsBar {
  readonly count = input.required<number>();
  readonly busy = input(false);

  readonly apply = output<RequestStatus>();
  readonly cleared = output<void>();

  // All statuses are offered: validity depends on each request's current status, which the server checks per item.
  protected readonly options = Object.entries(STATUS_DISPLAY).map(([value, d]) => ({
    value: value as RequestStatus,
    label: d.label,
  }));
  protected readonly status = signal<RequestStatus | ''>('');

  protected onApply(): void {
    const status = this.status();
    if (status) {
      this.apply.emit(status);
    }
  }
}
