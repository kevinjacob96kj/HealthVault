import { Routes } from '@angular/router';
import { HelloDoctorComponent } from './hello-doctor/hello-doctor.component';
import { UsersComponent } from './users/users.component';
import { PatientsComponent } from './patients/patients.component';
import { CaseDetailsPageComponent } from './case-details/case-details-page.component';
import { DataTrendsComponent } from './data-trends/data-trends.component';
import { FindDoctorComponent } from './find-doctor/find-doctor.component';
import { LoginComponent } from './auth/login.component';
import { PatientSignupComponent } from './auth/patient-signup.component';
import { ProfileComponent } from './auth/profile.component';
import { ChangePasswordComponent } from './auth/change-password.component';
import {
  adminGuard,
  authGuard,
  doctorGuard,
  mustChangePasswordGuard,
  passwordChangeRedirectGuard,
  patientGuard
} from './auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: HelloDoctorComponent,
    canActivate: [authGuard]
  },
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [passwordChangeRedirectGuard]
  },
  {
    path: 'signup',
    component: PatientSignupComponent,
    canActivate: [passwordChangeRedirectGuard]
  },
  {
    path: 'change-password',
    component: ChangePasswordComponent,
    canActivate: [mustChangePasswordGuard]
  },
  { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
  { path: 'admin', component: UsersComponent, canActivate: [adminGuard] },
  { path: 'patients', component: PatientsComponent, canActivate: [doctorGuard] },
  {
    path: 'patients/:patientId',
    component: CaseDetailsPageComponent,
    canActivate: [doctorGuard]
  },
  {
    path: 'data-trends',
    component: DataTrendsComponent,
    canActivate: [patientGuard]
  },
  {
    path: 'find-doctor',
    component: FindDoctorComponent,
    canActivate: [patientGuard]
  },
  { path: 'my-patients', redirectTo: 'patients' },
  { path: '**', redirectTo: 'login' }
];
