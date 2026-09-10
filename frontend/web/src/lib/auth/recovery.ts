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

export function directResetPayload(data: FormData):
  { error: string } | { body: { email: string; tenantSlug: string; recoveryCode: string; newPassword: string } } {
  const newPassword = String(data.get("newPassword") ?? "");
  const recoveryCode = String(data.get("recoveryCode") ?? "");
  if (!recoveryCode || recoveryCode.length > 256) return { error: "Enter your recovery code." };
  if (!validPassword(newPassword)) return { error: PASSWORD_DESCRIPTION };
  if (newPassword !== data.get("confirmPassword")) return { error: "Passwords do not match." };
  return { body: {
    email: String(data.get("email") ?? "").trim(),
    tenantSlug: String(data.get("tenantSlug") ?? "").trim().toLowerCase(),
    recoveryCode, newPassword,
  } };
}
