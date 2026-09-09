import { AuthCard } from "@/components/auth/auth-card";
import { RecoveryForm } from "@/components/auth/recovery-form";

export default async function ForgotPasswordPage({ searchParams }: { searchParams: Promise<{ tenantSlug?: string }> }) {
  const { tenantSlug } = await searchParams;
  return <AuthCard title="Forgot password?" description="Enter your email and school slug to request a password reset link.">
    <RecoveryForm mode="forgot" initialTenantSlug={typeof tenantSlug === "string" ? tenantSlug : "antioch-college"} />
  </AuthCard>;
}
