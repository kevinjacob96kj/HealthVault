import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { PasswordFieldComponent } from './password-field.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, PasswordFieldComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  email = '';
  password = '';
  submitting = false;
  error: string | null = null;

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
        password: this.password.trim()
      })
      .subscribe({
        next: (user) => {
          this.submitting = false;
          void this.router.navigate(
            user.mustChangePassword ? ['/change-password'] : ['/']
          );
        },
        error: (err) => {
          this.submitting = false;
          this.error =
            err?.error?.title ??
            'Unable to sign in. Check your email and password.';
        }
      });
  }
}
