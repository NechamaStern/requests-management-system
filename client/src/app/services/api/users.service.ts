import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';

import { User } from '../../models/request.model';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  // The list is small and rarely changes: loaded once. A failed load is not cached, so the next open retries.
  private users$?: Observable<User[]>;

  getUsers(): Observable<User[]> {
    this.users$ ??= this.http.get<User[]>('/api/users').pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.users$;
  }
}
