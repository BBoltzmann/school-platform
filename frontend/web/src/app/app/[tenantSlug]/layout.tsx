import type { ReactNode } from "react";
import { redirect } from "next/navigation";

import { AdminShell } from "@/components/layout/admin-shell";
import { getSessionContext } from "@/lib/auth/session";

type TenantLayoutProps = {
  children: ReactNode;
  params: Promise<{
    tenantSlug: string;
  }>;
};

function tenantDisplayName(slug: string) {
  if (slug === "antioch-college") {
    return "Antioch Royal College";
  }

  return slug
    .split("-")
    .map(
      (word) =>
        word.charAt(0).toUpperCase() +
        word.slice(1)
    )
    .join(" ");
}

export default async function TenantLayout({
  children,
  params,
}: TenantLayoutProps) {
  const { tenantSlug } = await params;

  const session = await getSessionContext();

  if (!session) {
    redirect("/login");
  }

  if (session.tenantSlug !== tenantSlug) {
    redirect(
      `/app/${session.tenantSlug}/dashboard`
    );
  }

  return (
    <AdminShell
      tenantSlug={session.tenantSlug}
      tenantName={tenantDisplayName(
        session.tenantSlug
      )}
      email={session.email}
      roles={session.roles}
    >
      {children}
    </AdminShell>
  );
}
