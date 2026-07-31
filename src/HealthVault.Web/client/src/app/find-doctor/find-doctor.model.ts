export interface ProviderSearch {
  id: number;
  providerCode: string;
  name: string;
  providerType: string;
  city: string;
  state: string;
}

export interface DoctorSearch {
  id: number;
  staffCode: string;
  firstName: string;
  lastName: string;
  email: string;
  specialty: string | null;
  isAssigned: boolean;
}

export interface AssignedDoctor {
  assignmentId: number;
  healthcareStaffId: number;
  firstName: string;
  lastName: string;
  email: string;
  specialty: string | null;
  providerName: string;
  isActive: boolean;
  assignedAt: string;
  unassignedAt: string | null;
  notes: string | null;
}

export interface DoctorPatient {
  id: number;
  abhaId: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  email: string;
  mobileNumber: string;
  assignedAt: string;
  notes: string | null;
}
