import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { isStrongPassword, PASSWORD_RULES_MESSAGE } from './password.util';
import { PasswordFieldComponent } from './password-field.component';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule, PasswordFieldComponent],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.css'
})
export class ChangePasswordComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  newPassword = '';
  confirmPassword = '';
  submitting = false;
  error: string | null = null;

  readonly rulesMessage = PASSWORD_RULES_MESSAGE;

  submit(): void {
    this.error = null;

    if (!isStrongPassword(this.newPassword.trim())) {
      this.error = PASSWORD_RULES_MESSAGE;
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.error = 'New password and confirmation do not match.';
      return;
    }

    this.submitting = true;
    this.auth.forceChangePassword(this.newPassword.trim()).subscribe({
      next: () => {
        this.submitting = false;
        void this.router.navigate(['/']);
      },
      error: (err) => {
        this.submitting = false;
        this.error =
          err?.error?.title ??
          Object.values(err?.error?.errors ?? {}).flat()[0]?.toString() ??
          'Unable to update your password.';
      }
    });
  }
}
