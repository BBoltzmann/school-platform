"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";

export function CarryForwardWorkspace({ sessionId, termId }: { sessionId?: string; termId?: string }) {
  const [targetSessionId, setTargetSessionId] = useState(sessionId ?? "");
  const [targetTermId, setTargetTermId] = useState(termId ?? "");
  const [result, setResult] = useState<{ studentsEvaluated: number; debitStudents: number; debitAmount: number; creditStudents: number; creditAmount: number; zeroStudents: number } | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const request = { sourceSessionId: sessionId, sourceTermId: termId || null, targetSessionId, targetTermId: targetTermId || null, idempotencyKey: `${sessionId}:${termId}:${targetSessionId}:${targetTermId}` };
  async function preview() {
    if (!request.sourceSessionId || !request.targetSessionId) { setMessage("Select a source and target academic period."); return; }
    const response = await fetch("/api/fees/carry-forward/preview", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(request) });
    const value = await response.json();
    if (!response.ok) { setMessage(value.error ?? "Unable to prepare preview."); return; }
    setResult(value); setMessage(null);
  }
  async function apply() {
    const response = await fetch("/api/fees/carry-forward", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(request) });
    const value = await response.json();
    setMessage(response.ok ? `Carry-forward completed for ${value.studentsProcessed} students.` : value.error ?? "Unable to carry balances forward.");
  }
  return <section className="rounded-xl border bg-card p-5"><h2 className="text-lg font-semibold">Carry balances forward</h2><p className="mt-1 text-sm text-muted-foreground">Preview a period transfer before posting it. Repeating the same run is idempotent.</p><div className="mt-5 grid gap-4 md:grid-cols-2"><label className="text-sm">Target session<input className="mt-1 w-full rounded-md border bg-background px-3 py-2" value={targetSessionId} onChange={event => setTargetSessionId(event.target.value)} /></label><label className="text-sm">Target term (optional)<input className="mt-1 w-full rounded-md border bg-background px-3 py-2" value={targetTermId} onChange={event => setTargetTermId(event.target.value)} /></label></div><div className="mt-4 flex gap-3"><Button onClick={preview}>Preview</Button><Button variant="outline" disabled={!result} onClick={apply}>Carry balances forward</Button></div>{result && <div className="mt-4 rounded-lg bg-muted p-4 text-sm">{result.studentsEvaluated} students evaluated · {result.debitStudents} debit students ({result.debitAmount.toLocaleString()}) · {result.creditStudents} credit students ({result.creditAmount.toLocaleString()}) · {result.zeroStudents} zero balances</div>}{message && <p className="mt-3 text-sm text-muted-foreground">{message}</p>}</section>;
}
