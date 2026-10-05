"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";

export function CarryForwardWorkspace({ sessionId, termId, sessionName, terms = [], students = [] }: { sessionId?: string; termId?: string; sessionName?: string; terms?: { id: string; name: string }[]; students?: { id: string; name: string; admissionNumber: string }[] }) {
  const targetSessionId = sessionId ?? "";
  const [targetTermId, setTargetTermId] = useState(terms[1]?.id ?? "");
  const [result, setResult] = useState<{ studentsEvaluated: number; debitStudents: number; debitAmount: number; creditStudents: number; creditAmount: number; zeroStudents: number; students?: { studentId: string; balance: number; isDebit: boolean }[] } | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [selectedStudents, setSelectedStudents] = useState<string[] | null>(null);
  const request = { sourceSessionId: sessionId, sourceTermId: termId || null, targetSessionId, targetTermId: targetTermId || null, studentIds: selectedStudents, idempotencyKey: `${sessionId}:${termId}:${targetSessionId}:${targetTermId}:${(selectedStudents ?? []).sort().join(",")}` };
  async function preview() {
    if (!request.sourceSessionId || !request.targetSessionId) { setMessage("Select a source and target academic period."); return; }
    const response = await fetch("/api/fees/carry-forward/preview", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(request) });
    const value = await response.json();
    if (!response.ok) { setMessage(value.error ?? "Unable to prepare preview."); return; }
    setResult(value); setSelectedStudents(value.students?.filter((student: { balance: number }) => student.balance !== 0).map((student: { studentId: string }) => student.studentId) ?? null); setMessage(null);
  }
  async function apply() {
    const response = await fetch("/api/fees/carry-forward", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(request) });
    const value = await response.json();
    setMessage(response.ok ? `Carry-forward completed for ${value.studentsProcessed} students.` : value.error ?? "Unable to carry balances forward.");
  }
  return <section className="rounded-xl border bg-card p-5"><h2 className="text-lg font-semibold">Carry balances forward</h2><p className="mt-1 text-sm text-muted-foreground">Review the current term and carry selected outstanding balances to the next term. This does not promote students.</p><div className="mt-5 grid gap-4 md:grid-cols-2"><label className="text-sm">Current term<input readOnly className="mt-1 w-full rounded-md border bg-muted px-3 py-2" value={`${sessionName ?? "Current session"} — ${terms.find(term => term.id === termId)?.name ?? "Current term"}`} /></label><label className="text-sm">Next term<select className="mt-1 w-full rounded-md border bg-background px-3 py-2" value={targetTermId} onChange={event => setTargetTermId(event.target.value)}>{terms.filter(term => term.id !== termId).map(term => <option key={term.id} value={term.id}>{sessionName ?? "Current session"} — {term.name}</option>)}</select></label></div><div className="mt-4 flex gap-3"><Button onClick={preview}>Preview balances</Button><Button variant="outline" disabled={!result} onClick={apply}>Carry selected balances</Button></div>{result && <div className="mt-4 rounded-lg bg-muted p-4 text-sm"><p>{result.studentsEvaluated} students evaluated · {result.debitStudents} debit students ({result.debitAmount.toLocaleString()}) · {result.creditStudents} credit students ({result.creditAmount.toLocaleString()}) · {result.zeroStudents} zero balances</p><div className="mt-3 space-y-2">{result.students?.map(student => { const person = students.find(item => item.id === student.studentId); const selected = selectedStudents?.includes(student.studentId) ?? false; return <label className="flex items-center gap-2" key={student.studentId}><input type="checkbox" checked={selected} onChange={event => setSelectedStudents(current => { const next = new Set(current ?? []); if (event.target.checked) { next.add(student.studentId); } else { next.delete(student.studentId); } return [...next]; })} /><span>{person?.name ?? student.studentId} — {student.isDebit ? `${student.balance.toLocaleString()} due` : `${Math.abs(student.balance).toLocaleString()} credit`}</span></label>; })}</div></div>}{message && <p className="mt-3 text-sm text-muted-foreground">{message}</p>}</section>;
}
