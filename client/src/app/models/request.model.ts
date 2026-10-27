import { RequestPriority, RequestStatus } from './dashboard.model';

// Mirrors the server DTO returned by GET /api/requests.
export interface RequestListItem {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedToUserId: number | null;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

// Body of PUT /api/requests/{id}. rowVersion is the value last read; the server rejects the update with 409 if it is stale.
export interface UpdateRequestPayload {
  status: RequestStatus;
  priority: RequestPriority;
  assignedToUserId: number | null;
  rowVersion: string;
  changedByUserId: number;
}

export interface StatusHistoryItem {
  id: number;
  requestId: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedByUserId: number;
  changedBy: string;
}

export interface User {
  id: number;
  displayName: string;
}

// Mirrors the allowed workflow enforced by the server (the server stays the authority); used only to offer valid choices.
export const STATUS_TRANSITIONS: Record<RequestStatus, RequestStatus[]> = {
  New: ['InProgress'],
  InProgress: ['Waiting', 'Completed'],
  Waiting: ['InProgress', 'Completed'],
  Completed: [],
};

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// Body of POST /api/requests/bulk-status (up to 100 requests).
export interface BulkStatusPayload {
  status: RequestStatus;
  changedByUserId: number;
  items: { id: number; rowVersion: string }[];
}

export type BulkFailureReason = 'NotFound' | 'Conflict' | 'InvalidTransition' | 'AlreadyInStatus';

export interface BulkStatusFailure {
  id: number;
  reason: BulkFailureReason;
  message: string;
}

// Partial success: each request either succeeded or is listed in `failed` with the reason.
export interface BulkStatusResult {
  requestedCount: number;
  succeededCount: number;
  failedCount: number;
  succeeded: RequestListItem[];
  failed: BulkStatusFailure[];
}
