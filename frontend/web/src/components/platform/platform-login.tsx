"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
export function PlatformLogin() {
  const router = useRouter(); const [email, setEmail] = useState(""); const [password, setPassword] = useState(""); const [error, setError] = useState(""); const [loading, setLoading] = useState(false);
  async function submit(event: React.FormEvent<HTMLFormElement>) { event.preventDefault(); setLoading(true); setError(""); try { const response = await fetch("/api/auth/platform-login", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email, password }) }); const result = await response.json(); if (!response.ok) { setError(result.error ?? "Unable to sign in."); return; } router.push("/super-admin"); router.refresh(); } catch { setError("Unable to connect to the server."); } finally { setLoading(false); } }
  return <main className="mx-auto flex min-h-screen max-w-md items-center px-6"><form onSubmit={submit} className="w-full space-y-5 rounded-xl border bg-card p-6"><h1 className="text-2xl font-semibold">Platform administration</h1><p className="text-sm text-muted-foreground">Sign in with a Platform Super Admin account.</p><label className="block space-y-2 text-sm">Email<Input type="email" value={email} onChange={e => setEmail(e.target.value)} required /></label><label className="block space-y-2 text-sm">Password<Input type="password" value={password} onChange={e => setPassword(e.target.value)} required /></label>{error && <p role="alert" className="text-sm text-red-700">{error}</p>}<Button type="submit" className="w-full" disabled={loading}>{loading ? "Signing in…" : "Sign in"}</Button></form></main>;
}
