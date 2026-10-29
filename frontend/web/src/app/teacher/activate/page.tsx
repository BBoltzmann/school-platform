"use client";
import { FormEvent, Suspense, useEffect, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { PASSWORD_DESCRIPTION, validPassword } from "@/lib/auth/recovery";

function TeacherActivateForm() {
  const params = useSearchParams(); const router = useRouter(); const token = params.get("token") ?? "";
  const [preview, setPreview] = useState<{ tenantName: string; staffName: string; email?: string | null } | null>(null); const [email, setEmail] = useState(""); const [password, setPassword] = useState(""); const [confirm, setConfirm] = useState(""); const [error, setError] = useState<string | null>(null); const [done, setDone] = useState(false); const [loading, setLoading] = useState(Boolean(token));
  useEffect(() => { if (!token) return; fetch(`/api/auth/teacher-invite/${encodeURIComponent(token)}`).then(async response => { if (response.ok) { const result = await response.json(); setPreview(result); setEmail(result.email ?? ""); } else setError("This invitation is invalid or expired."); }).catch(() => setError("Unable to load this invitation.")).finally(() => setLoading(false)); }, [token]);
  const [submitting, setSubmitting] = useState(false);
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (submitting) return;
    setError(null);
    if (!validPassword(password)) { setError(PASSWORD_DESCRIPTION); return; }
    if (password !== confirm) { setError("Passwords do not match."); return; }
    setSubmitting(true);
    try {
      const response = await fetch("/api/auth/teacher-invite/activate", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token, email, password }),
      });
      const result = await response.json();
      if (!response.ok) { setError(result.error ?? "Activation could not be completed."); return; }
      setDone(true);
      setTimeout(() => router.push(`/schools/${encodeURIComponent(result.tenantSlug)}/staff/login`), 700);
    } catch {
      setError("Unable to contact the invitation service. Please try again.");
    } finally {
      setSubmitting(false);
    }
  }
  const displayError = error ?? (!token ? "This invitation is invalid or expired." : null);
  return <main className="flex min-h-screen items-center justify-center bg-muted/30 px-5 py-12"><section className="w-full max-w-md rounded-2xl border bg-card p-7 shadow-sm"><h1 className="text-2xl font-bold">Activate Your Teacher Account</h1>{loading ? <p className="mt-6 text-sm text-muted-foreground">Checking your teacher invitation…</p> : preview && <p className="mt-2 text-sm text-muted-foreground">{preview.tenantName}<br />Welcome, {preview.staffName}.</p>}{done ? <div className="mt-8 rounded-md bg-green-50 p-4 text-sm text-green-800">Your Teacher Account Is Ready. Redirecting to login…</div> : !loading && <form onSubmit={submit} className="mt-6 space-y-4"><label className="block text-sm font-medium">Email Address<Input type="email" value={email} onChange={event => setEmail(event.target.value)} readOnly={Boolean(preview?.email)} required className="mt-1" /></label><p id="teacher-password-requirements" className="text-sm text-muted-foreground">{PASSWORD_DESCRIPTION}</p><label className="block text-sm font-medium">Password<Input type="password" value={password} onChange={event => setPassword(event.target.value)} autoComplete="new-password" minLength={12} maxLength={128} aria-describedby="teacher-password-requirements" required className="mt-1" /></label><label className="block text-sm font-medium">Confirm Password<Input type="password" value={confirm} onChange={event => setConfirm(event.target.value)} autoComplete="new-password" minLength={12} maxLength={128} required className="mt-1" /></label>{displayError && <p role="alert" className="rounded-md bg-red-50 p-3 text-sm text-red-700">{displayError}</p>}<Button type="submit" disabled={!preview || submitting} className="w-full bg-tenant-primary text-black">{submitting ? "Activating…" : "Activate Account"}</Button></form>}</section></main>;
}

export default function TeacherActivatePage() {
  return <Suspense fallback={<main className="flex min-h-screen items-center justify-center">Loading invitation…</main>}><TeacherActivateForm /></Suspense>;
}
