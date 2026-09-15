"use client";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";

export function TeacherInviteActions({ staffId }: { staffId: string }) {
  const [status, setStatus] = useState<{ status: string; expiresAtUtc?: string | null; email?: string | null } | null>(null);
  const [link, setLink] = useState<string | null>(null);
  const [error, setError] = useState(false);
  async function load() { setError(false); try { const response = await fetch(`/api/staff/${staffId}/teacher-access-status`, { cache: "no-store" }); if (!response.ok) throw new Error(); setStatus(await response.json()); } catch { setError(true); } }
  // Loading server state is the purpose of this effect.
  // eslint-disable-next-line react-hooks/set-state-in-effect, react-hooks/exhaustive-deps
  useEffect(() => { void load(); }, [staffId]);
  async function generate() { const response = await fetch(`/api/staff/${staffId}/teacher-invite`, { method: "POST" }); if (response.ok) { const result = await response.json(); setLink(result.inviteUrl); await navigator.clipboard?.writeText(result.inviteUrl); await load(); } }
  async function revoke() { await fetch(`/api/staff/${staffId}/teacher-invite/revoke`, { method: "POST" }); setLink(null); await load(); }
  return <section className="rounded-xl border bg-card p-5 shadow-sm"><div className="mb-3"><h2 className="font-semibold">Teacher Portal Access</h2><p className="text-sm text-muted-foreground">Manage this teacher&apos;s portal login.</p></div>{error ? <div className="space-y-3"><p className="text-sm text-destructive">Teacher portal status could not be loaded.</p><Button type="button" size="sm" variant="outline" onClick={() => void load()}>Retry</Button></div> : !status ? <span className="text-sm text-muted-foreground">Loading access…</span> : status.status === "active" ? <div className="text-sm"><div className="font-medium text-green-700">Active</div><div className="text-muted-foreground">{status.email}</div></div> : <div className="space-y-2"><div className="text-sm font-medium text-amber-700">{status.status === "pending" ? "Invitation Pending" : status.status === "expired" ? "Invitation Expired" : "Not Activated"}</div>{status.expiresAtUtc && <div className="text-xs text-muted-foreground">Expires {new Intl.DateTimeFormat("en-GB", { dateStyle: "medium" }).format(new Date(status.expiresAtUtc))}</div>}<div className="flex flex-wrap gap-2"><Button type="button" size="sm" variant="outline" onClick={generate}>{status.status === "pending" ? "Regenerate Invite" : "Generate Invite"}</Button>{link && <Button type="button" size="sm" variant="outline" onClick={() => navigator.clipboard?.writeText(link)}>Copy Invite Link</Button>}{status.status === "pending" && <Button type="button" size="sm" variant="ghost" onClick={revoke}>Revoke</Button>}</div></div>}</section>;
}
