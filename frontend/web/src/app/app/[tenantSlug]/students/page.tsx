import Link from "next/link";
import {
  GraduationCap,
  Mail,
  Phone,
  Search,
  UserRound,
  UsersRound,
} from "lucide-react";

import { CreateStudentDialog } from "@/components/students/create-student-dialog";
import { Input } from "@/components/ui/input";
import {
  getStudents,
  getStudentSetup,
} from "@/lib/api/students";

function getFullName(
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

function formatDate(value: string) {
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

type StudentsPageProps = {
  params: Promise<{
    tenantSlug: string;
  }>;
};

export default async function StudentsPage({
  params,
}: StudentsPageProps) {
  const { tenantSlug } = await params;
  const [students, setup] =
    await Promise.all([
      getStudents(),
      getStudentSetup(),
    ]);

  const activeStudents =
    students.filter(
      (student) =>
        student.isActive
    ).length;

  const enrolledStudents =
    students.filter(
      (student) =>
        student.currentEnrollment !==
        null
    ).length;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">
            Students
          </h1>

          <p className="mt-1 text-sm text-muted-foreground">
            Manage student records,
            academic placement and
            enrolment.
          </p>
        </div>

        <CreateStudentDialog
          setup={setup}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border bg-card p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">
                Total Students
              </p>

              <p className="mt-2 text-3xl font-bold">
                {students.length}
              </p>
            </div>

            <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-muted">
              <UsersRound className="h-5 w-5" />
            </div>
          </div>
        </div>

        <div className="rounded-xl border bg-card p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">
                Active Students
              </p>

              <p className="mt-2 text-3xl font-bold">
                {activeStudents}
              </p>
            </div>

            <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-green-50 text-green-700">
              <UserRound className="h-5 w-5" />
            </div>
          </div>
        </div>

        <div className="rounded-xl border bg-card p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground">
                Current Enrolments
              </p>

              <p className="mt-2 text-3xl font-bold">
                {enrolledStudents}
              </p>
            </div>

            <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-amber-50">
              <GraduationCap className="h-5 w-5" />
            </div>
          </div>
        </div>
      </div>

      <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="flex flex-col gap-4 border-b p-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="font-semibold">
              Student Directory
            </h2>

            <p className="mt-1 text-sm text-muted-foreground">
              {setup.currentSession
                ? `Current session: ${setup.currentSession.name}`
                : "No current academic session"}
            </p>
          </div>

          <div className="relative w-full sm:w-72">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />

            <Input
              placeholder="Search students..."
              className="pl-9"
              disabled
            />
          </div>
        </div>

        {students.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="border-b bg-muted/40 text-xs text-muted-foreground">
                <tr>
                  <th className="px-5 py-3 font-medium">
                    Student
                  </th>

                  <th className="px-3 py-3 font-medium">
                    Admission No.
                  </th>

                  <th className="px-3 py-3 font-medium">
                    Level
                  </th>

                  <th className="px-3 py-3 font-medium">
                    Class
                  </th>

                  <th className="px-3 py-3 font-medium">
                    Contact
                  </th>

                  <th className="px-3 py-3 font-medium">
                    Admission Date
                  </th>

                  <th className="px-3 py-3 font-medium">
                    Status
                  </th>
                </tr>
              </thead>

              <tbody>
                {students.map(
                  (student) => (
                    <tr
                      key={student.id}
                      className="border-b last:border-0"
                    >
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-3">
                          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-tenant-primary font-semibold text-black">
                            {student.firstName
                              .charAt(0)
                              .toUpperCase()}
                            {student.lastName
                              .charAt(0)
                              .toUpperCase()}
                          </div>

                          <div>
                            <Link
                              href={`/app/${tenantSlug}/students/${student.id}`}
                              className="font-semibold hover:underline"
                            >
                              {getFullName(
                                student.firstName,
                                student.middleName,
                                student.lastName
                              )}
                            </Link>

                            <div className="mt-0.5 text-xs text-muted-foreground">
                              {student.gender} · DOB{" "}
                              {formatDate(
                                student.dateOfBirth
                              )}
                            </div>
                          </div>
                        </div>
                      </td>

                      <td className="px-3 py-4 font-mono text-xs">
                        {
                          student.admissionNumber
                        }
                      </td>

                      <td className="px-3 py-4">
                        {student
                          .currentEnrollment
                          ?.academicLevelName ??
                          "—"}
                      </td>

                      <td className="px-3 py-4">
                        {student
                          .currentEnrollment
                          ?.classGroupName ??
                          "—"}
                      </td>

                      <td className="px-3 py-4">
                        <div className="space-y-1 text-xs text-muted-foreground">
                          {student.email && (
                            <div className="flex items-center gap-1.5">
                              <Mail className="h-3.5 w-3.5" />
                              {
                                student.email
                              }
                            </div>
                          )}

                          {student.phone && (
                            <div className="flex items-center gap-1.5">
                              <Phone className="h-3.5 w-3.5" />
                              {
                                student.phone
                              }
                            </div>
                          )}

                          {!student.email &&
                            !student.phone && (
                              <span>—</span>
                            )}
                        </div>
                      </td>

                      <td className="px-3 py-4 text-muted-foreground">
                        {formatDate(
                          student.admissionDate
                        )}
                      </td>

                      <td className="px-3 py-4">
                        <span
                          className={
                            student.isActive
                              ? "inline-flex rounded-full bg-green-50 px-2.5 py-1 text-xs font-medium text-green-700"
                              : "inline-flex rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-600"
                          }
                        >
                          {student.status}
                        </span>
                      </td>
                    </tr>
                  )
                )}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="px-5 py-16 text-center">
            <UsersRound className="mx-auto h-10 w-10 text-muted-foreground" />

            <h3 className="mt-4 font-semibold">
              No students yet
            </h3>

            <p className="mx-auto mt-1 max-w-md text-sm text-muted-foreground">
              Add the first student to
              create their school record and
              enrol them into the current
              academic session.
            </p>

            <div className="mt-5 flex justify-center">
              <CreateStudentDialog
                setup={setup}
              />
            </div>
          </div>
        )}
      </section>
    </div>
  );
}
