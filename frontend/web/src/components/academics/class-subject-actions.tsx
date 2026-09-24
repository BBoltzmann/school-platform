"use client";

import { useEffect, useState } from "react";
import { LoaderCircle } from "lucide-react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import type { ClassGroup, Subject } from "@/types/academics";

type Group = { id: string; displayName: string | null; sharedPeriodsPerWeek: number; members: { classSubjectId: string; subjectId: string; subjectName: string; teacherName: string | null }[] };

export function ClassSubjectActions({ classGroup, subjects, academicSessionId }: { classGroup: ClassGroup; subjects: Subject[]; academicSessionId: string }) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [groups, setGroups] = useState<Group[]>([]);
  const [persistedSubjects, setPersistedSubjects] = useState<{ id: string; subjectId: string; subjectName: string; subjectCode: string }[]>([]);
  const [groupOpen, setGroupOpen] = useState(false);
  const [editing, setEditing] = useState<Group | null>(null);
  const [groupName, setGroupName] = useState("");
  const [groupSelected, setGroupSelected] = useState<string[]>([]);
  const [groupError, setGroupError] = useState<string | null>(null);
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
      if (academicSessionId) {
        const groupsResponse = await fetch(`/api/academics/classes/${classGroup.id}/parallel-subject-groups?academicSessionId=${academicSessionId}`);
        if (groupsResponse.ok) setGroups(await groupsResponse.json());
      }
    })();
  }, [open, groupOpen, classGroup.id, academicSessionId]);
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
  async function saveGroup() { setGroupError(null); if (groupSelected.length < 2) { setGroupError("Select at least two subjects."); return; } const response = await fetch(`/api/academics/classes/${classGroup.id}/parallel-subject-groups${editing ? `/${editing.id}` : ""}`, { method: editing ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ academicSessionId, displayName: groupName || null, classSubjectIds: groupSelected }) }); const result = await response.json(); if (!response.ok) { setGroupError(result.error ?? "Unable to save parallel group."); return; } setGroups(current => editing ? current.map(x => x.id === result.id ? result : x) : [...current, result]); setGroupOpen(false); }
  async function removeGroup(id: string) { const response = await fetch(`/api/academics/classes/${classGroup.id}/parallel-subject-groups/${id}`, { method: "DELETE" }); if (response.ok) setGroups(current => current.filter(x => x.id !== id)); }
  function beginGroup(group?: Group) { setEditing(group ?? null); setGroupName(group?.displayName ?? ""); setGroupSelected(group?.members.map(x => x.classSubjectId) ?? []); setGroupError(null); setGroupOpen(true); }
  const offered = subjects.filter(x => x.isActive);
  return <><DropdownMenu><DropdownMenuTrigger render={<Button variant="outline" size="sm">Subjects</Button>} /><DropdownMenuContent align="end"><DropdownMenuItem onClick={() => setOpen(true)}>Manage Subjects</DropdownMenuItem><DropdownMenuItem onClick={() => beginGroup()}>Manage Parallel Groups</DropdownMenuItem></DropdownMenuContent></DropdownMenu><Dialog open={open} onOpenChange={setOpen}><DialogContent><DialogHeader><DialogTitle>Subjects Offered</DialogTitle><DialogDescription>{classGroup.name} · {classGroup.academicLevelName}</DialogDescription></DialogHeader><div className="max-h-80 space-y-2 overflow-y-auto">{offered.map(subject => <label key={subject.id} className="flex items-center gap-3 rounded-md border px-3 py-2 text-sm"><input type="checkbox" checked={selected.includes(subject.id)} onChange={e => setSelected(current => e.target.checked ? [...current, subject.id] : current.filter(id => id !== subject.id))} />{subject.name} <span className="text-muted-foreground">{subject.code}</span></label>)}</div>{error && <div className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}<div className="flex justify-between"><Button variant="outline" onClick={resetSubjects} disabled={saving}>Reset Selected Subjects</Button><Button onClick={save} disabled={saving} className="bg-tenant-primary text-black">{saving && <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />}Save Subjects</Button></div></DialogContent></Dialog><Dialog open={groupOpen} onOpenChange={setGroupOpen}><DialogContent><DialogHeader><DialogTitle>Parallel Subject Groups</DialogTitle><DialogDescription>Use this when students take different subjects during the same timetable period.</DialogDescription></DialogHeader><div className="space-y-3"><input className="w-full rounded-md border px-3 py-2 text-sm" placeholder="Display name (optional)" value={groupName} onChange={e => setGroupName(e.target.value)} /><div className="max-h-64 space-y-2 overflow-y-auto">{persistedSubjects.length === 0 ? <p className="text-sm text-muted-foreground">Save Subjects Offered before configuring a parallel group.</p> : persistedSubjects.map(subject => <label key={subject.id} className="flex cursor-pointer items-center gap-3 rounded-md border px-3 py-2 text-sm"><input type="checkbox" checked={groupSelected.includes(subject.id)} onChange={e => setGroupSelected(current => e.target.checked ? [...current, subject.id] : current.filter(id => id !== subject.id))} />{subject.subjectName}</label>)}</div>{groupError && <div className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{groupError}</div>}<Button onClick={saveGroup} className="bg-tenant-primary text-black">Save Group</Button></div><div className="border-t pt-3"><h4 className="font-semibold">Configured groups</h4>{groups.length === 0 ? <p className="mt-2 text-sm text-muted-foreground">No parallel groups configured.</p> : groups.map(group => <div key={group.id} className="mt-2 rounded-md border p-3 text-sm"><div className="font-medium">{group.displayName || group.members.map(x => x.subjectName).join(" / ")}</div><div className="text-muted-foreground">{group.sharedPeriodsPerWeek} shared periods/week · {group.members.map(x => `${x.subjectName} — ${x.teacherName || "Teacher not assigned"}`).join("; ")}</div><div className="mt-2 flex gap-2"><Button size="sm" variant="outline" onClick={() => beginGroup(group)}>Edit</Button><Button size="sm" variant="outline" onClick={() => removeGroup(group.id)}>Remove</Button></div></div>)}</div></DialogContent></Dialog></>;
}
