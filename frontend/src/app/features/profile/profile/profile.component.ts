import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { Auth } from '../../auth/auth.service';
import { FilePicker } from '../../../shared/ui/file-picker/file-picker.component';
import { Modal } from '../../../shared/ui/modal/modal.component';
import { getPasswordStrengthError } from '../../../shared/security/password-policy';

@Component({
  imports: [RouterLink, FilePicker, Modal],
  selector: 'app-profile',
  styleUrl: './profile.component.scss',
  templateUrl: './profile.component.html',
})
export class Profile implements OnDestroy {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  protected readonly user = this.auth.currentUser;
  protected readonly editingProfile = signal(false);
  protected readonly savingProfile = signal(false);
  protected readonly selectedImage = signal<File | null>(null);
  protected readonly profileImageSrc = signal<string | null>(null);
  protected readonly profileError = signal<string | null>(null);
  protected readonly changingPassword = signal(false);
  protected readonly passwordModalOpen = signal(false);
  protected readonly passwordError = signal<string | null>(null);
  protected readonly confirmationAction = signal<'logout-all' | 'delete-account' | null>(null);
  protected readonly confirmingAction = signal(false);
  protected readonly twoFactorEnabled = signal(false);
  protected readonly successModal = signal<{ title: string; message: string; redirectTo?: string } | null>(null);

  protected readonly displayName = computed(() => this.user()?.name ?? 'Utilizador');
  protected readonly firstName = computed(() => this.displayName().split(/\s+/)[0] ?? '');
  protected readonly lastName = computed(() => this.displayName().split(/\s+/).slice(1).join(' '));
  
  protected readonly memberSince = computed(() => {
    const createdAt = this.user()?.createdAt;

    if (!createdAt) {
      return 'Data não disponível';
    }

    return new Intl.DateTimeFormat('pt-PT', { month: 'long', year: 'numeric' }).format(
      new Date(createdAt),
    );
  });

  private profileImageObjectUrl: string | null = null;

  constructor() {
    this.loadProfileImage();
  }

  ngOnDestroy(): void {
    this.revokeProfileImageUrl();
  }

  private loadProfileImage(): void {
    this.revokeProfileImageUrl();

    if (!this.user()?.profileImageUrl) {
      this.profileImageSrc.set(null);
      return;
    }

    this.auth.getProfileImage().subscribe({
      next: (image) => {
        this.profileImageObjectUrl = URL.createObjectURL(image);
        this.profileImageSrc.set(this.profileImageObjectUrl);
      },
      error: () => this.profileImageSrc.set(null),
    });
  }

  private revokeProfileImageUrl(): void {
    if (this.profileImageObjectUrl) {
      URL.revokeObjectURL(this.profileImageObjectUrl);
      this.profileImageObjectUrl = null;
    }
  }

  protected startEditing(): void {
    this.profileError.set(null);
    this.selectedImage.set(null);
    this.editingProfile.set(true);
  }

  protected cancelEditing(): void {
    this.editingProfile.set(false);
    this.selectedImage.set(null);
    this.profileError.set(null);
  }

  protected selectImage(file: File | null): void {
    this.selectedImage.set(file);
  }

  protected saveProfile(event: SubmitEvent): void {
    event.preventDefault();

    const form = event.target as HTMLFormElement;
    const name = (form.elements.namedItem('profileName') as HTMLInputElement).value.trim();

    if (!name) {
      this.profileError.set('O nome é obrigatório.');
      return;
    }

    const data = new FormData();
    data.append('Name', name);

    const image = this.selectedImage();
    if (image) {
      data.append('ProfileImageUrl', image, image.name);
    }

    this.profileError.set(null);
    this.savingProfile.set(true);

    this.auth
      .updateProfile(data)
      .pipe(switchMap(() => this.auth.refresh()))
      .subscribe({
        next: () => {
          this.editingProfile.set(false);
          this.selectedImage.set(null);
          this.savingProfile.set(false);
          this.successModal.set({
            title: 'Perfil atualizado!',
            message: 'As tuas alterações foram guardadas com sucesso.',
          });
          this.loadProfileImage();
        },
        error: (error: HttpErrorResponse) => {
          this.profileError.set(error.error?.detail ?? 'Não foi possível atualizar o perfil.');
          this.savingProfile.set(false);
        },
      });
  }

  protected openPasswordModal(): void {
    this.passwordError.set(null);
    this.passwordModalOpen.set(true);
  }

  protected closePasswordModal(): void {
    if (!this.changingPassword()) {
      this.passwordModalOpen.set(false);
    }
  }

  protected closeSuccessModal(): void {
    const redirectTo = this.successModal()?.redirectTo;

    this.successModal.set(null);

    if (redirectTo) {
      void this.router.navigateByUrl(redirectTo);
    }
  }

  protected openConfirmation(action: 'logout-all' | 'delete-account'): void {
    this.profileError.set(null);
    this.confirmationAction.set(action);
  }

  protected closeConfirmation(): void {
    if (!this.confirmingAction()) {
      this.confirmationAction.set(null);
    }
  }

  protected confirmDangerAction(): void {
    const action = this.confirmationAction();

    if (!action) {
      return;
    }

    this.confirmingAction.set(true);

    const request = action === 'delete-account'
      ? this.auth.deleteAccount()
      : this.auth.logoutAll();

    request.subscribe({
      next: () => {
        this.confirmingAction.set(false);
        this.confirmationAction.set(null);
        void this.router.navigateByUrl(action === 'delete-account' ? '/signup' : '/signin');
      },
      error: () => {
        this.confirmingAction.set(false);
        this.profileError.set(
          action === 'delete-account'
            ? 'Não foi possível apagar a conta.'
            : 'Não foi possível terminar as sessões.',
        );
      },
    });
  }

  protected changePassword(event: SubmitEvent): void {
    event.preventDefault();

    const form = event.target as HTMLFormElement;
    const currentPassword = (form.elements.namedItem('currentPassword') as HTMLInputElement).value;
    const newPassword = (form.elements.namedItem('newPassword') as HTMLInputElement).value;
    const confirmPassword = (form.elements.namedItem('confirmPassword') as HTMLInputElement).value;

    const passwordStrengthError = getPasswordStrengthError(newPassword);

    if (passwordStrengthError) {
      this.passwordError.set(passwordStrengthError);
      return;
    }

    if (newPassword !== confirmPassword) {
      this.passwordError.set('As passwords não coincidem.');
      return;
    }

    this.passwordError.set(null);
    this.changingPassword.set(true);

    this.auth.changePassword({ CurrentPassword: currentPassword, NewPassword: newPassword }).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordModalOpen.set(false);
        this.auth.clearSession();
        this.successModal.set({
          title: 'Password alterada!',
          message: 'A tua password foi alterada com sucesso. Inicia sessão novamente.',
          redirectTo: '/signin',
        });
      },
      error: (error: HttpErrorResponse) => {
        this.passwordError.set(
          error.error?.code === 'INVALID_PASSWORD'
            ? 'A password atual está incorreta.'
            : error.error?.detail ?? 'Não foi possível alterar a password.',
        );
        this.changingPassword.set(false);
      },
    });
  }

}
