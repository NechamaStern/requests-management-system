import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { toApiError } from '../utils/api-error.utils';

// Single place that converts every failed HTTP call into an ApiError, so callers never parse HttpErrorResponse.
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const apiError = toApiError(err);
      console.error(`${req.method} ${req.urlWithParams} failed (${apiError.status})`);
      return throwError(() => apiError);
    }),
  );
