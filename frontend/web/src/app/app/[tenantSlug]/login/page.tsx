import { redirect } from "next/navigation";


export default async function TenantLoginPage({ params, searchParams }: { params: Promise<{ tenantSlug: string }>; searchParams: Promise<{ setup?: string }> }) {
  const { tenantSlug } = await params;
  const query = await searchParams;
  redirect(`/schools/${encodeURIComponent(tenantSlug)}/admin/login${query.setup === "success" ? "?setup=success" : ""}`);
}
