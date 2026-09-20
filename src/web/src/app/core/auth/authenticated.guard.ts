import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

/**
 * Keeps signed-out people off the routes whose endpoints are authenticated. Without it
 * the page would render, call the API, and show a 401 banner - technically safe, but it
 * asks a person to read an error to learn they need to sign in.
 *
 * This is a routing convenience, never a security boundary: the API is default-deny and
 * is what actually enforces access.
 */
export const authenticatedGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  // Carries where they were going, so signing in finishes the journey they started.
  return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
};
