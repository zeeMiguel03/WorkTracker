import { HttpClient } from '@angular/common/http';
import { computed, inject, Service, signal } from '@angular/core';
import { catchError, finalize, map, Observable, of, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest } from './models/auth.model';

@Service()
export class Auth {
    private readonly http = inject(HttpClient);

    private readonly session = signal<AuthResponse | null>(null);
    private sessionRestoreRequest: Observable<boolean> | null = null;

    readonly accessToken = computed(() => this.session()?.accessToken ?? null);
    readonly currentUser = computed(() => this.session()?.user ?? null);
    readonly isAuthenticated = computed(() => this.session() !== null);

    register(data: FormData): Observable<AuthResponse> {
        return this.http
            .post<AuthResponse>(`${environment.apiUrl}/auth/register`, data, {
                withCredentials: true,
            })
            .pipe(tap((response) => this.session.set(response)));
    }

    login(data: LoginRequest): Observable<AuthResponse> {
        return this.http
            .post<AuthResponse>(`${environment.apiUrl}/auth/login`, data, {
                withCredentials: true,
            })
            .pipe(tap((response) => this.session.set(response)));
    }

    refresh(): Observable<AuthResponse> {
        return this.http
            .post<AuthResponse>(
                `${environment.apiUrl}/auth/refresh`,
                {},
                { withCredentials: true },
            )
            .pipe(tap((response) => this.session.set(response)));
    }

    ensureSession(): Observable<boolean> {
        if (this.isAuthenticated()) {
            return of(true);
        }

        if (this.sessionRestoreRequest) {
            return this.sessionRestoreRequest;
        }

        this.sessionRestoreRequest = this.refresh().pipe(
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

    clearSession(): void {
        this.session.set(null);
    }
}
