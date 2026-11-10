import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService, AuthSession, authInterceptor } from './auth.service';

describe('Authentication', () => {
  const session: AuthSession = { accessToken: 'test-token', expiresAt: '2099-01-01T00:00:00Z', username: 'viewer', displayName: 'Viewer', roles: ['Viewer'] };
  let auth: AuthService;
  let http: HttpClient;
  let requests: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()] });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { requests.verify(); sessionStorage.clear(); });

  it('stores a login session without granting Viewer editing rights', () => {
    auth.login('viewer', 'password').subscribe();
    const request = requests.expectOne('/api/auth/login');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(session);
    expect(auth.token()).toBe('test-token');
    expect(auth.canEdit()).toBe(false);
    expect(JSON.parse(sessionStorage.getItem('itp-session')!).username).toBe('viewer');
  });

  it('registers a Viewer without sending authentication and stores the session', () => {
    auth.register('new-viewer', 'Password!123', 'New Viewer').subscribe();
    const request = requests.expectOne('/api/auth/register');
    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(request.request.body).toEqual({ username: 'new-viewer', password: 'Password!123', displayName: 'New Viewer' });
    request.flush({ ...session, username: 'new-viewer', displayName: 'New Viewer' });
    expect(auth.token()).toBe('test-token');
    expect(auth.canEdit()).toBe(false);
  });

  it('authenticates API downloads but never third-party requests', () => {
    auth.session.set(session);
    http.get('/api/quotations/1/pdf', { responseType: 'blob' }).subscribe();
    const pdf = requests.expectOne('/api/quotations/1/pdf');
    expect(pdf.request.headers.get('Authorization')).toBe('Bearer test-token');
    pdf.flush(new Blob());
    http.get('https://example.org/api/data').subscribe();
    const external = requests.expectOne('https://example.org/api/data');
    expect(external.request.headers.has('Authorization')).toBe(false);
    external.flush({});
  });

  it('clears expired sessions', () => {
    auth.session.set({ ...session, expiresAt: '2000-01-01T00:00:00Z' });
    expect(auth.token()).toBeNull();
    expect(auth.session()).toBeNull();
  });

  it('includes the bearer token when an administrator creates a user', () => {
    auth.session.set({ ...session, roles: ['Admin'] });
    http.post('/api/auth/users', { username: 'new-user' }).subscribe();
    const request = requests.expectOne('/api/auth/users');
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush({});
  });

  it('returns to login when the API rejects a session', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    auth.session.set(session);
    http.get('/api/customers').subscribe({ error: () => {} });
    requests.expectOne('/api/customers').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(auth.session()).toBeNull();
    expect(navigate).toHaveBeenCalled();
  });

  it('retains the session on a forbidden action', () => {
    auth.session.set(session);
    http.delete('/api/rfqs/1').subscribe({ error: () => {} });
    requests.expectOne('/api/rfqs/1').flush({}, { status: 403, statusText: 'Forbidden' });
    expect(auth.session()).toEqual(session);
  });
});
