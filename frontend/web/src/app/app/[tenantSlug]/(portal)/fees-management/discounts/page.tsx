import { DiscountsWorkspace } from "@/components/fees/discounts-workspace";
import { FeesPageHeader } from "@/components/fees/fees-page-header";
import { getFeesSetup } from "@/lib/api/fees";

export default async function DiscountsPage({ params }: { params: Promise<{ tenantSlug: string }> }) {
  const { tenantSlug } = await params;
  const setup = await getFeesSetup();
  return <div className="space-y-6"><FeesPageHeader tenantSlug={tenantSlug} sessionName={setup.currentSession?.name} /><DiscountsWorkspace setup={setup} /></div>;
}
