import { Component, computed, input, output } from '@angular/core';

import { RequestStatus } from '../../../models/dashboard.model';
import { BulkFailureReason, BulkStatusResult } from '../../../models/request.model';
import { STATUS_DISPLAY } from '../../../models/request-display';

const REASON_LABELS: Record<BulkFailureReason, string> = {
  NotFound: 'לא נמצאה',
  Conflict: 'עודכנה ע"י משתמש אחר',
  InvalidTransition: 'מעבר סטטוס לא תקין',
  AlreadyInStatus: 'כבר בסטטוס המבוקש',
};

export interface BulkOutcome {
  status: RequestStatus;
  result: BulkStatusResult;
  // Titles of the requests as they were on screen, so failures can be shown by name.
  titles: Record<number, string>;
}

@Component({
  selector: 'app-bulk-result',
  templateUrl: './bulk-result.html',
  styleUrl: './bulk-result.scss',
})
export class BulkResult {
  readonly outcome = input.required<BulkOutcome>();
  readonly dismissed = output<void>();

  protected readonly statusLabel = computed(() => STATUS_DISPLAY[this.outcome().status].label);
  protected readonly failures = computed(() =>
    this.outcome().result.failed.map((f) => ({
      id: f.id,
      title: this.outcome().titles[f.id] ?? `פניה ${f.id}`,
      reasonLabel: REASON_LABELS[f.reason],
      message: f.message,
    })),
  );
}
