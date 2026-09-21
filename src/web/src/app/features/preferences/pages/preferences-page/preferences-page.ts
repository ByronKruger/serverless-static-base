import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';
import { ApiError, isApiError } from '../../../../core/http/api-error';
import { DetailCard, DetailRow } from '../../../../shared/ui/detail-card/detail-card';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { PreferencesStore } from '../../preferences.store';

/** Everything the signed-in person has saved, one card per beverage type. */
@Component({
  selector: 'app-preferences-page',
  imports: [
    RouterLink,
    PageHeader,
    DetailCard,
    NzButtonModule,
    NzIconModule,
    NzAlertModule,
    NzEmptyModule,
    NzSkeletonModule,
  ],
  templateUrl: './preferences-page.html',
})
export class PreferencesPage {
  private readonly store = inject(PreferencesStore);

  protected readonly hasPreferences = this.store.hasPreferences;

  /**
   * The cards, in the shape the primitive takes. A `computed` rather than a method
   * called from the template, so the mapping runs when the preferences change instead of
   * on every render pass.
   */
  protected readonly cards = computed(() =>
    this.store.mine().map((preference) => ({
      id: preference.id,
      title: preference.beverageName,
      rows: preference.lines.map<DetailRow>((line) => ({
        term: line.ingredientName,
        value: line.amount,
      })),
    })),
  );

  protected readonly loading = signal(true);
  protected readonly error = signal<ApiError | null>(null);

  constructor() {
    this.store.loadMine().subscribe({
      next: () => this.loading.set(false),
      error: (error: unknown) => {
        this.loading.set(false);

        if (isApiError(error)) {
          this.error.set(error);
          return;
        }

        // Nothing this page can explain - let the global handler report it.
        throw error;
      },
    });
  }
}
