/**
 * Transport shapes for the beverage endpoints. These stop here: the rest of the feature
 * speaks the domain models in `preferences.models.ts`.
 */

export interface UnitTypeResponse {
  readonly id: number;
  readonly name: string;
  readonly symbol?: string;
  /** Serialized as a string by the API - a quantity, or a presence flag. */
  readonly kind: 'Numeric' | 'Boolean';
  readonly minValue?: number;
  readonly maxValue?: number;
}

export interface IngredientResponse {
  readonly id: number;
  readonly name: string;
  readonly description?: string;
  readonly unit: UnitTypeResponse;
  /** Empty for a simple ingredient; the choices to pick between for a complex one. */
  readonly variants: readonly IngredientResponse[];
}

export interface BeverageTypeResponse {
  readonly id: number;
  readonly name: string;
  readonly description?: string;
}

export interface BeverageTypeDetailResponse extends BeverageTypeResponse {
  readonly ingredients: readonly IngredientResponse[];
}

export interface CreateBeveragePreferenceRequest {
  readonly beverageTypeId: number;
  readonly ingredients: readonly BeveragePreferenceIngredientRequest[];
}

export interface BeveragePreferenceIngredientRequest {
  readonly ingredientId: number;
  /** Null means none of it. The unit comes from the ingredient, never from here. */
  readonly amount: number | null;
}

export interface BeveragePreferenceResponse {
  readonly id: number;
  readonly beverageType: BeverageTypeResponse;
  readonly createdAtUtc: string;
  readonly ingredients: readonly BeveragePreferenceIngredientResponse[];
}

export interface BeveragePreferenceIngredientResponse {
  readonly ingredientId: number;
  readonly ingredientName: string;
  /**
   * Omitted entirely when the person wants none of it: the API drops nulls from its
   * responses (`DefaultIgnoreCondition.WhenWritingNull`), so this arrives as `undefined`
   * rather than `null`. Normalise it before reasoning about it.
   */
  readonly amount?: number | null;
  readonly unit: UnitTypeResponse;
}
