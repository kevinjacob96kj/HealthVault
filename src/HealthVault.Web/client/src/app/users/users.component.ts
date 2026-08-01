import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../auth/auth.service';
import { DEFAULT_PASSWORD } from '../auth/password.util';
import { HealthcareProvider } from './healthcare-provider.model';
import { HealthcareProvidersService } from './healthcare-providers.service';
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
  private readonly providersService = inject(HealthcareProvidersService);
  readonly auth = inject(AuthService);

  people: Person[] = [];
  providers: HealthcareProvider[] = [];
  availableRoles: string[] = [];
  loading = true;
  error: string | null = null;
  expandedId: number | null = null;
  savingId: number | null = null;
  savingActiveId: number | null = null;
  creating = false;
  creatingProvider = false;
  addUserOpen = false;
  addProviderOpen = false;
  formError: string | null = null;
  providerFormError: string | null = null;
  roleError: string | null = null;
  activeError: string | null = null;
  createSuccess: string | null = null;
  readonly defaultPassword = DEFAULT_PASSWORD;

  draftRoles: Record<number, string[]> = {};

  newPerson = {
    firstName: '',
    lastName: '',
    email: '',
    roles: [] as string[]
  };

  newProvider = {
    providerCode: '',
    name: '',
    providerType: 'Hospital',
    address: '',
    city: '',
    state: '',
    postalCode: '',
    phone: '',
    email: '',
    adminFirstName: '',
    adminLastName: '',
    adminEmail: ''
  };

  get canCreatePerson(): boolean {
    return (
      this.newPerson.firstName.trim().length > 0 &&
      this.newPerson.lastName.trim().length > 0 &&
      this.newPerson.email.trim().length > 0 &&
      this.newPerson.roles.length > 0
    );
  }

  get canCreateProvider(): boolean {
    return (
      this.newProvider.providerCode.trim().length > 0 &&
      this.newProvider.name.trim().length > 0 &&
      this.newProvider.providerType.trim().length > 0 &&
      this.newProvider.address.trim().length > 0 &&
      this.newProvider.city.trim().length > 0 &&
      this.newProvider.state.trim().length > 0 &&
      this.newProvider.postalCode.trim().length > 0 &&
      this.newProvider.phone.trim().length > 0 &&
      this.newProvider.adminFirstName.trim().length > 0 &&
      this.newProvider.adminLastName.trim().length > 0 &&
      this.newProvider.adminEmail.trim().length > 0
    );
  }

  ngOnInit(): void {
    if (this.auth.isCentralAdmin) {
      this.providersService.getProviders().subscribe({
        next: (providers) => {
          this.providers = providers;
          this.loading = false;
        },
        error: (err) => {
          this.error = err?.error?.title ?? 'Unable to load healthcare providers.';
          this.loading = false;
        }
      });
      return;
    }

    forkJoin({
      people: this.peopleService.getPeople(),
      roles: this.peopleService.getAvailableRoles().pipe(catchError(() => of([] as string[])))
    }).subscribe({
      next: ({ people, roles }) => {
        this.people = people;
        this.availableRoles = roles;
        this.syncDraftRoles();
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.error?.title ?? 'Unable to load people.';
        this.loading = false;
      }
    });
  }

  openAddUser(): void {
    this.addUserOpen = true;
    this.formError = null;
    this.createSuccess = null;
  }

  closeAddUser(): void {
    this.addUserOpen = false;
    this.formError = null;
  }

  openAddProvider(): void {
    this.addProviderOpen = true;
    this.providerFormError = null;
    this.createSuccess = null;
  }

  closeAddProvider(): void {
    this.addProviderOpen = false;
    this.providerFormError = null;
  }

  toggleAccordion(id: number): void {
    this.expandedId = this.expandedId === id ? null : id;
    this.roleError = null;
    this.activeError = null;
  }

  isExpanded(id: number): boolean {
    return this.expandedId === id;
  }

  isCurrentUser(person: Person): boolean {
    return (
      !!this.auth.user?.email &&
      person.email.toLowerCase() === this.auth.user.email.toLowerCase()
    );
  }

  isSoleActiveHospitalAdmin(personId: number): boolean {
    const person = this.people.find((item) => item.id === personId);
    if (
      !person?.isActive ||
      !person.roles.some((role) => role.toLowerCase() === 'admin')
    ) {
      return false;
    }

    const otherActiveAdmins = this.people.filter(
      (item) =>
        item.id !== personId &&
        item.isActive &&
        item.roles.some((role) => role.toLowerCase() === 'admin')
    );

    return otherActiveAdmins.length === 0;
  }

  isActiveToggleLocked(person: Person): boolean {
    return person.isActive && (this.isCurrentUser(person) || this.isSoleActiveHospitalAdmin(person.id));
  }

  setActive(person: Person, isActive: boolean): void {
    this.activeError = null;

    if (!isActive && this.isActiveToggleLocked(person)) {
      this.activeError = this.isCurrentUser(person)
        ? 'You cannot deactivate your own account.'
        : 'Each hospital must keep at least one active Admin.';
      return;
    }

    this.savingActiveId = person.id;
    this.peopleService.updateActive(person.id, isActive).subscribe({
      next: (updated) => {
        this.people = this.people.map((p) => (p.id === updated.id ? updated : p));
        this.savingActiveId = null;
      },
      error: (err) => {
        this.activeError =
          err?.error?.errors?.IsActive?.[0] ??
          err?.error?.errors?.isActive?.[0] ??
          err?.error?.title ??
          `Unable to update status for ${person.firstName} ${person.lastName}.`;
        this.savingActiveId = null;
      }
    });
  }

  hasRole(personId: number, role: string): boolean {
    return (this.draftRoles[personId] ?? []).includes(role);
  }

  toggleRole(personId: number, role: string, checked: boolean): void {
    this.roleError = null;

    if (
      !checked &&
      role.toLowerCase() === 'admin' &&
      this.isSoleHospitalAdmin(personId)
    ) {
      this.roleError =
        'Each hospital must keep at least one Admin. Assign Admin to another staff member before removing it.';
      return;
    }

    const current = new Set(this.draftRoles[personId] ?? []);
    if (checked) {
      current.add(role);
    } else {
      current.delete(role);
    }
    this.draftRoles[personId] = [...current];
  }

  isAdminRoleLocked(personId: number, role: string): boolean {
    return (
      role.toLowerCase() === 'admin' &&
      this.isSoleHospitalAdmin(personId) &&
      this.hasRole(personId, role)
    );
  }

  isSoleHospitalAdmin(personId: number): boolean {
    const person = this.people.find((item) => item.id === personId);
    if (!person?.roles.some((role) => role.toLowerCase() === 'admin')) {
      return false;
    }

    const otherAdmins = this.people.filter(
      (item) =>
        item.id !== personId &&
        item.roles.some((role) => role.toLowerCase() === 'admin')
    );

    return otherAdmins.length === 0;
  }

  rolesChanged(person: Person): boolean {
    const draft = [...(this.draftRoles[person.id] ?? [])].sort();
    const original = [...person.roles].sort();
    return draft.length !== original.length || draft.some((role, i) => role !== original[i]);
  }

  saveRoles(person: Person): void {
    this.roleError = null;
    const draft = this.draftRoles[person.id] ?? [];
    const hadAdmin = person.roles.some((role) => role.toLowerCase() === 'admin');
    const keepsAdmin = draft.some((role) => role.toLowerCase() === 'admin');

    if (hadAdmin && !keepsAdmin && this.isSoleHospitalAdmin(person.id)) {
      this.roleError =
        'Each hospital must keep at least one Admin. Assign Admin to another staff member before removing it.';
      this.draftRoles[person.id] = [...person.roles];
      return;
    }

    this.savingId = person.id;

    this.peopleService.updateRoles(person.id, draft).subscribe({
      next: (updated) => {
        this.people = this.people.map((p) => (p.id === updated.id ? updated : p));
        this.draftRoles[updated.id] = [...updated.roles];
        this.savingId = null;

        if (this.auth.user?.id === updated.id) {
          this.auth.applySessionRoles(updated.roles);
        }
      },
      error: (err) => {
        this.roleError =
          err?.error?.errors?.Roles?.[0] ??
          err?.error?.errors?.roles?.[0] ??
          err?.error?.title ??
          `Unable to update roles for ${person.firstName} ${person.lastName}.`;
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

    if (
      !this.newPerson.firstName.trim() ||
      !this.newPerson.lastName.trim() ||
      !this.newPerson.email.trim()
    ) {
      this.formError = 'First name, last name, and email are required.';
      return;
    }

    if (this.newPerson.roles.length === 0) {
      this.formError = 'Select at least one role.';
      return;
    }

    this.creating = true;
    this.peopleService
      .createPerson({
        firstName: this.newPerson.firstName.trim(),
        lastName: this.newPerson.lastName.trim(),
        email: this.newPerson.email.trim(),
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
            roles: []
          };
          this.createSuccess = `Added ${person.firstName} ${person.lastName}. Default password is "${DEFAULT_PASSWORD}".`;
          this.expandedId = person.id;
          this.creating = false;
          this.addUserOpen = false;
        },
        error: (err) => {
          this.formError =
            err?.error?.title ??
            Object.values(err?.error?.errors ?? {}).flat()[0]?.toString() ??
            'Unable to create the new user.';
          this.creating = false;
        }
      });
  }

  createProvider(): void {
    this.providerFormError = null;
    this.createSuccess = null;

    if (
      !this.newProvider.providerCode.trim() ||
      !this.newProvider.name.trim() ||
      !this.newProvider.address.trim() ||
      !this.newProvider.city.trim() ||
      !this.newProvider.state.trim() ||
      !this.newProvider.postalCode.trim() ||
      !this.newProvider.phone.trim() ||
      !this.newProvider.adminFirstName.trim() ||
      !this.newProvider.adminLastName.trim() ||
      !this.newProvider.adminEmail.trim()
    ) {
      this.providerFormError =
        'Provider details and at least one hospital admin (name and email) are required.';
      return;
    }

    this.creatingProvider = true;
    this.providersService
      .createProvider({
        providerCode: this.newProvider.providerCode.trim(),
        name: this.newProvider.name.trim(),
        providerType: this.newProvider.providerType,
        address: this.newProvider.address.trim(),
        city: this.newProvider.city.trim(),
        state: this.newProvider.state.trim(),
        postalCode: this.newProvider.postalCode.trim(),
        phone: this.newProvider.phone.trim(),
        email: this.newProvider.email.trim() || null,
        adminFirstName: this.newProvider.adminFirstName.trim(),
        adminLastName: this.newProvider.adminLastName.trim(),
        adminEmail: this.newProvider.adminEmail.trim()
      })
      .subscribe({
        next: (provider) => {
          this.providers = [...this.providers, provider].sort((a, b) =>
            a.name.localeCompare(b.name)
          );
          this.createSuccess = `Created ${provider.name} with hospital admin ${this.newProvider.adminFirstName} ${this.newProvider.adminLastName}. Default password is "${DEFAULT_PASSWORD}".`;
          this.newProvider = {
            providerCode: '',
            name: '',
            providerType: 'Hospital',
            address: '',
            city: '',
            state: '',
            postalCode: '',
            phone: '',
            email: '',
            adminFirstName: '',
            adminLastName: '',
            adminEmail: ''
          };
          this.creatingProvider = false;
          this.addProviderOpen = false;
        },
        error: (err) => {
          this.providerFormError =
            err?.error?.title ??
            Object.values(err?.error?.errors ?? {}).flat()[0]?.toString() ??
            'Unable to create the healthcare provider.';
          this.creatingProvider = false;
        }
      });
  }

  private syncDraftRoles(): void {
    this.draftRoles = Object.fromEntries(this.people.map((person) => [person.id, [...person.roles]]));
  }
}
