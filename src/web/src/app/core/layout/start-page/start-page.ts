import { Component } from '@angular/core';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { ThreeColumn } from '../../../shared/ui/three-column/three-column';

/**
 * Placeholder landing page. It exists only so the shell has something to render
 * before the first feature is mounted - delete it with the first real route.
 */
@Component({
  selector: 'app-start-page',
  imports: [PageHeader, ThreeColumn],
  templateUrl: './start-page.html',
})
export class StartPage {}
