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

export function CreateSubjectDialog() {
  const router = useRouter();

  const [open, setOpen] =
    useState(false);

  const [name, setName] =
    useState("");

  const [code, setCode] =
    useState("");

  const [category, setCategory] =
    useState("Core");

  const [loading, setLoading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setLoading(true);
    setError(null);

    try {
      const response = await fetch(
        "/api/academics/subjects",
        {
          method: "POST",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            name,
            code,
            category,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to create subject."
        );
        return;
      }

      setOpen(false);
      setName("");
      setCode("");

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
          <Button
            size="sm"
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            <Plus className="mr-2 h-4 w-4" />
            Add Subject
          </Button>
        }
      />

      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            Add Subject
          </DialogTitle>

          <DialogDescription>
            Add a subject offered by the
            school.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit}
          className="space-y-5 pt-2"
        >
          <div className="space-y-2">
            <label
              htmlFor="subject-name"
              className="text-sm font-medium"
            >
              Subject Name
            </label>

            <Input
              id="subject-name"
              placeholder="Mathematics"
              value={name}
              onChange={(event) =>
                setName(event.target.value)
              }
              required
            />
          </div>

          <div className="space-y-2">
            <label
              htmlFor="subject-code"
              className="text-sm font-medium"
            >
              Subject Code
            </label>

            <Input
              id="subject-code"
              placeholder="MATH"
              value={code}
              onChange={(event) =>
                setCode(
                  event.target.value.toUpperCase()
                )
              }
              required
            />
          </div>

          <div className="space-y-2">
            <label
              htmlFor="subject-category"
              className="text-sm font-medium"
            >
              Category
            </label>

            <select
              id="subject-category"
              value={category}
              onChange={(event) =>
                setCategory(
                  event.target.value
                )
              }
              className="h-10 w-full rounded-md border bg-background px-3 text-sm"
            >
              <option value="Core">
                Core
              </option>

              <option value="Elective">
                Elective
              </option>

              <option value="Vocational">
                Vocational
              </option>

              <option value="Co-Curricular">
                Co-Curricular
              </option>
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

              Add Subject
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
