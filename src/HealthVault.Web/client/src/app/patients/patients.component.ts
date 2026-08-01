import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FindDoctorService } from '../find-doctor/find-doctor.service';
import { DoctorPatient } from '../find-doctor/find-doctor.model';

@Component({
  selector: 'app-patients',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './patients.component.html',
  styleUrl: './patients.component.css'
})
export class PatientsComponent implements OnInit {
  private readonly service = inject(FindDoctorService);
  private readonly router = inject(Router);

  patients: DoctorPatient[] = [];
  loading = true;
  error: string | null = null;

  ngOnInit(): void {
    this.service.getMyPatients().subscribe({
      next: (patients) => {
        this.patients = patients;
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.title ?? 'Unable to load your patients.';
        this.loading = false;
      }
    });
  }

  openCase(patient: DoctorPatient): void {
    void this.router.navigate(['/patients', patient.id]);
  }

  formatDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }
}
