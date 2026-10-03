import {
  BriefcaseBusiness,
  GraduationCap,
  UserCheck,
  UsersRound,
} from "lucide-react";

import { CreateStaffDialog } from "@/components/staff/create-staff-dialog";
import { StaffDirectory } from "@/components/staff/staff-directory";
import { getStaff } from "@/lib/api/staff";

type StaffPageProps = {
  params: Promise<{
    tenantSlug: string;
  }>;
};

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

      <StaffDirectory staff={staff} tenantSlug={tenantSlug} />
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
