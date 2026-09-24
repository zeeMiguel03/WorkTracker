import { HttpClient } from '@angular/common/http';
import { computed, inject, Service, signal } from '@angular/core';
import { catchError, finalize, map, Observable, of, shareReplay, tap, timeout } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest } from './models/auth.model';

@Service()
export class Auth {
    private readonly http = inject(HttpClient);

    private readonly session = signal<AuthResponse | null>(null);
    private sessionRestoreRequest: Observable<boolean> | null = null;
    private refreshRequest: Observable<AuthResponse> | null = null;

    readonly accessToken = computed(() => this.session()?.accessToken ?? null);
    readonly currentUser = computed(() => this.session()?.user ?? null);
    readonly isAuthenticated = computed(() => this.session() !== null);
    readonly profileImageRevision = signal(0);

    register(data: FormData): Observable<AuthResponse> {
        return this.http
            .post<AuthResponse>(`${environment.apiUrl}/auth/register`, data, {
                withCredentials: true,
            })
            .pipe(
                tap((response) => {
                    this.session.set(response);
                    this.profileImageRevision.update((revision) => revision + 1);
                }),
            );
    }

    login(data: LoginRequest): Observable<AuthResponse> {
        return this.http
            .post<AuthResponse>(`${environment.apiUrl}/auth/login`, data, {
                withCredentials: true,
            })
            .pipe(
                tap((response) => {
                    this.session.set(response);
                    this.profileImageRevision.update((revision) => revision + 1);
                }),
            );
    }

    refresh(): Observable<AuthResponse> {
        if (this.refreshRequest) {
            return this.refreshRequest;
        }

        this.refreshRequest = this.http
            .post<AuthResponse>(
                `${environment.apiUrl}/auth/refresh`,
                {},
                { withCredentials: true },
            )
            .pipe(
                tap((response) => {
                    this.session.set(response);
                    this.profileImageRevision.update((revision) => revision + 1);
                }),
                finalize(() => {
                    this.refreshRequest = null;
                }),
                shareReplay({ bufferSize: 1, refCount: false }),
            );

        return this.refreshRequest;
    }

    getProfileImage(): Observable<Blob> {
        return this.http.get(`${environment.apiUrl}/users/me/image`, {
            responseType: 'blob',
            withCredentials: true,
        });
    }

    ensureSession(): Observable<boolean> {
        if (this.isAuthenticated()) {
            return of(true);
        }

        if (this.sessionRestoreRequest) {
            return this.sessionRestoreRequest;
        }

        this.sessionRestoreRequest = this.refresh().pipe(
            // Do not block route activation indefinitely when the API is stopped
            // or still starting. The sign-in page must remain usable offline.
            timeout({ first: 5000 }),
            map(() => true),
            catchError(() => {
                this.clearSession();
                return of(false);
            }),
            finalize(() => {
                this.sessionRestoreRequest = null;
            }),
            shareReplay({ bufferSize: 1, refCount: false }),
        );

        return this.sessionRestoreRequest;
    }

    logout(): Observable<void> {
        return this.http
            .post<void>(
                `${environment.apiUrl}/auth/logout`,
                {},
                { withCredentials: true },
            )
            .pipe(tap(() => this.session.set(null)));
    }

    updateProfile(data: FormData): Observable<void> {
        return this.http.put<void>(`${environment.apiUrl}/users/me`, data, {
            withCredentials: true,
        });
    }

    changePassword(data: { CurrentPassword: string; NewPassword: string }): Observable<void> {
        return this.http.patch<void>(`${environment.apiUrl}/users/me/password`, data, {
            withCredentials: true,
        });
    }

    logoutAll(): Observable<void> {
        return this.http
            .post<void>(`${environment.apiUrl}/auth/logout-all`, {}, { withCredentials: true })
            .pipe(tap(() => this.session.set(null)));
    }

    deleteAccount(): Observable<void> {
        return this.http
            .delete<void>(`${environment.apiUrl}/users/me`, { withCredentials: true })
            .pipe(tap(() => this.session.set(null)));
    }

    clearSession(): void {
        this.session.set(null);
    }
}
