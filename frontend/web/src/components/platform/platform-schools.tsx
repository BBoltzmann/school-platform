"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";

type School = { tenantId: string; name: string; slug: string; isActive: boolean; createdAtUtc: string; students: number; staff: number; guardians: number; activatedAccounts: number; activeAdministrators: number };

export function PlatformSchools() {
  const [schools, setSchools] = useState<School[]>([]);
  const [query, setQuery] = useState("");
  const [error, setError] = useState("");
  useEffect(() => {
    fetch("/api/platform-admin/schools").then(async response => {
      if (!response.ok) throw new Error("Unable to load schools.");
      setSchools(await response.json());
    }).catch(reason => setError(reason instanceof Error ? reason.message : "Unable to load schools."));
  }, []);
  const filteredSchools = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    return normalized ? schools.filter(school => `${school.name} ${school.slug}`.toLowerCase().includes(normalized)) : schools;
  }, [query, schools]);
  return <main className="mx-auto max-w-7xl space-y-6 p-8">
    <header className="flex flex-wrap items-center justify-between gap-4"><div><p className="text-sm text-muted-foreground">Control plane</p><h1 className="text-3xl font-semibold">Schools</h1></div><Link href="/super-admin/schools/new"><Button>Add School</Button></Link></header>
    <div><label className="sr-only" htmlFor="school-search">Search schools</label><input id="school-search" value={query} onChange={event => setQuery(event.target.value)} placeholder="Search schools" className="w-full max-w-md rounded-md border bg-background px-3 py-2 text-sm" /></div>
    {error && <p className="text-sm text-red-700">{error}</p>}
    <section className="overflow-x-auto rounded-xl border bg-card"><table className="w-full text-left text-sm"><thead className="border-b bg-muted/30 text-xs uppercase tracking-wide text-muted-foreground"><tr><th className="p-4">School</th><th className="p-4">Status</th><th className="p-4">Students</th><th className="p-4">Staff</th><th className="p-4">Guardians</th><th className="p-4">Accounts</th><th className="p-4">Administrators</th><th className="p-4">Created</th><th className="p-4" /></tr></thead><tbody className="divide-y">
      {filteredSchools.map(school => <tr key={school.tenantId} className="hover:bg-muted/20"><td className="p-4"><div className="font-medium">{school.name}</div><div className="text-xs text-muted-foreground">{school.slug}</div></td><td className="p-4">{school.isActive ? "Active" : "Suspended"}</td><td className="p-4">{school.students}</td><td className="p-4">{school.staff}</td><td className="p-4">{school.guardians}</td><td className="p-4">{school.activatedAccounts}</td><td className="p-4">{school.activeAdministrators}</td><td className="p-4 whitespace-nowrap">{new Date(school.createdAtUtc).toLocaleDateString()}</td><td className="p-4"><Link className="underline" href={`/super-admin/schools/${school.tenantId}`}>View</Link></td></tr>)}
      {filteredSchools.length === 0 && <tr><td className="p-8 text-center text-muted-foreground" colSpan={9}>No schools match this search.</td></tr>}
    </tbody></table></section>
  </main>;
}
