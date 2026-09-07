import { FeeStructuresWorkspace } from "@/components/fees/fee-structures-workspace";
import { FeesPageHeader } from "@/components/fees/fees-page-header";
import { getFeesSetup } from "@/lib/api/fees";

type PageProps = {
  params: Promise<{
    tenantSlug: string;
  }>;
};

export default async function FeeStructuresPage({
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
          setup.currentSession
            ?.name
        }
      />

      <FeeStructuresWorkspace
        setup={setup}
      />
    </div>
  );
}
