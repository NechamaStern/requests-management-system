import { Component, computed, input } from '@angular/core';

import { BreakdownItem } from '../../../models/dashboard.model';

@Component({
  selector: 'app-breakdown-card',
  templateUrl: './breakdown-card.html',
  styleUrl: './breakdown-card.scss',
})
export class BreakdownCard {
  readonly title = input.required<string>();
  readonly items = input.required<BreakdownItem[]>();

  protected readonly rows = computed(() => {
    const items = this.items();
    const total = items.reduce((sum, item) => sum + item.count, 0);
    return items.map((item) => ({ ...item, percent: total === 0 ? 0 : (item.count / total) * 100 }));
  });
}
