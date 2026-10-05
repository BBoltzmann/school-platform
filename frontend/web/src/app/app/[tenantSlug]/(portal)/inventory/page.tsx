import { Boxes } from "lucide-react";

import { InventoryWorkspace } from "@/components/inventory/inventory-workspace";
import { getInventorySetup } from "@/lib/api/inventory";

export default async function InventoryPage() {
  const setup =
    await getInventorySetup();

  return (
    <div className="space-y-6">
      <div>
        <div className="flex items-center gap-2">
          <Boxes className="h-6 w-6" />

          <h1 className="text-2xl font-bold tracking-tight">
            Inventory
          </h1>
        </div>

        <p className="mt-1 text-sm text-muted-foreground">
          Manage textbooks,
          exercise books, uniforms,
          school supplies, stock
          locations, issuance and
          shortages.
        </p>
      </div>

      <InventoryWorkspace
        initialSetup={setup}
      />
    </div>
  );
}
