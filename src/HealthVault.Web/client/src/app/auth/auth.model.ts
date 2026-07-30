export interface LoginUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  mustChangePassword: boolean;
  roles: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}
