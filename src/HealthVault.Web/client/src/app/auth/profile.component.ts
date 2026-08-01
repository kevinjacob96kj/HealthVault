import { Component, HostListener, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from './auth.service';
import { isStrongPassword, PASSWORD_RULES_MESSAGE } from './password.util';
import { PasswordFieldComponent } from './password-field.component';
import { PatientsService } from '../patients/patients.service';
import { Patient } from '../patients/patient.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, PasswordFieldComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly patientsService = inject(PatientsService);

  firstName = '';
  lastName = '';
  email = '';
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  passwordModalOpen = false;

  patientRecord: Patient | null = null;
  patientLoading = false;
  patientError: string | null = null;

  profileSaving = false;
  passwordSaving = false;
  profileError: string | null = null;
  profileSuccess: string | null = null;
  passwordError: string | null = null;
  passwordSuccess: string | null = null;
  readonly passwordRulesMessage = PASSWORD_RULES_MESSAGE;

  get isPatient(): boolean {
    return this.auth.isPatient;
  }

  ngOnInit(): void {
    const user = this.auth.user;
    if (!user) {
      return;
    }

    this.firstName = user.firstName;
    this.lastName = user.lastName;
    this.email = user.email;

    if (this.auth.isPatient) {
      this.loadPatientRecord();
    }
  }

  formatDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
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

  openPasswordModal(): void {
    this.passwordError = null;
    this.passwordSuccess = null;
    this.currentPassword = '';
    this.newPassword = '';
    this.confirmPassword = '';
    this.passwordModalOpen = true;
  }

  closePasswordModal(): void {
    if (this.passwordSaving) {
      return;
    }

    this.passwordModalOpen = false;
    this.passwordError = null;
    this.currentPassword = '';
    this.newPassword = '';
    this.confirmPassword = '';
  }

  @HostListener('document:keydown.escape')
  closePasswordModalOnEscape(): void {
    if (this.passwordModalOpen) {
      this.closePasswordModal();
    }
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
          this.passwordModalOpen = false;
          this.passwordSaving = false;
        },
        error: (err) => {
          this.passwordError =
            err?.error?.title ?? 'Unable to update your password.';
          this.passwordSaving = false;
        }
      });
  }

  private loadPatientRecord(): void {
    this.patientLoading = true;
    this.patientError = null;

    this.patientsService.getMyPatient().subscribe({
      next: (patient) => {
        this.patientRecord = patient;
        this.patientLoading = false;
      },
      error: () => {
        this.patientError = 'Unable to load your patient record.';
        this.patientLoading = false;
      }
    });
  }
}
