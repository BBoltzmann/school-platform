import type { CSSProperties } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getSchoolBranding } from "@/lib/school-branding";
import { schoolLoginPath } from "@/lib/portal-routes";

export default async function SchoolWelcomePage({ params }: { params: Promise<{ schoolSlug: string }> }) {
  const { schoolSlug } = await params;
  const branding = await getSchoolBranding(schoolSlug);
  if (!branding) notFound();
  const name = branding.name ?? schoolSlug;
  const primary = branding.primaryColor ?? "#F5D900";
  const secondary = branding.secondaryColor ?? "#0B0B0B";
  return <main className="min-h-screen bg-background" style={{ "--tenant-primary": primary, "--tenant-secondary": secondary } as CSSProperties}>
    <section className="mx-auto flex min-h-screen max-w-5xl flex-col justify-center px-6 py-16 sm:px-10"><div className="flex flex-col items-center text-center">{branding.logoDataUrl ? <img src={branding.logoDataUrl} alt={`${name} logo`} className="h-20 w-20 rounded-2xl object-contain" /> : <div className="flex h-20 w-20 items-center justify-center rounded-2xl bg-tenant-primary text-2xl font-black text-black">{name.split(/\s+/).filter(Boolean).slice(0, 3).map(word => word[0]).join("").toUpperCase()}</div>}<p className="mt-6 text-sm font-semibold uppercase tracking-[0.18em] text-tenant-primary">{branding.motto ?? "School platform"}</p><h1 className="mt-3 text-4xl font-bold tracking-tight sm:text-5xl">{name}</h1><p className="mt-4 max-w-xl text-muted-foreground">Choose the secure portal for your account.</p></div><div className="mx-auto mt-12 grid w-full max-w-3xl gap-4 sm:grid-cols-2"><Link href={schoolLoginPath(schoolSlug, "admin")} className="rounded-xl border bg-card p-6 shadow-sm transition hover:border-tenant-primary"><h2 className="text-lg font-semibold">School administration</h2><p className="mt-2 text-sm text-muted-foreground">For administrators and authorized school office staff.</p><span className="mt-5 inline-block font-medium text-tenant-primary">Administrator login →</span></Link><Link href={schoolLoginPath(schoolSlug, "staff")} className="rounded-xl border bg-card p-6 shadow-sm transition hover:border-tenant-primary"><h2 className="text-lg font-semibold">Staff</h2><p className="mt-2 text-sm text-muted-foreground">For teaching staff with an activated staff portal account.</p><span className="mt-5 inline-block font-medium text-tenant-primary">Staff login →</span></Link></div><p className="mt-10 text-center text-xs text-muted-foreground">Guardian and student portals are not currently available for this school.</p></section>
  </main>;
}
