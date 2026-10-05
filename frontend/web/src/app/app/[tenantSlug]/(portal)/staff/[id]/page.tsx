import Link from "next/link";
import { notFound } from "next/navigation";
import {
  ArrowLeft,
  BriefcaseBusiness,
  Clock3,
  Mail,
  MapPin,
  Phone,
  UserRound,
} from "lucide-react";

import { EditStaffDialog } from "@/components/staff/edit-staff-dialog";
import { TeachingAssignmentsCard } from "@/components/staff/teaching-assignments-card";
import { TeacherInviteActions } from "@/components/staff/teacher-invite-actions";

import {
  getStaffAvailability,
  getStaffMember,
} from "@/lib/api/staff";

import {
  getTeachingAssignmentSetup,
  getStaffTeachingAssignments,
} from "@/lib/api/teaching-assignments";

type StaffProfilePageProps = {
  params: Promise<{
    tenantSlug: string;
    id: string;
  }>;
};

function fullName(
  firstName: string,
  middleName: string | null,
  lastName: string
) {
  return [
    firstName,
    middleName,
    lastName,
  ]
    .filter(Boolean)
    .join(" ");
}

function formatDate(
  value: string | null
) {
  if (!value) {
    return "Not recorded";
  }

  return new Intl.DateTimeFormat(
    "en-GB",
    {
      day: "numeric",
      month: "long",
      year: "numeric",
    }
  ).format(
    new Date(`${value}T00:00:00`)
  );
}

function dayName(
  value: number | string
) {
  if (
    typeof value === "string" &&
    Number.isNaN(Number(value))
  ) {
    return value;
  }

  const number =
    Number(value);

  const days: Record<number, string> = {
    0: "Sunday",
    1: "Monday",
    2: "Tuesday",
    3: "Wednesday",
    4: "Thursday",
    5: "Friday",
    6: "Saturday",
  };

  return days[number] ??
    "Unknown";
}

function dayOrder(
  value: number | string
) {
  const name =
    dayName(value);

  const order: Record<string, number> = {
    Monday: 1,
    Tuesday: 2,
    Wednesday: 3,
    Thursday: 4,
    Friday: 5,
    Saturday: 6,
    Sunday: 7,
  };

  return order[name] ?? 8;
}

function formatTime(
  value: string
) {
  return value.slice(0, 5);
}

export default async function StaffProfilePage({
  params,
}: StaffProfilePageProps) {
  const { tenantSlug, id } =
    await params;

  const staff =
    await getStaffMember(id);

  if (!staff) {
    notFound();
  }

  const availability =
    await getStaffAvailability(id);

  const [
    teachingAssignments,
    teachingAssignmentSetup,
  ] = staff.isTeachingStaff
    ? await Promise.all([
        getStaffTeachingAssignments(id),
        getTeachingAssignmentSetup(),
      ])
    : [[], null];

  const name = fullName(
    staff.firstName,
    staff.middleName,
    staff.lastName
  );

  const sortedAvailability =
    [...availability].sort(
      (a, b) =>
        dayOrder(a.dayOfWeek) -
        dayOrder(b.dayOfWeek)
    );

  return (
    <div className="space-y-6">
      <Link
        href={`/app/${tenantSlug}/staff`}
        className="inline-flex items-center gap-2 text-sm font-medium hover:underline"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to Staff
      </Link>

      <section className="rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex items-start gap-4">
            <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-tenant-primary text-xl font-bold text-black">
              {staff.firstName
                .charAt(0)
                .toUpperCase()}

              {staff.lastName
                .charAt(0)
                .toUpperCase()}
            </div>

            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="text-2xl font-bold tracking-tight">
                  {name}
                </h1>

                <span
                  className={
                    staff.isActive
                      ? "rounded-full bg-green-50 px-2.5 py-1 text-xs font-medium text-green-700"
                      : "rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-600"
                  }
                >
                  {staff.status}
                </span>

                <span className="rounded-full bg-muted px-2.5 py-1 text-xs font-medium">
                  {staff.isTeachingStaff
                    ? "Teaching Staff"
                    : "Non-Teaching Staff"}
                </span>
              </div>

              <div className="mt-2 font-mono text-sm text-muted-foreground">
                {staff.staffNumber}
              </div>

              <div className="mt-1 text-sm text-muted-foreground">
                {staff.jobTitle}
              </div>
            </div>
          </div>

          <EditStaffDialog
            staff={staff}
            availability={
              availability
            }
          />
        </div>
      </section>

      {staff.isTeachingStaff && (
        <TeacherInviteActions staffId={staff.id} />
      )}

      <div className="grid gap-6 xl:grid-cols-2">
        <section className="rounded-xl border bg-card shadow-sm">
          <div className="flex items-center gap-2 border-b px-5 py-4">
            <UserRound className="h-5 w-5" />

            <h2 className="font-semibold">
              Personal Information
            </h2>
          </div>

          <div className="grid gap-5 p-5 sm:grid-cols-2">
            <Info
              label="Full Name"
              value={name}
            />

            <Info
              label="Gender"
              value={staff.gender}
            />

            <Info
              label="Date of Birth"
              value={formatDate(
                staff.dateOfBirth
              )}
            />

            <Info
              label="Staff Number"
              value={
                staff.staffNumber
              }
            />

            <Contact
              icon={
                <Phone className="h-4 w-4" />
              }
              label="Telephone"
              value={staff.phone}
            />

            <Contact
              icon={
                <Mail className="h-4 w-4" />
              }
              label="Email"
              value={
                staff.email ??
                "Not provided"
              }
            />

            <div className="sm:col-span-2">
              <Contact
                icon={
                  <MapPin className="h-4 w-4" />
                }
                label="Address"
                value={
                  staff.address ??
                  "Not recorded"
                }
              />
            </div>
          </div>
        </section>

        <section className="rounded-xl border bg-card shadow-sm">
          <div className="flex items-center gap-2 border-b px-5 py-4">
            <BriefcaseBusiness className="h-5 w-5" />

            <h2 className="font-semibold">
              Employment Information
            </h2>
          </div>

          <div className="grid gap-5 p-5 sm:grid-cols-2">
            <Info
              label="Job Title"
              value={staff.jobTitle}
            />

            <Info
              label="Department"
              value={
                staff.department ??
                "Not assigned"
              }
            />

            <Info
              label="Employment Type"
              value={
                staff.employmentType
              }
            />

            <Info
              label="Staff Category"
              value={
                staff.isTeachingStaff
                  ? "Teaching Staff"
                  : "Non-Teaching Staff"
              }
            />

            <Info
              label="Employment Date"
              value={formatDate(
                staff.employmentDate
              )}
            />

            <Info
              label="Employment Status"
              value={staff.status}
            />
          </div>
        </section>
      </div>

      <section className="rounded-xl border bg-card shadow-sm">
        <div className="flex items-center gap-2 border-b px-5 py-4">
          <Clock3 className="h-5 w-5" />

          <div>
            <h2 className="font-semibold">
              Working Schedule
            </h2>

            <p className="mt-0.5 text-xs text-muted-foreground">
              Availability used when
              scheduling classes and
              timetables.
            </p>
          </div>
        </div>

        {sortedAvailability.length >
        0 ? (
          <div className="divide-y">
            {sortedAvailability.map(
              (item) => (
                <div
                  key={item.id}
                  className="flex items-center justify-between gap-4 px-5 py-4"
                >
                  <div className="font-medium">
                    {dayName(
                      item.dayOfWeek
                    )}
                  </div>

                  <div className="text-sm text-muted-foreground">
                    {formatTime(
                      item.startTime
                    )}
                    {" — "}
                    {formatTime(
                      item.endTime
                    )}
                  </div>
                </div>
              )
            )}
          </div>
        ) : (
          <div className="px-5 py-10 text-center">
            <Clock3 className="mx-auto h-8 w-8 text-muted-foreground" />

            <div className="mt-3 font-medium">
              {staff.employmentType ===
              "Full-Time"
                ? "Standard school schedule"
                : "No working schedule recorded"}
            </div>

            <p className="mx-auto mt-1 max-w-md text-sm text-muted-foreground">
              {staff.employmentType ===
              "Full-Time"
                ? "This Full-Time staff member will follow the school's standard working hours."
                : "Edit this staff member to record the days and hours they are available."}
            </p>
          </div>
        )}
      </section>

      {staff.isTeachingStaff &&
        teachingAssignmentSetup && (
          <TeachingAssignmentsCard
            staffId={staff.id}
            assignments={teachingAssignments}
            setup={teachingAssignmentSetup}
          />
        )}
    </div>
  );
}

function Info({
  label,
  value,
}: {
  label: string;
  value: string;
}) {
  return (
    <div>
      <div className="text-xs text-muted-foreground">
        {label}
      </div>

      <div className="mt-1 text-sm font-medium">
        {value}
      </div>
    </div>
  );
}

function Contact({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-start gap-3">
      <div className="mt-0.5 text-muted-foreground">
        {icon}
      </div>

      <div>
        <div className="text-xs text-muted-foreground">
          {label}
        </div>

        <div className="mt-1 text-sm">
          {value}
        </div>
      </div>
    </div>
  );
}
