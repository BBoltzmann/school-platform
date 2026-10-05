import { CarryForwardWorkspace } from "@/components/fees/carry-forward-workspace";
import { FeesPageHeader } from "@/components/fees/fees-page-header";
import { getFeesSetup } from "@/lib/api/fees";

export default async function CarryForwardPage({ params }: { params: Promise<{ tenantSlug: string }> }) {
  const { tenantSlug } = await params;
  const setup = await getFeesSetup();
  return <div className="space-y-6"><FeesPageHeader tenantSlug={tenantSlug} sessionName={setup.currentSession?.name} /><CarryForwardWorkspace sessionId={setup.currentSession?.id} termId={setup.terms[0]?.id} /></div>;
}
