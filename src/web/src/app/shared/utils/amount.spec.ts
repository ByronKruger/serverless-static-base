import { AmountUnit, describeAmount } from './amount';

const teaspoons: AmountUnit = { symbol: 'tsp', isYesNo: false };
const percentage: AmountUnit = { symbol: '%', isYesNo: false };
const unitless: AmountUnit = { symbol: null, isYesNo: false };
const yesNo: AmountUnit = { symbol: null, isYesNo: true };

describe('describeAmount', () => {
  it('puts the symbol after the number', () => {
    expect(describeAmount(2, teaspoons)).toBe('2 tsp');
    expect(describeAmount(40, percentage)).toBe('40 %');
  });

  it('keeps a fractional amount as written', () => {
    expect(describeAmount(1.5, teaspoons)).toBe('1.5 tsp');
  });

  it('omits the symbol when the unit has none', () => {
    expect(describeAmount(3, unitless)).toBe('3');
  });

  it('reads a missing amount as none', () => {
    expect(describeAmount(null, teaspoons)).toBe('None');
  });

  it('reads a yes/no unit as a word, not a number', () => {
    // The trap the last slice shipped: the API omits nulls, so a caller that fails to
    // normalise `undefined` to `null` renders "no ice" as "Yes".
    expect(describeAmount(1, yesNo)).toBe('Yes');
    expect(describeAmount(null, yesNo)).toBe('No');
  });

  it('treats zero as a quantity, not as absence', () => {
    // Zero is a number somebody typed; null is the domain's "none of this". They read
    // the same to a person but are not the same value.
    expect(describeAmount(0, percentage)).toBe('0 %');
  });
});
