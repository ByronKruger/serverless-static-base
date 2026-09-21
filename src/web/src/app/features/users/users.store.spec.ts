import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { APP_CONFIG, AppConfig } from '../../core/config/app-config';
import { UsersApi } from './data-access/users.api';
import { UserProfileResponse } from './data-access/users.contracts';
import { UsersStore } from './users.store';

const config: AppConfig = {
  environmentName: 'test',
  apiBaseUrl: '/api',
  auth: { authority: 'https://identity.test', clientId: 'coffeeg-web', audience: 'coffeeg-api' },
};

const teaspoons = { symbol: 'tsp', kind: 'Numeric' } as const;
const percentage = { symbol: '%', kind: 'Numeric' } as const;
const yesNo = { kind: 'Boolean' } as const;

/** Shaped like the API's own output, omissions included. */
function profile(): UserProfileResponse {
  return {
    id: 7,
    userName: 'ada-l',
    firstName: 'Augusta',
    lastName: 'Byron',
    preferences: [
      {
        id: 10,
        beverageType: { name: 'Coffee' },
        ingredients: [
          { ingredientName: 'Coffee', amount: 2, unit: teaspoons },
          { ingredientName: 'Soy milk', amount: 40, unit: percentage },
        ],
      },
      {
        id: 11,
        beverageType: { name: 'Whiskey' },
        ingredients: [
          { ingredientName: 'Ice', amount: 1, unit: yesNo },
          // Deliberately no `amount` key: the API omits nulls, so this is the shape that
          // actually arrives when somebody wants no water.
          { ingredientName: 'Water', unit: yesNo },
        ],
      },
    ],
  };
}

describe('UsersStore', () => {
  let store: UsersStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: APP_CONFIG, useValue: config },
        UsersApi,
        UsersStore,
      ],
    });

    store = TestBed.inject(UsersStore);
    http = TestBed.inject(HttpTestingController);
  });

  it('starts with nothing loaded', () => {
    expect(store.profile()).toBeNull();
    expect(store.hasPreferences()).toBe(false);
    expect(store.preferenceCount()).toBe(0);
  });

  it('loads a profile and counts what they have saved', () => {
    store.loadProfile(7).subscribe();

    const request = http.expectOne('/api/users/7');
    expect(request.request.method).toBe('GET');
    request.flush(profile());

    expect(store.profile()!.displayName).toBe('Augusta Byron');
    expect(store.profile()!.userName).toBe('ada-l');
    expect(store.hasPreferences()).toBe(true);
    expect(store.preferenceCount()).toBe(2);
  });

  it('phrases each amount with its unit', () => {
    store.loadProfile(7).subscribe();
    http.expectOne('/api/users/7').flush(profile());

    const coffee = store.profile()!.preferences[0];

    expect(coffee.beverageName).toBe('Coffee');
    expect(coffee.lines.map((line) => line.amount)).toEqual(['2 tsp', '40 %']);
  });

  it('reads an omitted amount as No rather than Yes', () => {
    // The bug the previous slice shipped and fixed, in a second reader of the same
    // payload: the API omits nulls, so `amount` is `undefined`, and `undefined === null`
    // is false. Without the `?? null` normalisation this renders "Yes".
    store.loadProfile(7).subscribe();
    http.expectOne('/api/users/7').flush(profile());

    const whiskey = store.profile()!.preferences[1];

    expect(whiskey.lines.map((line) => line.amount)).toEqual(['Yes', 'No']);
  });

  it('falls back to the username when the account has no names', () => {
    store.loadProfile(8).subscribe();
    http.expectOne('/api/users/8').flush({
      id: 8,
      userName: 'grace',
      preferences: [],
    } satisfies UserProfileResponse);

    expect(store.profile()!.displayName).toBe('grace');
  });

  it('handles a profile with nothing saved', () => {
    store.loadProfile(9).subscribe();
    http.expectOne('/api/users/9').flush({
      id: 9,
      userName: 'nobody',
      firstName: 'No',
      lastName: 'Body',
      preferences: [],
    } satisfies UserProfileResponse);

    expect(store.hasPreferences()).toBe(false);
    expect(store.preferenceCount()).toBe(0);
    expect(store.profile()!.preferences).toEqual([]);
  });
});
