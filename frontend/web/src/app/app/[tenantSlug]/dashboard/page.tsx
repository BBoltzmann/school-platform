import {
  Banknote,
  CalendarDays,
  ClipboardCheck,
  FileText,
  GraduationCap,
  Shirt,
  UserPlus,
  Users,
} from "lucide-react";

const stats = [
  {
    title: "Total Students",
    value: "1,248",
    note: "3.2% from last term",
    icon: Users,
    tone: "bg-amber-50",
  },
  {
    title: "Total Staff",
    value: "86",
    note: "1.6% from last term",
    icon: GraduationCap,
    tone: "bg-neutral-100",
  },
  {
    title: "Pending Applications",
    value: "28",
    note: "Requires review",
    icon: FileText,
    tone: "bg-blue-50",
  },
  {
    title: "Fees Collected",
    value: "₦18.54M",
    note: "8.7% from last term",
    icon: Banknote,
    tone: "bg-green-50",
  },
];

const actions = [
  {
    title: "Applications Awaiting Review",
    description:
      "There are 28 new applications awaiting your review.",
    count: 28,
    icon: UserPlus,
  },
  {
    title: "Results Awaiting Approval",
    description:
      "14 sets of results are pending your approval.",
    count: 14,
    icon: ClipboardCheck,
  },
  {
    title: "Unpaid Fees",
    description:
      "103 students have outstanding fee payments.",
    count: 103,
    icon: Banknote,
  },
  {
    title: "Uniforms Pending Issue",
    description:
      "37 students are pending uniform allocation.",
    count: 37,
    icon: Shirt,
  },
];

const activities = [
  {
    title: "New student application submitted",
    person: "John Okafor",
    time: "10:45 AM",
  },
  {
    title: "Term 2 results uploaded for JSS 2",
    person: "Mrs. A. Johnson",
    time: "09:30 AM",
  },
  {
    title: "Payment of ₦75,000 received",
    person: "Mary Samuel",
    time: "Yesterday, 4:15 PM",
  },
  {
    title: "Inter-house Sports Day created",
    person: "Admin User",
    time: "Yesterday, 11:20 AM",
  },
];

const events = [
  {
    month: "SEP",
    day: "08",
    title: "School Resumption",
    detail: "All classes",
  },
  {
    month: "SEP",
    day: "20",
    title: "PTA Meeting",
    detail: "School Hall · 10:00 AM",
  },
  {
    month: "OCT",
    day: "15",
    title: "Mid-Term Break",
    detail: "Academic calendar",
  },
  {
    month: "NOV",
    day: "07",
    title: "Inter-house Sports",
    detail: "School Field · 9:00 AM",
  },
];

export default function DashboardPage() {
  return (
    <div className="space-y-6">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-start">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">
            Administrator Dashboard
          </h1>

          <p className="mt-1 text-sm text-muted-foreground">
            Welcome back. Here&apos;s what&apos;s happening at Antioch Royal College.
          </p>
        </div>

        <div className="inline-flex items-center gap-2 self-start rounded-md border bg-card px-3 py-2 text-sm text-muted-foreground">
          <CalendarDays className="h-4 w-4" />
          2026/2027 · First Term
        </div>
      </div>

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {stats.map((stat) => {
          const Icon = stat.icon;

          return (
            <article
              key={stat.title}
              className="rounded-xl border bg-card p-5 shadow-sm"
            >
              <div className="flex items-start gap-4">
                <div
                  className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-full ${stat.tone}`}
                >
                  <Icon className="h-5 w-5" />
                </div>

                <div>
                  <p className="text-sm text-muted-foreground">
                    {stat.title}
                  </p>

                  <p className="mt-1 text-2xl font-bold">
                    {stat.value}
                  </p>

                  <p className="mt-2 text-xs text-muted-foreground">
                    {stat.note}
                  </p>
                </div>
              </div>
            </article>
          );
        })}
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.05fr_0.95fr]">
        <div className="overflow-hidden rounded-xl border bg-card shadow-sm">
          <div className="flex items-center justify-between border-b px-5 py-4">
            <div className="flex items-center gap-3">
              <span className="h-6 w-1 rounded-full bg-tenant-primary" />
              <h2 className="font-semibold">
                Action Required
              </h2>
            </div>

            <button className="text-sm font-medium text-muted-foreground hover:text-foreground">
              View All
            </button>
          </div>

          <div>
            {actions.map((item) => {
              const Icon = item.icon;

              return (
                <div
                  key={item.title}
                  className="flex items-center gap-4 border-b px-5 py-4 last:border-b-0"
                >
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-amber-50">
                    <Icon className="h-5 w-5" />
                  </div>

                  <div className="min-w-0 flex-1">
                    <div className="font-medium">
                      {item.title}
                    </div>

                    <div className="mt-0.5 text-sm text-muted-foreground">
                      {item.description}
                    </div>
                  </div>

                  <span className="rounded-md bg-amber-50 px-2.5 py-1 text-sm font-semibold">
                    {item.count}
                  </span>
                </div>
              );
            })}
          </div>
        </div>

        <div className="overflow-hidden rounded-xl border bg-card shadow-sm">
          <div className="flex items-center justify-between border-b px-5 py-4">
            <div className="flex items-center gap-3">
              <span className="h-6 w-1 rounded-full bg-tenant-primary" />
              <h2 className="font-semibold">
                Recent Activity
              </h2>
            </div>

            <button className="text-sm font-medium text-muted-foreground hover:text-foreground">
              View All
            </button>
          </div>

          <div>
            {activities.map((activity) => (
              <div
                key={`${activity.title}-${activity.person}`}
                className="flex items-start gap-3 border-b px-5 py-4 last:border-b-0"
              >
                <div className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-green-50">
                  <Users className="h-4 w-4 text-green-700" />
                </div>

                <div className="min-w-0 flex-1">
                  <div className="text-sm">
                    {activity.title}
                  </div>

                  <div className="mt-1 text-sm font-semibold">
                    {activity.person}
                  </div>
                </div>

                <div className="whitespace-nowrap text-xs text-muted-foreground">
                  {activity.time}
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="flex items-center justify-between border-b px-5 py-4">
          <div className="flex items-center gap-3">
            <span className="h-6 w-1 rounded-full bg-tenant-primary" />

            <h2 className="font-semibold">
              Upcoming Events
            </h2>
          </div>

          <button className="text-sm font-medium text-muted-foreground hover:text-foreground">
            View Calendar
          </button>
        </div>

        <div className="grid divide-y md:grid-cols-2 md:divide-x md:divide-y-0 xl:grid-cols-4">
          {events.map((event) => (
            <div
              key={`${event.month}-${event.day}`}
              className="flex gap-4 p-5"
            >
              <div className="flex h-[70px] w-14 shrink-0 flex-col items-center justify-center rounded-md border bg-background">
                <div className="text-[10px] font-bold text-red-500">
                  {event.month}
                </div>

                <div className="text-xl font-bold">
                  {event.day}
                </div>
              </div>

              <div className="pt-1">
                <div className="font-semibold">
                  {event.title}
                </div>

                <div className="mt-2 text-sm text-muted-foreground">
                  {event.detail}
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
