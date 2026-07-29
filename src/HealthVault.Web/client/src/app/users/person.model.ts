export interface Person {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  status: string;
  roles: string[];
}

export interface CreatePersonRequest {
  firstName: string;
  lastName: string;
  email: string;
  status?: string;
  roles: string[];
}
