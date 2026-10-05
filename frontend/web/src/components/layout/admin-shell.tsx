import type { ReactNode } from "react";
import type { CSSProperties } from "react";

import { AdminHeader } from "./admin-header";
import { AdminSidebar } from "@/components/navigation/admin-sidebar";

type AdminShellProps = {
  children: ReactNode;
  tenantSlug: string;
  tenantName: string;
  email: string;
  roles: string[];
  branding: {
    motto?: string | null;
    logoDataUrl?: string | null;
    iconDataUrl?: string | null;
    primaryColor: string;
    secondaryColor: string;
    accentColor: string;
  };
};

export function AdminShell({
  children,
  tenantSlug,
  tenantName,
  email,
  roles,
  branding,
}: AdminShellProps) {
  return (
    <div className="min-h-screen bg-background" style={{ "--tenant-primary": branding.primaryColor, "--tenant-secondary": branding.secondaryColor, "--accent": branding.accentColor, "--primary": branding.primaryColor, "--sidebar": branding.secondaryColor, "--sidebar-primary": branding.primaryColor, "--sidebar-ring": branding.primaryColor } as CSSProperties}>
      <AdminHeader
        tenantName={tenantName}
        email={email}
        role={roles[0] ?? "User"}
        logoDataUrl={branding.logoDataUrl}
        secondaryColor={branding.secondaryColor}
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
