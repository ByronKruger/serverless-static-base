import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  NzAutocompleteModule,
  NzAutocompleteOptionComponent,
} from 'ng-zorro-antd/auto-complete';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { UserSearchStore } from './user-search.store';

/**
 * The header's "find someone" box.
 *
 * All it does is feed the input's text to the store and navigate on a pick: the debounce,
 * the length floor and the request live in {@link UserSearchStore}, because orchestration
 * is the store's job (Docs/ARCHITECTURE.md).
 *
 * `nz-autocomplete` is used as it ships. Its dropdown is light-themed against the dark
 * header, which is accepted rather than fixed - the component library is not overridden,
 * and D-0012 is the record of what happens when a utility fights Ant Design.
 */
@Component({
  selector: 'app-user-search',
  imports: [NzAutocompleteModule, NzInputModule, NzIconModule, NzSpinModule],
  templateUrl: './user-search.html',
})
export class UserSearch {
  private readonly store = inject(UserSearchStore);
  private readonly router = inject(Router);

  protected readonly term = this.store.term;
  protected readonly results = this.store.results;
  protected readonly searching = this.store.searching;
  protected readonly error = this.store.error;
  protected readonly empty = this.store.empty;

  protected search(term: string): void {
    this.store.search(term);
  }

  /**
   * Opens the picked person's profile.
   *
   * Reached only from `nz-autocomplete`'s own `selectionChange`, which ng-zorro emits
   * behind an `isUserInput` filter. Binding this to `nz-auto-option`'s identically named
   * output instead is the bug this replaced: that one also fires for the `deselect()`
   * ng-zorro runs across every option whenever the list changes, so the dropdown
   * navigated away by itself the moment results arrived.
   *
   * The option's `nzValue` is the person's id; a disabled placeholder row cannot get
   * here, because `selectViaInteraction()` ignores disabled options.
   */
  protected open(option: NzAutocompleteOptionComponent): void {
    const userId = Number(option.nzValue);

    if (!Number.isInteger(userId) || userId <= 0) {
      return;
    }

    this.store.clear();
    void this.router.navigate(['/users', userId]);
  }
}
