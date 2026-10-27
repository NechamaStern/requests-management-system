import { BreakdownItem, CountItem } from '../../models/dashboard.model';
import { Display } from '../../models/request-display';

export function toBreakdownItems<TKey extends string>(
  display: Record<TKey, Display>,
  counts: CountItem<TKey>[],
): BreakdownItem[] {
  return (Object.keys(display) as TKey[]).map((key) => ({
    key,
    label: display[key].label,
    color: display[key].color,
    count: counts.find((c) => c.key === key)?.count ?? 0,
  }));
}
