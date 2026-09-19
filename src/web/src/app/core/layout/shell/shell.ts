import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { AuthStore } from '../../auth/auth.store';

/**
 * The application shell: top navigation, who is signed in, and the content region every
 * page renders into. The shell owns the navigation collapse; pages own their own layout.
 */
@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    NzLayoutModule,
    NzMenuModule,
    NzIconModule,
    NzButtonModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly userName = this.auth.userName;
  protected readonly isAuthenticated = this.auth.isAuthenticated;

  /** Small viewports collapse the nav into a toggle; the shell decides, not the page. */
  protected readonly navOpen = signal(false);

  protected toggleNav(): void {
    this.navOpen.update((open) => !open);
  }

  protected closeNav(): void {
    this.navOpen.set(false);
  }

  protected signOut(): void {
    this.auth.signOut();
    void this.router.navigate(['/auth/login']);
  }
}
