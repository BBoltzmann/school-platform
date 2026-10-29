import type { CSSProperties } from "react";
import { redirect } from "next/navigation";

import { LoginForm } from "@/components/auth/login-form";
import { getSessionContext } from "@/lib/auth/session";
import type { SchoolBranding } from "@/lib/school-branding";
import { schoolLoginPath } from "@/lib/portal-routes";

export async function SchoolPortalLogin({ schoolSlug, portal, branding, setup }: { schoolSlug: string; portal: "admin" | "staff"; branding: SchoolBranding; setup?: string }) {
  const session = await getSessionContext();
  if (session?.tenantSlug === schoolSlug) {
    const roles = (session.roles ?? []).map(role => role.toLowerCase());
    const teacherOnly = roles.includes("teacher") && !roles.some(role => role.includes("administrator") || role.includes("admin"));
    redirect(`/app/${encodeURIComponent(schoolSlug)}/${teacherOnly ? "teacher" : "dashboard"}`);
  }
  const primary = branding.primaryColor ?? "#F5D900";
  const secondary = branding.secondaryColor ?? "#0B0B0B";
  const accent = branding.accentColor ?? "#FFF8C9";
  const schoolName = branding.name ?? schoolSlug;
  const initials = schoolName.trim().split(/\s+/).filter(Boolean).slice(0, 3).map(word => word[0]).join("").toUpperCase() || "SCH";
  return <main className="grid min-h-screen bg-background lg:grid-cols-[1.05fr_0.95fr]" style={{ "--tenant-primary": primary, "--tenant-secondary": secondary, "--accent": accent } as CSSProperties}>
    {branding.iconDataUrl && <link rel="icon" href={branding.iconDataUrl} />}
    <section className="relative hidden overflow-hidden p-12 text-white lg:flex lg:flex-col lg:justify-between" style={{ backgroundColor: secondary }}>
      <div className="relative z-10 flex items-center gap-4">{branding.logoDataUrl ? <img src={branding.logoDataUrl} alt={`${schoolName} logo`} className="h-14 w-14 rounded-lg object-contain" /> : <div className="flex h-14 w-14 items-center justify-center rounded-lg bg-tenant-primary font-black text-black">{initials}</div>}<div><div className="text-xl font-bold">{schoolName}</div><div className="mt-1 text-sm text-white/60">{portal === "staff" ? "Staff portal" : "School administration"}</div></div></div>
      <div className="relative z-10 max-w-xl"><div className="mb-5 text-sm font-bold uppercase tracking-[0.18em] text-tenant-primary">{branding.motto ?? "School platform"}</div><h1 className="text-4xl font-bold leading-tight">{portal === "staff" ? "Teach and work" : "Manage your school"}<br />from one place.</h1></div>
      <div className="relative z-10 text-xs text-white/40">{schoolName}</div>
    </section>
    <section className="flex items-center justify-center px-6 py-12 sm:px-10"><div className="w-full max-w-md"><div className="mb-8 flex items-center gap-3 lg:hidden">{branding.logoDataUrl ? <img src={branding.logoDataUrl} alt={`${schoolName} logo`} className="h-11 w-11 rounded-md object-contain" /> : <div className="flex h-11 w-11 items-center justify-center rounded-md bg-tenant-primary font-black text-black">{initials}</div>}<div><div className="font-bold">{schoolName}</div><div className="text-xs text-muted-foreground">{portal === "staff" ? "Staff portal" : "School administration"}</div></div></div><h2 className="text-3xl font-bold tracking-tight">Welcome back</h2>{setup === "success" && <p className="mt-2 text-sm font-medium text-green-700">Your account is ready. Sign in to continue.</p>}<div className="mt-8"><LoginForm initialTenantSlug={schoolSlug} tenantLocked successPath={`/app/${encodeURIComponent(schoolSlug)}/${portal === "staff" ? "teacher" : "dashboard"}`} /></div><p className="mt-6 text-center text-sm text-muted-foreground"><a href={schoolLoginPath(schoolSlug, portal === "staff" ? "admin" : "staff")} className="underline">{portal === "staff" ? "School administration login" : "Staff portal login"}</a></p></div></section>
  </main>;
}
