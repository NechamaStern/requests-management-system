import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { RequestsSummary } from '../../models/dashboard.model';
import { RequestFilters } from '../../models/request-filters.model';
import {
  BulkStatusPayload,
  BulkStatusResult,
  PagedResult,
  RequestListItem,
  StatusHistoryItem,
  UpdateRequestPayload,
} from '../../models/request.model';
import { RequestSort } from '../../models/request-sort.model';

@Injectable({ providedIn: 'root' })
export class RequestsService {
  private readonly http = inject(HttpClient);

  // Empty filter values are omitted from the query string.
  searchRequests(
    filters: RequestFilters,
    sort: RequestSort,
    page: number,
    pageSize: number,
  ): Observable<PagedResult<RequestListItem>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('sortBy', sort.sortBy)
      .set('sortDirection', sort.sortDirection);
    for (const [key, value] of Object.entries(filters)) {
      if (value !== '') {
        params = params.set(key, value);
      }
    }
    return this.http.get<PagedResult<RequestListItem>>('/api/requests', { params });
  }

  getRequest(id: number): Observable<RequestListItem> {
    return this.http.get<RequestListItem>(`/api/requests/${id}`);
  }

  getHistory(id: number): Observable<StatusHistoryItem[]> {
    return this.http.get<StatusHistoryItem[]>(`/api/requests/${id}/history`);
  }

  updateRequest(id: number, payload: UpdateRequestPayload): Observable<RequestListItem> {
    return this.http.put<RequestListItem>(`/api/requests/${id}`, payload);
  }

  bulkUpdateStatus(payload: BulkStatusPayload): Observable<BulkStatusResult> {
    return this.http.post<BulkStatusResult>('/api/requests/bulk-status', payload);
  }

  getSummary(): Observable<RequestsSummary> {
    return this.http.get<RequestsSummary>('/api/requests/summary');
  }
}
