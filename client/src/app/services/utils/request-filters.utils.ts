import { Params } from '@angular/router';

import { EMPTY_FIELD_FILTERS, RequestFilters } from '../../models/request-filters.model';
import { DEFAULT_SORT, RequestSort, SORT_OPTIONS } from '../../models/request-sort.model';

export const EMPTY_FILTERS: RequestFilters = { search: '', ...EMPTY_FIELD_FILTERS };

// The URL is the single source of truth for the current search: filters + sort + page.
// Default values are omitted to keep the URL short.
export function toQueryParams(filters: RequestFilters, page = 1, sort: RequestSort = DEFAULT_SORT): Params {
  const params: Params = {};
  for (const [key, value] of Object.entries(filters)) {
    if (value !== '') {
      params[key] = value;
    }
  }
  if (sort.sortBy !== DEFAULT_SORT.sortBy || sort.sortDirection !== DEFAULT_SORT.sortDirection) {
    params['sortBy'] = sort.sortBy;
    params['sortDirection'] = sort.sortDirection;
  }
  if (page > 1) {
    params['page'] = page;
  }
  return params;
}

export function fromQueryParams(params: Params): { filters: RequestFilters; sort: RequestSort; page: number } {
  const filters = { ...EMPTY_FILTERS };
  for (const key of Object.keys(filters) as (keyof RequestFilters)[]) {
    if (typeof params[key] === 'string') {
      (filters as Record<string, string>)[key] = params[key];
    }
  }
  const page = Number(params['page']);
  return { filters, sort: parseSort(params), page: Number.isInteger(page) && page > 1 ? page : 1 };
}

// Unknown values fall back to the default instead of being sent to the server.
function parseSort(params: Params): RequestSort {
  const field = SORT_OPTIONS.find((o) => o.field === params['sortBy'])?.field;
  const direction = params['sortDirection'] === 'Asc' || params['sortDirection'] === 'Desc'
    ? params['sortDirection']
    : undefined;
  return field && direction ? { sortBy: field, sortDirection: direction } : DEFAULT_SORT;
}
