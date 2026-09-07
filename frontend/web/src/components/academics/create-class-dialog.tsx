"use client";

import { FormEvent, useEffect, useState } from "react";
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
import type {
  AcademicLevel,
  Campus,
} from "@/types/academics";

type CreateClassDialogProps = {
  campuses: Campus[];
  levels: AcademicLevel[];
};

export function CreateClassDialog({
  campuses,
  levels,
}: CreateClassDialogProps) {
  const router = useRouter();

  const [open, setOpen] = useState(false);
  const [name, setName] = useState("");

  const [campusId, setCampusId] =
    useState(campuses[0]?.id ?? "");

  const [
    academicLevelId,
    setAcademicLevelId,
  ] = useState(levels[0]?.id ?? "");

  const [loading, setLoading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    if (!campusId && campuses.length > 0) {
      setCampusId(campuses[0].id);
    }

    if (
      !academicLevelId &&
      levels.length > 0
    ) {
      setAcademicLevelId(levels[0].id);
    }
  }, [
    campuses,
    levels,
    campusId,
    academicLevelId,
  ]);

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setLoading(true);
    setError(null);

    try {
      const response = await fetch(
        "/api/academics/classes",
        {
          method: "POST",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            campusId,
            academicLevelId,
            name,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to create class."
        );
        return;
      }

      setOpen(false);
      setName("");

      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setLoading(false);
    }
  }

  const disabled =
    campuses.length === 0 ||
    levels.length === 0;

  return (
    <Dialog
      open={open}
      onOpenChange={setOpen}
    >
      <DialogTrigger
        render={
          <Button
            size="sm"
            disabled={disabled}
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            <Plus className="mr-2 h-4 w-4" />
            Add Class
          </Button>
        }
      />

      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            Add Class
          </DialogTitle>

          <DialogDescription>
            Create a class under an academic
            level and campus.
          </DialogDescription>
        </DialogHeader>

        {disabled ? (
          <div className="rounded-md border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
            You need at least one active campus
            and one academic level before
            creating a class.
          </div>
        ) : (
          <form
            onSubmit={handleSubmit}
            className="space-y-5 pt-2"
          >
            <div className="space-y-2">
              <label
                htmlFor="class-name"
                className="text-sm font-medium"
              >
                Class Name
              </label>

              <Input
                id="class-name"
                placeholder="Primary 1A"
                value={name}
                onChange={(event) =>
                  setName(event.target.value)
                }
                required
              />
            </div>

            <div className="space-y-2">
              <label
                htmlFor="class-level"
                className="text-sm font-medium"
              >
                Academic Level
              </label>

              <select
                id="class-level"
                value={academicLevelId}
                onChange={(event) =>
                  setAcademicLevelId(
                    event.target.value
                  )
                }
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                required
              >
                {levels.map((level) => (
                  <option
                    key={level.id}
                    value={level.id}
                  >
                    {level.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-2">
              <label
                htmlFor="class-campus"
                className="text-sm font-medium"
              >
                Campus
              </label>

              <select
                id="class-campus"
                value={campusId}
                onChange={(event) =>
                  setCampusId(
                    event.target.value
                  )
                }
                className="h-10 w-full rounded-md border bg-background px-3 text-sm"
                required
              >
                {campuses.map((campus) => (
                  <option
                    key={campus.id}
                    value={campus.id}
                  >
                    {campus.name}
                  </option>
                ))}
              </select>
            </div>

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

                Add Class
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
