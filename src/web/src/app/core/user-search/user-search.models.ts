import { UserSummaryResponse } from './user-search.contracts';

/** One person as the search dropdown talks about them. */
export interface PersonSummary {
  readonly id: number;
  readonly userName: string;
  /**
   * What to show as their name: both names when they have them, otherwise the username.
   * Never empty.
   */
  readonly displayName: string;
  /** True when {@link displayName} is just the username, so a row need not repeat it. */
  readonly nameless: boolean;
}

/**
 * The one place the two classes of account are reconciled. Names are required at
 * registration but nullable on the row, so an account created before that rule has
 * neither - and the username, which is always present and unique, stands in.
 */
export function toPersonSummary(response: UserSummaryResponse): PersonSummary {
  const parts = [response.firstName, response.lastName].filter(
    (part): part is string => part !== undefined && part.trim() !== '',
  );

  return {
    id: response.id,
    userName: response.userName,
    displayName: parts.length === 0 ? response.userName : parts.join(' '),
    nameless: parts.length === 0,
  };
}
