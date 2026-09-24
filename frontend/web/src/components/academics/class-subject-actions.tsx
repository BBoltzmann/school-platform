"use client";

import { useCallback, useEffect, useState } from "react";
import { LoaderCircle } from "lucide-react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import type { ClassGroup, Subject } from "@/types/academics";

type Group = { id: string; displayName: string | null; sharedPeriodsPerWeek: number; members: { classSubjectId: string; subjectId: string; subjectName: string; teacherName: string | null }[] };
type PersistedSubject = { id: string; subjectId: string; subjectName: string; subjectCode: string };

export function ClassSubjectActions({ classGroup, subjects, academicSessionId }: { classGroup: ClassGroup; subjects: Subject[]; academicSessionId: string }) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [groups, setGroups] = useState<Group[]>([]);
  const [persistedSubjects, setPersistedSubjects] = useState<PersistedSubject[]>([]);
  const [groupOpen, setGroupOpen] = useState(false);
  const [editing, setEditing] = useState<Group | null>(null);
  const [groupName, setGroupName] = useState("");
  const [groupSelected, setGroupSelected] = useState<string[]>([]);
  const [groupError, setGroupError] = useState<string | null>(null);
  const [requirements, setRequirements] = useState<Record<string, number>>({});
  const [missingRequirements, setMissingRequirements] = useState<PersistedSubject[]>([]);
  const [savingRequirements, setSavingRequirements] = useState(false);

  const loadAuthoritativeSubjects = useCallback(async () => {
    const response = await fetch(`/api/academics/classes/${classGroup.id}/subjects`, { cache: "no-store" });
    const result = await response.json();
    if (!response.ok) throw new Error(result.error ?? "Unable to load subjects offered for this class.");
    const persisted = (result.subjects ?? []) as PersistedSubject[];
    return persisted.filter(subject => Boolean(subject.id));
  }, [classGroup.id]);

  const loadRequirements = useCallback(async () => {
    const response = await fetch(`/api/timetable/classes/${classGroup.id}/requirements`, { cache: "no-store" });
    const result = await response.json();
    if (!response.ok) throw new Error(result.error ?? "Unable to load weekly period requirements.");
    const next: Record<string, number> = {};
    for (const requirement of result.requirements ?? []) next[requirement.subjectId] = requirement.periodsPerWeek;
    return next;
  }, [classGroup.id]);

  useEffect(() => {
    if (!open && !groupOpen) return;
    let cancelled = false;
    (async () => {
      try {
        const persisted = await loadAuthoritativeSubjects();
        if (!cancelled) {
          setSelected(persisted.map(subject => subject.subjectId));
          setPersistedSubjects(persisted);
        }
        if (groupOpen) {
          const loadedRequirements = await loadRequirements();
          if (!cancelled) setRequirements(loadedRequirements);
        }
        if (academicSessionId) {
          const groupsResponse = await fetch(`/api/academics/classes/${classGroup.id}/parallel-subject-groups?academicSessionId=${academicSessionId}`, { cache: "no-store" });
          if (!groupsResponse.ok) throw new Error((await groupsResponse.json()).error ?? "Unable to load parallel groups.");
          if (!cancelled) setGroups(await groupsResponse.json());
        }
        if (!cancelled && groupOpen) setMissingRequirements([]);
      } catch (exception) {
        if (!cancelled && groupOpen) setGroupError(exception instanceof Error ? exception.message : "Unable to load timetable setup.");
      }
    })();
    return () => { cancelled = true; };
  }, [open, groupOpen, classGroup.id, academicSessionId, loadAuthoritativeSubjects, loadRequirements]);
  async function save() {
    if (saving) return;
    setSaving(true); setError(null);
    try {
      const response = await fetch(`/api/academics/classes/${classGroup.id}/subjects`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ subjectIds: selected }) });
      const result = await response.json();
      if (!response.ok) { setError(result.error ?? "Unable to save subjects offered for this class."); return; }
      const persisted = (result.subjects ?? []) as PersistedSubject[];
      setSelected(persisted.map(subject => subject.subjectId));
      setPersistedSubjects(persisted.filter(subject => Boolean(subject.id)));
      setOpen(false); router.refresh();
    } catch { setError("Unable to save subjects offered. Check your connection and try again."); }
    finally { setSaving(false); }
  }

  async function saveRequirementsAndContinue() {
    setSavingRequirements(true); setGroupError(null);
    try {
      const response = await fetch(`/api/timetable/classes/${classGroup.id}/requirements`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ requirements: persistedSubjects.map(subject => ({ subjectId: subject.subjectId, periodsPerWeek: requirements[subject.subjectId] ?? 0 })).filter(item => item.periodsPerWeek > 0) }) });
      const result = await response.json();
      if (!response.ok) { setGroupError(result.error ?? "Unable to save weekly period requirements."); return; }
      const next: Record<string, number> = {};
      for (const requirement of result.requirements ?? []) next[requirement.subjectId] = requirement.periodsPerWeek;
      setRequirements(next); setMissingRequirements(persistedSubjects.filter(subject => !(next[subject.subjectId] > 0)));
    } catch { setGroupError("Unable to save weekly period requirements. Check your connection and try again."); }
    finally { setSavingRequirements(false); }
  }

  async function saveGroup() {
    setGroupError(null);
    if (groupSelected.length < 2) { setGroupError("Select at least two offered subjects."); return; }
    const missing = persistedSubjects.filter(subject => !(requirements[subject.subjectId] > 0) && groupSelected.includes(subject.id));
    if (missing.length > 0) { setMissingRequirements(missing); setGroupError("Set weekly periods for the highlighted subjects before saving this group."); return; }
    try {
      const response = await fetch(`/api/academics/classes/${classGroup.id}/parallel-subject-groups${editing ? `/${editing.id}` : ""}`, { method: editing ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ academicSessionId, displayName: groupName || null, classSubjectIds: groupSelected }) });
      const result = await response.json(); if (!response.ok) { setGroupError(result.error ?? "Unable to save parallel group."); return; }
      setGroups(current => editing ? current.map(x => x.id === result.id ? result : x) : [...current, result]); setGroupOpen(false);
    } catch { setGroupError("Unable to save parallel group. Check your connection and try again."); }
  }
  async function removeGroup(id: string) { const response = await fetch(`/api/academics/classes/${classGroup.id}/parallel-subject-groups/${id}`, { method: "DELETE" }); if (response.ok) setGroups(current => current.filter(x => x.id !== id)); }
  function beginGroup(group?: Group) { setEditing(group ?? null); setGroupName(group?.displayName ?? ""); setGroupSelected(group?.members.map(x => x.classSubjectId) ?? []); setGroupError(null); setMissingRequirements([]); setGroupOpen(true); }
  const offered = subjects.filter(x => x.isActive);
  return <><DropdownMenu><DropdownMenuTrigger render={<Button variant="outline" size="sm">Subjects</Button>} /><DropdownMenuContent align="end"><DropdownMenuItem onClick={() => setOpen(true)}>Manage Subjects</DropdownMenuItem><DropdownMenuItem onClick={() => beginGroup()}>Manage Parallel Groups</DropdownMenuItem></DropdownMenuContent></DropdownMenu><Dialog open={open} onOpenChange={setOpen}><DialogContent><DialogHeader><DialogTitle>Subjects Offered</DialogTitle><DialogDescription>{classGroup.name} · {classGroup.academicLevelName}</DialogDescription></DialogHeader><div className="max-h-80 space-y-2 overflow-y-auto">{offered.map(subject => <label key={subject.id} className="flex items-center gap-3 rounded-md border px-3 py-2 text-sm"><input type="checkbox" checked={selected.includes(subject.id)} onChange={e => setSelected(current => e.target.checked ? [...current, subject.id] : current.filter(id => id !== subject.id))} />{subject.name} <span className="text-muted-foreground">{subject.code}</span></label>)}</div>{error && <div className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}<div className="flex justify-end"><Button onClick={save} disabled={saving} className="bg-tenant-primary text-black">{saving && <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />}Save Subjects</Button></div></DialogContent></Dialog><Dialog open={groupOpen} onOpenChange={setGroupOpen}><DialogContent><DialogHeader><DialogTitle>Parallel Subject Groups</DialogTitle><DialogDescription>Use this when students take different subjects during the same timetable period.</DialogDescription></DialogHeader><div className="space-y-3"><input className="w-full rounded-md border px-3 py-2 text-sm" placeholder="Display name (optional)" value={groupName} onChange={e => setGroupName(e.target.value)} /><div className="max-h-64 space-y-2 overflow-y-auto">{persistedSubjects.length === 0 ? <p className="text-sm text-muted-foreground">Save Subjects Offered before configuring a parallel group.</p> : persistedSubjects.map(subject => <label key={subject.id} className="flex cursor-pointer items-center gap-3 rounded-md border px-3 py-2 text-sm"><input type="checkbox" checked={groupSelected.includes(subject.id)} onChange={e => setGroupSelected(current => e.target.checked ? [...current, subject.id] : current.filter(id => id !== subject.id))} />{subject.subjectName}</label>)}</div>{missingRequirements.length > 0 && <div className="space-y-2 rounded-md border border-amber-300 bg-amber-50 p-3 text-sm"><p className="font-medium text-amber-900">Set weekly periods before saving this group.</p>{missingRequirements.map(subject => <label key={subject.id} className="flex items-center justify-between gap-3 text-amber-900"><span>{subject.subjectName}</span><input className="w-20 rounded-md border px-2 py-1" type="number" min={1} max={50} value={requirements[subject.subjectId] || ""} onChange={event => setRequirements(current => ({ ...current, [subject.subjectId]: Number(event.target.value) || 0 }))} aria-label={`${subject.subjectName} periods per week`} /></label>)}<Button size="sm" variant="outline" onClick={saveRequirementsAndContinue} disabled={savingRequirements}>{savingRequirements && <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />}Save requirements and continue</Button></div>}{groupError && <div className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{groupError}</div>}<Button onClick={saveGroup} className="bg-tenant-primary text-black">Save Group</Button></div><div className="border-t pt-3"><h4 className="font-semibold">Configured groups</h4>{groups.length === 0 ? <p className="mt-2 text-sm text-muted-foreground">No parallel groups configured.</p> : groups.map(group => <div key={group.id} className="mt-2 rounded-md border p-3 text-sm"><div className="font-medium">{group.displayName || group.members.map(x => x.subjectName).join(" / ")}</div><div className="text-muted-foreground">{group.sharedPeriodsPerWeek} shared periods/week · {group.members.map(x => `${x.subjectName} — ${x.teacherName || "Teacher not assigned"}`).join("; ")}</div><div className="mt-2 flex gap-2"><Button size="sm" variant="outline" onClick={() => beginGroup(group)}>Edit</Button><Button size="sm" variant="outline" onClick={() => removeGroup(group.id)}>Remove</Button></div></div>)}</div></DialogContent></Dialog></>;
}
