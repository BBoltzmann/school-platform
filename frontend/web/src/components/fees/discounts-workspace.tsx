"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";

type Discount = { id: string; name: string; description: string | null; defaultAmount: number; isActive: boolean };

export function DiscountsWorkspace({ setup }: { setup: { currentSession: { id: string } | null; terms: { id: string; name: string }[]; levels: { id: string; name: string }[]; classes: { id: string; name: string }[]; students: { id: string; name: string; admissionNumber: string }[]; structures?: { id: string; name: string }[] } }) {
  const [discounts, setDiscounts] = useState<Discount[]>([]);
  const [name, setName] = useState("");
  const [amount, setAmount] = useState("");
  const [description, setDescription] = useState("");
  const [definitionId, setDefinitionId] = useState("");
  const [audience, setAudience] = useState("School");
  const [termId, setTermId] = useState(setup.terms[0]?.id ?? "");
  const [levelId, setLevelId] = useState("");
  const [classId, setClassId] = useState("");
  const [studentId, setStudentId] = useState("");
  const [structureId, setStructureId] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [preview, setPreview] = useState<{ studentCount: number; amountPerStudent: number; totalAmount: number } | null>(null);
  const load = async () => {
    const response = await fetch("/api/fees/discounts");
    const result = await response.json();
    if (response.ok) setDiscounts(result);
  };
  // The initial request hydrates this client workspace from the authenticated API.
  useEffect(() => { // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, []);
  async function create() {
    const value = Number(amount);
    if (!name.trim() || !Number.isFinite(value) || value <= 0) { setMessage("Enter a name and a positive fixed amount."); return; }
    const response = await fetch("/api/fees/discounts", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name, description, defaultAmount: value }) });
    const result = await response.json();
    if (!response.ok) { setMessage(result.error ?? "Unable to create discount."); return; }
    setName(""); setAmount(""); setDescription(""); setMessage("Discount definition created."); await load();
  }
  function requestBody() {
    return { discountDefinitionId: definitionId, academicSessionId: setup.currentSession?.id, academicTermId: termId || null, audienceType: audience === "Student" || audience === "FeeStructure" ? "SelectedStudents" : audience, academicLevelId: audience === "Level" ? levelId : null, classGroupId: audience === "Class" ? classId : null, studentIds: audience === "Student" ? (studentId ? [studentId] : []) : null, amountOverride: null, idempotencyKey: `${definitionId}:${setup.currentSession?.id}:${termId}:${audience}:${levelId}:${classId}:${studentId}:${structureId}` };
  }
  async function prepare() {
    if (!definitionId || !setup.currentSession) { setMessage("Choose a discount and current session."); return; }
    let body = requestBody();
    if (audience === "FeeStructure") {
      if (!structureId) { setMessage("Choose a fee structure."); return; }
      const assigned = await fetch(`/api/fees/structures/${structureId}/students`);
      if (!assigned.ok) { setMessage("Unable to load assigned students."); return; }
      body = { ...body, studentIds: (await assigned.json()).map((item: { studentId: string }) => item.studentId) };
    }
    const response = await fetch("/api/fees/discounts/preview", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
    const value = await response.json(); if (!response.ok) { setMessage(value.error ?? "Unable to prepare preview."); return; } setPreview(value); setMessage(null);
  }
  async function apply() {
    let body = requestBody();
    if (audience === "FeeStructure") {
      const assigned = await fetch(`/api/fees/structures/${structureId}/students`);
      if (assigned.ok) body = { ...body, studentIds: (await assigned.json()).map((item: { studentId: string }) => item.studentId) };
    }
    const response = await fetch("/api/fees/discounts/apply", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
    const value = await response.json(); setMessage(response.ok ? `Discount applied to ${value.studentCount} students.` : value.error ?? "Unable to apply discount."); setPreview(null);
  }
  return <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
    <section className="rounded-xl border bg-card p-5"><h2 className="text-lg font-semibold">Discount definitions</h2><p className="mt-1 text-sm text-muted-foreground">Reusable fixed-amount discounts for this school.</p><div className="mt-4 divide-y">{discounts.length === 0 ? <p className="py-5 text-sm text-muted-foreground">No discounts have been defined.</p> : discounts.map(item => <div className="flex items-center justify-between py-3" key={item.id}><div><p className="font-medium">{item.name}</p><p className="text-sm text-muted-foreground">{item.description ?? "Fixed amount"}</p></div><span className="font-medium">{item.defaultAmount.toLocaleString()}</span></div>)}</div></section>
    <section className="space-y-6"><div className="rounded-xl border bg-card p-5"><h2 className="text-lg font-semibold">Add discount</h2><div className="mt-4 space-y-3"><input className="w-full rounded-md border bg-background px-3 py-2" placeholder="Name" value={name} onChange={event => setName(event.target.value)} /><input className="w-full rounded-md border bg-background px-3 py-2" placeholder="Fixed amount" inputMode="decimal" value={amount} onChange={event => setAmount(event.target.value)} /><textarea className="min-h-20 w-full rounded-md border bg-background px-3 py-2" placeholder="Description (optional)" value={description} onChange={event => setDescription(event.target.value)} /><Button onClick={create}>Create discount</Button></div></div><div className="rounded-xl border bg-card p-5"><h2 className="text-lg font-semibold">Apply to students</h2><div className="mt-4 space-y-3"><select className="w-full rounded-md border bg-background px-3 py-2" value={definitionId} onChange={event => setDefinitionId(event.target.value)}><option value="">Choose discount</option>{discounts.filter(item => item.isActive).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select><select className="w-full rounded-md border bg-background px-3 py-2" value={audience} onChange={event => setAudience(event.target.value)}><option>Student</option><option>Class</option><option>FeeStructure</option><option>School</option><option>Level</option></select>{audience === "Student" && <select className="w-full rounded-md border bg-background" value={studentId} onChange={event => setStudentId(event.target.value)}><option value="">Choose student</option>{setup.students.map(item => <option key={item.id} value={item.id}>{item.name} — {item.admissionNumber}</option>)}</select>}{audience === "FeeStructure" && <select className="w-full rounded-md border bg-background" value={structureId} onChange={event => setStructureId(event.target.value)}><option value="">Choose fee structure</option>{(setup.structures ?? []).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>}{audience === "Level" && <select className="w-full rounded-md border bg-background" value={levelId} onChange={event => setLevelId(event.target.value)}><option value="">Choose level</option>{setup.levels.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>}{audience === "Class" && <select className="w-full rounded-md border bg-background" value={classId} onChange={event => setClassId(event.target.value)}><option value="">Choose class</option>{setup.classes.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select>}<select className="w-full rounded-md border bg-background" value={termId} onChange={event => setTermId(event.target.value)}><option value="">Session-wide</option>{setup.terms.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select><Button onClick={prepare}>Preview</Button>{preview && <div className="rounded-lg bg-muted p-3 text-sm">{preview.studentCount} students · {preview.amountPerStudent.toLocaleString()} each · {preview.totalAmount.toLocaleString()} total<div className="mt-2"><Button size="sm" onClick={apply}>Confirm and apply</Button></div></div>}</div></div>{message && <p className="text-sm text-muted-foreground">{message}</p>}</section>
  </div>;
}
