import { Component, input } from '@angular/core';

/**
 * The band at the top of every page: heading, optional description, and a slot for
 * page-level actions. Presentational only - it has no data-access imports.
 */
@Component({
  selector: 'app-page-header',
  templateUrl: './page-header.html',
})
export class PageHeader {
  readonly heading = input.required<string>();
  readonly description = input<string>();
}
