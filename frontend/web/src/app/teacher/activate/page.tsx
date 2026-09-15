"use client";
import { FormEvent, Suspense, useEffect, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

function TeacherActivateForm() {
  const params = useSearchParams(); const router = useRouter(); const token = params.get("token") ?? "";
  const [preview, setPreview] = useState<{ tenantName: string; staffName: string } | null>(null); const [email, setEmail] = useState(""); const [password, setPassword] = useState(""); const [confirm, setConfirm] = useState(""); const [error, setError] = useState<string | null>(null); const [done, setDone] = useState(false);
  useEffect(() => { if (token) fetch(`/api/auth/teacher-invite/${token}`).then(async response => { if (response.ok) setPreview(await response.json()); else setError("This invitation is invalid or expired."); }).catch(() => setError("Unable to load this invitation.")); }, [token]);
  async function submit(event: FormEvent) { event.preventDefault(); setError(null); if (password !== confirm) { setError("Passwords do not match."); return; } const response = await fetch("/api/auth/teacher-invite/activate", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ token, email, password }) }); const result = await response.json(); if (!response.ok) { setError(result.error ?? "Activation could not be completed."); return; } setDone(true); setTimeout(() => router.push(`/login?tenantSlug=${result.tenantSlug}`), 700); }
  return <main className="flex min-h-screen items-center justify-center bg-muted/30 px-5 py-12"><section className="w-full max-w-md rounded-2xl border bg-card p-7 shadow-sm"><h1 className="text-2xl font-bold">Activate Your Teacher Account</h1>{preview && <p className="mt-2 text-sm text-muted-foreground">{preview.tenantName}<br />Welcome, {preview.staffName}.</p>}{done ? <div className="mt-8 rounded-md bg-green-50 p-4 text-sm text-green-800">Your Teacher Account Is Ready. Redirecting to login…</div> : <form onSubmit={submit} className="mt-6 space-y-4"><label className="block text-sm font-medium">Email Address<Input type="email" value={email} onChange={event => setEmail(event.target.value)} required className="mt-1" /></label><label className="block text-sm font-medium">Password<Input type="password" value={password} onChange={event => setPassword(event.target.value)} required className="mt-1" /></label><label className="block text-sm font-medium">Confirm Password<Input type="password" value={confirm} onChange={event => setConfirm(event.target.value)} required className="mt-1" /></label>{error && <p className="rounded-md bg-red-50 p-3 text-sm text-red-700">{error}</p>}<Button type="submit" disabled={!preview} className="w-full bg-tenant-primary text-black">Activate Account</Button></form>}</section></main>;
}

export default function TeacherActivatePage() {
  return <Suspense fallback={<main className="flex min-h-screen items-center justify-center">Loading invitation…</main>}><TeacherActivateForm /></Suspense>;
}
