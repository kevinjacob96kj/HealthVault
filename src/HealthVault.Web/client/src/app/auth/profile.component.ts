import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from './auth.service';
import { isStrongPassword, PASSWORD_RULES_MESSAGE } from './password.util';
import { PasswordFieldComponent } from './password-field.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, PasswordFieldComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);

  firstName = '';
  lastName = '';
  email = '';
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  profileSaving = false;
  passwordSaving = false;
  profileError: string | null = null;
  profileSuccess: string | null = null;
  passwordError: string | null = null;
  passwordSuccess: string | null = null;
  readonly passwordRulesMessage = PASSWORD_RULES_MESSAGE;

  ngOnInit(): void {
    const user = this.auth.user;
    if (!user) {
      return;
    }

    this.firstName = user.firstName;
    this.lastName = user.lastName;
    this.email = user.email;
  }

  saveProfile(): void {
    this.profileError = null;
    this.profileSuccess = null;

    if (!this.firstName.trim() || !this.lastName.trim()) {
      this.profileError = 'First name and last name are required.';
      return;
    }

    this.profileSaving = true;
    this.auth
      .updateProfile(this.firstName.trim(), this.lastName.trim())
      .subscribe({
        next: (person) => {
          this.firstName = person.firstName;
          this.lastName = person.lastName;
          this.profileSuccess = 'Profile updated.';
          this.profileSaving = false;
        },
        error: () => {
          this.profileError = 'Unable to update your profile.';
          this.profileSaving = false;
        }
      });
  }

  savePassword(): void {
    this.passwordError = null;
    this.passwordSuccess = null;

    if (!this.currentPassword.trim()) {
      this.passwordError = 'Current password is required.';
      return;
    }

    if (!isStrongPassword(this.newPassword.trim())) {
      this.passwordError = PASSWORD_RULES_MESSAGE;
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.passwordError = 'New password and confirmation do not match.';
      return;
    }

    this.passwordSaving = true;
    this.auth
      .changePassword(this.currentPassword.trim(), this.newPassword.trim())
      .subscribe({
        next: () => {
          this.currentPassword = '';
          this.newPassword = '';
          this.confirmPassword = '';
          this.passwordSuccess = 'Password updated.';
          this.passwordSaving = false;
        },
        error: (err) => {
          this.passwordError =
            err?.error?.title ?? 'Unable to update your password.';
          this.passwordSaving = false;
        }
      });
  }
}
