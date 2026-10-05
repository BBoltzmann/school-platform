import Link from "next/link";
import { notFound } from "next/navigation";
import {
  ArrowLeft,
  BookOpen,
  CalendarDays,
  GraduationCap,
  Mail,
  MapPin,
  Phone,
  ShieldCheck,
  UserRound,
  UsersRound,
} from "lucide-react";

import { AddGuardianDialog } from "@/components/students/add-guardian-dialog";
import { EditStudentDialog } from "@/components/students/edit-student-dialog";
import { ChangeStudentPlacementDialog } from "@/components/students/change-student-placement-dialog";
import { Button } from "@/components/ui/button";
import { getStudentGuardians } from "@/lib/api/guardians";
import { getStudent, getStudentSetup } from "@/lib/api/students";

type StudentProfilePageProps = {
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

export default async function StudentProfilePage({
  params,
}: StudentProfilePageProps) {
  const { tenantSlug, id } =
    await params;

  const [student, guardians, setup] =
    await Promise.all([
      getStudent(id),
      getStudentGuardians(id),
      getStudentSetup(),
    ]);

  if (!student) {
    notFound();
  }

  const name = fullName(
    student.firstName,
    student.middleName,
    student.lastName
  );

  const currentEnrollment =
    student.enrollments.find(
      (enrollment) =>
        enrollment.isCurrent
    );

  return (
    <div className="space-y-6">
      <div>
        <Button
          variant="ghost"
          size="sm"
          nativeButton={false}
          render={
            <Link
              href={`/app/${tenantSlug}/students`}
            />
          }
        >
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to Students
        </Button>
      </div>

      <section className="rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex flex-col gap-5 md:flex-row md:items-center md:justify-between">
          <div className="flex items-center gap-4">
            <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-tenant-primary text-xl font-bold text-black">
              {student.firstName
                .charAt(0)
                .toUpperCase()}
              {student.lastName
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
                    student.isActive
                      ? "rounded-full bg-green-50 px-2.5 py-1 text-xs font-medium text-green-700"
                      : "rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-600"
                  }
                >
                  {student.status}
                </span>
              </div>

              <p className="mt-1 font-mono text-sm text-muted-foreground">
                {student.admissionNumber}
              </p>
            </div>
          </div>

          <div className="flex flex-col gap-3 lg:flex-row lg:items-center">
            <EditStudentDialog
              student={student}
            />

            <ChangeStudentPlacementDialog
              studentId={student.id}
              setup={setup}
              currentEnrollment={currentEnrollment}
            />

            {currentEnrollment && (
              <div className="rounded-lg border bg-muted/30 px-5 py-3">
              <div className="text-xs text-muted-foreground">
                Current Placement
              </div>

              <div className="mt-1 font-semibold">
                {
                  currentEnrollment
                    .academicLevelName
                }{" "}
                ·{" "}
                {
                  currentEnrollment
                    .classGroupName
                }
              </div>

              <div className="mt-1 text-xs text-muted-foreground">
                {
                  currentEnrollment
                    .academicSessionName
                }
              </div>
            </div>
            )}
          </div>
        </div>
      </section>

      <div className="grid gap-6 xl:grid-cols-[1fr_1.4fr]">
        <div className="space-y-6">
          <section className="rounded-xl border bg-card shadow-sm">
            <div className="border-b px-5 py-4">
              <div className="flex items-center gap-2">
                <UserRound className="h-5 w-5" />

                <h2 className="font-semibold">
                  Student Information
                </h2>
              </div>
            </div>

            <div className="divide-y">
              <div className="grid grid-cols-2 gap-4 px-5 py-4">
                <div>
                  <div className="text-xs text-muted-foreground">
                    Gender
                  </div>

                  <div className="mt-1 text-sm font-medium">
                    {student.gender}
                  </div>
                </div>

                <div>
                  <div className="text-xs text-muted-foreground">
                    Date of Birth
                  </div>

                  <div className="mt-1 text-sm font-medium">
                    {formatDate(
                      student.dateOfBirth
                    )}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4 px-5 py-4">
                <div>
                  <div className="text-xs text-muted-foreground">
                    Admission Date
                  </div>

                  <div className="mt-1 text-sm font-medium">
                    {formatDate(
                      student.admissionDate
                    )}
                  </div>
                </div>

                <div>
                  <div className="text-xs text-muted-foreground">
                    Admission Number
                  </div>

                  <div className="mt-1 font-mono text-sm font-medium">
                    {
                      student.admissionNumber
                    }
                  </div>
                </div>
              </div>
            </div>
          </section>

          <section className="rounded-xl border bg-card shadow-sm">
            <div className="border-b px-5 py-4">
              <h2 className="font-semibold">
                Contact Information
              </h2>
            </div>

            <div className="space-y-4 p-5">
              <div className="flex items-center gap-3">
                <Mail className="h-4 w-4 text-muted-foreground" />

                <div>
                  <div className="text-xs text-muted-foreground">
                    Email
                  </div>

                  <div className="text-sm">
                    {student.email ??
                      "Not provided"}
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-3">
                <Phone className="h-4 w-4 text-muted-foreground" />

                <div>
                  <div className="text-xs text-muted-foreground">
                    Phone
                  </div>

                  <div className="text-sm">
                    {student.phone ??
                      "Not provided"}
                  </div>
                </div>
              </div>
            </div>
          </section>
        </div>

        <div className="space-y-6">
          <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
            <div className="flex items-center justify-between gap-4 border-b px-5 py-4">
              <div className="flex items-center gap-3">
                <UsersRound className="h-5 w-5" />

                <div>
                  <h2 className="font-semibold">
                    Guardians & Parents
                  </h2>

                  <p className="mt-0.5 text-xs text-muted-foreground">
                    Parent, guardian and
                    emergency contact records.
                  </p>
                </div>
              </div>

              <AddGuardianDialog
                studentId={student.id}
              />
            </div>

            {guardians.length > 0 ? (
              <div className="divide-y">
                {guardians.map(
                  (link) => {
                    const guardian =
                      link.guardian;

                    return (
                      <div
                        key={link.id}
                        className="p-5"
                      >
                        <div className="flex items-start gap-4">
                          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-muted">
                            <UserRound className="h-5 w-5" />
                          </div>

                          <div className="min-w-0 flex-1">
                            <div className="flex flex-wrap items-center gap-2">
                              <div className="font-semibold">
                                {fullName(
                                  guardian.firstName,
                                  guardian.middleName,
                                  guardian.lastName
                                )}
                              </div>

                              <span className="rounded-full bg-muted px-2 py-0.5 text-xs">
                                {
                                  link.relationship
                                }
                              </span>

                              {link.isPrimaryContact && (
                                <span className="rounded-full bg-yellow-50 px-2 py-0.5 text-xs font-medium text-yellow-800">
                                  Primary Contact
                                </span>
                              )}
                            </div>

                            <div className="mt-3 grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
                              <div className="flex items-center gap-2">
                                <Phone className="h-4 w-4" />

                                <span>
                                  {
                                    guardian.phone
                                  }
                                </span>
                              </div>

                              {guardian.email && (
                                <div className="flex items-center gap-2">
                                  <Mail className="h-4 w-4" />

                                  <span className="truncate">
                                    {
                                      guardian.email
                                    }
                                  </span>
                                </div>
                              )}

                              {guardian.address && (
                                <div className="flex items-start gap-2 sm:col-span-2">
                                  <MapPin className="mt-0.5 h-4 w-4 shrink-0" />

                                  <span>
                                    {
                                      guardian.address
                                    }
                                  </span>
                                </div>
                              )}
                            </div>

                            <div className="mt-4 flex flex-wrap gap-2">
                              {link.isEmergencyContact && (
                                <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-2.5 py-1 text-xs text-red-700">
                                  <ShieldCheck className="h-3 w-3" />
                                  Emergency
                                </span>
                              )}

                              {link.canPickUpStudent && (
                                <span className="rounded-full bg-green-50 px-2.5 py-1 text-xs text-green-700">
                                  Pickup Authorised
                                </span>
                              )}

                              {link.livesWithStudent && (
                                <span className="rounded-full bg-blue-50 px-2.5 py-1 text-xs text-blue-700">
                                  Lives With Student
                                </span>
                              )}

                              {guardian.occupation && (
                                <span className="rounded-full bg-muted px-2.5 py-1 text-xs">
                                  {
                                    guardian.occupation
                                  }
                                </span>
                              )}
                            </div>
                          </div>
                        </div>
                      </div>
                    );
                  }
                )}
              </div>
            ) : (
              <div className="px-5 py-12 text-center">
                <UsersRound className="mx-auto h-9 w-9 text-muted-foreground" />

                <h3 className="mt-3 font-medium">
                  No guardians yet
                </h3>

                <p className="mx-auto mt-1 max-w-sm text-sm text-muted-foreground">
                  Add a parent or guardian
                  responsible for this student.
                </p>

                <div className="mt-5 flex justify-center">
                  <AddGuardianDialog
                    studentId={student.id}
                  />
                </div>
              </div>
            )}
          </section>

          <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
            <div className="border-b px-5 py-4">
              <div className="flex items-center gap-2">
                <GraduationCap className="h-5 w-5" />

                <div>
                  <h2 className="font-semibold">
                    Enrolment History
                  </h2>

                  <p className="mt-0.5 text-xs text-muted-foreground">
                    Academic placement across
                    school sessions.
                  </p>
                </div>
              </div>
            </div>

            {student.enrollments.length >
            0 ? (
              <div className="divide-y">
                {student.enrollments.map(
                  (enrollment) => (
                    <div
                      key={enrollment.id}
                      className="p-5"
                    >
                      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                        <div>
                          <div className="flex items-center gap-2">
                            <CalendarDays className="h-4 w-4 text-muted-foreground" />

                            <span className="font-semibold">
                              {
                                enrollment
                                  .academicSessionName
                              }
                            </span>

                            {enrollment.isCurrent && (
                              <span className="rounded-full bg-green-50 px-2 py-0.5 text-xs font-medium text-green-700">
                                Current
                              </span>
                            )}
                          </div>

                          <div className="mt-4 grid gap-4 sm:grid-cols-2">
                            <div className="flex items-center gap-3">
                              <BookOpen className="h-4 w-4 text-muted-foreground" />

                              <div>
                                <div className="text-xs text-muted-foreground">
                                  Level
                                </div>

                                <div className="text-sm font-medium">
                                  {
                                    enrollment
                                      .academicLevelName
                                  }
                                </div>
                              </div>
                            </div>

                            <div className="flex items-center gap-3">
                              <GraduationCap className="h-4 w-4 text-muted-foreground" />

                              <div>
                                <div className="text-xs text-muted-foreground">
                                  Class
                                </div>

                                <div className="text-sm font-medium">
                                  {
                                    enrollment
                                      .classGroupName
                                  }
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>

                        <div className="text-xs text-muted-foreground">
                          Enrolled{" "}
                          {formatDate(
                            enrollment.enrollmentDate
                          )}
                        </div>
                      </div>
                    </div>
                  )
                )}
              </div>
            ) : (
              <div className="p-12 text-center text-sm text-muted-foreground">
                No enrolment records found.
              </div>
            )}
          </section>
        </div>
      </div>
    </div>
  );
}
