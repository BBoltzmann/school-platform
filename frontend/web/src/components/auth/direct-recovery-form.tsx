"use client";

import { type FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { directResetPayload, loginDestination, PASSWORD_DESCRIPTION } from "@/lib/auth/recovery";

// TEMPORARY recovery-code UI. The code is entered by the operator, never configured here.
export function DirectRecoveryForm({ initialTenantSlug = "antioch-college" }: { initialTenantSlug?: string }) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    const form = event.currentTarget;
    const payload = directResetPayload(new FormData(form));
    if ("error" in payload) { setError(payload.error); return; }
    setLoading(true);
    try {
      const response = await fetch("/api/auth/direct-password-reset", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload.body),
      });
      const result = await response.json();
      if (!response.ok) { setError(result.error ?? "Unable to change password. Please try again later."); return; }
      form.reset();
      router.replace(loginDestination(result.tenantSlug, true));
    } catch { setError("The authentication service is unavailable. Please try again later."); }
    finally { setLoading(false); }
  }

  return <form onSubmit={submit} className="space-y-5">
    <label className="block space-y-2"><span>Email address</span>
      <Input name="email" type="email" autoComplete="email" maxLength={320}
        defaultValue={initialTenantSlug === "antioch-college" ? "antiochcollege41@gmail.com" : ""} required />
    </label>
    <label className="block space-y-2"><span>School slug</span>
      <Input name="tenantSlug" defaultValue={initialTenantSlug} minLength={3} maxLength={100} pattern="[a-z0-9]+(-[a-z0-9]+)*" required />
    </label>
    <label className="block space-y-2"><span>Recovery code</span>
      <Input name="recoveryCode" type="password" autoComplete="off" maxLength={256} required />
    </label>
    <p id="direct-password-requirements" className="text-sm text-muted-foreground">{PASSWORD_DESCRIPTION}</p>
    <label className="block space-y-2"><span>New password</span>
      <Input name="newPassword" type="password" autoComplete="new-password" minLength={12} maxLength={128} aria-describedby="direct-password-requirements" required />
    </label>
    <label className="block space-y-2"><span>Confirm new password</span>
      <Input name="confirmPassword" type="password" autoComplete="new-password" minLength={12} maxLength={128} required />
    </label>
    {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
    <Button type="submit" disabled={loading} className="w-full">{loading ? "Changing password…" : "Change password"}</Button>
  </form>;
}
