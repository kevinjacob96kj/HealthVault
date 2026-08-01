import { Component, Input, OnChanges, OnInit, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../auth/auth.service';
import { AssignedDoctor } from '../find-doctor/find-doctor.model';
import { FindDoctorService } from '../find-doctor/find-doctor.service';
import { PatientCase } from '../patients/patient.model';
import { PatientsService } from '../patients/patients.service';

@Component({
  selector: 'app-case-details',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './case-details.component.html',
  styleUrl: './case-details.component.css'
})
export class CaseDetailsComponent implements OnInit, OnChanges {
  private readonly auth = inject(AuthService);
  private readonly patientsService = inject(PatientsService);
  private readonly findDoctorService = inject(FindDoctorService);

  /** When set, loads that patient's case for the signed-in doctor. */
  @Input() patientId: number | null = null;

  /** Compact layout used when embedded on the patient home page. */
  @Input() embedded = false;

  patient: PatientCase | null = null;
  careTeam: AssignedDoctor[] = [];
  loading = true;
  error: string | null = null;

  get isDoctorView(): boolean {
    return this.patientId != null;
  }

  get title(): string {
    if (!this.patient) {
      return 'Case details';
    }

    return `${this.patient.firstName} ${this.patient.lastName}`.trim();
  }

  ngOnInit(): void {
    this.load();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['patientId'] && !changes['patientId'].firstChange) {
      this.load();
    }
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.patient = null;
    this.careTeam = [];

    if (this.patientId != null) {
      this.findDoctorService.getMyPatientCase(this.patientId).subscribe({
        next: (patient) => {
          this.patient = patient;
          this.loading = false;
        },
        error: (err) => {
          this.error =
            err?.error?.title ??
            err?.error?.detail ??
            'Unable to load this patient case.';
          this.loading = false;
        }
      });
      return;
    }

    if (!this.auth.isPatient) {
      this.loading = false;
      return;
    }

    forkJoin({
      patient: this.patientsService.getMyPatient(),
      careTeam: this.findDoctorService.getMyDoctors().pipe(catchError(() => of([] as AssignedDoctor[])))
    }).subscribe({
      next: ({ patient, careTeam }) => {
        this.patient = patient;
        this.careTeam = careTeam.filter((doctor) => doctor.isActive);
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.title ?? 'Unable to load your case details.';
        this.loading = false;
      }
    });
  }

  formatDate(value: string | null | undefined): string {
    if (!value) {
      return '—';
    }

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
