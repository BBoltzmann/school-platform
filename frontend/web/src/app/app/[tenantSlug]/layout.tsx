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
      tenantName={session.tenantName}
      email={session.email}
      roles={session.roles}
    >
      {children}
    </AdminShell>
  );
}
