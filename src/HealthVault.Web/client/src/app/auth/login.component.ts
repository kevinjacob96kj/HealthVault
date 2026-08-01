import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';
import { PasswordFieldComponent } from './password-field.component';

type LoginMode = 'staff' | 'patient';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PasswordFieldComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  mode: LoginMode = 'staff';
  email = '';
  password = '';
  submitting = false;
  error: string | null = null;

  setMode(mode: LoginMode): void {
    this.mode = mode;
    this.error = null;
  }

  submit(): void {
    this.error = null;

    if (!this.email.trim() || !this.password.trim()) {
      this.error = 'Email and password are required.';
      return;
    }

    this.submitting = true;
    this.auth
      .login({
        email: this.email.trim(),
        password: this.password.trim(),
        mode: this.mode
      })
      .subscribe({
        next: (user) => this.onLoginSuccess(user.mustChangePassword),
        error: (err) =>
          this.onLoginError(err, 'Unable to sign in. Check your email and password.')
      });
  }

  private onLoginSuccess(mustChangePassword: boolean): void {
    this.submitting = false;
    void this.router.navigate(mustChangePassword ? ['/change-password'] : ['/']);
  }

  private onLoginError(err: { error?: { title?: string } }, fallback: string): void {
    this.submitting = false;
    this.error = err?.error?.title ?? fallback;
  }
}
