import type { ReactNode } from "react";

import { AdminHeader } from "./admin-header";
import { AdminSidebar } from "@/components/navigation/admin-sidebar";

type AdminShellProps = {
  children: ReactNode;
  tenantSlug: string;
  tenantName: string;
  email: string;
  roles: string[];
};

export function AdminShell({
  children,
  tenantSlug,
  tenantName,
  email,
  roles,
}: AdminShellProps) {
  return (
    <div className="min-h-screen bg-background">
      <AdminHeader
        tenantName={tenantName}
        email={email}
        role={roles[0] ?? "User"}
      />

      <AdminSidebar tenantSlug={tenantSlug} roles={roles} />

      <main className="min-h-screen pt-16 lg:pl-64">
        <div className="mx-auto max-w-[1600px] px-5 py-8 sm:px-7 lg:px-10">
          {children}
        </div>
      </main>
    </div>
  );
}
