import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import {
  GoogleAuthConfig,
  LoginRequest,
  LoginUser,
  PatientGoogleSignupRequest,
  PatientLoginRequest,
  PatientOtpRequest,
  PatientOtpSent,
  PatientSignupCompleteRequest,
  PatientSignupVerified,
  PatientSignupVerifyRequest,
  StaffLoginRequest
} from './auth.model';
import { Person } from '../users/person.model';

const STORAGE_KEY = 'healthvault.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private currentUser: LoginUser | null = this.readStoredUser();

  get user(): LoginUser | null {
    return this.currentUser;
  }

  get isLoggedIn(): boolean {
    return this.currentUser !== null;
  }

  get mustChangePassword(): boolean {
    return this.currentUser?.mustChangePassword === true;
  }

  get isAdmin(): boolean {
    return (this.currentUser?.roles ?? []).some(
      (role) => role.toLowerCase() === 'admin'
    );
  }

  get isCentralAdmin(): boolean {
    return (this.currentUser?.roles ?? []).some(
      (role) => role.toLowerCase() === 'centraladmin'
    );
  }

  get canAccessAdmin(): boolean {
    return this.isAdmin || this.isCentralAdmin;
  }

  get isPatient(): boolean {
    return (this.currentUser?.roles ?? []).some(
      (role) => role.toLowerCase() === 'patient'
    );
  }

  get isDoctor(): boolean {
    return (this.currentUser?.roles ?? []).some(
      (role) => role.toLowerCase() === 'doctor'
    );
  }

  get displayName(): string {
    if (!this.currentUser) {
      return '';
    }

    return `${this.currentUser.firstName} ${this.currentUser.lastName}`;
  }

  login(request: StaffLoginRequest | LoginRequest): Observable<LoginUser> {
    return this.http.post<LoginUser>('/api/auth/login', request).pipe(
      tap((user) => {
        this.setCurrentUser(user);
      })
    );
  }

  sendPatientOtp(request: PatientOtpRequest): Observable<PatientOtpSent> {
    return this.http.post<PatientOtpSent>('/api/auth/login/patient/otp', request);
  }

  loginAsPatient(request: PatientLoginRequest): Observable<LoginUser> {
    return this.http.post<LoginUser>('/api/auth/login/patient', request).pipe(
      tap((user) => {
        this.setCurrentUser(user);
      })
    );
  }

  getGoogleConfig(): Observable<GoogleAuthConfig> {
    return this.http.get<GoogleAuthConfig>('/api/auth/google-config');
  }

  verifyPatientSignup(request: PatientSignupVerifyRequest): Observable<PatientSignupVerified> {
    return this.http.post<PatientSignupVerified>('/api/auth/signup/patient/verify', request);
  }

  completePatientSignup(request: PatientSignupCompleteRequest): Observable<LoginUser> {
    return this.http.post<LoginUser>('/api/auth/signup/patient', request).pipe(
      tap((user) => this.setCurrentUser(user))
    );
  }

  completePatientSignupWithGoogle(request: PatientGoogleSignupRequest): Observable<LoginUser> {
    return this.http.post<LoginUser>('/api/auth/signup/patient/google', request).pipe(
      tap((user) => this.setCurrentUser(user))
    );
  }

  updateProfile(firstName: string, lastName: string): Observable<Person> {
    const user = this.currentUser;
    if (!user) {
      throw new Error('Not signed in.');
    }

    return this.http
      .put<Person>(`/api/people/${user.id}/profile`, { firstName, lastName })
      .pipe(
        tap((person) => {
          this.setCurrentUser({
            ...user,
            firstName: person.firstName,
            lastName: person.lastName,
            email: person.email,
            roles: person.roles
          });
        })
      );
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    const user = this.currentUser;
    if (!user) {
      throw new Error('Not signed in.');
    }

    return this.http.put<void>(`/api/people/${user.id}/password`, {
      currentPassword,
      newPassword
    });
  }

  forceChangePassword(newPassword: string): Observable<void> {
    const user = this.currentUser;
    if (!user) {
      throw new Error('Not signed in.');
    }

    return this.http
      .put<void>(`/api/people/${user.id}/password`, { newPassword })
      .pipe(
        tap(() => {
          this.setCurrentUser({
            ...user,
            mustChangePassword: false
          });
        })
      );
  }

  logout(): void {
    this.currentUser = null;
    localStorage.removeItem(STORAGE_KEY);
    void this.router.navigate(['/login']);
  }

  private setCurrentUser(user: LoginUser): void {
    this.currentUser = {
      ...user,
      mustChangePassword: user.mustChangePassword === true
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.currentUser));
  }

  private readStoredUser(): LoginUser | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    try {
      const user = JSON.parse(raw) as LoginUser;
      return {
        ...user,
        mustChangePassword: user.mustChangePassword === true
      };
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
  }
}
