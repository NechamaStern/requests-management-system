export type RequestStatus = 'New' | 'InProgress' | 'Waiting' | 'Completed';
export type RequestPriority = 'Low' | 'Medium' | 'High';

// Mirrors the server DTO returned by GET /api/requests/summary.
export interface CountItem<TKey extends string> {
  key: TKey;
  count: number;
}

export interface RequestsSummary {
  total: number;
  byStatus: CountItem<RequestStatus>[];
  byPriority: CountItem<RequestPriority>[];
}

// A row as displayed in a breakdown card.
export interface BreakdownItem {
  key: string;
  label: string;
  count: number;
  color: string;
}
