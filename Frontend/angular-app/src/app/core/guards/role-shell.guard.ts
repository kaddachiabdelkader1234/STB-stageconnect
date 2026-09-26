import { inject } from '@angular/core';
import { Router, CanActivateFn, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Role-shell guard — keeps each role inside its own shell.
 *
 * Attached at the shell level (/admin/**, /espace/**): a trainer pasting an admin URL lands
 * back in their own workspace and vice versa. Feature-level rules stay with permissionGuard.
 *
 * Redirect targets are the role's own home:
 *   ADMIN    → /admin
 *   TRAINER  → /espace
 *   LEARNER  → /espace
 */
export const roleShellGuard = (allowed: string[]): CanActivateFn =>
  (route: ActivatedRouteSnapshot) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      router.navigate(['/auth/sign-in']);
      return false;
    }

    if (authService.hasAnyRole(allowed)) {
      return true;
    }

    // Wrong shell — send the user to their own home.
    const home = authService.hasRole('ADMIN') ? '/admin' : '/espace';
    router.navigate([home]);
    return false;
  };
