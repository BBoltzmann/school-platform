"use client";

import { type FormEvent, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { loginDestination, PASSWORD_DESCRIPTION, RECOVERY_MESSAGE, validPassword } from "@/lib/auth/recovery";

export function RecoveryForm({ mode, initialTenantSlug = "antioch-college" }: {
  mode: "forgot" | "reset"; initialTenantSlug?: string;
}) {
  const router = useRouter();
  const token = useRef("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    if (mode !== "reset") return;
    const url = new URL(window.location.href);
    const supplied = url.searchParams.get("token");
    if (supplied) token.current = supplied;
    url.searchParams.delete("token");
    window.history.replaceState(window.history.state, "", url.pathname + url.search);
  }, [mode]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    const data = new FormData(event.currentTarget);
    const password = String(data.get("newPassword") ?? "");
    if (mode === "reset") {
      if (!token.current) { setError("This reset link is missing its token. Request a new link."); return; }
      if (!validPassword(password)) { setError(PASSWORD_DESCRIPTION); return; }
      if (password !== data.get("confirmPassword")) { setError("Passwords do not match."); return; }
    }
    setLoading(true);
    try {
      const response = await fetch(`/api/auth/${mode === "forgot" ? "forgot-password" : "reset-password"}`, {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify(mode === "forgot"
          ? { email: String(data.get("email")).trim(), tenantSlug: String(data.get("tenantSlug")).trim().toLowerCase() }
          : { token: token.current, newPassword: password }),
      });
      const result = await response.json();
      if (!response.ok) { setError(result.error ?? "The authentication service is unavailable."); return; }
      if (mode === "forgot") setMessage(RECOVERY_MESSAGE);
      else {
        token.current = "";
        router.replace(loginDestination(result.tenantSlug, true));
      }
    } catch { setError("The authentication service is unavailable. Please try again later."); }
    finally { setLoading(false); }
  }

  if (message) return <p role="status" className="rounded-md bg-muted p-4 text-sm">{message}</p>;
  return (
    <form onSubmit={submit} className="space-y-5">
      {mode === "forgot" ? <>
        <label className="block space-y-2"><span>Email address</span>
          <Input name="email" type="email" autoComplete="email" maxLength={320} required />
        </label>
        <label className="block space-y-2"><span>School slug</span>
          <Input name="tenantSlug" defaultValue={initialTenantSlug} minLength={3} maxLength={100} pattern="[a-z0-9]+(-[a-z0-9]+)*" required />
        </label>
      </> : <>
        <p id="password-requirements" className="text-sm text-muted-foreground">{PASSWORD_DESCRIPTION}</p>
        <label className="block space-y-2"><span>New password</span>
          <Input name="newPassword" type="password" autoComplete="new-password" minLength={12} maxLength={128} aria-describedby="password-requirements" required />
        </label>
        <label className="block space-y-2"><span>Confirm password</span>
          <Input name="confirmPassword" type="password" autoComplete="new-password" minLength={12} maxLength={128} required />
        </label>
      </>}
      {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
      <Button type="submit" disabled={loading} className="w-full">{loading ? "Please wait…" : mode === "forgot" ? "Send reset link" : "Reset password"}</Button>
    </form>
  );
}
