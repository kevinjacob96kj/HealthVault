import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { PeopleService } from './people.service';
import { Person } from './person.model';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class UsersComponent implements OnInit {
  private readonly peopleService = inject(PeopleService);

  people: Person[] = [];
  availableRoles: string[] = [];
  loading = true;
  error: string | null = null;
  expandedId: number | null = null;
  savingId: number | null = null;
  creating = false;
  formError: string | null = null;
  roleError: string | null = null;
  createSuccess: string | null = null;

  draftRoles: Record<number, string[]> = {};

  newPerson = {
    firstName: '',
    lastName: '',
    email: '',
    status: 'Active',
    roles: [] as string[]
  };

  ngOnInit(): void {
    forkJoin({
      people: this.peopleService.getPeople(),
      roles: this.peopleService.getAvailableRoles()
    }).subscribe({
      next: ({ people, roles }) => {
        this.people = people;
        this.availableRoles = roles;
        this.syncDraftRoles();
        this.loading = false;
      },
      error: () => {
        this.error = 'Unable to load people.';
        this.loading = false;
      }
    });
  }

  toggleAccordion(id: number): void {
    this.expandedId = this.expandedId === id ? null : id;
    this.roleError = null;
  }

  isExpanded(id: number): boolean {
    return this.expandedId === id;
  }

  hasRole(personId: number, role: string): boolean {
    return (this.draftRoles[personId] ?? []).includes(role);
  }

  toggleRole(personId: number, role: string, checked: boolean): void {
    const current = new Set(this.draftRoles[personId] ?? []);
    if (checked) {
      current.add(role);
    } else {
      current.delete(role);
    }
    this.draftRoles[personId] = [...current];
  }

  rolesChanged(person: Person): boolean {
    const draft = [...(this.draftRoles[person.id] ?? [])].sort();
    const original = [...person.roles].sort();
    return draft.length !== original.length || draft.some((role, i) => role !== original[i]);
  }

  saveRoles(person: Person): void {
    this.savingId = person.id;
    this.roleError = null;

    this.peopleService.updateRoles(person.id, this.draftRoles[person.id] ?? []).subscribe({
      next: (updated) => {
        this.people = this.people.map((p) => (p.id === updated.id ? updated : p));
        this.draftRoles[updated.id] = [...updated.roles];
        this.savingId = null;
      },
      error: () => {
        this.roleError = `Unable to update roles for ${person.firstName} ${person.lastName}.`;
        this.savingId = null;
      }
    });
  }

  toggleNewRole(role: string, checked: boolean): void {
    const current = new Set(this.newPerson.roles);
    if (checked) {
      current.add(role);
    } else {
      current.delete(role);
    }
    this.newPerson.roles = [...current];
  }

  createPerson(): void {
    this.formError = null;
    this.createSuccess = null;

    if (!this.newPerson.firstName.trim() || !this.newPerson.lastName.trim() || !this.newPerson.email.trim()) {
      this.formError = 'First name, last name, and email are required.';
      return;
    }

    this.creating = true;
    this.peopleService
      .createPerson({
        firstName: this.newPerson.firstName.trim(),
        lastName: this.newPerson.lastName.trim(),
        email: this.newPerson.email.trim(),
        status: this.newPerson.status,
        roles: this.newPerson.roles
      })
      .subscribe({
        next: (person) => {
          this.people = [...this.people, person].sort((a, b) => a.id - b.id);
          this.draftRoles[person.id] = [...person.roles];
          this.newPerson = {
            firstName: '',
            lastName: '',
            email: '',
            status: 'Active',
            roles: []
          };
          this.createSuccess = `Added ${person.firstName} ${person.lastName}.`;
          this.expandedId = person.id;
          this.creating = false;
        },
        error: () => {
          this.formError = 'Unable to create the new user.';
          this.creating = false;
        }
      });
  }

  private syncDraftRoles(): void {
    this.draftRoles = Object.fromEntries(this.people.map((person) => [person.id, [...person.roles]]));
  }
}
