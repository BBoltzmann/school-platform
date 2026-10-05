import { FeesPageHeader } from "@/components/fees/fees-page-header";
import { OutstandingFeesWorkspace } from "@/components/fees/outstanding-fees-workspace";
import { getFeesSetup } from "@/lib/api/fees";

type PageProps = {
  params: Promise<{
    tenantSlug: string;
  }>;
};

export default async function OutstandingFeesPage({
  params,
}: PageProps) {
  const {
    tenantSlug,
  } = await params;

  const setup =
    await getFeesSetup();

  return (
    <div className="space-y-6">
      <FeesPageHeader
        tenantSlug={tenantSlug}
        sessionName={
          setup.currentSession?.name
        }
      />

      <OutstandingFeesWorkspace
        setup={setup}
      />
    </div>
  );
}
