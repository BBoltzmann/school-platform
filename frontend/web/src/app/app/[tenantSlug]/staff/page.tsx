import Link from "next/link";
import {
  BriefcaseBusiness,
  GraduationCap,
  UserCheck,
  UsersRound,
} from "lucide-react";

import { CreateStaffDialog } from "@/components/staff/create-staff-dialog";
import { TeacherInviteActions } from "@/components/staff/teacher-invite-actions";
import { getStaff } from "@/lib/api/staff";

type StaffPageProps = {
  params: Promise<{
    tenantSlug: string;
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
  value: string
) {
  return new Intl.DateTimeFormat(
    "en-GB",
    {
      day: "numeric",
      month: "short",
      year: "numeric",
    }
  ).format(
    new Date(`${value}T00:00:00`)
  );
}

export default async function StaffPage({
  params,
}: StaffPageProps) {
  const { tenantSlug } =
    await params;

  const staff =
    await getStaff();

  const teaching =
    staff.filter(
      (item) =>
        item.isTeachingStaff
    ).length;

  const nonTeaching =
    staff.filter(
      (item) =>
        !item.isTeachingStaff
    ).length;

  const active =
    staff.filter(
      (item) =>
        item.isActive
    ).length;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            Staff
          </h1>

          <p className="mt-1 text-sm text-muted-foreground">
            Manage teaching and
            non-teaching employees.
          </p>
        </div>

        <CreateStaffDialog />
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Metric
          label="Total Staff"
          value={staff.length}
          icon={
            <UsersRound className="h-5 w-5 text-muted-foreground" />
          }
        />

        <Metric
          label="Teaching Staff"
          value={teaching}
          icon={
            <GraduationCap className="h-5 w-5 text-muted-foreground" />
          }
        />

        <Metric
          label="Non-Teaching"
          value={nonTeaching}
          icon={
            <BriefcaseBusiness className="h-5 w-5 text-muted-foreground" />
          }
        />

        <Metric
          label="Active Staff"
          value={active}
          icon={
            <UserCheck className="h-5 w-5 text-muted-foreground" />
          }
        />
      </div>

      <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="border-b px-5 py-4">
          <h2 className="font-semibold">
            Staff Directory
          </h2>

          <p className="mt-1 text-xs text-muted-foreground">
            Current and historical staff
            records.
          </p>
        </div>

        {staff.length === 0 ? (
          <div className="px-5 py-16 text-center">
            <UsersRound className="mx-auto h-10 w-10 text-muted-foreground" />

            <h3 className="mt-4 font-semibold">
              No staff records yet
            </h3>

            <p className="mx-auto mt-1 max-w-md text-sm text-muted-foreground">
            Add the school&apos;s first staff
              member to begin building the
              staff directory.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1100px] text-sm">
              <thead className="border-b bg-muted/40 text-left">
                <tr>
                  <th className="px-5 py-3 font-medium">
                    Staff Member
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Staff Number
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Role
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Department
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Type
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Employment
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Started
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Status
                  </th>
                  <th className="px-5 py-3 font-medium">Teacher Portal</th>
                </tr>
              </thead>

              <tbody className="divide-y">
                {staff.map(
                  (member) => (
                    <tr
                      key={member.id}
                      className="hover:bg-muted/20"
                    >
                      <td className="px-5 py-4">
                        <Link
                          href={`/app/${tenantSlug}/staff/${member.id}`}
                          className="font-medium hover:underline"
                        >
                          {fullName(
                            member.firstName,
                            member.middleName,
                            member.lastName
                          )}
                        </Link>

                        <div className="mt-1 text-xs text-muted-foreground">
                          {member.phone}
                        </div>
                      </td>
                      <td className="px-5 py-4 font-mono text-xs">
                        {
                          member.staffNumber
                        }
                      </td>

                      <td className="px-5 py-4 font-medium">
                        {
                          member.jobTitle
                        }
                      </td>

                      <td className="px-5 py-4 text-muted-foreground">
                        {member.department ??
                          "—"}
                      </td>

                      <td className="px-5 py-4">
                        <span className="rounded-full bg-muted px-2.5 py-1 text-xs font-medium">
                          {member.isTeachingStaff
                            ? "Teaching"
                            : "Non-Teaching"}
                        </span>
                      </td>
                      <td className="px-5 py-4">{member.isTeachingStaff && <TeacherInviteActions staffId={member.id} />}</td>

                      <td className="px-5 py-4">
                        {
                          member.employmentType
                        }
                      </td>

                      <td className="px-5 py-4 text-muted-foreground">
                        {formatDate(
                          member.employmentDate
                        )}
                      </td>

                      <td className="px-5 py-4">
                        <span
                          className={
                            member.isActive
                              ? "rounded-full bg-green-50 px-2.5 py-1 text-xs font-medium text-green-700"
                              : "rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-600"
                          }
                        >
                          {member.status}
                        </span>
                      </td>
                    </tr>
                  )
                )}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}

function Metric({
  label,
  value,
  icon,
}: {
  label: string;
  value: number;
  icon: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border bg-card p-5 shadow-sm">
      <div className="flex items-center justify-between">
        <div className="text-sm text-muted-foreground">
          {label}
        </div>

        {icon}
      </div>

      <div className="mt-4 text-3xl font-bold">
        {value}
      </div>
    </div>
  );
}
