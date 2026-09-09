export const RECOVERY_MESSAGE = "If an account exists, a password reset link has been sent.";
export const PASSWORD_DESCRIPTION = "Use 12–128 characters, including a letter and a number.";
export const SIGNUP_MESSAGE = "If these details are eligible, your school has been created. Sign in with your school slug and password. If you cannot sign in, contact support or your school administrator.";

export function validPassword(password: string): boolean {
  return password.length >= 12 && password.length <= 128 && /[a-zA-Z]/.test(password) && /[0-9]/.test(password);
}

export function loginDestination(tenantSlug: string, reset = false): string {
  const query = new URLSearchParams({ tenantSlug });
  if (reset) query.set("reset", "success");
  return `/login?${query}`;
}
