"use client";

import {
  ChevronDown,
  LogOut,
  Menu,
  X,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";

import { TenantBrand } from "@/components/tenant/tenant-brand";
import { SidebarNavigation } from "@/components/navigation/admin-sidebar";
import { Sheet, SheetClose, SheetContent, SheetTitle, SheetTrigger } from "@/components/ui/sheet";

type AdminHeaderProps = {
  tenantSlug: string;
  tenantName: string;
  logoDataUrl?: string | null;
  secondaryColor: string;
  email: string;
  role: string;
  roles: string[];
};

export function AdminHeader({
  tenantSlug,
  tenantName,
  logoDataUrl,
  secondaryColor,
  email,
  role,
  roles,
}: AdminHeaderProps) {
  const router = useRouter();

  const [loggingOut, setLoggingOut] =
    useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  async function logout() {
    setLoggingOut(true);

    try {
      await fetch("/api/auth/logout", {
        method: "POST",
      });
    } finally {
      const portal = role.toLowerCase().includes("teacher") ? "staff" : "admin";
      router.replace(`/schools/${encodeURIComponent(tenantSlug)}/${portal}/login`);
      router.refresh();
    }
  }

  const initial =
    email.charAt(0).toUpperCase();

  return (
    <header className="fixed inset-x-0 top-0 z-40 flex h-16 items-center border-b border-white/10 px-4 text-white lg:px-6" style={{ backgroundColor: secondaryColor }}>
      <div className="flex w-full items-center justify-between">
        <div className="flex min-w-0 items-center gap-2 sm:gap-4">
          <Sheet open={mobileMenuOpen} onOpenChange={setMobileMenuOpen}>
            <SheetTrigger
              type="button"
              aria-label="Open navigation"
              aria-controls="school-mobile-navigation"
              className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md text-white transition hover:bg-white/10 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white lg:hidden"
            >
              <Menu aria-hidden="true" className="h-6 w-6" />
            </SheetTrigger>
            <SheetContent
              side="left"
              showCloseButton={false}
              className="w-[min(20rem,88vw)] gap-0 bg-sidebar text-sidebar-foreground lg:hidden"
            >
              <div className="flex shrink-0 items-center justify-between border-b border-sidebar-border px-5 py-4">
                <SheetTitle className="font-semibold text-sidebar-foreground">Navigation</SheetTitle>
                <SheetClose
                  aria-label="Close navigation"
                  className="flex h-10 w-10 items-center justify-center rounded-md hover:bg-sidebar-accent focus-visible:outline-2 focus-visible:outline-tenant-primary"
                >
                  <X className="h-5 w-5" aria-hidden="true" />
                </SheetClose>
              </div>
              <div id="school-mobile-navigation" className="min-h-0 flex-1 overflow-y-auto overscroll-contain pb-6">
                <SidebarNavigation
                  tenantSlug={tenantSlug}
                  roles={roles}
                  onNavigate={() => setMobileMenuOpen(false)}
                />
              </div>
            </SheetContent>
          </Sheet>
          <div className="min-w-0">
            <TenantBrand name={tenantName} logoDataUrl={logoDataUrl} />
          </div>
        </div>

        <div className="flex items-center gap-2 sm:gap-4">
          <div className="mx-1 h-8 w-px bg-white/15" />

          <div className="flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-full bg-tenant-primary font-bold text-black">
              {initial}
            </div>

            <div className="hidden max-w-48 text-left md:block">
              <div className="truncate text-sm font-semibold">
                {email}
              </div>

              <div className="text-xs text-white/60">
                {role}
              </div>
            </div>

            <ChevronDown className="hidden h-4 w-4 text-white/60 md:block" />

            <button
              type="button"
              onClick={logout}
              disabled={loggingOut}
              className="ml-1 rounded-md p-2 text-white/60 transition hover:bg-white/10 hover:text-white disabled:opacity-50"
              title="Sign out"
              aria-label="Sign out"
            >
              <LogOut className="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>
    </header>
  );
}
