import { DatePipe } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';

import { RequestsService } from '../../../services/api/requests.service';
import { toApiError } from '../../../services/utils/api-error.utils';
import { STATUS_DISPLAY } from '../../../models/request-display';

@Component({
  selector: 'app-request-history',
  imports: [DatePipe],
  templateUrl: './request-history.html',
  styleUrl: './request-history.scss',
})
export class RequestHistory {
  private readonly requestsService = inject(RequestsService);

  readonly requestId = input.required<number>();
  // Changes after every update of the request, which reloads the history.
  readonly version = input.required<string>();

  // rxResource switches to the latest request and cancels the previous one.
  protected readonly history = rxResource({
    params: () => ({ id: this.requestId(), version: this.version() }),
    stream: ({ params }) => this.requestsService.getHistory(params.id),
  });

  protected readonly errorMessage = computed(() => {
    const error = this.history.error();
    return error ? toApiError(error).message : null;
  });

  protected readonly display = STATUS_DISPLAY;
}
