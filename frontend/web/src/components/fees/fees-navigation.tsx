"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const items = [
  {
    label: "Overview",
    href: "",
  },
  {
    label: "Fee Structures",
    href: "/fee-structures",
  },
  {
    label: "Student Accounts",
    href: "/student-accounts",
  },
  {
    label: "Record Payment",
    href: "/record-payment",
  },
  {
    label: "Outstanding Fees",
    href: "/outstanding-fees",
  },
];

export function FeesNavigation({
  tenantSlug,
}: {
  tenantSlug: string;
}) {
  const pathname =
    usePathname();

  const base =
    `/app/${tenantSlug}/fees-management`;

  return (
    <div className="overflow-x-auto">
      <nav className="inline-flex min-w-max rounded-xl border bg-card p-1">
        {items.map(item => {
          const href =
            `${base}${item.href}`;

          const active =
            item.href === ""
              ? pathname === base
              : pathname === href;

          return (
            <Link
              key={item.label}
              href={href}
              className={
                active
                  ? "rounded-lg bg-tenant-primary px-5 py-3 text-sm font-medium text-black"
                  : "rounded-lg px-5 py-3 text-sm text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
              }
            >
              {item.label}
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
