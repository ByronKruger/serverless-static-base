import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { PreferencesApi } from './data-access/preferences.api';
import { CreateBeveragePreferenceRequest } from './data-access/preferences.contracts';
import {
  Beverage,
  BeverageDetail,
  Preference,
  toBeverage,
  toBeverageDetail,
  toPreference,
} from './data-access/preferences.models';

/**
 * State for the preferences feature. Provided on the feature's route, so it is destroyed
 * when the person navigates away rather than living for the life of the app.
 *
 * Writable signals stay private; the pages read the readonly ones and call the methods.
 */
@Injectable()
export class PreferencesStore {
  private readonly api = inject(PreferencesApi);

  private readonly beveragesState = signal<readonly Beverage[]>([]);
  private readonly mineState = signal<readonly Preference[]>([]);
  private readonly selectedState = signal<BeverageDetail | null>(null);

  readonly beverages = this.beveragesState.asReadonly();
  readonly mine = this.mineState.asReadonly();
  readonly selected = this.selectedState.asReadonly();

  /**
   * Beverage types the person already has a preference for. The API answers a second one
   * with 409, so the picker disables them rather than letting a doomed submit through.
   */
  readonly takenBeverageNames = computed(
    () => new Set(this.mineState().map((preference) => preference.beverageName)),
  );

  readonly hasPreferences = computed(() => this.mineState().length > 0);

  loadBeverages(): Observable<readonly Beverage[]> {
    return this.api.listBeverageTypes().pipe(
      map((response) => response.map(toBeverage)),
      tap((beverages) => this.beveragesState.set(beverages)),
    );
  }

  loadMine(): Observable<readonly Preference[]> {
    return this.api.listMine().pipe(
      map((response) => response.map(toPreference)),
      tap((mine) => this.mineState.set(mine)),
    );
  }

  /** Loads the composition the create form renders from. */
  selectBeverage(id: number): Observable<BeverageDetail> {
    return this.api.getBeverageType(id).pipe(
      map(toBeverageDetail),
      tap((detail) => this.selectedState.set(detail)),
    );
  }

  clearSelection(): void {
    this.selectedState.set(null);
  }

  create(request: CreateBeveragePreferenceRequest): Observable<Preference> {
    return this.api.create(request).pipe(
      map(toPreference),
      tap((created) => this.mineState.update((mine) => [created, ...mine])),
    );
  }
}
