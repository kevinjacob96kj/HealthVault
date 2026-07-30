export const DEFAULT_PASSWORD = 'Password@1';

export const PASSWORD_RULES_MESSAGE =
  'Password must be at least 8 characters and include 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.';

const PASSWORD_COMPLEXITY = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$/;

export function isStrongPassword(password: string): boolean {
  return password.length >= 8 && PASSWORD_COMPLEXITY.test(password);
}
