import { redirect } from "next/navigation";

type ResultsPageProps = {
  params: Promise<{
    tenantSlug: string;
  }>;
};

export default async function ResultsPage({
  params,
}: ResultsPageProps) {
  const {
    tenantSlug,
  } = await params;

  redirect(
    `/app/${tenantSlug}/assessments`
  );
}
