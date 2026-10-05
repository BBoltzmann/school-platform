import Link from "next/link";
import {
  CheckCircle2,
  ClipboardList,
  Clock3,
  FileText,
  UserRound,
} from "lucide-react";

import { AdmissionActions } from "@/components/admissions/admission-actions";
import { CreateAdmissionDialog } from "@/components/admissions/create-admission-dialog";
import {
  getAdmissions,
  getAdmissionSetup,
} from "@/lib/api/admissions";

type AdmissionsPageProps = {
  params: Promise<{
    tenantSlug: string;
  }>;
};

function applicantName(
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

function formatDateTime(
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
    new Date(value)
  );
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

export default async function AdmissionsPage({
  params,
}: AdmissionsPageProps) {
  const { tenantSlug } =
    await params;

  const [applications, setup] =
    await Promise.all([
      getAdmissions(),
      getAdmissionSetup(),
    ]);

  const submitted =
    applications.filter(
      (application) =>
        application.status ===
        "Submitted"
    ).length;

  const underReview =
    applications.filter(
      (application) =>
        application.status ===
        "Under Review"
    ).length;

  const approved =
    applications.filter(
      (application) =>
        application.status ===
        "Approved"
    ).length;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            Admissions
          </h1>

          <p className="mt-1 text-sm text-muted-foreground">
            Manage applications, admission
            decisions and conversion to
            permanent student records.
          </p>
        </div>

        <CreateAdmissionDialog
          setup={setup}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          label="Total Applications"
          value={applications.length}
          icon={
            <ClipboardList className="h-5 w-5 text-muted-foreground" />
          }
        />

        <MetricCard
          label="Submitted"
          value={submitted}
          icon={
            <FileText className="h-5 w-5 text-muted-foreground" />
          }
        />

        <MetricCard
          label="Under Review"
          value={underReview}
          icon={
            <Clock3 className="h-5 w-5 text-muted-foreground" />
          }
        />

        <MetricCard
          label="Approved"
          value={approved}
          icon={
            <CheckCircle2 className="h-5 w-5 text-muted-foreground" />
          }
        />
      </div>

      <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
        <div className="border-b px-5 py-4">
          <h2 className="font-semibold">
            Admission Applications
          </h2>

          <p className="mt-1 text-xs text-muted-foreground">
            Applications for{" "}
            {setup.currentSession?.name ??
              "the current academic session"}.
          </p>
        </div>

        {applications.length === 0 ? (
          <div className="px-5 py-16 text-center">
            <UserRound className="mx-auto h-10 w-10 text-muted-foreground" />

            <h3 className="mt-4 font-semibold">
              No applications yet
            </h3>

            <p className="mx-auto mt-1 max-w-md text-sm text-muted-foreground">
              Create the first admission
              application to begin the
              admissions workflow.
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1850px] text-sm">
              <thead className="border-b bg-muted/40 text-left">
                <tr>
                  <th className="px-5 py-3 font-medium">
                    Applicant
                  </th>

                  <th className="px-5 py-3 font-medium">
                    DOB / Religion
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Last School
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Admission Sought
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Guardian
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Guardian Address
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Office Address
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Status
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Submitted
                  </th>

                  <th className="px-5 py-3 font-medium">
                    Actions
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y">
                {applications.map(
                  (application) => (
                    <tr
                      key={application.id}
                      className="align-top"
                    >
                      <td className="px-5 py-4">
                        <Link
                          href={`/app/${tenantSlug}/admissions/${application.id}`}
                          className="font-medium hover:underline"
                        >
                          {applicantName(
                            application.firstName,
                            application.middleName,
                            application.lastName
                          )}
                        </Link>

                        <div className="mt-1 font-mono text-xs text-muted-foreground">
                          {
                            application.applicationNumber
                          }
                        </div>

                        <div className="mt-1 text-xs text-muted-foreground">
                          {
                            application.gender
                          }
                        </div>
                      </td>

                      <td className="px-5 py-4">
                        <div className="font-medium">
                          {formatDate(
                            application.dateOfBirth
                          )}
                        </div>

                        <div className="mt-1 text-xs text-muted-foreground">
                          {application.religion ??
                            "Not recorded"}
                        </div>
                      </td>

                      <td className="max-w-64 px-5 py-4">
                        <div className="font-medium">
                          {application.previousSchoolName ??
                            "No previous school"}
                        </div>

                        <div className="mt-1 text-xs text-muted-foreground">
                          Present class:{" "}
                          {application.presentClass ??
                            "—"}
                        </div>
                      </td>

                      <td className="px-5 py-4">
                        <div className="font-medium">
                          {
                            application.academicLevelName
                          }
                        </div>

                        <div className="mt-1 text-xs text-muted-foreground">
                          {
                            application.academicSessionName
                          }
                        </div>
                      </td>

                      <td className="max-w-64 px-5 py-4">
                        <div className="font-medium">
                          {application.guardianName ??
                            "Not recorded"}
                        </div>

                        <div className="mt-1 text-xs text-muted-foreground">
                          {application.guardianPhone ??
                            "No telephone"}
                        </div>

                        <div className="mt-1 text-xs text-muted-foreground">
                          {application.guardianOccupation ??
                            "Occupation not recorded"}
                        </div>

                        {application.email && (
                          <div className="mt-1 text-xs text-muted-foreground">
                            {application.email}
                          </div>
                        )}
                      </td>

                      <td className="max-w-72 px-5 py-4 text-xs text-muted-foreground">
                        {application.guardianHomeAddress ??
                          "Not recorded"}
                      </td>

                      <td className="max-w-72 px-5 py-4 text-xs text-muted-foreground">
                        {application.guardianOfficeAddress ??
                          "Not recorded"}
                      </td>

                      <td className="px-5 py-4">
                        <span
                          className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${statusClass(
                            application.status
                          )}`}
                        >
                          {
                            application.status
                          }
                        </span>

                        {application.decisionNote && (
                          <div className="mt-2 max-w-52 text-xs text-muted-foreground">
                            {
                              application.decisionNote
                            }
                          </div>
                        )}
                      </td>

                      <td className="whitespace-nowrap px-5 py-4 text-muted-foreground">
                        {formatDateTime(
                          application.submittedAtUtc
                        )}
                      </td>

                      <td className="min-w-72 px-5 py-4">
                        {application.status ===
                          "Approved" &&
                        application.approvedStudentId ? (
                          <Link
                            href={`/app/${tenantSlug}/students/${application.approvedStudentId}`}
                            className="inline-flex h-9 items-center rounded-md border px-3 text-sm font-medium transition-colors hover:bg-muted"
                          >
                            View Student
                          </Link>
                        ) : (
                          <AdmissionActions
                            application={
                              application
                            }
                            setup={setup}
                          />
                        )}
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

function MetricCard({
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
