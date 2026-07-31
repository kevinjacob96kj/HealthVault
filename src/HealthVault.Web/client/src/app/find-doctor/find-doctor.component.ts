import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FindDoctorService } from './find-doctor.service';
import { AssignedDoctor, DoctorSearch, ProviderSearch } from './find-doctor.model';

@Component({
  selector: 'app-find-doctor',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './find-doctor.component.html',
  styleUrl: './find-doctor.component.css'
})
export class FindDoctorComponent implements OnInit {
  private readonly service = inject(FindDoctorService);

  searchTerm = '';
  providers: ProviderSearch[] = [];
  selectedProvider: ProviderSearch | null = null;
  doctors: DoctorSearch[] = [];
  assignments: AssignedDoctor[] = [];
  loadingProviders = false;
  loadingDoctors = false;
  loadingAssignments = true;
  assigningId: number | null = null;
  unassigningId: number | null = null;
  error: string | null = null;
  success: string | null = null;

  notes = '';
  consentChecked = false;
  confirmDoctor: DoctorSearch | null = null;
  confirmUnassign: AssignedDoctor | null = null;

  ngOnInit(): void {
    this.reloadAssignments();
    this.searchProviders();
  }

  searchProviders(): void {
    this.loadingProviders = true;
    this.error = null;
    this.service.searchProviders(this.searchTerm).subscribe({
      next: (providers) => {
        this.providers = providers;
        this.loadingProviders = false;
      },
      error: (err) => {
        this.error = err?.error?.title ?? 'Unable to search institutions.';
        this.loadingProviders = false;
      }
    });
  }

  selectProvider(provider: ProviderSearch): void {
    this.selectedProvider = provider;
    this.doctors = [];
    this.loadingDoctors = true;
    this.error = null;
    this.service.getProviderDoctors(provider.id).subscribe({
      next: (doctors) => {
        this.doctors = doctors;
        this.loadingDoctors = false;
      },
      error: (err) => {
        this.error = err?.error?.title ?? 'Unable to load doctors.';
        this.loadingDoctors = false;
      }
    });
  }

  openAssign(doctor: DoctorSearch): void {
    this.confirmDoctor = doctor;
    this.confirmUnassign = null;
    this.consentChecked = false;
    this.notes = '';
    this.error = null;
    this.success = null;
  }

  openUnassign(assignment: AssignedDoctor): void {
    this.confirmUnassign = assignment;
    this.confirmDoctor = null;
    this.error = null;
    this.success = null;
  }

  closeDialog(): void {
    this.confirmDoctor = null;
    this.confirmUnassign = null;
    this.consentChecked = false;
    this.notes = '';
  }

  declineAssign(): void {
    this.closeDialog();
  }

  confirmAssign(): void {
    if (!this.confirmDoctor) {
      return;
    }

    if (!this.consentChecked) {
      this.error = 'Check the box to accept sharing your data with this doctor.';
      return;
    }

    this.assigningId = this.confirmDoctor.id;
    this.error = null;
    this.service.assignDoctor(this.confirmDoctor.id, this.notes).subscribe({
      next: (assigned) => {
        this.success = `Added Dr. ${assigned.firstName} ${assigned.lastName}.`;
        this.assigningId = null;
        this.closeDialog();
        this.reloadAssignments();
        if (this.selectedProvider) {
          this.selectProvider(this.selectedProvider);
        }
      },
      error: (err) => {
        this.error =
          err?.error?.errors?.ConsentAccepted?.[0] ??
          err?.error?.errors?.consentAccepted?.[0] ??
          err?.error?.title ??
          Object.values(err?.error?.errors ?? {}).flat()[0]?.toString() ??
          'Unable to assign doctor.';
        this.assigningId = null;
      }
    });
  }

  confirmRemove(): void {
    if (!this.confirmUnassign) {
      return;
    }

    this.unassigningId = this.confirmUnassign.healthcareStaffId;
    this.error = null;
    this.service.unassignDoctor(this.confirmUnassign.healthcareStaffId).subscribe({
      next: () => {
        this.success = `Removed Dr. ${this.confirmUnassign?.firstName} ${this.confirmUnassign?.lastName}.`;
        this.unassigningId = null;
        this.closeDialog();
        this.reloadAssignments();
        if (this.selectedProvider) {
          this.selectProvider(this.selectedProvider);
        }
      },
      error: (err) => {
        this.error = err?.error?.title ?? 'Unable to remove doctor.';
        this.unassigningId = null;
      }
    });
  }

  formatDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  private reloadAssignments(): void {
    this.loadingAssignments = true;
    this.service.getMyDoctors().subscribe({
      next: (assignments) => {
        this.assignments = assignments;
        this.loadingAssignments = false;
      },
      error: (err) => {
        this.error = err?.error?.title ?? 'Unable to load your doctors.';
        this.loadingAssignments = false;
      }
    });
  }
}
