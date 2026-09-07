import {
  BadgeDollarSign,
} from "lucide-react";

import { FeesNavigation } from "@/components/fees/fees-navigation";

export function FeesPageHeader({
  tenantSlug,
  sessionName,
}: {
  tenantSlug: string;
  sessionName?: string;
}) {
  return (
    <>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <div className="flex items-center gap-2">
            <BadgeDollarSign className="h-6 w-6" />

            <h1 className="text-2xl font-bold tracking-tight">
              Fees Management
            </h1>
          </div>

          <p className="mt-1 text-sm text-muted-foreground">
            Configure student fees,
            record payments and monitor
            outstanding balances.
          </p>
        </div>

        {sessionName && (
          <div className="rounded-lg border bg-card px-4 py-2 text-sm">
            <span className="text-muted-foreground">
              Session:{" "}
            </span>

            <span className="font-medium">
              {sessionName}
            </span>
          </div>
        )}
      </div>

      <FeesNavigation
        tenantSlug={tenantSlug}
      />
    </>
  );
}
