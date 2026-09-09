"use client";

import Link from "next/link";
import { type FormEvent, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { loginDestination, PASSWORD_DESCRIPTION, SIGNUP_MESSAGE, validPassword } from "@/lib/auth/recovery";

export function CreateSchoolForm() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [completedSlug, setCompletedSlug] = useState("");
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    const data = new FormData(event.currentTarget);
    const password = String(data.get("password") ?? "");
    if (!validPassword(password)) { setError(PASSWORD_DESCRIPTION); return; }
    if (password !== data.get("confirmPassword")) { setError("Passwords do not match."); return; }
    const body = Object.fromEntries(["schoolName", "slug", "campusName", "adminEmail", "adminFirstName", "adminLastName"]
      .map(key => [key, String(data.get(key) ?? "").trim()]));
    body.slug = body.slug.toLowerCase();
    setLoading(true);
    try {
      const response = await fetch("/api/auth/signup", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ ...body, password }) });
      const result = await response.json();
      if (!response.ok) { setError(result.error ?? "Unable to create the school. Please try again later."); return; }
      setCompletedSlug(body.slug);
    } catch { setError("The authentication service is unavailable. Please try again later."); }
    finally { setLoading(false); }
  }
  if (completedSlug) return <div className="space-y-4"><p role="status" className="text-sm">{SIGNUP_MESSAGE}</p>
    <Link href={loginDestination(completedSlug)} className="underline">Sign in to your school</Link></div>;
  return <form onSubmit={submit} className="space-y-4">
    {[
      { name: "schoolName", label: "School name", max: 200 },
      { name: "slug", label: "School slug", max: 100, pattern: "[a-z0-9]+(-[a-z0-9]+)*" },
      { name: "campusName", label: "Primary campus name", max: 200 },
      { name: "adminFirstName", label: "Your first name", max: 100, autoComplete: "given-name" },
      { name: "adminLastName", label: "Your last name", max: 100, autoComplete: "family-name" },
      { name: "adminEmail", label: "Your email address", max: 320, type: "email", autoComplete: "email" },
    ].map(field => <label key={field.name} className="block space-y-1 text-sm"><span>{field.label}</span>
      <Input name={field.name} type={field.type ?? "text"} maxLength={field.max} minLength={field.name === "slug" ? 3 : 1} pattern={field.pattern} autoComplete={field.autoComplete} required />
    </label>)}
    <p id="signup-password-requirements" className="text-sm text-muted-foreground">{PASSWORD_DESCRIPTION}</p>
    <label className="block space-y-1 text-sm"><span>Password</span>
      <Input name="password" type="password" autoComplete="new-password" minLength={12} maxLength={128} aria-describedby="signup-password-requirements" required /></label>
    <label className="block space-y-1 text-sm"><span>Confirm password</span>
      <Input name="confirmPassword" type="password" autoComplete="new-password" minLength={12} maxLength={128} required /></label>
    {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
    <Button type="submit" disabled={loading} className="w-full">{loading ? "Creating school…" : "Create school"}</Button>
  </form>;
}
