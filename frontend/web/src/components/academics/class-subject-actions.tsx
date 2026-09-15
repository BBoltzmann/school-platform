"use client";

import { useEffect, useState } from "react";
import { LoaderCircle } from "lucide-react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import type { ClassGroup, Subject } from "@/types/academics";

export function ClassSubjectActions({ classGroup, subjects }: { classGroup: ClassGroup; subjects: Subject[] }) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => { if (!open) return; (async () => { const response = await fetch(`/api/academics/classes/${classGroup.id}/subjects`); const result = await response.json(); if (response.ok) setSelected(result.subjects.map((x: { subjectId: string }) => x.subjectId)); })(); }, [open, classGroup.id]);
  async function save() { setSaving(true); setError(null); try { const response = await fetch(`/api/academics/classes/${classGroup.id}/subjects`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ subjectIds: selected }) }); const result = await response.json(); if (!response.ok) { setError(result.error ?? "Unable to save subjects."); return; } setOpen(false); router.refresh(); } catch { setError("Unable to connect to the server."); } finally { setSaving(false); } }
  return <><DropdownMenu><DropdownMenuTrigger render={<Button variant="outline" size="sm">Subjects</Button>} /><DropdownMenuContent align="end"><DropdownMenuItem onClick={() => setOpen(true)}>Manage Subjects</DropdownMenuItem></DropdownMenuContent></DropdownMenu><Dialog open={open} onOpenChange={setOpen}><DialogContent><DialogHeader><DialogTitle>Subjects Offered</DialogTitle><DialogDescription>{classGroup.name} · {classGroup.academicLevelName}</DialogDescription></DialogHeader><div className="max-h-80 space-y-2 overflow-y-auto">{subjects.filter(x => x.isActive).map(subject => <label key={subject.id} className="flex items-center gap-3 rounded-md border px-3 py-2 text-sm"><input type="checkbox" checked={selected.includes(subject.id)} onChange={e => setSelected(current => e.target.checked ? [...current, subject.id] : current.filter(id => id !== subject.id))} />{subject.name} <span className="text-muted-foreground">{subject.code}</span></label>)}</div>{error && <div className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}<div className="flex justify-end"><Button onClick={save} disabled={saving} className="bg-tenant-primary text-black">{saving && <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />}Save Subjects</Button></div></DialogContent></Dialog></>;
}
