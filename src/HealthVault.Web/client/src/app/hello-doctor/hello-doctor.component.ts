import { Component } from '@angular/core';

@Component({
  selector: 'app-hello-doctor',
  standalone: true,
  templateUrl: './hello-doctor.component.html',
  styleUrl: './hello-doctor.component.css'
})
export class HelloDoctorComponent {
  readonly message = 'Hello Doctor';
}
