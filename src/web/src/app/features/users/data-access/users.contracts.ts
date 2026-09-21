/**
 * Transport shapes for `GET /api/users/{id}`. These stop here: the rest of the feature
 * speaks the domain models in `users.models.ts`.
 *
 * They declare **only the fields this page reads** - a narrower view of the same payload,
 * not a copy of the API's DTO. The preferences feature reads the same endpoint family
 * through its own contracts, and neither imports the other (Docs/DECISIONS.md D-0020).
 */

export interface ProfileUnitResponse {
  readonly symbol?: string;
  /** Serialized as a string by the API - a quantity, or a presence flag. */
  readonly kind: 'Numeric' | 'Boolean';
}

export interface ProfilePreferenceLineResponse {
  readonly ingredientName: string;
  /**
   * Omitted entirely when the person wants none of it: the API drops nulls from its
   * responses, so this arrives as `undefined` rather than `null`. Normalise it before
   * reasoning about it, or "no ice" renders as "Yes".
   */
  readonly amount?: number | null;
  readonly unit: ProfileUnitResponse;
}

export interface ProfilePreferenceResponse {
  readonly id: number;
  readonly beverageType: { readonly name: string };
  readonly ingredients: readonly ProfilePreferenceLineResponse[];
}

export interface UserProfileResponse {
  readonly id: number;
  readonly userName: string;
  /** Absent for an account registered before names were collected. */
  readonly firstName?: string;
  readonly lastName?: string;
  readonly preferences: readonly ProfilePreferenceResponse[];
}
