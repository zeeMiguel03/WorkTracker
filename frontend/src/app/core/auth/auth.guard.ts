import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { Auth } from '../../features/auth/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);

  return auth.ensureSession().pipe(
    map((isAuthenticated) =>
      isAuthenticated ? true : router.createUrlTree(['/signin']),
    ),
  );
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);

  return auth.ensureSession().pipe(
    map((isAuthenticated) =>
      isAuthenticated ? router.createUrlTree(['/dashboard']) : true,
    ),
  );
};
