import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth.service';

function requireSession(
  decide: (auth: AuthService, router: Router) => boolean | UrlTree
): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isLoggedIn) {
      return router.createUrlTree(['/login']);
    }

    return auth.refreshSession().pipe(
      map((user) => {
        if (!user) {
          return router.createUrlTree(['/login']);
        }

        return decide(auth, router);
      })
    );
  };
}

/** Redirects signed-in users who still must change their password. */
export const passwordChangeRedirectGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn) {
    return true;
  }

  return auth.refreshSession().pipe(
    map((user) => {
      if (user?.mustChangePassword) {
        return router.createUrlTree(['/change-password']);
      }

      return true;
    })
  );
};

/** Requires any signed-in user who has already set their password. */
export const authGuard: CanActivateFn = requireSession((auth, router) => {
  if (auth.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  return true;
});

/** Requires a signed-in hospital Admin or Central Admin. */
export const adminGuard: CanActivateFn = requireSession((auth, router) => {
  if (auth.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  if (auth.canAccessAdmin) {
    return true;
  }

  return router.createUrlTree(['/']);
});

/** Requires a signed-in Patient. */
export const patientGuard: CanActivateFn = requireSession((auth, router) => {
  if (auth.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  if (auth.isPatient) {
    return true;
  }

  return router.createUrlTree(['/']);
});

/** Requires a signed-in Doctor. */
export const doctorGuard: CanActivateFn = requireSession((auth, router) => {
  if (auth.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  if (auth.isDoctor) {
    return true;
  }

  return router.createUrlTree(['/']);
});

/** Requires a signed-in user who still must change their password. */
export const mustChangePasswordGuard: CanActivateFn = requireSession((auth, router) => {
  if (!auth.mustChangePassword) {
    return router.createUrlTree(['/']);
  }

  return true;
});
