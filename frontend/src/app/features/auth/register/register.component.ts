import { NgOptimizedImage } from '@angular/common';
import { Component, inject, signal } from '@angular/core'
import { email, form, FormField, minLength, required } from '@angular/forms/signals';
import { RouterLink, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { RegisterFormModel } from '../models/register.model';
import { Auth } from '../auth.service';
import { HttpErrorResponse } from '@angular/common/http';
import { FilePicker } from '../../../shared/ui/file-picker/file-picker.component';
import { ThemeToggle } from '../../../shared/ui/theme-toggle/theme-toggle.component';
import { GoogleSignInButton } from '../google-sign-in-button.component';
import { LanguageToggle } from '../../../shared/ui/language-toggle/language-toggle.component';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import {
  getPasswordStrengthError,
  PASSWORD_MIN_LENGTH,
} from '../../../shared/security/password-policy';

@Component({
  imports: [NgOptimizedImage, RouterLink, FormField, FilePicker, ThemeToggle, LanguageToggle, GoogleSignInButton, TranslatePipe],
  selector: 'app-register',
  styleUrl: './register.component.scss',
  templateUrl: './register.component.html',
})

export class Register {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly profileImage = signal<File | null>(null);

  protected readonly registerModel = signal<RegisterFormModel>({
    name: '',
    email: '',
    password: '',
    confirmPassword: '',
    acceptTerms: false,
  });

  protected readonly registerForm = form(this.registerModel, (fields) => {
    required(fields.name, { message: 'O nome é obrigatório.' });

    required(fields.email, { message: 'O email é obrigatório.' });
    email(fields.email, { message: 'Indica um endereço de email válido.' });

    required(fields.password, { message: 'A password é obrigatória.' });
    minLength(fields.password, PASSWORD_MIN_LENGTH, { message: 'A password deve ter pelo menos 12 caracteres.', });

    required(fields.confirmPassword, { message: 'Confirma a tua password.', });

    required(fields.acceptTerms, { message: 'Tens de aceitar os termos.', });
  });


  protected selectImage(file: File | null): void {
    this.profileImage.set(file);
  }

  protected register(event: SubmitEvent): void {
    event.preventDefault();

    const values = this.registerModel();

    if (this.registerForm().invalid()) {
      this.errorMessage.set('Preenche todos os campos obrigatórios.');
      return;
    }

    const passwordStrengthError = getPasswordStrengthError(values.password);

    if (passwordStrengthError) {
      this.errorMessage.set(passwordStrengthError);
      return;
    }

    if (values.password !== values.confirmPassword) {
      this.errorMessage.set('As passwords não coincidem.');
      return;
    }

    const data = new FormData();

    data.append('Name', values.name.trim());
    data.append('Email', values.email.trim());
    data.append('Password', values.password);

    const image = this.profileImage();

    if (image) {
      data.append('ProfileImageUrl', image, image.name);
    }

    this.errorMessage.set(null);
    this.submitting.set(true);

    this.auth.register(data).pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: () => void this.router.navigateByUrl('/dashboard'),
      error: (error: HttpErrorResponse) => {
        const code = error.error?.code;

        if (code === 'EMAIL_ALREADY_EXISTS') {
          this.errorMessage.set('Já existe uma conta com este email.');
          return;
        }

        this.errorMessage.set(
          error.error?.detail ??
          'Não foi possível criar a conta.',
        );
      },
    });
  }

  protected registerWithGoogle(credential: string): void {
    if (!this.registerModel().acceptTerms) {
      this.errorMessage.set('Aceita os Termos e a Política de Privacidade para criar a conta.');
      return;
    }

    this.errorMessage.set(null);
    this.submitting.set(true);

    this.auth
      .loginWithGoogle(credential)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl('/dashboard'),
        error: (error: HttpErrorResponse) => {
          const code = error.error?.code;

          this.errorMessage.set(
            code === 'GOOGLE_LINK_REQUIRED'
              ? 'Já existe uma conta com este email. Inicia sessão com a password e associa o Google no perfil.'
              : error.error?.detail ?? 'Não foi possível criar a conta com o Google.',
          );
        },
      });
  }
}
