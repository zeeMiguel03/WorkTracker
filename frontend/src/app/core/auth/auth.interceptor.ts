import {HttpErrorResponse, HttpInterceptorFn,} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Auth } from '../../features/auth/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(Auth);
  const router = inject(Router);

  const isApiRequest = request.url.startsWith(environment.apiUrl);

  const isPublicAuthRequest =
    request.url.endsWith('/auth/login') ||
    request.url.endsWith('/auth/register') ||
    request.url.endsWith('/auth/refresh');

  const accessToken = auth.accessToken();

  const requestWithAuthentication = isApiRequest
    ? request.clone({
      withCredentials: true,
      setHeaders:
        accessToken && !isPublicAuthRequest
          ? {
            Authorization: `Bearer ${accessToken}`,
          }
          : {},
    })
    : request;

  return next(requestWithAuthentication).pipe(
    catchError((error: unknown) => {
      const shouldRefreshToken =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        isApiRequest &&
        !isPublicAuthRequest;

      if (!shouldRefreshToken) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap(() => {
          const refreshedToken = auth.accessToken();

          const retryRequest = request.clone({
            withCredentials: true,
            setHeaders: refreshedToken
              ? {
                Authorization: `Bearer ${refreshedToken}`,
              }
              : {},
          });

          return next(retryRequest);
        }),

        catchError((refreshError: unknown) => {
          auth.clearSession();
          void router.navigateByUrl('/signin');

          return throwError(() => refreshError);
        }),
      );
    }),
  );
};