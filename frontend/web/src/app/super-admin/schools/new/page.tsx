"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

type Form = {
  name: string; slug: string; campusName: string; administratorFirstName: string;
  administratorLastName: string; administratorEmail: string; websiteUrl: string;
  contactEmail: string; contactPhone: string; address: string; motto: string;
  mission: string; vision: string; shortAbout: string; primaryColor: string;
  secondaryColor: string; accentColor: string; logoDataUrl?: string; iconDataUrl?: string;
};
const initial: Form = { name: "", slug: "", campusName: "", administratorFirstName: "", administratorLastName: "", administratorEmail: "", websiteUrl: "", contactEmail: "", contactPhone: "", address: "", motto: "", mission: "", vision: "", shortAbout: "", primaryColor: "#F5D900", secondaryColor: "#0B0B0B", accentColor: "#FFF8C9" };

function Preview({ form }: { form: Form }) {
  return <div className="overflow-hidden rounded-xl border" style={{ background: form.secondaryColor, color: "#fff" }}><div className="p-6"><div className="flex items-center gap-3">{form.logoDataUrl ? <img src={form.logoDataUrl} alt="School logo preview" className="h-12 w-12 rounded object-contain" /> : <div className="flex h-12 w-12 items-center justify-center rounded font-black" style={{ background: form.primaryColor, color: form.secondaryColor }}>{(form.name || "SCH").slice(0, 3).toUpperCase()}</div>}<div className="font-semibold">{form.name || "Your school name"}</div></div><p className="mt-8 text-sm font-bold uppercase tracking-wider" style={{ color: form.primaryColor }}>{form.motto || "Your school motto"}</p><h3 className="mt-3 text-2xl font-bold">Welcome back</h3><p className="mt-2 text-sm opacity-70">Sign in with your school slug and account details.</p></div></div>;
}

export default function Page() {
  const router = useRouter();
  const [step, setStep] = useState(1);
  const [form, setForm] = useState<Form>(initial);
  const [screenshots, setScreenshots] = useState<string[]>([]);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const [scanning, setScanning] = useState(false);
  const update = (key: keyof Form, value: string) => setForm(current => ({ ...current, [key]: value }));
  async function scan() {
    setScanning(true); setError(""); setMessage("");
    try {
      const response = await fetch("/api/platform-admin/school-import/website", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ url: form.websiteUrl }) });
      const draft = await response.json();
      if (!response.ok) throw new Error(draft.error ?? "Website scan failed.");
      setForm(current => ({ ...current, name: current.name || draft.nameCandidates?.[0] || current.name, websiteUrl: draft.finalUrl || current.websiteUrl, contactEmail: current.contactEmail || draft.emails?.[0] || "" }));
      setMessage("Website findings loaded as an editable draft. Nothing has been saved yet."); setStep(3);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Website scan failed. You can continue manually."); }
    finally { setScanning(false); }
  }
  const fileAsData = (key: "logoDataUrl" | "iconDataUrl") => (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]; if (!file) return;
    if (!["image/png", "image/jpeg", "image/webp"].includes(file.type) || file.size > 1_500_000) { setError("Use a PNG, JPEG or WEBP image up to 1.5 MB."); return; }
    const reader = new FileReader(); reader.onload = () => setForm(current => ({ ...current, [key]: String(reader.result) })); reader.readAsDataURL(file);
  };
  const addScreenshots = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? []).slice(0, 3);
    Promise.all(files.map(file => new Promise<string>(resolve => { const reader = new FileReader(); reader.onload = () => resolve(String(reader.result)); reader.readAsDataURL(file); }))).then(setScreenshots);
  };
  async function submit(event: React.FormEvent) {
    event.preventDefault(); setSaving(true); setError("");
    try {
      const response = await fetch("/api/platform-admin/schools", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: form.name, slug: form.slug, campusName: form.campusName, administratorFirstName: form.administratorFirstName, administratorLastName: form.administratorLastName, administratorEmail: form.administratorEmail }) });
      const result = await response.json(); if (!response.ok) throw new Error(result.error ?? "Unable to create school.");
      const tenantId = result.school?.summary?.tenantId ?? result.summary?.tenantId;
      const branding = await fetch(`/api/platform-admin/schools/${tenantId}/branding`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(form) });
      if (!branding.ok) throw new Error((await branding.json()).error ?? "School created, but branding could not be saved.");
      router.push(`/super-admin/schools/${tenantId}`);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Unable to create school."); }
    finally { setSaving(false); }
  }
  const schoolFields: [keyof Form, string][] = [["name", "School name"], ["slug", "Slug"], ["campusName", "Campus name"], ["administratorFirstName", "Administrator first name"], ["administratorLastName", "Administrator last name"], ["administratorEmail", "Administrator email"]];
  return <main className="mx-auto max-w-4xl space-y-6 p-8"><header><p className="text-sm text-muted-foreground">Platform onboarding</p><h1 className="text-3xl font-semibold">Add School</h1><p className="mt-2 text-sm text-muted-foreground">Create the school first, then review optional identity and branding before saving.</p></header><div className="flex flex-wrap gap-2 text-sm">{["School & Administrator", "Website & Branding", "Review Identity", "Preview & Create"].map((label, index) => <div key={label} className={`rounded-full px-3 py-1 ${step === index + 1 ? "bg-primary font-semibold" : "bg-muted text-muted-foreground"}`}>{index + 1}. {label}</div>)}</div><form onSubmit={submit} className="space-y-6 rounded-xl border bg-card p-6">
    {step === 1 && <section className="grid gap-4 sm:grid-cols-2">{schoolFields.map(([key, label]) => <label className="block space-y-2 text-sm" key={key}>{label}<Input type={key === "administratorEmail" ? "email" : "text"} value={form[key] as string} onChange={event => update(key, event.target.value)} required /></label>)}</section>}
    {step === 2 && <section className="space-y-5"><label className="block space-y-2 text-sm">Website URL<Input type="url" value={form.websiteUrl} onChange={event => update("websiteUrl", event.target.value)} placeholder="https://school.example" /></label><Button type="button" onClick={scan} disabled={scanning || !form.websiteUrl}>{scanning ? "Scanning…" : "Scan Website"}</Button><div className="grid gap-4 sm:grid-cols-2"><label className="block space-y-2 text-sm">Logo<input type="file" accept="image/png,image/jpeg,image/webp" onChange={fileAsData("logoDataUrl")} /></label><label className="block space-y-2 text-sm">Icon / favicon<input type="file" accept="image/png,image/jpeg,image/webp" onChange={fileAsData("iconDataUrl")} /></label></div><label className="block space-y-2 text-sm">Reference screenshots (up to 3)<input type="file" accept="image/png,image/jpeg,image/webp" multiple onChange={addScreenshots} /></label>{screenshots.length > 0 && <div className="grid grid-cols-3 gap-3">{screenshots.map(image => <img key={image} src={image} alt="Reference screenshot" className="h-24 w-full rounded border object-cover" />)}</div>}</section>}
    {step === 3 && <section className="grid gap-4 sm:grid-cols-2"><label className="space-y-2 text-sm sm:col-span-2">Display name<Input value={form.name} onChange={event => update("name", event.target.value)} required /></label>{(["contactEmail", "contactPhone", "address", "motto", "primaryColor", "secondaryColor", "accentColor"] as (keyof Form)[]).map(key => <label className="block space-y-2 text-sm" key={key}>{key}<Input value={form[key] as string} onChange={event => update(key, event.target.value)} /></label>)}{(["mission", "vision", "shortAbout"] as (keyof Form)[]).map(key => <label className="block space-y-2 text-sm sm:col-span-2" key={key}>{key}<textarea className="min-h-24 w-full rounded-md border bg-background p-3 text-sm" value={form[key] as string} onChange={event => update(key, event.target.value)} /></label>)}</section>}
    {step === 4 && <section className="space-y-5"><h2 className="text-xl font-semibold">Preview &amp; Create</h2><Preview form={form} /><p className="text-sm text-muted-foreground">Website scanning is advisory and optional. Nothing is finalized until creation.</p></section>}
    {message && <p role="status" className="text-sm text-green-700">{message}</p>}{error && <p role="alert" className="text-sm text-red-700">{error}</p>}<div className="flex justify-between gap-3"><Button type="button" variant="outline" onClick={() => step === 1 ? router.back() : setStep(step - 1)}>{step === 1 ? "Cancel" : "Back"}</Button>{step < 4 ? <Button type="button" onClick={() => setStep(step + 1)}>{step === 2 && !form.websiteUrl ? "Continue without website" : "Continue"}</Button> : <Button type="submit" disabled={saving}>{saving ? "Creating…" : "Create School"}</Button>}</div>
  </form></main>;
}
