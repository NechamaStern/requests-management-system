import { Component, input, output } from '@angular/core';

import { RequestSort, SORT_OPTIONS, SortField } from '../../../models/request-sort.model';

@Component({
  selector: 'app-requests-sort',
  templateUrl: './requests-sort.html',
  styleUrl: './requests-sort.scss',
})
export class RequestsSort {
  readonly sort = input.required<RequestSort>();
  readonly sortChange = output<RequestSort>();

  protected readonly options = SORT_OPTIONS;

  // Clicking the active field flips its direction; clicking another field selects it.
  protected select(field: SortField): void {
    const current = this.sort();
    if (current.sortBy === field) {
      this.sortChange.emit({ sortBy: field, sortDirection: current.sortDirection === 'Asc' ? 'Desc' : 'Asc' });
      return;
    }
    const option = this.options.find((o) => o.field === field)!;
    this.sortChange.emit({ sortBy: field, sortDirection: option.defaultDirection });
  }
}
