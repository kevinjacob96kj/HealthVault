export interface LoginUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  mustChangePassword: boolean;
  roles: string[];
}

export interface StaffLoginRequest {
  email: string;
  password: string;
  mode?: 'staff' | 'patient';
}

export interface PatientLoginRequest {
  abhaId: string;
  email: string;
  otp: string;
}

export interface PatientOtpRequest {
  abhaId: string;
  email: string;
}

export interface PatientOtpSent {
  message: string;
  destination: string;
  expiresInSeconds: number;
  otp?: string | null;
}

export interface PatientSignupVerifyRequest {
  abhaId: string;
  aadhaarNumber: string;
  dateOfBirth: string;
}

export interface PatientSignupVerified {
  signupToken: string;
  message: string;
}

export interface PatientSignupCompleteRequest {
  signupToken: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  gender?: string;
  mobileNumber?: string;
}

export interface PatientGoogleSignupRequest {
  signupToken: string;
  idToken?: string;
  demoEmail?: string;
  demoFirstName?: string;
  demoLastName?: string;
  gender?: string;
  mobileNumber?: string;
}

export interface GoogleAuthConfig {
  clientId: string | null;
  allowDemoSignIn: boolean;
}

/** @deprecated Prefer StaffLoginRequest */
export type LoginRequest = StaffLoginRequest;
