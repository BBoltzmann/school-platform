"use client";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";

export function TeacherInviteActions({ staffId }: { staffId: string }) {
  const [status, setStatus] = useState<{ status: string; expiresAtUtc?: string | null; email?: string | null } | null>(null);
  const [link, setLink] = useState<string | null>(null);
  async function load() { const response = await fetch(`/api/staff/${staffId}/teacher-access-status`); if (response.ok) setStatus(await response.json()); }
  // Loading server state is the purpose of this effect.
  // eslint-disable-next-line react-hooks/set-state-in-effect, react-hooks/exhaustive-deps
  useEffect(() => { void load(); }, [staffId]);
  async function generate() { const response = await fetch(`/api/staff/${staffId}/teacher-invite`, { method: "POST" }); if (response.ok) { const result = await response.json(); setLink(result.inviteUrl); await navigator.clipboard?.writeText(result.inviteUrl); await load(); } }
  async function revoke() { await fetch(`/api/staff/${staffId}/teacher-invite/revoke`, { method: "POST" }); setLink(null); await load(); }
  if (!status) return <span className="text-xs text-muted-foreground">Loading access…</span>;
  if (status.status === "active") return <div className="text-xs"><div className="font-medium text-green-700">Login active</div><div className="text-muted-foreground">{status.email}</div></div>;
  return <div className="space-y-1"><div className="text-xs font-medium text-amber-700">{status.status === "pending" ? "Invitation pending" : status.status === "expired" ? "Invitation expired" : "Login not activated"}</div><div className="flex flex-wrap gap-1"><Button type="button" size="sm" variant="outline" onClick={generate}>{status.status === "pending" ? "Regenerate" : "Generate Invite"}</Button>{link && <Button type="button" size="sm" variant="outline" onClick={() => navigator.clipboard?.writeText(link)}>Copy Invite</Button>}{status.status === "pending" && <Button type="button" size="sm" variant="ghost" onClick={revoke}>Revoke</Button>}</div></div>;
}
