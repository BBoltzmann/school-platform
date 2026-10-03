"use client";

import Link from "next/link";
import { Mail, Phone, Search, UsersRound } from "lucide-react";
import { useMemo, useState } from "react";

import { Input } from "@/components/ui/input";
import type { Student, StudentSetup } from "@/types/students";

function fullName(student: Student) {
  return [student.firstName, student.middleName, student.lastName]
    .filter(Boolean)
    .join(" ");
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(new Date(`${value}T00:00:00`));
}

export function StudentsDirectory({
  students,
  setup,
  tenantSlug,
}: {
  students: Student[];
  setup: StudentSetup;
  tenantSlug: string;
}) {
  const [search, setSearch] = useState("");
  const [classId, setClassId] = useState("all");
  const [sort, setSort] = useState("class");
  const classOrder = useMemo(
    () => new Map(setup.classes.map((item, index) => [item.id, index])),
    [setup.classes]
  );

  const visibleStudents = useMemo(() => {
    const query = search.trim().toLowerCase();
    return students
      .filter((student) => {
        const enrollment = student.currentEnrollment;
        const searchable = [
          fullName(student),
          student.admissionNumber,
          enrollment?.classGroupName,
          enrollment?.academicLevelName,
        ].filter(Boolean).join(" ").toLowerCase();
        return (!query || searchable.includes(query)) &&
          (classId === "all" || enrollment?.classGroupId === classId);
      })
      .sort((left, right) => {
        const leftName = fullName(left).toLowerCase();
        const rightName = fullName(right).toLowerCase();
        if (sort === "name") return leftName.localeCompare(rightName);
        if (sort === "admission") {
          return left.admissionNumber.localeCompare(right.admissionNumber);
        }
        const leftClass = left.currentEnrollment;
        const rightClass = right.currentEnrollment;
        const leftOrder = leftClass ? (classOrder.get(leftClass.classGroupId) ?? Number.MAX_SAFE_INTEGER) : Number.MAX_SAFE_INTEGER;
        const rightOrder = rightClass ? (classOrder.get(rightClass.classGroupId) ?? Number.MAX_SAFE_INTEGER) : Number.MAX_SAFE_INTEGER;
        return leftOrder - rightOrder || leftName.localeCompare(rightName);
      });
  }, [classId, classOrder, search, sort, students]);

  return (
    <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className="flex flex-col gap-4 border-b p-5 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h2 className="font-semibold">Student Directory</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            {setup.currentSession ? `Current session: ${setup.currentSession.name}` : "No current academic session"}
          </p>
        </div>
        <div className="grid w-full gap-3 sm:grid-cols-[minmax(220px,1fr)_180px_170px] lg:max-w-3xl">
          <label className="relative block">
            <span className="sr-only">Search students</span>
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search name, admission no. or class" className="pl-9" />
          </label>
          <label className="text-xs font-medium text-muted-foreground">
            Class
            <select value={classId} onChange={(event) => setClassId(event.target.value)} className="mt-1 h-10 w-full rounded-md border bg-background px-3 text-sm font-normal text-foreground">
              <option value="all">All Classes</option>
              {setup.classes.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </label>
          <label className="text-xs font-medium text-muted-foreground">
            Sort
            <select value={sort} onChange={(event) => setSort(event.target.value)} className="mt-1 h-10 w-full rounded-md border bg-background px-3 text-sm font-normal text-foreground">
              <option value="class">Class then Name</option>
              <option value="name">Name A–Z</option>
              <option value="admission">Admission Number</option>
            </select>
          </label>
        </div>
      </div>

      {visibleStudents.length > 0 ? (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[900px] text-left text-sm">
            <thead className="border-b bg-muted/40 text-xs text-muted-foreground"><tr>
              <th className="px-5 py-3 font-medium">Student</th><th className="px-3 py-3 font-medium">Admission No.</th><th className="px-3 py-3 font-medium">Level</th><th className="px-3 py-3 font-medium">Class</th><th className="px-3 py-3 font-medium">Contact</th><th className="px-3 py-3 font-medium">Admission Date</th><th className="px-3 py-3 font-medium">Status</th>
            </tr></thead>
            <tbody>
              {visibleStudents.map((student, index) => <tr key={student.id} className={`border-b last:border-0 ${index % 2 ? "bg-muted/20" : "bg-background"}`}>
                <td className="px-5 py-3"><div className="flex items-center gap-3"><div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-tenant-primary font-semibold text-black">{student.firstName.charAt(0)}{student.lastName.charAt(0)}</div><div><Link href={`/app/${tenantSlug}/students/${student.id}`} className="font-semibold hover:underline">{fullName(student)}</Link><div className="mt-0.5 text-xs text-muted-foreground">{student.gender} · DOB {formatDate(student.dateOfBirth)}</div></div></div></td>
                <td className="px-3 py-3 font-mono text-xs">{student.admissionNumber}</td><td className="px-3 py-3">{student.currentEnrollment?.academicLevelName ?? "—"}</td><td className="px-3 py-3">{student.currentEnrollment?.classGroupName ?? "—"}</td>
                <td className="px-3 py-3"><div className="space-y-1 text-xs text-muted-foreground">{student.email && <div className="flex items-center gap-1.5"><Mail className="h-3.5 w-3.5" />{student.email}</div>}{student.phone && <div className="flex items-center gap-1.5"><Phone className="h-3.5 w-3.5" />{student.phone}</div>}{!student.email && !student.phone && <span>—</span>}</div></td>
                <td className="px-3 py-3 text-muted-foreground">{formatDate(student.admissionDate)}</td><td className="px-3 py-3"><span className={student.isActive ? "inline-flex rounded-full bg-green-50 px-2.5 py-1 text-xs font-medium text-green-700" : "inline-flex rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium text-neutral-600"}>{student.status}</span></td>
              </tr>)}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="px-5 py-16 text-center"><UsersRound className="mx-auto h-10 w-10 text-muted-foreground" /><h3 className="mt-4 font-semibold">No matching students</h3><p className="mt-1 text-sm text-muted-foreground">Try a different search or class filter.</p></div>
      )}
    </section>
  );
}
