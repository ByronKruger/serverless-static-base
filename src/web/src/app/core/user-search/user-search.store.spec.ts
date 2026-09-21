import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { APP_CONFIG, AppConfig } from '../config/app-config';
import { apiErrorInterceptor } from '../http/api-error.interceptor';
import { UserSearchApi } from './user-search.api';
import { UserSummaryResponse } from './user-search.contracts';
import { UserSearchStore } from './user-search.store';

const config: AppConfig = {
  environmentName: 'test',
  apiBaseUrl: '/api',
  auth: { authority: 'https://identity.test', clientId: 'coffeeg-web', audience: 'coffeeg-api' },
};

const ada: UserSummaryResponse = { id: 7, userName: 'ada-l', firstName: 'Augusta', lastName: 'Byron' };

/** The shape an account registered before names existed arrives in: both keys omitted. */
const grace: UserSummaryResponse = { id: 8, userName: 'grace' };

/** The store debounces by 250ms; this is comfortably past it. */
const PAST_DEBOUNCE = 300;

describe('UserSearchStore', () => {
  let store: UserSearchStore;
  let http: HttpTestingController;

  beforeEach(() => {
    vi.useFakeTimers();

    TestBed.configureTestingModule({
      providers: [
        // The error interceptor is registered because the store is written against an
        // `ApiError`, which is what the app's own HTTP stack delivers. Without it a
        // failure arrives as a raw HttpErrorResponse and the test would be exercising a
        // configuration that does not exist.
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: APP_CONFIG, useValue: config },
        UserSearchApi,
        UserSearchStore,
      ],
    });

    store = TestBed.inject(UserSearchStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('starts with nothing searched', () => {
    expect(store.term()).toBe('');
    expect(store.results()).toEqual([]);
    expect(store.active()).toBe(false);
    expect(store.searching()).toBe(false);
    http.verify();
  });

  it('collapses a burst of keystrokes into one request', () => {
    store.search('a');
    store.search('ad');
    store.search('ada');

    vi.advanceTimersByTime(PAST_DEBOUNCE);

    const request = http.expectOne('/api/users?q=ada&limit=10');
    expect(request.request.method).toBe('GET');
    request.flush([ada]);

    expect(store.results().map((person) => person.displayName)).toEqual(['Augusta Byron']);
    expect(store.searching()).toBe(false);
  });

  it('sends nothing for a term below the two-character floor', () => {
    store.search('a');

    vi.advanceTimersByTime(PAST_DEBOUNCE);

    // Matches the API's own floor, so the 400 is never provoked in the first place.
    http.expectNone('/api/users?q=a&limit=10');
    expect(store.active()).toBe(false);
    expect(store.searching()).toBe(false);
    http.verify();
  });

  it('trims the term before measuring and sending it', () => {
    store.search('  ada  ');

    vi.advanceTimersByTime(PAST_DEBOUNCE);

    http.expectOne('/api/users?q=ada&limit=10').flush([ada]);

    expect(store.results()).toHaveLength(1);
  });

  it('does not repeat a request for a term that has not changed', () => {
    store.search('ada');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=ada&limit=10').flush([ada]);

    store.search('ada');
    vi.advanceTimersByTime(PAST_DEBOUNCE);

    http.verify();
  });

  it('ignores a slow response for a term that has been superseded', () => {
    store.search('ad');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    const first = http.expectOne('/api/users?q=ad&limit=10');

    store.search('ada');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    const second = http.expectOne('/api/users?q=ada&limit=10');

    second.flush([ada]);

    // switchMap unsubscribed from the first the moment the second term arrived, so the
    // stale answer can never land - HttpTestingController will not even let it be
    // flushed, which is a stronger statement than asserting the result afterwards.
    expect(first.cancelled).toBe(true);
    expect(store.results().map((person) => person.userName)).toEqual(['ada-l']);
  });

  it('falls back to the username for an account with no names', () => {
    store.search('grace');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=grace&limit=10').flush([grace]);

    const person = store.results()[0];

    // The API omits nulls, so both name keys are absent rather than null.
    expect(person.displayName).toBe('grace');
    expect(person.nameless).toBe(true);
  });

  it('reports an empty result rather than looking like it is still loading', () => {
    store.search('zzz');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=zzz&limit=10').flush([]);

    expect(store.results()).toEqual([]);
    expect(store.searching()).toBe(false);
    expect(store.empty()).toBe(true);
  });

  it('survives a failed request and keeps searching afterwards', () => {
    store.search('ada');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=ada&limit=10').flush(
      { title: 'Server error', status: 500 },
      { status: 500, statusText: 'Server Error' },
    );

    expect(store.error()).not.toBeNull();
    expect(store.results()).toEqual([]);

    // The trap this pins down: an error reaching the outer pipeline would complete it,
    // leaving the search box permanently dead with nothing on screen to say so.
    store.search('grace');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=grace&limit=10').flush([grace]);

    expect(store.error()).toBeNull();
    expect(store.results().map((person) => person.userName)).toEqual(['grace']);
  });

  it('empties the box and the dropdown when cleared', () => {
    store.search('ada');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=ada&limit=10').flush([ada]);

    store.clear();

    expect(store.term()).toBe('');
    expect(store.results()).toEqual([]);
    expect(store.active()).toBe(false);

    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.verify();
  });
});
