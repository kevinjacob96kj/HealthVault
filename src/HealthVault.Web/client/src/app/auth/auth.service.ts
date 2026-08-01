import { Injectable, Injector, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, catchError, of, tap } from 'rxjs';
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
import { InactivityService } from './inactivity.service';

const STORAGE_KEY = 'healthvault.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);
  private readonly userSubject = new BehaviorSubject<LoginUser | null>(this.readStoredUser());

  readonly user$ = this.userSubject.asObservable();

  get user(): LoginUser | null {
    return this.userSubject.value;
  }

  private get currentUser(): LoginUser | null {
    return this.userSubject.value;
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

  /** Reloads roles and profile fields from the server into the local session. */
  refreshSession(): Observable<LoginUser | null> {
    if (!this.currentUser?.email) {
      return of(null);
    }

    return this.http.get<LoginUser>('/api/auth/me').pipe(
      tap((user) => this.setCurrentUser(user)),
      catchError(() => {
        this.logout();
        return of(null);
      })
    );
  }

  /** Updates the signed-in user's roles in the local session immediately. */
  applySessionRoles(roles: string[]): void {
    const user = this.currentUser;
    if (!user) {
      return;
    }

    this.setCurrentUser({
      ...user,
      roles: [...roles]
    });
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
    this.injector.get(InactivityService).stop();
    this.userSubject.next(null);
    localStorage.removeItem(STORAGE_KEY);
    void this.router.navigate(['/login']);
  }

  private setCurrentUser(user: LoginUser): void {
    const nextUser = {
      ...user,
      mustChangePassword: user.mustChangePassword === true
    };
    this.userSubject.next(nextUser);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(nextUser));
    this.injector.get(InactivityService).start();
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
