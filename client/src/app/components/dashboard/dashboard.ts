import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { RequestsSummary } from '../../models/dashboard.model';
import { RequestsService } from '../../services/api/requests.service';
import { RequestFilters } from '../../models/request-filters.model';
import { toQueryParams } from '../../services/utils/request-filters.utils';
import { RequestsFilters } from '../shared/requests-filters/requests-filters';
import { BreakdownCard } from './breakdown-card/breakdown-card';
import { PRIORITY_DISPLAY, STATUS_DISPLAY } from '../../models/request-display';
import { toBreakdownItems } from './dashboard.utils';

type LoadState = 'loading' | 'error' | 'ready';

@Component({
  selector: 'app-dashboard',
  imports: [BreakdownCard, RequestsFilters],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly requestsService = inject(RequestsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  protected readonly state = signal<LoadState>('loading');
  protected readonly summary = signal<RequestsSummary | null>(null);

  protected readonly statusItems = computed(() =>
    toBreakdownItems(STATUS_DISPLAY, this.summary()?.byStatus ?? []));
  protected readonly priorityItems = computed(() =>
    toBreakdownItems(PRIORITY_DISPLAY, this.summary()?.byPriority ?? []));

  protected onSearch(filters: RequestFilters): void {
    this.router.navigate(['/requests'], { queryParams: toQueryParams(filters) });
  }

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.requestsService
      .getSummary()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (summary) => {
          this.summary.set(summary);
          this.state.set('ready');
        },
        error: () => this.state.set('error'),
      });
  }
}
