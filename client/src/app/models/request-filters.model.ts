import { RequestPriority, RequestStatus } from './dashboard.model';

// Filters applied explicitly with the "show requests" button.
export interface FieldFilters {
  status: RequestStatus | '';
  priority: RequestPriority | '';
  organizationName: string;
  assignedTo: string;
  createdFrom: string;
  createdTo: string;
}

// Mirrors the query parameters of GET /api/requests. Empty values mean "no filter".
export interface RequestFilters extends FieldFilters {
  search: string;
}

export const EMPTY_FIELD_FILTERS: FieldFilters = {
  status: '',
  priority: '',
  organizationName: '',
  assignedTo: '',
  createdFrom: '',
  createdTo: '',
};
