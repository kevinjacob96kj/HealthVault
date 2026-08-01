import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CreateHealthcareProviderRequest,
  HealthcareProvider
} from './healthcare-provider.model';

@Injectable({ providedIn: 'root' })
export class HealthcareProvidersService {
  private readonly http = inject(HttpClient);

  getProviders(): Observable<HealthcareProvider[]> {
    return this.http.get<HealthcareProvider[]>('/api/healthcare-providers');
  }

  createProvider(request: CreateHealthcareProviderRequest): Observable<HealthcareProvider> {
    return this.http.post<HealthcareProvider>('/api/healthcare-providers', request);
  }
}
