import type { Metadata } from "next";
import { redirect } from "next/navigation";

export const dynamic = "force-dynamic";

export const metadata: Metadata = { robots: { index: false, follow: false }, referrer: "no-referrer" };

export default function ResetPasswordPage() {
  // TEMPORARY: email/token recovery implementation remains in the repository.
  // Do not carry a legacy token into the direct-reset URL.
  redirect("/forgot-password");
}
