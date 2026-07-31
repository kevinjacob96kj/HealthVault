import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Redirects signed-in users who still must change their password. */
export const passwordChangeRedirectGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn && auth.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  return true;
};

/** Requires any signed-in user who has already set their password. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn) {
    return router.createUrlTree(['/login']);
  }

  if (auth.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  return true;
};

/** Requires a signed-in hospital Admin or Central Admin. */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn) {
    return router.createUrlTree(['/login']);
  }

  if (auth.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  if (auth.canAccessAdmin) {
    return true;
  }

  return router.createUrlTree(['/']);
};

/** Requires a signed-in Patient. */
export const patientGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn) {
    return router.createUrlTree(['/login']);
  }

  if (auth.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  if (auth.isPatient) {
    return true;
  }

  return router.createUrlTree(['/']);
};

/** Requires a signed-in Doctor. */
export const doctorGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn) {
    return router.createUrlTree(['/login']);
  }

  if (auth.mustChangePassword) {
    return router.createUrlTree(['/change-password']);
  }

  if (auth.isDoctor) {
    return true;
  }

  return router.createUrlTree(['/']);
};

/** Requires a signed-in user who still must change their password. */
export const mustChangePasswordGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn) {
    return router.createUrlTree(['/login']);
  }

  if (!auth.mustChangePassword) {
    return router.createUrlTree(['/']);
  }

  return true;
};
