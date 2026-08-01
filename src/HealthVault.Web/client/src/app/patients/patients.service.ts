import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Patient } from './patient.model';
import { PatientObservation } from '../data-trends/patient-observation.model';

@Injectable({ providedIn: 'root' })
export class PatientsService {
  private readonly http = inject(HttpClient);

  getPatients(): Observable<Patient[]> {
    return this.http.get<Patient[]>('/api/patients');
  }

  getMyPatient(): Observable<Patient> {
    return this.http.get<Patient>('/api/patients/me');
  }

  getMyPatientData(): Observable<PatientObservation[]> {
    return this.http.get<PatientObservation[]>('/api/patients/me/data');
  }
}
