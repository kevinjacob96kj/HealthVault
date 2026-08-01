import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AssignedDoctor,
  DoctorPatient,
  DoctorSearch,
  ProviderSearch
} from './find-doctor.model';

@Injectable({ providedIn: 'root' })
export class FindDoctorService {
  private readonly http = inject(HttpClient);

  searchProviders(q: string): Observable<ProviderSearch[]> {
    return this.http.get<ProviderSearch[]>('/api/providers', {
      params: q.trim() ? { q: q.trim() } : {}
    });
  }

  getProviderDoctors(providerId: number): Observable<DoctorSearch[]> {
    return this.http.get<DoctorSearch[]>(`/api/providers/${providerId}/doctors`);
  }

  getMyDoctors(): Observable<AssignedDoctor[]> {
    return this.http.get<AssignedDoctor[]>('/api/patients/me/doctors');
  }

  assignDoctor(healthcareStaffId: number, notes: string): Observable<AssignedDoctor> {
    return this.http.post<AssignedDoctor>('/api/patients/me/doctors', {
      healthcareStaffId,
      consentAccepted: true,
      notes: notes.trim() || null
    });
  }

  unassignDoctor(healthcareStaffId: number): Observable<void> {
    return this.http.delete<void>(`/api/patients/me/doctors/${healthcareStaffId}`);
  }

  getMyPatients(): Observable<DoctorPatient[]> {
    return this.http.get<DoctorPatient[]>('/api/doctors/me/patients');
  }

  getMyPatientCase(patientId: number): Observable<DoctorPatient> {
    return this.http.get<DoctorPatient>(`/api/doctors/me/patients/${patientId}`);
  }
}
