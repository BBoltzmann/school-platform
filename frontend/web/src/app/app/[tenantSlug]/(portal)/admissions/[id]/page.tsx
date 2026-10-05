import Link from "next/link";
import { notFound } from "next/navigation";
import {
  ArrowLeft,
  BriefcaseBusiness,
  CalendarDays,
  CheckCircle2,
  GraduationCap,
  Home,
  Mail,
  MapPin,
  Phone,
  School,
  UserRound,
  UsersRound,
} from "lucide-react";

import { AdmissionActions } from "@/components/admissions/admission-actions";
import { AdmissionDocumentsCard } from "@/components/admissions/admission-documents-card";
import {
  getAdmission,
  getAdmissionDocuments,
  getAdmissionRequirements,
  getAdmissionSetup,
} from "@/lib/api/admissions";

type AdmissionProfilePageProps = {
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
  value: string
) {
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

function formatDateTime(
  value: string | null
) {
  if (!value) {
    return "—";
  }

  return new Intl.DateTimeFormat(
    "en-GB",
    {
      day: "numeric",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    }
  ).format(new Date(value));
}

function statusClass(
  status: string
) {
  switch (status) {
    case "Approved":
      return "bg-green-50 text-green-700";

    case "Rejected":
      return "bg-red-50 text-red-700";

    case "Under Review":
      return "bg-blue-50 text-blue-700";

    case "Waitlisted":
      return "bg-amber-50 text-amber-700";

    default:
      return "bg-neutral-100 text-neutral-700";
  }
}

export default async function AdmissionProfilePage({
  params,
}: AdmissionProfilePageProps) {
  const { tenantSlug, id } =
    await params;

  const [application, setup] =
    await Promise.all([
      getAdmission(id),
      getAdmissionSetup(),
    ]);

  if (!application) {
    notFound();
  }

  const [
    documents,
    requirements,
  ] = await Promise.all([
    getAdmissionDocuments(id),
    getAdmissionRequirements(id),
  ]);

  const name = fullName(
    application.firstName,
    application.middleName,
    application.lastName
  );

  return (
    <div className="space-y-6">
      <Link
        href={`/app/${tenantSlug}/admissions`}
        className="inline-flex items-center gap-2 text-sm font-medium hover:underline"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to Admissions
      </Link>

      <section className="rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex items-start gap-4">
            <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full bg-tenant-primary text-xl font-bold text-black">
              {application.firstName
                .charAt(0)
                .toUpperCase()}

              {application.lastName
                .charAt(0)
                .toUpperCase()}
            </div>

            <div>
              <div className="flex flex-wrap items-center gap-3">
                <h1 className="text-2xl font-bold tracking-tight">
                  {name}
                </h1>

                <span
                  className={`rounded-full px-2.5 py-1 text-xs font-medium ${statusClass(
                    application.status
                  )}`}
                >
                  {application.status}
                </span>
              </div>

              <div className="mt-2 font-mono text-sm text-muted-foreground">
                {application.applicationNumber}
              </div>

              <div className="mt-1 text-sm text-muted-foreground">
                Submitted{" "}
                {formatDateTime(
                  application.submittedAtUtc
                )}
              </div>
            </div>
          </div>

          <div className="flex flex-wrap gap-2">
            {application.status ===
              "Approved" &&
            application.approvedStudentId ? (
              <Link
                href={`/app/${tenantSlug}/students/${application.approvedStudentId}`}
                className="inline-flex h-10 items-center rounded-md bg-tenant-primary px-4 text-sm font-medium text-black transition-opacity hover:opacity-90"
              >
                <CheckCircle2 className="mr-2 h-4 w-4" />
                View Student
              </Link>
            ) : (
              <AdmissionActions
                application={application}
                setup={setup}
              />
            )}
          </div>
        </div>
      </section>

      <div className="grid gap-6 xl:grid-cols-2">
        <div className="space-y-6">
          <section className="rounded-xl border bg-card shadow-sm">
            <div className="flex items-center gap-2 border-b px-5 py-4">
              <UserRound className="h-5 w-5" />

              <h2 className="font-semibold">
                Applicant Information
              </h2>
            </div>

            <div className="grid gap-5 p-5 sm:grid-cols-2">
              <Info
                label="Full Name"
                value={name}
              />

              <Info
                label="Date of Birth"
                value={formatDate(
                  application.dateOfBirth
                )}
              />

              <Info
                label="Gender"
                value={application.gender}
              />

              <Info
                label="Religion"
                value={
                  application.religion ??
                  "Not recorded"
                }
              />

              <Info
                label="Applicant Phone"
                value={
                  application.phone ??
                  "Not provided"
                }
              />
            </div>
          </section>

          <section className="rounded-xl border bg-card shadow-sm">
            <div className="flex items-center gap-2 border-b px-5 py-4">
              <School className="h-5 w-5" />

              <h2 className="font-semibold">
                School Information
              </h2>
            </div>

            <div className="grid gap-5 p-5 sm:grid-cols-2">
              <Info
                label="Present / Last School"
                value={
                  application.previousSchoolName ??
                  "No previous school recorded"
                }
              />

              <Info
                label="Present Class"
                value={
                  application.presentClass ??
                  "Not recorded"
                }
              />

              <Info
                label="Class Admission Sought"
                value={
                  application.academicLevelName
                }
              />

              <Info
                label="Academic Session"
                value={
                  application.academicSessionName
                }
              />
            </div>
          </section>

          <AdmissionDocumentsCard
            applicationId={application.id}
            documents={documents}
            requirements={requirements}
          />
        </div>

        <div className="space-y-6">
          <section className="rounded-xl border bg-card shadow-sm">
            <div className="flex items-center gap-2 border-b px-5 py-4">
              <UsersRound className="h-5 w-5" />

              <h2 className="font-semibold">
                Guardian Information
              </h2>
            </div>

            <div className="space-y-5 p-5">
              <div className="grid gap-5 sm:grid-cols-2">
                <ContactItem
                  icon={
                    <UserRound className="h-4 w-4" />
                  }
                  label="Guardian Name"
                  value={
                    application.guardianName ??
                    "Not recorded"
                  }
                />

                <ContactItem
                  icon={
                    <Phone className="h-4 w-4" />
                  }
                  label="Telephone"
                  value={
                    application.guardianPhone ??
                    "Not recorded"
                  }
                />

                <ContactItem
                  icon={
                    <BriefcaseBusiness className="h-4 w-4" />
                  }
                  label="Occupation"
                  value={
                    application.guardianOccupation ??
                    "Not recorded"
                  }
                />

                <ContactItem
                  icon={
                    <Mail className="h-4 w-4" />
                  }
                  label="Email"
                  value={
                    application.email ??
                    "Not recorded"
                  }
                />
              </div>

              <ContactItem
                icon={
                  <Home className="h-4 w-4" />
                }
                label="Home Address"
                value={
                  application.guardianHomeAddress ??
                  "Not recorded"
                }
              />

              <ContactItem
                icon={
                  <MapPin className="h-4 w-4" />
                }
                label="Office Address"
                value={
                  application.guardianOfficeAddress ??
                  "Not recorded"
                }
              />
            </div>
          </section>

          <section className="rounded-xl border bg-card shadow-sm">
            <div className="flex items-center gap-2 border-b px-5 py-4">
              <CalendarDays className="h-5 w-5" />

              <h2 className="font-semibold">
                Application History
              </h2>
            </div>

            <div className="space-y-5 p-5">
              <TimelineItem
                title="Application Submitted"
                value={formatDateTime(
                  application.submittedAtUtc
                )}
              />

              <TimelineItem
                title="Reviewed"
                value={formatDateTime(
                  application.reviewedAtUtc
                )}
              />

              <TimelineItem
                title="Decision"
                value={formatDateTime(
                  application.decisionAtUtc
                )}
              />

              {application.decisionNote && (
                <div className="rounded-lg bg-muted/50 p-4">
                  <div className="text-xs font-medium text-muted-foreground">
                    Decision Note
                  </div>

                  <div className="mt-2 text-sm">
                    {
                      application.decisionNote
                    }
                  </div>
                </div>
              )}
            </div>
          </section>

          <section className="rounded-xl border bg-card shadow-sm">
            <div className="flex items-center gap-2 border-b px-5 py-4">
              <GraduationCap className="h-5 w-5" />

              <h2 className="font-semibold">
                Admission Outcome
              </h2>
            </div>

            <div className="p-5">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <div className="text-xs text-muted-foreground">
                    Current Status
                  </div>

                  <div className="mt-1 font-semibold">
                    {application.status}
                  </div>
                </div>

                {application.approvedStudentId && (
                  <Link
                    href={`/app/${tenantSlug}/students/${application.approvedStudentId}`}
                    className="text-sm font-medium underline underline-offset-4"
                  >
                    Open permanent student record
                  </Link>
                )}
              </div>
            </div>
          </section>
        </div>
      </div>
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

function ContactItem({
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

function TimelineItem({
  title,
  value,
}: {
  title: string;
  value: string;
}) {
  return (
    <div className="flex items-start justify-between gap-5 border-b pb-4 last:border-0 last:pb-0">
      <div className="text-sm font-medium">
        {title}
      </div>

      <div className="text-right text-sm text-muted-foreground">
        {value}
      </div>
    </div>
  );
}
