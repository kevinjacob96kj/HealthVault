export interface HealthcareProvider {
  id: number;
  providerCode: string;
  name: string;
  providerType: string;
  address: string;
  city: string;
  state: string;
  postalCode: string;
  phone: string;
  email: string | null;
  isActive: boolean;
  adminCount: number;
}

export interface CreateHealthcareProviderRequest {
  providerCode: string;
  name: string;
  providerType: string;
  address: string;
  city: string;
  state: string;
  postalCode: string;
  phone: string;
  email?: string | null;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
}
