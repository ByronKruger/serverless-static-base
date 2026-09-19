import { Component } from '@angular/core';

/**
 * The page content region: three columns on a wide viewport, folding to one on a
 * narrow one. Slots are projected content rather than boolean inputs, so a page
 * that needs only two columns simply projects nothing into `[slot=end]`.
 *
 * Breakpoints follow the shell's viewport media queries (Docs/ARCHITECTURE.md):
 * one column below 1025px, two up to 1201px, three above it.
 */
@Component({
  selector: 'app-three-column',
  templateUrl: './three-column.html',
})
export class ThreeColumn {}
