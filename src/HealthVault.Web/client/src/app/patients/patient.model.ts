export interface Patient {
  id: number;
  abhaId: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  email: string;
  mobileNumber: string;
  isActive: boolean;
}

export interface PatientCase {
  id: number;
  abhaId: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  email: string;
  mobileNumber: string;
  isActive: boolean;
  assignedAt?: string | null;
  notes?: string | null;
}
