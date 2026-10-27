import { RequestPriority, RequestStatus } from './dashboard.model';

export interface Display {
  label: string;
  color: string;
}

// Insertion order defines the display order of the rows.
export const STATUS_DISPLAY: Record<RequestStatus, Display> = {
  New: { label: 'חדשה', color: '#3b82f6' },
  InProgress: { label: 'בטיפול', color: '#f59e0b' },
  Waiting: { label: 'ממתינה', color: '#8b5cf6' },
  Completed: { label: 'הושלמה', color: '#10b981' },
};

export const PRIORITY_DISPLAY: Record<RequestPriority, Display> = {
  High: { label: 'גבוהה', color: '#ef4444' },
  Medium: { label: 'בינונית', color: '#f59e0b' },
  Low: { label: 'נמוכה', color: '#10b981' },
};
