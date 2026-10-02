import { NgOptimizedImage } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { email, form, FormField, required } from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Auth } from '../auth.service';
import { LoginRequest } from '../models/auth.model';
import { ThemeToggle } from '../../../shared/ui/theme-toggle/theme-toggle.component';
import { GoogleSignInButton } from '../google-sign-in-button.component';

@Component({
  imports: [NgOptimizedImage, RouterLink, FormField, ThemeToggle, GoogleSignInButton],
  selector: 'app-login',
  styleUrl: './login.component.scss',
  templateUrl: './login.component.html',
})
export class Login {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly loginModel = signal<LoginRequest>({
    email: '',
    password: '',
  });

  protected readonly loginForm = form(this.loginModel, (fields) => {
    required(fields.email, { message: 'Email is required.' });
    email(fields.email, { message: 'Enter a valid email address.' });
    required(fields.password, { message: 'Password is required.' });
  });

  protected login(event: SubmitEvent): void {
    event.preventDefault();

    if (this.loginForm().invalid()) {
      this.errorMessage.set('Please enter a valid email and password.');
      return;
    }

    const values = this.loginModel();

    this.errorMessage.set(null);
    this.submitting.set(true);

    this.auth
      .login({
        email: values.email.trim(),
        password: values.password,
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl('/dashboard'),
        error: (error: HttpErrorResponse) => {
          const code = error.error?.code;

          if (
            code === 'INVALID_CREDENTIALS' ||
            code === 'INVALID_PASSWORD'
          ) {
            this.errorMessage.set('Invalid email or password.');
            return;
          }

          this.errorMessage.set(
            error.error?.detail ?? 'It was not possible to sign in.',
          );
        },
      });
  }

  protected loginWithGoogle(credential: string): void {
    this.errorMessage.set(null);
    this.submitting.set(true);

    this.auth
      .loginWithGoogle(credential)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl('/dashboard'),
        error: (error: HttpErrorResponse) => {
          const code = error.error?.code;

          if (code === 'GOOGLE_LINK_REQUIRED') {
            this.errorMessage.set(
              'Esta conta já existe. Inicia sessão com a password para associares o Google no perfil.',
            );
            return;
          }

          this.errorMessage.set(
            error.error?.detail ?? 'Não foi possível iniciar sessão com o Google.',
          );
        },
      });
  }
}
