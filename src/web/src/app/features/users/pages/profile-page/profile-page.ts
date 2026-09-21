import {
  Component,
  ErrorHandler,
  computed,
  inject,
  input,
  numberAttribute,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { EMPTY, catchError, switchMap, tap } from 'rxjs';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';
import { ApiError, isApiError } from '../../../../core/http/api-error';
import { DetailCard, DetailRow } from '../../../../shared/ui/detail-card/detail-card';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { UsersStore } from '../../users.store';

/**
 * Somebody else's profile: who they are, and how they like their drinks.
 *
 * Readable by anybody signed in (D-0017). It renders the same card as the
 * My-preferences page, through the same `shared/ui` primitive, so a saved preference
 * looks the same wherever it appears.
 */
@Component({
  selector: 'app-profile-page',
  imports: [PageHeader, DetailCard, NzAlertModule, NzEmptyModule, NzSkeletonModule],
  templateUrl: './profile-page.html',
})
export class ProfilePage {
  /**
   * Bound from the route by `withComponentInputBinding`. Transformed, because a route
   * parameter arrives as a **string**: without this the id reaches the API as `"7"` and
   * the URL is built from a value the type says is a number but is not.
   */
  readonly id = input.required({ transform: numberAttribute });

  private readonly store = inject(UsersStore);
  private readonly errors = inject(ErrorHandler);

  protected readonly profile = this.store.profile;
  protected readonly hasPreferences = this.store.hasPreferences;
  protected readonly count = this.store.preferenceCount;

  protected readonly loading = signal(true);
  protected readonly error = signal<ApiError | null>(null);

  protected readonly heading = computed(() => this.profile()?.displayName ?? 'Profile');

  protected readonly subtitle = computed(() => {
    const profile = this.profile();

    if (profile === null) {
      return undefined;
    }

    const saved = this.count() === 1 ? '1 preference' : `${this.count()} preferences`;

    return `@${profile.userName} · ${saved}`;
  });

  /** Cards in the shape the primitive takes. A computed, so it maps once per change. */
  protected readonly cards = computed(() =>
    (this.profile()?.preferences ?? []).map((preference) => ({
      id: preference.id,
      title: preference.beverageName,
      rows: preference.lines.map<DetailRow>((line) => ({
        term: line.ingredientName,
        value: line.amount,
      })),
    })),
  );

  /**
   * Loads whoever the route currently names.
   *
   * Driven by the input rather than done once in the constructor, for two reasons. A
   * required input has no value during construction - the router binds it afterwards, and
   * reading it there is a compile error. And the router reuses this component when you go
   * from one profile straight to another, so a one-shot load would leave the previous
   * person on screen.
   */
  constructor() {
    toObservable(this.id)
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap((id) =>
          this.store.loadProfile(id).pipe(
            catchError((error: unknown) => {
              this.loading.set(false);

              if (isApiError(error)) {
                this.error.set(error);
              } else {
                // A page would normally rethrow and let GlobalErrorHandler take it, but
                // a throw here completes the stream and the next profile would never
                // load. Same sink, reached directly.
                this.errors.handleError(error);
              }

              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.loading.set(false));
  }
}
