import {
  BeveragePreferenceResponse,
  BeverageTypeDetailResponse,
  BeverageTypeResponse,
  IngredientResponse,
  UnitTypeResponse,
} from './preferences.contracts';

/**
 * What the feature speaks. The mapping below is not a rename: the API describes the
 * catalogue (an ingredient, which may have variants), and the form needs slots (a thing
 * to decide about, with one or more options). Doing that once here keeps the branching
 * out of the template.
 */

export interface Unit {
  readonly name: string;
  readonly symbol: string | null;
  /** Presence rather than quantity - rendered as a switch, sent as 1 or null. */
  readonly isYesNo: boolean;
  readonly min: number | null;
  readonly max: number | null;
}

/** One option within a slot. A simple ingredient is a slot with exactly one of these. */
export interface SlotOption {
  readonly id: number;
  readonly name: string;
  readonly unit: Unit;
}

/** One row of the create form. */
export interface BeverageSlot {
  /** Form control name. Derived from the ingredient id, so it is stable and unique. */
  readonly key: string;
  readonly label: string;
  readonly description: string | null;
  readonly options: readonly SlotOption[];
  /** True when the person must first say which one - milk is soy, raw or low-fat. */
  readonly requiresChoice: boolean;
}

export interface Beverage {
  readonly id: number;
  readonly name: string;
  readonly description: string | null;
}

export interface BeverageDetail extends Beverage {
  readonly slots: readonly BeverageSlot[];
}

export interface Preference {
  readonly id: number;
  readonly beverageName: string;
  readonly createdAtUtc: string;
  readonly lines: readonly PreferenceLine[];
}

export interface PreferenceLine {
  readonly ingredientName: string;
  /** Already phrased for display: "2 tsp", "Yes", "No". */
  readonly amount: string;
}

export function toBeverage(response: BeverageTypeResponse): Beverage {
  return { id: response.id, name: response.name, description: response.description ?? null };
}

export function toBeverageDetail(response: BeverageTypeDetailResponse): BeverageDetail {
  return { ...toBeverage(response), slots: response.ingredients.map(toSlot) };
}

export function toPreference(response: BeveragePreferenceResponse): Preference {
  return {
    id: response.id,
    beverageName: response.beverageType.name,
    createdAtUtc: response.createdAtUtc,
    lines: response.ingredients.map((line) => ({
      ingredientName: line.ingredientName,
      amount: describeAmount(line.amount ?? null, toUnit(line.unit)),
    })),
  };
}

/**
 * How an amount reads once the unit is applied. A null amount is the domain's "none of
 * this", which is the same statement as 0 teaspoons or `No`.
 */
export function describeAmount(amount: number | null, unit: Unit): string {
  if (unit.isYesNo) {
    return amount === null ? 'No' : 'Yes';
  }

  if (amount === null) {
    return 'None';
  }

  return unit.symbol === null ? `${amount}` : `${amount} ${unit.symbol}`;
}

function toSlot(ingredient: IngredientResponse): BeverageSlot {
  // A complex ingredient is a category: the choices are its variants, and the category
  // itself is never sent. A simple one is its own single choice.
  const options =
    ingredient.variants.length === 0 ? [ingredient] : [...ingredient.variants];

  return {
    key: `ingredient-${ingredient.id}`,
    label: ingredient.name,
    description: ingredient.description ?? null,
    options: options.map((option) => ({
      id: option.id,
      name: option.name,
      unit: toUnit(option.unit),
    })),
    requiresChoice: ingredient.variants.length > 0,
  };
}

function toUnit(unit: UnitTypeResponse): Unit {
  return {
    name: unit.name,
    symbol: unit.symbol ?? null,
    isYesNo: unit.kind === 'Boolean',
    min: unit.minValue ?? null,
    max: unit.maxValue ?? null,
  };
}
