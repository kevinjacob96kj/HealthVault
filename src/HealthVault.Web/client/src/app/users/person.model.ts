export interface Person {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  roles: string[];
}

export interface CreatePersonRequest {
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
}
