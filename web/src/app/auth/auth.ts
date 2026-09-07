import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { WarehouseApi } from '../api/warehouse-api';

const StorageKey = 'warehouse-manager.access-token';

@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly api = inject(WarehouseApi);
  private readonly token = signal<string | null>(localStorage.getItem(StorageKey));

  readonly isAuthenticated = computed(() => this.token() !== null);

  accessToken(): string | null {
    return this.token();
  }

  login(email: string, password: string): Observable<void> {
    return this.api.login({ email, password }).pipe(
      tap(response => {
        localStorage.setItem(StorageKey, response.accessToken);
        this.token.set(response.accessToken);
      }),
      map(() => undefined)
    );
  }

  logout(): void {
    localStorage.removeItem(StorageKey);
    this.token.set(null);
  }
}
