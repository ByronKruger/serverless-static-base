import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { APP_CONFIG, AppConfig } from '../../core/config/app-config';
import { PreferencesApi } from './data-access/preferences.api';
import {
  BeveragePreferenceResponse,
  BeverageTypeDetailResponse,
  UnitTypeResponse,
} from './data-access/preferences.contracts';
import { PreferencesStore } from './preferences.store';

const config: AppConfig = {
  environmentName: 'test',
  apiBaseUrl: '/api',
  auth: { authority: 'https://identity.test', clientId: 'coffeeg-web', audience: 'coffeeg-api' },
};

const teaspoons: UnitTypeResponse = {
  id: 1,
  name: 'Teaspoons',
  symbol: 'tsp',
  kind: 'Numeric',
  minValue: 0,
  maxValue: 20,
};

const percentage: UnitTypeResponse = {
  id: 2,
  name: 'Percentage',
  symbol: '%',
  kind: 'Numeric',
  minValue: 0,
  maxValue: 100,
};

const yesNo: UnitTypeResponse = { id: 3, name: 'Yes / no', kind: 'Boolean', minValue: 1, maxValue: 1 };

/** Shaped like the API's own seed: a simple ingredient, a complex one, and a yes/no. */
const coffee: BeverageTypeDetailResponse = {
  id: 1,
  name: 'Coffee',
  description: 'Brewed coffee.',
  ingredients: [
    { id: 1, name: 'Coffee', unit: teaspoons, variants: [] },
    {
      id: 2,
      name: 'Milk',
      description: 'Pick which milk.',
      unit: percentage,
      variants: [
        { id: 3, name: 'Low-fat milk', unit: percentage, variants: [] },
        { id: 5, name: 'Soy milk', unit: percentage, variants: [] },
      ],
    },
    { id: 11, name: 'Ice', unit: yesNo, variants: [] },
  ],
};

function savedPreference(): BeveragePreferenceResponse {
  return {
    id: 10,
    beverageType: { id: 1, name: 'Coffee' },
    createdAtUtc: '2026-09-19T00:00:00+00:00',
    ingredients: [
      { ingredientId: 1, ingredientName: 'Coffee', amount: 2, unit: teaspoons },
      // Deliberately no `amount` key: the API omits nulls, so this is the shape that
      // actually arrives when somebody wants no ice.
      { ingredientId: 11, ingredientName: 'Ice', unit: yesNo },
    ],
  };
}

describe('PreferencesStore', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: APP_CONFIG, useValue: config },
        PreferencesApi,
        PreferencesStore,
      ],
    });
  });

  it('starts with nothing loaded', () => {
    const store = TestBed.inject(PreferencesStore);

    expect(store.beverages()).toEqual([]);
    expect(store.mine()).toEqual([]);
    expect(store.selected()).toBeNull();
    expect(store.hasPreferences()).toBe(false);
  });

  it('loads the catalogue a person picks from', () => {
    const store = TestBed.inject(PreferencesStore);
    const http = TestBed.inject(HttpTestingController);

    store.loadBeverages().subscribe();

    const request = http.expectOne('/api/beverage-types');
    expect(request.request.method).toBe('GET');
    request.flush([{ id: 1, name: 'Coffee' }, { id: 2, name: 'Whiskey' }]);

    expect(store.beverages().map((beverage) => beverage.name)).toEqual(['Coffee', 'Whiskey']);
  });

  it('turns a simple ingredient into a slot with one option', () => {
    const store = TestBed.inject(PreferencesStore);
    const http = TestBed.inject(HttpTestingController);

    store.selectBeverage(1).subscribe();
    http.expectOne('/api/beverage-types/1').flush(coffee);

    const slot = store.selected()!.slots[0];

    expect(slot.label).toBe('Coffee');
    expect(slot.requiresChoice).toBe(false);
    expect(slot.options.map((option) => option.id)).toEqual([1]);
  });

  it('turns a complex ingredient into a slot that must be chosen from', () => {
    const store = TestBed.inject(PreferencesStore);
    const http = TestBed.inject(HttpTestingController);

    store.selectBeverage(1).subscribe();
    http.expectOne('/api/beverage-types/1').flush(coffee);

    const milk = store.selected()!.slots[1];

    // The category itself is never an option - the API rejects it.
    expect(milk.requiresChoice).toBe(true);
    expect(milk.options.map((option) => option.name)).toEqual(['Low-fat milk', 'Soy milk']);
  });

  it('marks a yes/no unit so the form renders a switch', () => {
    const store = TestBed.inject(PreferencesStore);
    const http = TestBed.inject(HttpTestingController);

    store.selectBeverage(1).subscribe();
    http.expectOne('/api/beverage-types/1').flush(coffee);

    const ice = store.selected()!.slots[2];

    expect(ice.options[0].unit.isYesNo).toBe(true);
    expect(ice.options[0].unit.symbol).toBeNull();
  });

  it('carries the range a numeric unit accepts', () => {
    const store = TestBed.inject(PreferencesStore);
    const http = TestBed.inject(HttpTestingController);

    store.selectBeverage(1).subscribe();
    http.expectOne('/api/beverage-types/1').flush(coffee);

    const unit = store.selected()!.slots[0].options[0].unit;

    expect(unit.min).toBe(0);
    expect(unit.max).toBe(20);
    expect(unit.symbol).toBe('tsp');
  });

  it('phrases a saved amount with its unit, and a null one as none', () => {
    const store = TestBed.inject(PreferencesStore);
    const http = TestBed.inject(HttpTestingController);

    store.loadMine().subscribe();
    http.expectOne('/api/beverage-preferences').flush([savedPreference()]);

    const lines = store.mine()[0].lines;

    expect(lines[0].amount).toBe('2 tsp');
    // A yes/no unit reads as No rather than None - same statement, right words.
    expect(lines[1].amount).toBe('No');
  });

  it('disables a beverage the person has already saved', () => {
    const store = TestBed.inject(PreferencesStore);
    const http = TestBed.inject(HttpTestingController);

    store.loadMine().subscribe();
    http.expectOne('/api/beverage-preferences').flush([savedPreference()]);

    expect(store.takenBeverageNames().has('Coffee')).toBe(true);
    expect(store.takenBeverageNames().has('Whiskey')).toBe(false);
    expect(store.hasPreferences()).toBe(true);
  });

  it('posts a new preference and adds it to the list', () => {
    const store = TestBed.inject(PreferencesStore);
    const http = TestBed.inject(HttpTestingController);

    store.create({ beverageTypeId: 1, ingredients: [{ ingredientId: 1, amount: 2 }] }).subscribe();

    const request = http.expectOne('/api/beverage-preferences');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      beverageTypeId: 1,
      ingredients: [{ ingredientId: 1, amount: 2 }],
    });
    request.flush(savedPreference());

    expect(store.mine().map((preference) => preference.beverageName)).toEqual(['Coffee']);
  });
});
