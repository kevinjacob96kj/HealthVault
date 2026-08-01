import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const email = auth.user?.email;

  if (!email || request.url.includes('/api/auth/login')) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        'X-User-Email': email
      }
    })
  );
};
