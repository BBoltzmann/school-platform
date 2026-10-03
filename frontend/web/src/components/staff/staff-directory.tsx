"use client";

import Link from "next/link";
import { Search, UsersRound } from "lucide-react";
import { useMemo, useState } from "react";

import { Input } from "@/components/ui/input";
import type { StaffMember } from "@/types/staff";

function fullName(member: StaffMember) {
  return [member.firstName, member.middleName, member.lastName].filter(Boolean).join(" ");
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short", year: "numeric" }).format(new Date(`${value}T00:00:00`));
}

export function StaffDirectory({ staff, tenantSlug }: { staff: StaffMember[]; tenantSlug: string }) {
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("all");
  const [employment, setEmployment] = useState("all");
  const [status, setStatus] = useState("all");
  const visibleStaff = useMemo(() => {
    const query = search.trim().toLowerCase();
    return staff.filter((member) => {
      const categoryName = member.isTeachingStaff ? "Teaching" : "Non-Teaching";
      const searchable = [fullName(member), member.staffNumber, member.jobTitle, member.department, member.email, member.phone, categoryName, member.employmentType].filter(Boolean).join(" ").toLowerCase();
      return (!query || searchable.includes(query)) &&
        (category === "all" || categoryName === category) &&
        (employment === "all" || member.employmentType === employment) &&
        (status === "all" || (status === "active" ? member.isActive : !member.isActive));
    }).sort((left, right) => {
      const categoryCompare = Number(!left.isTeachingStaff) - Number(!right.isTeachingStaff);
      return categoryCompare || fullName(left).localeCompare(fullName(right));
    });
  }, [category, employment, search, staff, status]);
  const employmentTypes = Array.from(new Set(staff.map((member) => member.employmentType).filter(Boolean))).sort();

  return (
    <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className="border-b px-5 py-4">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div><h2 className="font-semibold">Staff Directory</h2><p className="mt-1 text-xs text-muted-foreground">Current and historical staff records.</p></div>
          <div className="grid w-full gap-3 sm:grid-cols-[minmax(220px,1fr)_150px_150px_130px] lg:max-w-4xl">
            <label className="relative block"><span className="sr-only">Search staff</span><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" /><Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search name, number, role or contact" className="pl-9" /></label>
            <label className="text-xs font-medium text-muted-foreground">Category<select value={category} onChange={(event) => setCategory(event.target.value)} className="mt-1 h-10 w-full rounded-md border bg-background px-3 text-sm font-normal text-foreground"><option value="all">All Staff</option><option value="Teaching">Teaching</option><option value="Non-Teaching">Non-Teaching</option></select></label>
            <label className="text-xs font-medium text-muted-foreground">Employment<select value={employment} onChange={(event) => setEmployment(event.target.value)} className="mt-1 h-10 w-full rounded-md border bg-background px-3 text-sm font-normal text-foreground"><option value="all">All</option>{employmentTypes.map((type) => <option key={type} value={type}>{type}</option>)}</select></label>
            <label className="text-xs font-medium text-muted-foreground">Status<select value={status} onChange={(event) => setStatus(event.target.value)} className="mt-1 h-10 w-full rounded-md border bg-background px-3 text-sm font-normal text-foreground"><option value="all">All</option><option value="active">Active</option><option value="inactive">Inactive</option></select></label>
          </div>
        </div>
      </div>
      {visibleStaff.length === 0 ? <div className="px-5 py-16 text-center"><UsersRound className="mx-auto h-10 w-10 text-muted-foreground" /><h3 className="mt-4 font-semibold">No matching staff</h3><p className="mt-1 text-sm text-muted-foreground">Try a different search or filter.</p></div> : <div className="overflow-x-auto"><table className="w-full min-w-[950px] text-left text-sm"><thead className="border-b bg-muted/40 text-xs text-muted-foreground"><tr><th className="px-5 py-3 font-medium">Staff Member</th><th className="px-5 py-3 font-medium">Staff Number</th><th className="px-5 py-3 font-medium">Role</th><th className="px-5 py-3 font-medium">Department</th><th className="px-5 py-3 font-medium">Category</th><th className="px-5 py-3 font-medium">Employment</th><th className="px-5 py-3 font-medium">Started</th><th className="px-5 py-3 font-medium">Status</th><th className="px-5 py-3 font-medium">Action</th></tr></thead><tbody>
        {visibleStaff.map((member, index) => <tr key={member.id} className={`border-b last:border-0 ${index % 2 ? "bg-muted/20" : "bg-background"}`}><td className="px-5 py-2.5"><Link href={`/app/${tenantSlug}/staff/${member.id}`} className="font-medium hover:underline">{fullName(member)}</Link><div className="text-xs text-muted-foreground">{member.email ?? member.phone}</div></td><td className="px-5 py-2.5 font-mono text-xs">{member.staffNumber}</td><td className="px-5 py-2.5 font-medium">{member.jobTitle}</td><td className="px-5 py-2.5 text-muted-foreground">{member.department ?? "—"}</td><td className="px-5 py-2.5"><span className="rounded-full bg-muted px-2 py-1 text-xs font-medium">{member.isTeachingStaff ? "Teaching" : "Non-Teaching"}</span></td><td className="px-5 py-2.5"><span className="rounded-full border px-2 py-1 text-xs">{member.employmentType}</span></td><td className="px-5 py-2.5 text-muted-foreground">{formatDate(member.employmentDate)}</td><td className="px-5 py-2.5"><span className={member.isActive ? "rounded-full bg-green-50 px-2 py-1 text-xs font-medium text-green-700" : "rounded-full bg-neutral-100 px-2 py-1 text-xs font-medium text-neutral-600"}>{member.status}</span></td><td className="px-5 py-2.5"><Link href={`/app/${tenantSlug}/staff/${member.id}`} className="text-sm font-medium text-tenant-primary hover:underline">View</Link></td></tr>)}
      </tbody></table></div>}
    </section>
  );
}
