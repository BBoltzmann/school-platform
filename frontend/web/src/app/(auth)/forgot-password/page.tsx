import { AuthCard } from "@/components/auth/auth-card";
import { DirectRecoveryForm } from "@/components/auth/direct-recovery-form";

export default async function ForgotPasswordPage({ searchParams }: { searchParams: Promise<{ tenantSlug?: string }> }) {
  const { tenantSlug } = await searchParams;
  return <AuthCard title="Forgot password?" description="Enter your account details, recovery code, and a new password.">
    <DirectRecoveryForm initialTenantSlug={typeof tenantSlug === "string" ? tenantSlug : "antioch-college"} />
  </AuthCard>;
}
