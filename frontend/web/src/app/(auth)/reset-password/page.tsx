import type { Metadata } from "next";
import { AuthCard } from "@/components/auth/auth-card";
import { RecoveryForm } from "@/components/auth/recovery-form";

export const dynamic = "force-dynamic";

export const metadata: Metadata = { robots: { index: false, follow: false }, referrer: "no-referrer" };

export default function ResetPasswordPage() {
  return <AuthCard title="Reset password" description="Choose a new password for your account. Reset links expire after 30 minutes.">
    <RecoveryForm mode="reset" />
  </AuthCard>;
}
