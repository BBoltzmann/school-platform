"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { Building2, LoaderCircle, Plus } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";

export function CreateCampusDialog() {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const response = await fetch("/api/academics/campuses", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name }),
      });
      const result = await response.json();
      if (!response.ok) {
        setError(result.error ?? "Unable to create campus.");
        return;
      }
      setName("");
      setOpen(false);
      router.refresh();
    } catch {
      setError("Unable to connect to the server.");
    } finally {
      setLoading(false);
    }
  }

  return <Dialog open={open} onOpenChange={setOpen}>
    <DialogTrigger render={<Button size="sm" className="bg-tenant-primary text-black hover:bg-tenant-primary/90"><Plus className="mr-2 h-4 w-4" />Add campus</Button>} />
    <DialogContent>
      <DialogHeader>
        <DialogTitle className="flex items-center gap-2"><Building2 className="h-5 w-5" />Add campus</DialogTitle>
        <DialogDescription>Add another campus under this school tenant. It will be available when creating classes.</DialogDescription>
      </DialogHeader>
      <form onSubmit={submit} className="space-y-4">
        <label className="block space-y-2 text-sm font-medium">Campus name<Input autoFocus required value={name} onChange={event => setName(event.target.value)} placeholder="Primary School" /></label>
        {error && <p className="text-sm text-red-700">{error}</p>}
        <Button type="submit" disabled={loading || !name.trim()}>{loading && <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />}{loading ? "Creating…" : "Create campus"}</Button>
      </form>
    </DialogContent>
  </Dialog>;
}
