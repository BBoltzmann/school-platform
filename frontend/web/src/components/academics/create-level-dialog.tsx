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

export function CreateLevelDialog() {
  const router = useRouter();

  const [open, setOpen] = useState(false);
  const [name, setName] = useState("");
  const [category, setCategory] = useState("Primary");
  const [sortOrder, setSortOrder] = useState(1);

  const [loading, setLoading] = useState(false);
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
        "/api/academics/levels",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            name,
            category,
            sortOrder,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to create academic level."
        );
        return;
      }

      setOpen(false);
      setName("");
      setSortOrder((current) => current + 1);

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
            Add Level
          </Button>
        }
      />

      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            Add Academic Level
          </DialogTitle>

          <DialogDescription>
            Create a level such as Primary 1,
            JSS 1 or SSS 3.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit}
          className="space-y-5 pt-2"
        >
          <div className="space-y-2">
            <label
              htmlFor="level-name"
              className="text-sm font-medium"
            >
              Level Name
            </label>

            <Input
              id="level-name"
              placeholder="Primary 1"
              value={name}
              onChange={(event) =>
                setName(event.target.value)
              }
              required
            />
          </div>

          <div className="space-y-2">
            <label
              htmlFor="level-category"
              className="text-sm font-medium"
            >
              Category
            </label>

            <select
              id="level-category"
              value={category}
              onChange={(event) =>
                setCategory(event.target.value)
              }
              className="h-10 w-full rounded-md border bg-background px-3 text-sm"
            >
              <option value="Creche">
                Creche
              </option>

              <option value="Nursery">
                Nursery
              </option>

              <option value="Primary">
                Primary
              </option>

              <option value="Junior Secondary">
                Junior Secondary
              </option>

              <option value="Senior Secondary">
                Senior Secondary
              </option>
            </select>
          </div>

          <div className="space-y-2">
            <label
              htmlFor="level-order"
              className="text-sm font-medium"
            >
              Display Order
            </label>

            <Input
              id="level-order"
              type="number"
              min={1}
              value={sortOrder}
              onChange={(event) =>
                setSortOrder(
                  Number(event.target.value)
                )
              }
              required
            />
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
              onClick={() => setOpen(false)}
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

              Add Level
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
