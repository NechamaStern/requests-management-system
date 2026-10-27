import { Component, OnInit, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, ValidatorFn } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { EMPTY_FIELD_FILTERS, FieldFilters, RequestFilters } from '../../../models/request-filters.model';
import { EMPTY_FILTERS } from '../../../services/utils/request-filters.utils';
import { PRIORITY_DISPLAY, STATUS_DISPLAY } from '../../../models/request-display';

const SEARCH_DEBOUNCE_MS = 400;

// ISO dates (yyyy-mm-dd) compare correctly as strings.
const dateRangeValidator: ValidatorFn = (group) => {
  const { createdFrom, createdTo } = group.value;
  return createdFrom && createdTo && createdFrom > createdTo ? { dateRange: true } : null;
};

@Component({
  selector: 'app-requests-filters',
  imports: [ReactiveFormsModule],
  templateUrl: './requests-filters.html',
  styleUrl: './requests-filters.scss',
})
export class RequestsFilters implements OnInit {
  // Values the form starts with (e.g. restored from the URL). Read once on init.
  readonly initial = input<RequestFilters>(EMPTY_FILTERS);
  readonly search = output<RequestFilters>();

  protected readonly statuses = Object.entries(STATUS_DISPLAY).map(([value, d]) => ({ value, label: d.label }));
  protected readonly priorities = Object.entries(PRIORITY_DISPLAY).map(([value, d]) => ({ value, label: d.label }));

  // Free text search: applied automatically (debounced).
  protected readonly searchControl = new FormControl('', { nonNullable: true });

  // Field filters: applied only when the user presses the button.
  protected readonly form = new FormBuilder().nonNullable.group({ ...EMPTY_FIELD_FILTERS }, { validators: dateRangeValidator });

  // Last applied field filters. Edits in the form that were not applied yet are not sent.
  private appliedFields: FieldFilters = { ...EMPTY_FIELD_FILTERS };

  constructor() {
    this.searchControl.valueChanges
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => this.emit());
  }

  ngOnInit(): void {
    const { search, ...fields } = this.initial();
    this.searchControl.setValue(search, { emitEvent: false });
    this.form.reset(fields);
    this.appliedFields = fields;
  }

  protected applyFilters(): void {
    if (this.form.invalid) {
      return;
    }
    this.appliedFields = this.form.getRawValue();
    this.emit();
  }

  protected reset(): void {
    this.form.reset(EMPTY_FIELD_FILTERS);
    this.searchControl.reset('', { emitEvent: false });
    this.appliedFields = { ...EMPTY_FIELD_FILTERS };
    this.emit();
  }

  // Every request carries the typed text together with the last applied field filters.
  private emit(): void {
    this.search.emit({ search: this.searchControl.value, ...this.appliedFields });
  }
}
