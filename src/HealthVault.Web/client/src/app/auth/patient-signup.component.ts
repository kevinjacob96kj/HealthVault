import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';
import { PasswordFieldComponent } from './password-field.component';
import { isStrongPassword, PASSWORD_RULES_MESSAGE } from './password.util';
import { GoogleAuthConfig } from './auth.model';

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string;
            callback: (response: { credential: string }) => void;
          }) => void;
          renderButton: (
            parent: HTMLElement,
            options: Record<string, string | number>
          ) => void;
        };
      };
    };
  }
}

type SignupStep = 'identity' | 'account';

@Component({
  selector: 'app-patient-signup',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PasswordFieldComponent],
  templateUrl: './patient-signup.component.html',
  styleUrl: './patient-signup.component.css'
})
export class PatientSignupComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  step: SignupStep = 'identity';
  abhaId = '';
  aadhaarNumber = '';
  dateOfBirth = '';
  signupToken = '';

  email = '';
  password = '';
  confirmPassword = '';
  firstName = '';
  lastName = '';
  gender = 'Man';
  mobileNumber = '';

  googleConfig: GoogleAuthConfig | null = null;
  showDemoGoogle = false;
  demoGoogleEmail = '';
  demoGoogleFirstName = '';
  demoGoogleLastName = '';

  submitting = false;
  error: string | null = null;
  info: string | null = null;
  readonly passwordRulesMessage = PASSWORD_RULES_MESSAGE;

  ngOnInit(): void {
    this.auth.getGoogleConfig().subscribe({
      next: (config) => {
        this.googleConfig = config;
        if (config.clientId) {
          queueMicrotask(() => this.initGoogleButton(config.clientId!));
        }
      },
      error: () => {
        this.googleConfig = { clientId: null, allowDemoSignIn: true };
      }
    });
  }

  onAbhaIdInput(value: string): void {
    const digits = value.replace(/\D/g, '').slice(0, 14);
    const parts: string[] = [];
    if (digits.length > 0) {
      parts.push(digits.slice(0, 2));
    }
    if (digits.length > 2) {
      parts.push(digits.slice(2, 6));
    }
    if (digits.length > 6) {
      parts.push(digits.slice(6, 10));
    }
    if (digits.length > 10) {
      parts.push(digits.slice(10, 14));
    }
    this.abhaId = parts.join('-');
  }

  onAadhaarInput(value: string): void {
    this.aadhaarNumber = value.replace(/\D/g, '').slice(0, 12);
  }

  continueIdentity(): void {
    this.error = null;
    this.info = null;

    if (!this.abhaId.trim() || !this.aadhaarNumber.trim() || !this.dateOfBirth) {
      this.error = 'ABHA ID, Aadhaar number, and date of birth are required.';
      return;
    }

    if (this.aadhaarNumber.replace(/\D/g, '').length !== 12) {
      this.error = 'Aadhaar number must be 12 digits.';
      return;
    }

    this.submitting = true;
    this.auth
      .verifyPatientSignup({
        abhaId: this.abhaId.trim(),
        aadhaarNumber: this.aadhaarNumber.trim(),
        dateOfBirth: this.dateOfBirth
      })
      .subscribe({
        next: (result) => {
          this.submitting = false;
          this.signupToken = result.signupToken;
          this.info = result.message;
          this.step = 'account';
          if (this.googleConfig?.clientId) {
            queueMicrotask(() => this.initGoogleButton(this.googleConfig!.clientId!));
          }
        },
        error: (err) => this.onError(err, 'Unable to verify your details.')
      });
  }

  backToIdentity(): void {
    this.step = 'identity';
    this.error = null;
    this.info = null;
    this.showDemoGoogle = false;
  }

  createAccount(): void {
    this.error = null;
    this.info = null;

    if (!this.email.trim() || !this.firstName.trim() || !this.lastName.trim()) {
      this.error = 'Username, first name, and last name are required.';
      return;
    }

    if (!isStrongPassword(this.password)) {
      this.error = PASSWORD_RULES_MESSAGE;
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.error = 'Password and confirmation do not match.';
      return;
    }

    this.submitting = true;
    this.auth
      .completePatientSignup({
        signupToken: this.signupToken,
        email: this.email.trim(),
        password: this.password,
        firstName: this.firstName.trim(),
        lastName: this.lastName.trim(),
        gender: this.gender,
        mobileNumber: this.mobileNumber.trim()
      })
      .subscribe({
        next: () => {
          this.submitting = false;
          void this.router.navigate(['/']);
        },
        error: (err) => this.onError(err, 'Unable to create your account.')
      });
  }

  openDemoGoogle(): void {
    this.showDemoGoogle = true;
    this.error = null;
  }

  submitDemoGoogle(): void {
    this.error = null;
    if (
      !this.demoGoogleEmail.trim() ||
      !this.demoGoogleFirstName.trim() ||
      !this.demoGoogleLastName.trim()
    ) {
      this.error = 'Enter your Google account email and name.';
      return;
    }

    this.submitting = true;
    this.auth
      .completePatientSignupWithGoogle({
        signupToken: this.signupToken,
        demoEmail: this.demoGoogleEmail.trim(),
        demoFirstName: this.demoGoogleFirstName.trim(),
        demoLastName: this.demoGoogleLastName.trim(),
        gender: this.gender,
        mobileNumber: this.mobileNumber.trim()
      })
      .subscribe({
        next: () => {
          this.submitting = false;
          void this.router.navigate(['/']);
        },
        error: (err) => this.onError(err, 'Unable to continue with Google.')
      });
  }

  private initGoogleButton(clientId: string): void {
    const host = document.getElementById('google-signup-button');
    if (!host || this.step !== 'account') {
      return;
    }

    const render = () => {
      if (!window.google?.accounts?.id) {
        return;
      }

      host.innerHTML = '';
      window.google.accounts.id.initialize({
        client_id: clientId,
        callback: (response) => this.onGoogleCredential(response.credential)
      });
      window.google.accounts.id.renderButton(host, {
        theme: 'outline',
        size: 'large',
        width: 320,
        text: 'continue_with'
      });
    };

    if (window.google?.accounts?.id) {
      render();
      return;
    }

    const existing = document.getElementById('google-gsi-script');
    if (existing) {
      existing.addEventListener('load', render, { once: true });
      return;
    }

    const script = document.createElement('script');
    script.id = 'google-gsi-script';
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = render;
    document.head.appendChild(script);
  }

  private onGoogleCredential(idToken: string): void {
    this.submitting = true;
    this.error = null;
    this.auth
      .completePatientSignupWithGoogle({
        signupToken: this.signupToken,
        idToken,
        gender: this.gender,
        mobileNumber: this.mobileNumber.trim()
      })
      .subscribe({
        next: () => {
          this.submitting = false;
          void this.router.navigate(['/']);
        },
        error: (err) => this.onError(err, 'Unable to continue with Google.')
      });
  }

  private onError(err: { error?: { title?: string; errors?: Record<string, string[]> } }, fallback: string): void {
    this.submitting = false;
    this.error =
      err?.error?.title ??
      Object.values(err?.error?.errors ?? {}).flat()[0]?.toString() ??
      fallback;
  }
}
