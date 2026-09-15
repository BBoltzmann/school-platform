import Link from "next/link";
import { AlertCircle, Banknote, CalendarDays, Clock3, GraduationCap, Settings2, UserPlus, Users } from "lucide-react";
import { CreateSessionDialog } from "@/components/academics/create-session-dialog";
import { DashboardError } from "@/components/dashboard/dashboard-error";
import { getDashboard } from "@/lib/api/dashboard";
import type { DashboardResponse } from "@/types/dashboard";
import { getSessionContext } from "@/lib/auth/session";
import { authenticatedBackendFetch } from "@/lib/api/authenticated-backend";
import { TeacherPortalView } from "@/components/teacher/teacher-portal-view";
import type { TeacherPortal } from "@/types/teacher";

type Props = { params: Promise<{ tenantSlug: string }> };
const actionCopy: Record<string, { title: string; description: string; href?: string; icon: typeof Users }> = {
  NO_CURRENT_SESSION: { title: "Create academic session", description: "Set up the academic year before using academic workflows.", icon: CalendarDays },
  NO_CURRENT_TERM: { title: "Configure current term", description: "Set a term within the current academic session.", href: "/academics", icon: CalendarDays },
  ACADEMICS_NOT_CONFIGURED: { title: "Complete academic setup", description: "Add usable academic levels and classes to continue.", href: "/academics", icon: Settings2 },
  PENDING_APPLICATIONS: { title: "Applications need review", description: "Review submitted applications waiting for a decision.", href: "/admissions", icon: UserPlus },
  OUTSTANDING_FEES: { title: "Outstanding fees", description: "Review students with actual unpaid fee balances.", href: "/fees-management/outstanding-fees", icon: Banknote },
  TIMETABLE_SETUP_REQUIRED: { title: "Complete timetable setup", description: "Resolve the timetable readiness issues for this session.", href: "/timetable", icon: Clock3 },
};
function path(slug: string, value: string) { return `/app/${slug}${value}`; }
function naira(amount: number, currency: string) { return new Intl.NumberFormat("en-NG", { style: "currency", currency: currency || "NGN", maximumFractionDigits: 0 }).format(amount); }
function activityDate(value: string) { return new Intl.DateTimeFormat("en-NG", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value)); }
function Heading({ title }: { title: string }) { return <div className="flex items-center gap-3"><span className="h-6 w-1 rounded-full bg-tenant-primary" /><h2 className="font-semibold">{title}</h2></div>; }

function Metric({ title, value, note, href, icon: Icon, tone }: { title: string; value: string; note: string; href: string; icon: typeof Users; tone: string }) {
  return <Link href={href} className="block rounded-xl border bg-card p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md"><div className="flex items-start gap-4"><div className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-full ${tone}`}><Icon className="h-5 w-5" /></div><div className="min-w-0"><p className="text-sm text-muted-foreground">{title}</p><p className="mt-1 text-2xl font-bold">{value}</p><p className="mt-2 text-xs text-muted-foreground">{note}</p></div></div></Link>;
}
function Setup({ slug, name }: { slug: string; name: string }) {
  return <section className="flex flex-col gap-5 rounded-2xl border border-yellow-300 bg-yellow-50 p-6 shadow-sm sm:flex-row sm:items-center sm:justify-between sm:p-8"><div className="max-w-2xl"><div className="flex items-center gap-2 text-sm font-bold uppercase tracking-wide text-yellow-900"><AlertCircle className="h-4 w-4" />Set up your school</div><h2 className="mt-2 text-2xl font-bold text-black">Start with your first academic session</h2><p className="mt-2 text-sm leading-6 text-neutral-700">Create an academic session for {name}. Admissions, class setup, timetables, assessments and other academic workflows depend on an active academic session.</p></div><CreateSessionDialog tenantSlug={slug} triggerLabel="Create Academic Session" /></section>;
}
function Actions({ data, slug }: { data: DashboardResponse; slug: string }) {
  if (!data.actions.length) return <section className="rounded-xl border bg-card p-6 shadow-sm"><Heading title="Action Required" /><div className="py-10 text-center"><p className="font-medium">You&apos;re all caught up.</p><p className="mt-1 text-sm text-muted-foreground">No actions require your attention right now.</p></div></section>;
  return <section className="overflow-hidden rounded-xl border bg-card shadow-sm"><div className="border-b px-5 py-4"><Heading title="Action Required" /></div>{data.actions.map((action) => { const copy = actionCopy[action.code]; if (!copy) return null; const Icon = copy.icon; const body = <div className="flex items-center gap-4 px-5 py-4 transition hover:bg-muted/40"><div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-yellow-50"><Icon className="h-5 w-5" /></div><div className="min-w-0 flex-1"><div className="font-medium">{action.code === "PENDING_APPLICATIONS" ? `${action.count} applications need review` : copy.title}</div><div className="mt-0.5 text-sm text-muted-foreground">{copy.description}</div></div>{action.count !== null && <span className="rounded-md bg-yellow-50 px-2.5 py-1 text-sm font-semibold">{action.count}</span>}</div>; return action.code === "NO_CURRENT_SESSION" ? <div key={action.code}>{body}</div> : <Link key={action.code} href={path(slug, copy.href ?? "/academics")}>{body}</Link>; })}</section>;
}
function Activity({ data }: { data: DashboardResponse }) {
  return <section className="overflow-hidden rounded-xl border bg-card shadow-sm"><div className="border-b px-5 py-4"><Heading title="Recent Activity" /></div>{!data.recentActivity.length ? <div className="py-10 text-center"><p className="font-medium">No recent activity yet.</p><p className="mt-1 text-sm text-muted-foreground">Activity will appear here as students, staff, payments and academic records are created.</p></div> : <div>{data.recentActivity.map((item) => <div key={item.id} className="flex items-start gap-3 border-b px-5 py-4 last:border-b-0"><div className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-green-50"><Users className="h-4 w-4 text-green-700" /></div><div className="min-w-0 flex-1"><div className="text-sm">{item.action}</div><div className="mt-1 text-xs text-muted-foreground">{item.entityType}</div></div><time className="whitespace-nowrap text-xs text-muted-foreground" dateTime={item.createdAtUtc}>{activityDate(item.createdAtUtc)}</time></div>)}</div>}</section>;
}
function Events({ data }: { data: DashboardResponse }) {
  if (data.upcomingEvents.items.length > 0) {
    return <section className="overflow-hidden rounded-xl border bg-card shadow-sm"><div className="border-b px-5 py-4"><Heading title="Upcoming Events" /></div><div className="grid divide-y md:grid-cols-2 md:divide-x md:divide-y-0">{data.upcomingEvents.items.map((event) => <div key={event.id} className="flex gap-4 p-5"><CalendarDays className="mt-1 h-5 w-5 shrink-0 text-muted-foreground" /><div><div className="font-semibold">{event.title}</div><time className="mt-1 block text-sm text-muted-foreground" dateTime={event.startsAt}>{activityDate(event.startsAt)}</time>{event.location && <div className="mt-1 text-xs text-muted-foreground">{event.location}</div>}</div></div>)}</div></section>;
  }
  const message = data.academic.currentSession === null ? "Complete academic setup first. Calendar and school events can be configured afterward." : data.upcomingEvents.availability === "notImplemented" ? "School calendar setup will be available here." : "There are no upcoming events.";
  return <section className="overflow-hidden rounded-xl border bg-card shadow-sm"><div className="border-b px-5 py-4"><Heading title="Upcoming Events" /></div><div className="py-10 text-center"><p className="font-medium">No calendar events yet.</p><p className="mx-auto mt-1 max-w-md text-sm text-muted-foreground">{message}</p></div></section>;
}

export default async function DashboardPage({ params }: Props) {
  const { tenantSlug } = await params;
  const session = await getSessionContext();
  if (session?.roles.includes("Teacher")) {
    const teacherResponse = await authenticatedBackendFetch("/api/me/teacher-portal");
    if (teacherResponse?.ok) return <TeacherPortalView data={await teacherResponse.json() as TeacherPortal} />;
  }
  let data: DashboardResponse;
  try { data = await getDashboard(); } catch { return <DashboardError />; }
  const { metrics, academic } = data;
  const period = academic.currentSession ? `${academic.currentSession.name} · ${academic.currentTerm?.name ?? "No current term"}` : "No active session";
  return <div className="space-y-6"><div className="flex flex-col justify-between gap-4 md:flex-row md:items-start"><div><h1 className="text-3xl font-bold tracking-tight">Administrator Dashboard</h1><p className="mt-1 text-sm text-muted-foreground">Welcome back. Let&apos;s get {data.tenant.name} ready for the academic year.</p></div><Link href={path(tenantSlug, "/academics")} className="inline-flex items-center gap-2 self-start rounded-md border bg-card px-3 py-2 text-sm text-muted-foreground transition hover:text-foreground"><CalendarDays className="h-4 w-4" />{period}</Link></div>{!academic.currentSession && <Setup slug={tenantSlug} name={data.tenant.name} />}<section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4"><Metric title="Total Students" value={String(metrics.totalStudents)} note={metrics.totalStudents === 0 ? "No students added yet" : "Students in the school directory"} href={path(tenantSlug, "/students")} icon={Users} tone="bg-amber-50" /><Metric title="Total Staff" value={String(metrics.totalStaff)} note={metrics.totalStaff === 0 ? "No staff added yet" : "Staff in the school directory"} href={path(tenantSlug, "/staff")} icon={GraduationCap} tone="bg-neutral-100" /><Metric title="Pending Applications" value={String(metrics.pendingApplications)} note={metrics.pendingApplications === 0 ? "No pending applications" : "Applications awaiting review"} href={path(tenantSlug, "/admissions")} icon={UserPlus} tone="bg-blue-50" /><Metric title="Fees Collected" value={naira(metrics.feesCollected.amount, metrics.feesCollected.currency)} note={metrics.feesCollected.amount === 0 ? "No fee payments recorded · All time" : "All time"} href={path(tenantSlug, "/fees-management")} icon={Banknote} tone="bg-green-50" /></section><section className="grid gap-6 xl:grid-cols-[1.05fr_0.95fr]"><Actions data={data} slug={tenantSlug} /><Events data={data} /></section><Activity data={data} /></div>;
}
