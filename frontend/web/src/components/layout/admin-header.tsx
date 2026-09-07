"use client";

import {
  Bell,
  ChevronDown,
  LogOut,
  Mail,
  Menu,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { useState } from "react";

import { TenantBrand } from "@/components/tenant/tenant-brand";

type AdminHeaderProps = {
  tenantName: string;
  email: string;
  role: string;
};

export function AdminHeader({
  tenantName,
  email,
  role,
}: AdminHeaderProps) {
  const router = useRouter();

  const [loggingOut, setLoggingOut] =
    useState(false);

  async function logout() {
    setLoggingOut(true);

    try {
      await fetch("/api/auth/logout", {
        method: "POST",
      });
    } finally {
      router.replace("/login");
      router.refresh();
    }
  }

  const initial =
    email.charAt(0).toUpperCase();

  return (
    <header className="fixed inset-x-0 top-0 z-40 flex h-16 items-center border-b border-white/10 bg-black px-4 text-white lg:px-6">
      <div className="flex w-full items-center justify-between">
        <div className="flex items-center gap-5">
          <TenantBrand name={tenantName} />

          <button
            type="button"
            className="hidden rounded-md p-2 text-white/70 transition hover:bg-white/10 hover:text-white lg:block"
            aria-label="Toggle navigation"
          >
            <Menu className="h-5 w-5" />
          </button>
        </div>

        <div className="flex items-center gap-2 sm:gap-4">
          <button
            type="button"
            className="relative rounded-md p-2 text-white/80 hover:bg-white/10"
            aria-label="Notifications"
          >
            <Bell className="h-5 w-5" />

            <span className="absolute right-0 top-0 flex h-4 min-w-4 items-center justify-center rounded-full bg-tenant-primary px-1 text-[9px] font-bold text-black">
              3
            </span>
          </button>

          <button
            type="button"
            className="relative hidden rounded-md p-2 text-white/80 hover:bg-white/10 sm:block"
            aria-label="Messages"
          >
            <Mail className="h-5 w-5" />

            <span className="absolute right-0 top-0 flex h-4 min-w-4 items-center justify-center rounded-full bg-tenant-primary px-1 text-[9px] font-bold text-black">
              7
            </span>
          </button>

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
