/**
 * How an amount reads once its unit is applied.
 *
 * This lives in `shared/utils` because two features phrase the same thing: the
 * preferences feature for your own saved drinks, and the users feature for somebody
 * else's. Only the pure function is shared - each feature keeps its own transport types
 * and its own mapping, so neither imports the other (Docs/DECISIONS.md D-0020).
 */

/**
 * The part of a unit that decides how an amount reads. Deliberately smaller than either
 * feature's own unit model: a name, a range and an id are nothing to do with phrasing, so
 * requiring them here would make this care about things it does not use.
 */
export interface AmountUnit {
  readonly symbol: string | null;
  /** Presence rather than quantity - the amount is 1 for yes and null for no. */
  readonly isYesNo: boolean;
}

/**
 * A null amount is the domain's "none of this", which is the same statement as 0
 * teaspoons or `No` (Docs/domain/beverage-preference.md rule 8). A yes/no unit says it
 * with the right word rather than with "None".
 */
export function describeAmount(amount: number | null, unit: AmountUnit): string {
  if (unit.isYesNo) {
    return amount === null ? 'No' : 'Yes';
  }

  if (amount === null) {
    return 'None';
  }

  return unit.symbol === null ? `${amount}` : `${amount} ${unit.symbol}`;
}
