export type SortField = 'CreatedAt' | 'UpdatedAt' | 'Title' | 'OrganizationName' | 'Status' | 'Priority';
export type SortDirection = 'Asc' | 'Desc';

// Mirrors the sortBy / sortDirection query parameters of GET /api/requests.
export interface RequestSort {
  sortBy: SortField;
  sortDirection: SortDirection;
}

export const DEFAULT_SORT: RequestSort = { sortBy: 'CreatedAt', sortDirection: 'Desc' };

// Display order of the sort buttons. `defaultDirection` is used when a field is first selected.
export const SORT_OPTIONS: { field: SortField; label: string; defaultDirection: SortDirection }[] = [
  { field: 'CreatedAt', label: 'תאריך יצירה', defaultDirection: 'Desc' },
  { field: 'UpdatedAt', label: 'עודכן לאחרונה', defaultDirection: 'Desc' },
  { field: 'Title', label: 'כותרת', defaultDirection: 'Asc' },
  { field: 'OrganizationName', label: 'ארגון', defaultDirection: 'Asc' },
  { field: 'Status', label: 'סטטוס', defaultDirection: 'Asc' },
  { field: 'Priority', label: 'עדיפות', defaultDirection: 'Desc' },
];
