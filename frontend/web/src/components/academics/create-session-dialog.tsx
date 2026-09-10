"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { LoaderCircle, Plus } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";

export function CreateSessionDialog({
  triggerLabel = "Create Session",
}: {
  tenantSlug?: string;
  triggerLabel?: string;
}) {
  const router = useRouter();

  const [open, setOpen] = useState(false);
  const [name, setName] = useState("");
  const [startDate, setStartDate] =
    useState("");
  const [endDate, setEndDate] =
    useState("");
  const [isCurrent, setIsCurrent] =
    useState(true);

  const [loading, setLoading] =
    useState(false);
  const [error, setError] =
    useState<string | null>(null);

  async function submit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setLoading(true);
    setError(null);

    try {
      const response = await fetch(
        "/api/academics/sessions",
        {
          method: "POST",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            name,
            startDate,
            endDate,
            isCurrent,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to create session."
        );
        return;
      }

      setOpen(false);
      setName("");
      setStartDate("");
      setEndDate("");

      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={setOpen}
    >
      <DialogTrigger
        render={
          <Button className="bg-tenant-primary text-black hover:bg-tenant-primary/90">
            <Plus className="mr-2 h-4 w-4" />
            {triggerLabel}
          </Button>
        }
      />

      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            Create Academic Session
          </DialogTitle>

          <DialogDescription>
            Create a new academic session for
            this school.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={submit}
          className="space-y-5 pt-2"
        >
          <div className="space-y-2">
            <label
              htmlFor="session-name"
              className="text-sm font-medium"
            >
              Session Name
            </label>

            <Input
              id="session-name"
              placeholder="2027/2028"
              value={name}
              onChange={(event) =>
                setName(event.target.value)
              }
              required
            />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <label
                htmlFor="session-start"
                className="text-sm font-medium"
              >
                Start Date
              </label>

              <Input
                id="session-start"
                type="date"
                value={startDate}
                onChange={(event) =>
                  setStartDate(
                    event.target.value
                  )
                }
                required
              />
            </div>

            <div className="space-y-2">
              <label
                htmlFor="session-end"
                className="text-sm font-medium"
              >
                End Date
              </label>

              <Input
                id="session-end"
                type="date"
                value={endDate}
                onChange={(event) =>
                  setEndDate(
                    event.target.value
                  )
                }
                required
              />
            </div>
          </div>

          <label className="flex items-center gap-3 rounded-lg border p-3">
            <input
              type="checkbox"
              checked={isCurrent}
              onChange={(event) =>
                setIsCurrent(
                  event.target.checked
                )
              }
              className="h-4 w-4 accent-yellow-400"
            />

            <div>
              <div className="text-sm font-medium">
                Make current session
              </div>

              <div className="text-xs text-muted-foreground">
                This becomes the active academic
                session.
              </div>
            </div>
          </label>

          {error && (
            <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              {error}
            </div>
          )}

          <div className="flex justify-end gap-3">
            <Button
              type="button"
              variant="outline"
              onClick={() =>
                setOpen(false)
              }
            >
              Cancel
            </Button>

            <Button
              type="submit"
              disabled={loading}
              className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
            >
              {loading && (
                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
              )}

              Create Session
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
