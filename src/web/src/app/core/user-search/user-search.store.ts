import { ErrorHandler, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subject, catchError, debounceTime, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { ApiError, isApiError } from '../http/api-error';
import { UserSearchApi } from './user-search.api';
import { PersonSummary, toPersonSummary } from './user-search.models';

/** Long enough to stop a call per keystroke, short enough not to feel laggy. */
const DEBOUNCE_MS = 250;

/** Matches the API's own floor. A single letter matches most of the table. */
const MINIMUM_TERM_LENGTH = 2;

/** Matches the API's default. A dropdown, not a page of results. */
const LIMIT = 10;

/**
 * Who you are looking for. A true application-wide singleton: the shell hosts one search
 * box for the life of the app, which is what puts this in `core/` rather than in a
 * feature (Docs/DECISIONS.md D-0019).
 *
 * Orchestration is RxJS and state is signals, per Docs/ARCHITECTURE.md. The term goes in
 * through a `Subject` rather than a signal read by `toObservable`, so the pipeline is
 * plain RxJS with no effect to flush - which is also what makes it testable with nothing
 * but fake timers.
 */
@Injectable({ providedIn: 'root' })
export class UserSearchStore {
  private readonly api = inject(UserSearchApi);
  private readonly errors = inject(ErrorHandler);

  private readonly terms = new Subject<string>();

  private readonly termState = signal('');
  private readonly resultsState = signal<readonly PersonSummary[]>([]);
  private readonly searchingState = signal(false);
  private readonly errorState = signal<ApiError | null>(null);

  readonly term = this.termState.asReadonly();
  readonly results = this.resultsState.asReadonly();
  readonly searching = this.searchingState.asReadonly();
  readonly error = this.errorState.asReadonly();

  /** True once the term is long enough to have been sent. Drives the dropdown's visibility. */
  readonly active = computed(() => this.termState().trim().length >= MINIMUM_TERM_LENGTH);

  /** True when a completed search for a usable term found nobody. */
  readonly empty = computed(
    () => this.active() && !this.searching() && this.error() === null && this.results().length === 0,
  );

  constructor() {
    this.terms
      .pipe(
        map((term) => term.trim()),
        debounceTime(DEBOUNCE_MS),
        distinctUntilChanged(),
        // switchMap, so a slow earlier response cannot land after a newer one and
        // repopulate the dropdown with results for a term nobody is typing any more.
        switchMap((term) => (term.length < MINIMUM_TERM_LENGTH ? of([]) : this.lookUp(term))),
        takeUntilDestroyed(),
      )
      .subscribe((results) => {
        this.resultsState.set(results);
        this.searchingState.set(false);
      });
  }

  /** Queues a term. Nothing is sent until it settles and clears the length floor. */
  search(term: string): void {
    this.termState.set(term);
    this.errorState.set(null);
    this.searchingState.set(term.trim().length >= MINIMUM_TERM_LENGTH);
    this.terms.next(term);
  }

  /** Empties the box and the dropdown, e.g. after picking somebody. */
  clear(): void {
    this.termState.set('');
    this.resultsState.set([]);
    this.searchingState.set(false);
    this.errorState.set(null);
    this.terms.next('');
  }

  /**
   * One lookup. `catchError` is deliberately **inside** the `switchMap`: an error allowed
   * to reach the outer pipeline would complete it, and the search box would then be dead
   * for the rest of the session with nothing on screen to say so.
   */
  private lookUp(term: string): Observable<readonly PersonSummary[]> {
    return this.api.search(term, LIMIT).pipe(
      map((response) => response.map(toPersonSummary)),
      catchError((error: unknown) => {
        if (isApiError(error)) {
          this.errorState.set(error);
        } else {
          // Nothing the dropdown can explain. A page would rethrow and let
          // GlobalErrorHandler take it, but a throw here would complete the pipeline -
          // so it is handed to the same sink directly instead of being swallowed.
          this.errors.handleError(error);
        }

        return of([]);
      }),
    );
  }
}
