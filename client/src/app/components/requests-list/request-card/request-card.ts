import { DatePipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';

import { RequestListItem } from '../../../models/request.model';
import { PRIORITY_DISPLAY, STATUS_DISPLAY } from '../../../models/request-display';

@Component({
  selector: 'app-request-card',
  imports: [DatePipe],
  templateUrl: './request-card.html',
  styleUrl: './request-card.scss',
})
export class RequestCard {
  readonly request = input.required<RequestListItem>();

  readonly selected = input(false);

  readonly viewDetails = output<RequestListItem>();
  readonly selectionChange = output<boolean>();

  protected readonly status = computed(() => STATUS_DISPLAY[this.request().status]);
  protected readonly priority = computed(() => PRIORITY_DISPLAY[this.request().priority]);
}
