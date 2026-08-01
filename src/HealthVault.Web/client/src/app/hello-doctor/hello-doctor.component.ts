import { Component, inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-hello-doctor',
  standalone: true,
  templateUrl: './hello-doctor.component.html',
  styleUrl: './hello-doctor.component.css'
})
export class HelloDoctorComponent {
  readonly auth = inject(AuthService);

  get message(): string {
    const user = this.auth.user;
    if (!user) {
      return 'Hello';
    }

    const isDoctor = (user.roles ?? []).some(
      (role) => role.toLowerCase() === 'doctor'
    );
    const name = `${user.firstName} ${user.lastName}`.trim();
    return isDoctor ? `Hello Dr. ${name}` : `Hello ${name}`;
  }
}
