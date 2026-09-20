import { Component, inject, signal } from '@angular/core';
import {
  FormControl,
  FormGroup,
  FormRecord,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { ApiError, fieldErrorsFor, isApiError } from '../../../../core/http/api-error';
import { PageHeader } from '../../../../shared/ui/page-header/page-header';
import { BeveragePreferenceIngredientRequest } from '../../data-access/preferences.contracts';
import { BeverageSlot, Unit } from '../../data-access/preferences.models';
import { PreferencesStore } from '../../preferences.store';

/** One row of the form: which option, and how much of it. */
type SlotForm = FormGroup<{
  choice: FormControl<number | null>;
  amount: FormControl<number | boolean | null>;
}>;

/**
 * Save how you like one kind of drink.
 *
 * The form is built from whatever the API says the beverage is composed of - nothing
 * about coffee, milk or teaspoons is written here. That is what "arbitrary beverage type"
 * and "arbitrary units" mean in practice: a moderator adding a drink or a unit changes
 * this page without touching it.
 */
@Component({
  selector: 'app-create-preference-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeader,
    NzFormModule,
    NzSelectModule,
    NzInputNumberModule,
    NzSwitchModule,
    NzButtonModule,
    NzCardModule,
    NzAlertModule,
    NzIconModule,
    NzSkeletonModule,
  ],
  templateUrl: './create-preference-page.html',
})
export class CreatePreferencePage {
  private readonly store = inject(PreferencesStore);
  private readonly router = inject(Router);

  protected readonly beverages = this.store.beverages;
  protected readonly selected = this.store.selected;
  protected readonly taken = this.store.takenBeverageNames;

  protected readonly loading = signal(true);
  protected readonly loadingComposition = signal(false);
  protected readonly submitting = signal(false);
  protected readonly error = signal<ApiError | null>(null);

  /** Keyed by slot, because the set of controls is whatever the chosen beverage has. */
  protected readonly ingredients = new FormRecord<SlotForm>({});

  protected readonly form = new FormGroup({
    beverageTypeId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    ingredients: this.ingredients,
  });

  /**
   * Slot keys in the order they were sent. The API reports a bad line as
   * `ingredients[2].amount`, and only slots the person actually filled in are sent, so
   * the index cannot be worked out from the slot list alone.
   */
  private submittedOrder: readonly string[] = [];

  constructor() {
    // Both, because the picker disables beverages the person already has: the API would
    // answer a second one with 409, and a disabled option explains that up front.
    this.store.loadBeverages().subscribe({
      next: () => this.loading.set(false),
      error: (error: unknown) => this.report(error, () => this.loading.set(false)),
    });

    this.store.loadMine().subscribe({
      error: (error: unknown) => this.report(error, () => undefined),
    });
  }

  protected slotForm(slot: BeverageSlot): SlotForm {
    return this.ingredients.controls[slot.key];
  }

  /**
   * The unit in force for a slot. It comes from the chosen option rather than the slot,
   * because the API lets two variants of one ingredient be measured differently.
   */
  protected unitFor(slot: BeverageSlot): Unit {
    const chosen = this.slotForm(slot)?.controls.choice.value ?? null;
    const option = slot.options.find((candidate) => candidate.id === chosen);

    return (option ?? slot.options[0]).unit;
  }

  protected serverErrorsFor(slot: BeverageSlot): readonly string[] {
    const index = this.submittedOrder.indexOf(slot.key);

    return index < 0 ? [] : fieldErrorsFor(this.error(), `ingredients[${index}].amount`);
  }

  protected isTaken(name: string): boolean {
    return this.taken().has(name);
  }

  /** Rebuilds the form from whatever the newly chosen beverage is composed of. */
  protected chooseBeverage(beverageTypeId: number | null): void {
    this.error.set(null);
    this.submittedOrder = [];

    for (const key of Object.keys(this.ingredients.controls)) {
      this.ingredients.removeControl(key);
    }

    if (beverageTypeId === null) {
      this.store.clearSelection();
      return;
    }

    this.loadingComposition.set(true);

    this.store.selectBeverage(beverageTypeId).subscribe({
      next: (detail) => {
        for (const slot of detail.slots) {
          this.ingredients.addControl(slot.key, this.buildSlot(slot));
        }

        this.loadingComposition.set(false);
      },
      error: (error: unknown) => this.report(error, () => this.loadingComposition.set(false)),
    });
  }

  /**
   * Resets the amount when the option changes: a different variant may be measured in a
   * different unit, and a number left behind in a yes/no control would be nonsense.
   */
  protected chooseOption(slot: BeverageSlot): void {
    const amount = this.slotForm(slot).controls.amount;

    amount.setValue(this.unitFor(slot).isYesNo ? false : null);
  }

  protected submit(): void {
    const detail = this.selected();

    if (this.form.invalid || detail === null) {
      this.form.markAllAsTouched();
      return;
    }

    const lines: BeveragePreferenceIngredientRequest[] = [];
    const order: string[] = [];

    for (const slot of detail.slots) {
      const group = this.slotForm(slot);
      const choice = group.controls.choice.value;

      // No choice means the person said nothing about this ingredient, which is the same
      // as not wanting it - so the line is simply not sent.
      if (choice === null) {
        continue;
      }

      const option = slot.options.find((candidate) => candidate.id === choice);

      if (option === undefined) {
        continue;
      }

      lines.push({ ingredientId: choice, amount: this.toAmount(group, option.unit) });
      order.push(slot.key);
    }

    this.submittedOrder = order;
    this.submitting.set(true);
    this.error.set(null);

    this.store.create({ beverageTypeId: detail.id, ingredients: lines }).subscribe({
      next: () => void this.router.navigate(['/preferences']),
      error: (error: unknown) => this.report(error, () => this.submitting.set(false)),
    });
  }

  /** A yes/no unit carries presence, so the switch becomes 1 for yes and null for no. */
  private toAmount(group: SlotForm, unit: Unit): number | null {
    const raw = group.controls.amount.value;

    if (unit.isYesNo) {
      return raw === true ? 1 : null;
    }

    return typeof raw === 'number' ? raw : null;
  }

  private buildSlot(slot: BeverageSlot): SlotForm {
    const single = slot.requiresChoice ? null : slot.options[0].id;

    return new FormGroup({
      choice: new FormControl<number | null>(single),
      amount: new FormControl<number | boolean | null>(slot.options[0].unit.isYesNo ? false : null),
    });
  }

  private report(error: unknown, always: () => void): void {
    always();

    if (isApiError(error)) {
      this.error.set(error);
      return;
    }

    // Nothing this page can explain - let the global handler report it.
    throw error;
  }
}
