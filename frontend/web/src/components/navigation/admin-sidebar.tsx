"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { ChevronRight } from "lucide-react";

import { adminNavigation } from "./admin-navigation";

type AdminSidebarProps = {
  tenantSlug: string;
};

export function AdminSidebar({
  tenantSlug,
}: AdminSidebarProps) {
  const pathname = usePathname();

  return (
    <aside className="fixed bottom-0 left-0 top-16 z-30 hidden w-64 overflow-y-auto border-r border-sidebar-border bg-sidebar lg:block">
      <nav className="space-y-6 px-4 py-6">
        {adminNavigation.map((group) => (
          <section key={group.label}>
            <div className="mb-2 px-3 text-[11px] font-bold tracking-wider text-tenant-primary">
              {group.label}
            </div>

            <div className="space-y-1">
              {group.items.map((item) => {
                const href =
                  `/app/${tenantSlug}/${item.href}`;

                const active =
                  pathname === href ||
                  pathname.startsWith(`${href}/`);

                const Icon = item.icon;

                return (
                  <Link
                    key={item.title}
                    href={href}
                    className={[
                      "group flex h-11 items-center gap-3 rounded-md px-3 text-sm font-medium transition",
                      active
                        ? "bg-sidebar-primary text-sidebar-primary-foreground"
                        : "text-sidebar-foreground/75 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground",
                    ].join(" ")}
                  >
                    <Icon className="h-[18px] w-[18px] shrink-0" />

                    <span className="flex-1">
                      {item.title}
                    </span>

                    {item.title !== "Dashboard" && (
                      <ChevronRight className="h-4 w-4 opacity-60" />
                    )}
                  </Link>
                );
              })}
            </div>
          </section>
        ))}
      </nav>
    </aside>
  );
}
