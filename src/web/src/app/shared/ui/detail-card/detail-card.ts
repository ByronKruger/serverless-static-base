import { Component, input } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';

/** One labelled row. Both halves are already phrased for display. */
export interface DetailRow {
  readonly term: string;
  readonly value: string;
}

/**
 * A titled card of term/value rows.
 *
 * Presentational and domain-free on purpose: it takes strings, so `shared/ui` keeps its
 * no-feature-imports rule literally and both the preferences feature and the users
 * feature can render a saved preference with it. It is named for what it is - a card of
 * details - rather than for the one thing that happens to fill it today.
 *
 * Page-level actions are projected rather than configured, so a host that needs a button
 * in the card supplies one and a host that does not passes nothing.
 */
@Component({
  selector: 'app-detail-card',
  imports: [NzCardModule],
  templateUrl: './detail-card.html',
})
export class DetailCard {
  readonly title = input.required<string>();
  readonly rows = input.required<readonly DetailRow[]>();

  /** Shown instead of the rows when there are none. */
  readonly empty = input<string>('Nothing to show.');
}
