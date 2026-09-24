"use client";

import { useCallback, useEffect, useState } from "react";
import { LoaderCircle } from "lucide-react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import type { ClassGroup, Subject } from "@/types/academics";

type GroupMember = { classSubjectId: string; subjectId: string; subjectName: string; subjectCode?: string | null; periodsPerWeek: number; teacherName: string | null };
type Group = { id: string; displayName: string | null; sharedPeriodsPerWeek: number; members: GroupMember[] };
type PersistedSubject = { id: string; subjectId: string; subjectName: string; subjectCode: string };

export function ClassSubjectActions({ classGroup, subjects, academicSessionId }: { classGroup: ClassGroup; subjects: Subject[]; academicSessionId: string }) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [groups, setGroups] = useState<Group[]>([]);
  const [persistedSubjects, setPersistedSubjects] = useState<PersistedSubject[]>([]);
  const [subjectRequirements, setSubjectRequirements] = useState<Record<string, number>>({});
  const [savingRequirement, setSavingRequirement] = useState<string | null>(null);
  const [groupOpen, setGroupOpen] = useState(false);
  const [editing, setEditing] = useState<Group | null>(null);
  const [groupName, setGroupName] = useState("");
  const [groupSelected, setGroupSelected] = useState<string[]>([]);
  const [groupError, setGroupError] = useState<string | null>(null);
  const [groupLoading, setGroupLoading] = useState(false);
  const [resettingGroups, setResettingGroups] = useState(false);
  const loadGroups = useCallback(async () => {
    if (!academicSessionId) {
      setGroups([]);
      setGroupError("A current academic session is required before configuring parallel groups.");
      return;
    }
    setGroupLoading(true);
    setGroupError(null);
    try {
      const groupsResponse = await fetch(`/api/academics/classes/${classGroup.id}/parallel-subject-groups?academicSessionId=${encodeURIComponent(academicSessionId)}`);
      const result = await groupsResponse.json().catch(() => ({}));
      if (!groupsResponse.ok) {
        throw new Error(result.error ?? "Unable to load parallel groups.");
      }
      setGroups(Array.isArray(result) ? result : []);
    } catch (exception) {
      setGroups([]);
      setGroupError(exception instanceof Error ? exception.message : "Unable to load parallel groups.");
    } finally {
      setGroupLoading(false);
    }
  }, [academicSessionId, classGroup.id]);
  const loadRequirements = useCallback(async () => {
    const response = await fetch(`/api/timetable/classes/${classGroup.id}/requirements`);
    const result = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(result.error ?? "Unable to load weekly subject requirements.");
    const next = Object.fromEntries((result.requirements ?? []).map((item: { subjectId: string; periodsPerWeek: number }) => [item.subjectId, item.periodsPerWeek]));
    setSubjectRequirements(next);
  }, [classGroup.id]);
  useEffect(() => {
    if (!open && !groupOpen) return;
    (async () => {
      const response = await fetch(`/api/academics/classes/${classGroup.id}/subjects`);
      const result = await response.json();
      if (response.ok) {
        const persisted = result.subjects.map((x: { id: string; subjectId: string; subjectName: string; subjectCode: string }) => x);
        setSelected(persisted.map((x: { subjectId: string }) => x.subjectId));
        setPersistedSubjects(persisted.filter((x: { id: string }) => Boolean(x.id)));
      }
      if (academicSessionId) await loadGroups();
      if (groupOpen) {
        try {
          await loadRequirements();
        } catch (exception) {
          setGroupError(exception instanceof Error ? exception.message : "Unable to load weekly subject requirements.");
        }
      }
    })();
  }, [open, groupOpen, classGroup.id, academicSessionId, loadGroups, loadRequirements]);
  async function save() { setSaving(true); setError(null); try { const response = await fetch(`/api/academics/classes/${classGroup.id}/subjects`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ subjectIds: selected }) }); const result = await response.json(); if (!response.ok) { setError(result.error ?? "Unable to save subjects."); return; } setSelected(result.subjects.map((x: { subjectId: string }) => x.subjectId)); setPersistedSubjects(result.subjects.filter((x: { id: string }) => Boolean(x.id))); setOpen(false); router.refresh(); } catch { setError("Unable to save subjects. Check your connection and try again."); } finally { setSaving(false); } }
  async function resetSubjects() {
    if (!window.confirm(`Reset Subjects Offered for ${classGroup.name}? This clears only this class configuration and does not delete school subjects.`)) return;
    setSaving(true); setError(null);
    try {
      const response = await fetch(`/api/academics/classes/${classGroup.id}/subjects`, { method: "POST" });
      const result = await response.json();
      if (!response.ok) { setError(result.error ?? "Unable to reset subjects offered."); return; }
      setSelected([]); setPersistedSubjects([]); setOpen(false); router.refresh();
    } catch { setError("Unable to reset subjects offered. Check your connection and try again."); }
    finally { setSaving(false); }
  }
  async function saveGroup() {
    setGroupError(null);
    if (groupSelected.length < 2) {
      setGroupError("Select at least two subjects.");
      return;
    }
    const missing = persistedSubjects
      .filter(subject => groupSelected.includes(subject.id) && (subjectRequirements[subject.subjectId] ?? 0) <= 0)
      .map(subject => subject.subjectName);
    if (missing.length > 0) {
      setGroupError(`${missing.join(", ")} ${missing.length === 1 ? "has" : "have"} no weekly period requirement. Set periods/week before creating this parallel group.`);
      return;
    }
    const response = await fetch(`/api/academics/classes/${classGroup.id}/parallel-subject-groups${editing ? `/${editing.id}` : ""}`, { method: editing ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ academicSessionId, displayName: groupName || null, classSubjectIds: groupSelected }) });
    const result = await response.json().catch(() => ({}));
    if (!response.ok) { setGroupError(result.error ?? "Unable to save parallel group."); return; }
    setGroups(current => editing ? current.map(x => x.id === result.id ? result : x) : [...current, result]);
    setGroupOpen(false);
  }
  async function removeGroup(id: string) {
    if (!window.confirm("Delete this parallel group configuration? Subjects and class offerings will remain.")) return;
    const response = await fetch(`/api/academics/classes/${classGroup.id}/parallel-subject-groups/${id}`, { method: "DELETE" });
    const result = await response.json().catch(() => ({}));
    if (!response.ok) { setGroupError(result.error ?? "Unable to delete parallel group."); return; }
    setGroups(current => current.filter(x => x.id !== id));
  }
  function beginGroup(group?: Group) { setEditing(group ?? null); setGroupName(group?.displayName ?? ""); setGroupSelected(group?.members.map(x => x.classSubjectId) ?? []); setGroupError(null); setGroupOpen(true); }
  async function saveRequirement(subjectId: string) {
    const periods = subjectRequirements[subjectId] ?? 0;
    if (periods < 1) {
      setGroupError("Periods/week must be at least 1.");
      return;
    }
    setSavingRequirement(subjectId);
    setGroupError(null);
    try {
      const response = await fetch(`/api/timetable/classes/${classGroup.id}/requirements`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ requirements: Object.entries(subjectRequirements).filter(([, value]) => value > 0).map(([id, value]) => ({ subjectId: id, periodsPerWeek: value })) }),
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.error ?? "Unable to save weekly periods.");
      setSubjectRequirements(Object.fromEntries((result.requirements ?? []).map((item: { subjectId: string; periodsPerWeek: number }) => [item.subjectId, item.periodsPerWeek])));
    } catch (exception) {
      setGroupError(exception instanceof Error ? exception.message : "Unable to save weekly periods.");
    } finally {
      setSavingRequirement(null);
    }
  }
  async function resetGroups() {
    if (!window.confirm(`Reset parallel groups for ${classGroup.name}? This removes only grouping configuration.`)) return;
    setResettingGroups(true);
    setGroupError(null);
    try {
      const response = await fetch(`/api/academics/classes/${classGroup.id}/parallel-subject-groups/reset?academicSessionId=${encodeURIComponent(academicSessionId)}`, { method: "POST" });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.error ?? "Unable to reset parallel groups.");
      setGroupSelected([]);
      setEditing(null);
      await loadGroups();
    } catch (exception) {
      setGroupError(exception instanceof Error ? exception.message : "Unable to reset parallel groups.");
    } finally {
      setResettingGroups(false);
    }
  }
  const offered = subjects.filter(x => x.isActive);
  return <>
    <DropdownMenu>
      <DropdownMenuTrigger render={<Button variant="outline" size="sm">Subjects</Button>} />
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => setOpen(true)}>Manage Subjects</DropdownMenuItem>
        <DropdownMenuItem onClick={() => beginGroup()}>Manage Parallel Groups</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent>
        <DialogHeader><DialogTitle>Subjects Offered</DialogTitle><DialogDescription>{classGroup.name} · {classGroup.academicLevelName}</DialogDescription></DialogHeader>
        <div className="max-h-80 space-y-2 overflow-y-auto">{offered.map(subject => <label key={subject.id} className="flex items-center gap-3 rounded-md border px-3 py-2 text-sm"><input type="checkbox" checked={selected.includes(subject.id)} onChange={e => setSelected(current => e.target.checked ? [...current, subject.id] : current.filter(id => id !== subject.id))} />{subject.name} <span className="text-muted-foreground">{subject.code}</span></label>)}</div>
        {error && <div className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}
        <div className="flex justify-between"><Button variant="outline" onClick={resetSubjects} disabled={saving}>Reset Selected Subjects</Button><Button onClick={save} disabled={saving} className="bg-tenant-primary text-black">{saving && <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />}Save Subjects</Button></div>
      </DialogContent>
    </Dialog>
    <Dialog open={groupOpen} onOpenChange={setGroupOpen}>
      <DialogContent>
        <DialogHeader><DialogTitle>Parallel Subject Groups</DialogTitle><DialogDescription>Use this when students take different subjects during the same timetable period.</DialogDescription></DialogHeader>
        <div className="space-y-3">
          <input className="w-full rounded-md border px-3 py-2 text-sm" placeholder="Display name (optional)" value={groupName} onChange={e => setGroupName(e.target.value)} />
          <div className="max-h-64 space-y-2 overflow-y-auto">{persistedSubjects.length === 0 ? <p className="text-sm text-muted-foreground">Save Subjects Offered before configuring a parallel group.</p> : persistedSubjects.map(subject => {
            const periods = subjectRequirements[subject.subjectId] ?? 0;
            const membership = groups.find(group => group.id !== editing?.id && group.members.some(member => member.classSubjectId === subject.id));
            const membershipLabel = membership?.displayName || membership?.members.map(member => member.subjectName).join(" / ");
            return <div key={subject.id} className="rounded-md border px-3 py-2 text-sm">
              <label className="flex cursor-pointer items-start gap-3">
                <input type="checkbox" checked={groupSelected.includes(subject.id)} disabled={Boolean(membership)} onChange={e => setGroupSelected(current => e.target.checked ? [...current, subject.id] : current.filter(id => id !== subject.id))} />
                <span className="min-w-0 flex-1"><span className="block truncate font-medium" title={subject.subjectName}>{subject.subjectName}</span><span className={periods > 0 ? "text-muted-foreground" : "text-amber-700"}>{periods} periods/week{periods === 0 ? " · Not configured" : ""}</span>{membershipLabel && <span className="block text-xs text-blue-700">Parallel group: {membershipLabel}</span>}</span>
              </label>
              <div className="mt-2 flex items-center gap-2 pl-7"><input aria-label={`${subject.subjectName} periods per week`} className="w-20 rounded-md border px-2 py-1 text-sm" min={0} type="number" value={periods} onChange={e => setSubjectRequirements(current => ({ ...current, [subject.subjectId]: Number(e.target.value) }))} /><span className="text-xs text-muted-foreground">periods/week</span><Button size="sm" variant="outline" onClick={() => saveRequirement(subject.subjectId)} disabled={savingRequirement === subject.subjectId}>{savingRequirement === subject.subjectId ? "Saving…" : "Save periods"}</Button></div>
            </div>;
          })}</div>
          {groupLoading && <p className="text-sm text-muted-foreground">Loading configured groups…</p>}
          {groupError && <div className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{groupError}</div>}
          <Button onClick={saveGroup} disabled={groupLoading || !academicSessionId} className="bg-tenant-primary text-black">Save Group</Button>
        </div>
        <div className="border-t pt-3">
          <div className="flex items-center justify-between"><h4 className="font-semibold">Configured groups</h4><Button size="sm" variant="outline" onClick={resetGroups} disabled={resettingGroups || !academicSessionId}>{resettingGroups ? "Resetting…" : "Reset Parallel Groups"}</Button></div>
          {groupLoading ? <p className="mt-2 text-sm text-muted-foreground">Loading configured groups…</p> : groupError ? <div className="mt-2 flex items-center justify-between gap-2 rounded-md bg-red-50 px-3 py-2 text-sm text-red-700"><span>{groupError}</span><Button size="sm" variant="outline" onClick={() => void loadGroups()}>Retry</Button></div> : groups.length === 0 ? <p className="mt-2 text-sm text-muted-foreground">No parallel groups configured for this class and session.</p> : groups.map(group => <div key={group.id} className="mt-2 rounded-md border p-3 text-sm"><div className="font-medium">{group.displayName || group.members.map(x => x.subjectName).join(" / ")}</div><div className="text-muted-foreground">{group.sharedPeriodsPerWeek} shared periods/week · {group.members.map(x => `${x.subjectName} (${x.periodsPerWeek}/week) — ${x.teacherName || "Teacher not assigned"}`).join("; ")}</div><div className="mt-2 flex gap-2"><Button size="sm" variant="outline" onClick={() => beginGroup(group)}>Edit</Button><Button size="sm" variant="outline" onClick={() => removeGroup(group.id)}>Delete</Button></div></div>)}
        </div>
      </DialogContent>
    </Dialog>
  </>;
}
