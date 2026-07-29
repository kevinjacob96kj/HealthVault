import { Routes } from '@angular/router';
import { HelloDoctorComponent } from './hello-doctor/hello-doctor.component';
import { UsersComponent } from './users/users.component';

export const routes: Routes = [
  { path: '', component: HelloDoctorComponent },
  { path: 'admin', component: UsersComponent },
  { path: '**', redirectTo: '' }
];
