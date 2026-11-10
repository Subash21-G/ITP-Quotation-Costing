import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, tap, throwError } from 'rxjs';

export interface AuthSession {
  accessToken: string;
  expiresAt: string;
  username: string;
  displayName: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly key = 'itp-session';
  readonly session = signal<AuthSession | null>(this.restore());
  readonly canEdit = computed(() => true);

  token(): string | null {
    const session = this.session();
    if (!session || Date.parse(session.expiresAt) <= Date.now()) {
      this.clear();
      return null;
    }
    return session.accessToken;
  }

  login(username: string, password: string) {
    return this.http.post<AuthSession>('/api/auth/login', { username, password }).pipe(tap(session => {
      this.session.set(session);
      try { sessionStorage.setItem(this.key, JSON.stringify(session)); } catch { /* Memory-only session. */ }
    }));
  }

  register(username: string, password: string, displayName: string) {
    return this.http.post<AuthSession>('/api/auth/register', { username, password, displayName }).pipe(tap(session => {
      this.session.set(session);
      try { sessionStorage.setItem(this.key, JSON.stringify(session)); } catch { /* Memory-only session. */ }
    }));
  }

  clear() {
    this.session.set(null);
    try { sessionStorage.removeItem(this.key); } catch { /* Storage may be unavailable. */ }
  }

  logout() {
    this.clear();
    void this.router.navigateByUrl('/login');
  }

  private restore(): AuthSession | null {
    try {
      const value = JSON.parse(sessionStorage.getItem(this.key) ?? 'null');
      return value && typeof value.accessToken === 'string' && value.accessToken.length > 0 &&
        typeof value.username === 'string' && typeof value.displayName === 'string' &&
        Array.isArray(value.roles) && value.roles.every((role: unknown) => typeof role === 'string') &&
        Date.parse(value.expiresAt) > Date.now() ? value : null;
    } catch { return null; }
  }
}

export const authGuard: CanActivateFn = (_route, state) => {
  return inject(AuthService).token() ? true : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const editorGuard: CanActivateFn = () => inject(AuthService).canEdit() || inject(Router).createUrlTree(['/dashboard']);

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  // Never attach credentials to third-party URLs.
  if (!request.url.startsWith('/api/') || ['/api/auth/login', '/api/auth/register'].includes(request.url.split('?')[0])) return next(request);
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.token();
  const authenticated = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  return next(authenticated).pipe(catchError((error: HttpErrorResponse) => {
    if (error.status === 401 && (!token || auth.session()?.accessToken === token)) {
      const returnUrl = router.url;
      auth.clear();
      void router.navigate(['/login'], { queryParams: { returnUrl } });
    }
    return throwError(() => error);
  }));
};
