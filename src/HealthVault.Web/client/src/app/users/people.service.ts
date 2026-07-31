import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreatePersonRequest, Person } from './person.model';

@Injectable({ providedIn: 'root' })
export class PeopleService {
  private readonly http = inject(HttpClient);

  getPeople(): Observable<Person[]> {
    return this.http.get<Person[]>('/api/people');
  }

  getAvailableRoles(): Observable<string[]> {
    return this.http.get<string[]>('/api/people/roles');
  }

  createPerson(request: CreatePersonRequest): Observable<Person> {
    return this.http.post<Person>('/api/people', request);
  }

  updateRoles(id: number, roles: string[]): Observable<Person> {
    return this.http.put<Person>(`/api/people/${id}/roles`, { roles });
  }

  updateActive(id: number, isActive: boolean): Observable<Person> {
    return this.http.put<Person>(`/api/people/${id}/active`, { isActive });
  }
}
