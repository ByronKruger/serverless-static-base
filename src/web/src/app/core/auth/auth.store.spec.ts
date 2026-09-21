import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { APP_CONFIG, AppConfig } from '../config/app-config';
import { AuthenticationResponse } from './auth.contracts';
import { AuthStore } from './auth.store';

const STORAGE_KEY = 'coffeeg.auth.session';

const config: AppConfig = {
  environmentName: 'test',
  apiBaseUrl: '/api',
  auth: { authority: 'https://identity.test', clientId: 'coffeeg-web', audience: 'coffeeg-api' },
};

function authenticationResponse(expiresAtUtc: string): AuthenticationResponse {
  return {
    accessToken: 'a.signed.token',
    expiresAtUtc,
    user: { id: 1, email: 'sam@coffeeg.test', userName: 'sam', roles: ['User'] },
  };
}

function inOneHour(): string {
  return new Date(Date.now() + 3_600_000).toISOString();
}

describe('AuthStore', () => {
  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: APP_CONFIG, useValue: config }],
    });
  });

  afterEach(() => localStorage.clear());

  it('starts signed out when nothing is stored', () => {
    const store = TestBed.inject(AuthStore);

    expect(store.isAuthenticated()).toBe(false);
    expect(store.userName()).toBeNull();
    expect(store.accessToken()).toBeNull();
  });

  it('signs in with an email and exposes the username for the header', () => {
    const store = TestBed.inject(AuthStore);
    const http = TestBed.inject(HttpTestingController);

    store.login({ email: 'sam@coffeeg.test', password: 'Str0ng!Passw0rd' }).subscribe();

    const request = http.expectOne('/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'sam@coffeeg.test',
      password: 'Str0ng!Passw0rd',
    });
    request.flush(authenticationResponse(inOneHour()));

    expect(store.isAuthenticated()).toBe(true);
    expect(store.userName()).toBe('sam');
    expect(store.accessToken()).toBe('a.signed.token');
    expect(store.hasRole('User')).toBe(true);
  });

  it('persists the session so a reload stays signed in', () => {
    const store = TestBed.inject(AuthStore);
    const http = TestBed.inject(HttpTestingController);

    store
      .register({
        email: 'sam@coffeeg.test',
        userName: 'sam',
        firstName: 'Sam',
        lastName: 'Altman',
        password: 'Str0ng!Passw0rd',
      })
      .subscribe();
    http.expectOne('/api/auth/register').flush(authenticationResponse(inOneHour()));

    expect(localStorage.getItem(STORAGE_KEY)).not.toBeNull();

    // A fresh injector stands in for a page reload.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: APP_CONFIG, useValue: config }],
    });

    expect(TestBed.inject(AuthStore).userName()).toBe('sam');
  });

  it('signs out and forgets the stored session', () => {
    const store = TestBed.inject(AuthStore);
    const http = TestBed.inject(HttpTestingController);

    store.login({ email: 'sam@coffeeg.test', password: 'Str0ng!Passw0rd' }).subscribe();
    http.expectOne('/api/auth/login').flush(authenticationResponse(inOneHour()));

    store.signOut();

    expect(store.isAuthenticated()).toBe(false);
    expect(store.userName()).toBeNull();
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
  });

  it('ignores a stored session whose token has expired', () => {
    localStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({
        user: { id: 1, email: 'sam@coffeeg.test', userName: 'sam', roles: ['User'] },
        accessToken: 'a.stale.token',
        expiresAt: Date.now() - 1_000,
      }),
    );

    const store = TestBed.inject(AuthStore);

    expect(store.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
  });

  it('ignores stored junk rather than failing to start', () => {
    localStorage.setItem(STORAGE_KEY, 'not json at all');

    expect(TestBed.inject(AuthStore).isAuthenticated()).toBe(false);
  });
});
