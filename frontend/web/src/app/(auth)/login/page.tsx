import Link from "next/link";
import { redirect } from "next/navigation";
import { LoginForm } from "@/components/auth/login-form";

export default async function LoginPage({ searchParams }: { searchParams: Promise<{ tenantSlug?: string; reset?: string }> }) {
  const query = await searchParams;
  if (typeof query.tenantSlug === "string" && query.tenantSlug.trim()) {
    const setup = query.reset === "success" ? "?setup=success" : "";
    redirect(`/schools/${encodeURIComponent(query.tenantSlug.trim().toLowerCase())}/admin/login${setup}`);
  }
  return <main className="flex min-h-screen items-center justify-center bg-muted/30 px-6 py-12"><section className="w-full max-w-md rounded-2xl border bg-card p-8 shadow-sm"><p className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">School login</p><h1 className="mt-2 text-3xl font-bold">Sign in to your school</h1><p className="mt-2 text-sm text-muted-foreground">Enter your school login code, email, and password. For a permanent school link, visit <code>/schools/[schoolSlug]</code>.</p><div className="mt-8"><LoginForm initialTenantSlug={typeof query.tenantSlug === "string" ? query.tenantSlug : ""} /></div><p className="mt-6 text-center text-sm text-muted-foreground"><Link href="/super-admin/login" className="underline">Platform Super Admin login</Link></p></section></main>;
}
