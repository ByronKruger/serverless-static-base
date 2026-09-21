import { describeAmount } from '../../../shared/utils/amount';
import { UserProfileResponse } from './users.contracts';

/** What the feature speaks. */

export interface ProfileLine {
  readonly ingredientName: string;
  /** Already phrased for display: "2 tsp", "Yes", "No". */
  readonly amount: string;
}

export interface ProfilePreference {
  readonly id: number;
  readonly beverageName: string;
  readonly lines: readonly ProfileLine[];
}

export interface PersonProfile {
  readonly id: number;
  readonly userName: string;
  /** Both names when they have them, otherwise the username. Never empty. */
  readonly displayName: string;
  readonly preferences: readonly ProfilePreference[];
}

export function toPersonProfile(response: UserProfileResponse): PersonProfile {
  const parts = [response.firstName, response.lastName].filter(
    (part): part is string => part !== undefined && part.trim() !== '',
  );

  return {
    id: response.id,
    userName: response.userName,
    displayName: parts.length === 0 ? response.userName : parts.join(' '),
    preferences: response.preferences.map((preference) => ({
      id: preference.id,
      beverageName: preference.beverageType.name,
      lines: preference.ingredients.map((line) => ({
        ingredientName: line.ingredientName,
        // `?? null` is the whole defence against the omitted-amount trap: without it
        // `describeAmount` sees `undefined`, which is not `null`, and reads "no ice" as
        // "Yes". `describeAmount` is the shared one, so the phrasing cannot drift from
        // how the owner sees their own preference.
        amount: describeAmount(line.amount ?? null, {
          symbol: line.unit.symbol ?? null,
          isYesNo: line.unit.kind === 'Boolean',
        }),
      })),
    })),
  };
}
