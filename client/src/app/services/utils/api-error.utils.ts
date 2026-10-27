import { HttpErrorResponse } from '@angular/common/http';

export interface ApiError {
  status: number;
  message: string;
  retryable: boolean;
}

// Outcome of a failed update: a conflict (409) needs a "load latest data" action, anything else only a message.
export interface UpdateProblem {
  kind: 'conflict' | 'other';
  message: string;
}

const MESSAGES_BY_STATUS: Record<number, string> = {
  0: 'אין חיבור לשרת. בדקו את החיבור ונסו שוב.',
  404: 'המשאב המבוקש לא נמצא.',
  409: 'הפנייה עודכנה על ידי משתמש אחר. רעננו את הנתונים ונסו שוב.',
};
const GENERIC_ERROR = 'אירעה שגיאה. נסו שוב מאוחר יותר.';

function isApiError(err: unknown): err is ApiError {
  return typeof err === 'object' && err !== null && !(err instanceof HttpErrorResponse)
    && 'status' in err && 'message' in err && 'retryable' in err;
}

// The server's ProblemDetails `detail` is the preferred message; the status table is the fallback.
// Idempotent: the error interceptor already converts HTTP errors, so an ApiError is returned as is.
// Input validation is done in the client before sending, so a 400 is unexpected and not retryable.
export function toApiError(err: unknown): ApiError {
  if (isApiError(err)) {
    return err;
  }
  if (err instanceof HttpErrorResponse) {
    return {
      status: err.status,
      message: err.error?.detail ?? MESSAGES_BY_STATUS[err.status] ?? GENERIC_ERROR,
      retryable: err.status !== 400 && err.status !== 404 && err.status !== 409 && err.status !== 422,
    };
  }
  return { status: -1, message: GENERIC_ERROR, retryable: true };
}

export function toUpdateProblem(err: unknown): UpdateProblem {
  const { status, message } = toApiError(err);
  return { kind: status === 409 ? 'conflict' : 'other', message };
}
