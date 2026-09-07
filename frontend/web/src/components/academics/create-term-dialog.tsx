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

type CreateTermDialogProps = {
  academicSessionId: string;
};

export function CreateTermDialog({
  academicSessionId,
}: CreateTermDialogProps) {
  const router = useRouter();

  const [open, setOpen] = useState(false);
  const [name, setName] = useState("");
  const [startDate, setStartDate] =
    useState("");
  const [endDate, setEndDate] =
    useState("");
  const [sortOrder, setSortOrder] =
    useState(1);

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
        "/api/academics/terms",
        {
          method: "POST",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            academicSessionId,
            name,
            startDate,
            endDate,
            sortOrder,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to create term."
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
          <Button
            variant="outline"
            size="sm"
          >
            <Plus className="mr-2 h-4 w-4" />
            Add Term
          </Button>
        }
      />

      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            Add Academic Term
          </DialogTitle>

          <DialogDescription>
            Add a term to the current academic
            session.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={submit}
          className="space-y-5 pt-2"
        >
          <div className="space-y-2">
            <label
              htmlFor="term-name"
              className="text-sm font-medium"
            >
              Term Name
            </label>

            <Input
              id="term-name"
              placeholder="First Term"
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
                htmlFor="term-start"
                className="text-sm font-medium"
              >
                Start Date
              </label>

              <Input
                id="term-start"
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
                htmlFor="term-end"
                className="text-sm font-medium"
              >
                End Date
              </label>

              <Input
                id="term-end"
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

          <div className="space-y-2">
            <label
              htmlFor="term-order"
              className="text-sm font-medium"
            >
              Term Order
            </label>

            <Input
              id="term-order"
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

              Add Term
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
