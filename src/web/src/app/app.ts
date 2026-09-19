import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * Root component. It holds nothing but the outlet - the visible chrome is the
 * shell, which is a routed component so it can be swapped per route tree.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {}
