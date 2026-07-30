import { Routes } from '@angular/router';
import { HelloDoctorComponent } from './hello-doctor/hello-doctor.component';
import { UsersComponent } from './users/users.component';
import { LoginComponent } from './auth/login.component';
import { ProfileComponent } from './auth/profile.component';
import { ChangePasswordComponent } from './auth/change-password.component';
import {
  adminGuard,
  authGuard,
  mustChangePasswordGuard,
  passwordChangeRedirectGuard
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
    path: 'change-password',
    component: ChangePasswordComponent,
    canActivate: [mustChangePasswordGuard]
  },
  { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
  { path: 'admin', component: UsersComponent, canActivate: [adminGuard] },
  { path: '**', redirectTo: 'login' }
];
