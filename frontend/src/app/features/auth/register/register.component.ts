import { NgOptimizedImage } from '@angular/common';
import { Component, inject, signal } from '@angular/core'
import { email, form, FormField, minLength, required } from '@angular/forms/signals';
import { RouterLink, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { RegisterFormModel } from '../models/register.model';
import { Auth } from '../auth.service';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  imports: [NgOptimizedImage, RouterLink, FormField],
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
    required(fields.name, { message: 'Name is required.' });

    required(fields.email, { message: 'Email is required.' });
    email(fields.email, { message: 'Enter a valid email address.' });

    required(fields.password, { message: 'Password is required.' });
    minLength(fields.password, 8, { message: 'Password must contain at least 8 characters.', });

    required(fields.confirmPassword, { message: 'Please confirm your password.', });

    required(fields.acceptTerms, { message: 'You must accept the terms.', });
  });


  protected selectImage(event: Event): void {
    const input = event.target as HTMLInputElement;

    this.profileImage.set(input.files?.item(0) ?? null);
  }

  protected register(event: SubmitEvent): void {
    event.preventDefault();

    const values = this.registerModel();

    if (this.registerForm().invalid()) {
      this.errorMessage.set('Please complete all required fields.');
      return;
    }

    if (values.password !== values.confirmPassword) {
      this.errorMessage.set('Passwords do not match.');
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
          this.errorMessage.set('An account with this email already exists.');
          return;
        }

        this.errorMessage.set(
          error.error?.detail ??
          'It was not possible to create your account.',
        );
      },
    });
  }
}
