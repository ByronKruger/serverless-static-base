import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { APP_CONFIG, AppConfig } from '../config/app-config';
import { apiErrorInterceptor } from '../http/api-error.interceptor';
import { UserSearch } from './user-search';
import { UserSearchApi } from './user-search.api';
import { UserSummaryResponse } from './user-search.contracts';
import { UserSearchStore } from './user-search.store';

const config: AppConfig = {
  environmentName: 'test',
  apiBaseUrl: '/api',
  auth: { authority: 'https://identity.test', clientId: 'coffeeg-web', audience: 'coffeeg-api' },
};

const people: UserSummaryResponse[] = [
  { id: 7, userName: 'ada-l', firstName: 'Augusta', lastName: 'Byron' },
  { id: 8, userName: 'grace' },
];

const PAST_DEBOUNCE = 300;

/**
 * Renders the real component with the real `nz-autocomplete`, because the bug this pins
 * down lived entirely in which of ng-zorro's two identically named `selectionChange`
 * outputs was bound. Nothing short of letting ng-zorro run would have caught it.
 */
describe('UserSearch', () => {
  let fixture: ComponentFixture<UserSearch>;
  let http: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  function input(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input');
  }

  /** Options render into an overlay, not into the component's own element. */
  function options(): HTMLElement[] {
    return Array.from(document.querySelectorAll('nz-auto-option'));
  }

  function type(text: string): void {
    const element = input();

    element.focus();
    element.value = text;
    element.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  beforeEach(async () => {
    vi.useFakeTimers();

    await TestBed.configureTestingModule({
      imports: [UserSearch],
      providers: [
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: APP_CONFIG, useValue: config },
        UserSearchApi,
        UserSearchStore,
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserSearch);
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('shows one option per match', () => {
    type('ada');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=ada&limit=10').flush(people);
    fixture.detectChanges();

    expect(options()).toHaveLength(2);
  });

  it('does not navigate when results arrive', () => {
    // The reported bug: ng-zorro runs `deselect()` over every option whenever the list
    // changes, and `deselect()` emits the option-level `selectionChange` regardless of
    // user input. A handler bound there navigated on its own, the moment results landed.
    type('ada');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=ada&limit=10').flush(people);
    fixture.detectChanges();

    // ng-zorro reacts to the option list behind a `delay(0)`, so without draining the
    // timers the spurious emissions have not happened yet and this test would pass
    // against the very wiring it exists to reject.
    vi.advanceTimersByTime(1);
    fixture.detectChanges();

    expect(navigate).not.toHaveBeenCalled();
  });

  it('does not navigate while the term keeps changing', () => {
    type('ada');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=ada&limit=10').flush(people);
    fixture.detectChanges();

    // Every keystroke also reaches `resetActiveItem()`, which is the other path to the
    // same spurious emissions.
    type('ada-');
    type('ada-l');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=ada-l&limit=10').flush(people);
    fixture.detectChanges();

    expect(navigate).not.toHaveBeenCalled();
  });

  it('navigates once to the person actually clicked', () => {
    type('ada');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=ada&limit=10').flush(people);
    fixture.detectChanges();

    // The second row, to prove the id comes from the clicked option rather than from
    // whichever one happened to be first.
    options()[1].click();
    fixture.detectChanges();

    expect(navigate).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledWith(['/users', 8]);
  });

  it('empties the box after a pick', () => {
    type('ada');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=ada&limit=10').flush(people);
    fixture.detectChanges();

    options()[0].click();
    fixture.detectChanges();

    expect(TestBed.inject(UserSearchStore).term()).toBe('');
  });

  it('does not navigate when the no-matches row is clicked', () => {
    type('zzz');
    vi.advanceTimersByTime(PAST_DEBOUNCE);
    http.expectOne('/api/users?q=zzz&limit=10').flush([]);
    fixture.detectChanges();

    const rows = options();

    expect(rows).toHaveLength(1);

    rows[0].click();
    fixture.detectChanges();

    // Disabled, so `selectViaInteraction()` ignores it outright.
    expect(navigate).not.toHaveBeenCalled();
  });
});
