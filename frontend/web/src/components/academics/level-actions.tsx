"use client";

import {
  FormEvent,
  useEffect,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  CircleOff,
  CirclePlay,
  LoaderCircle,
  MoreVertical,
  Pencil,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import type { AcademicLevel } from "@/types/academics";

type LevelActionsProps = {
  level: AcademicLevel;
};

export function LevelActions({
  level,
}: LevelActionsProps) {
  const router = useRouter();

  const [editOpen, setEditOpen] =
    useState(false);

  const [name, setName] =
    useState(level.name);

  const [category, setCategory] =
    useState(level.category);

  const [sortOrder, setSortOrder] =
    useState(level.sortOrder);

  const [saving, setSaving] =
    useState(false);

  const [changingStatus, setChangingStatus] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    if (!editOpen) {
      return;
    }

    setName(level.name);
    setCategory(level.category);
    setSortOrder(level.sortOrder);
    setError(null);
  }, [
    editOpen,
    level.name,
    level.category,
    level.sortOrder,
  ]);

  async function updateLevel(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    setSaving(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/academics/levels/${level.id}`,
        {
          method: "PATCH",
          headers: {
            "Content-Type":
              "application/json",
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
            "Unable to update academic level."
        );
        return;
      }

      setEditOpen(false);
      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setSaving(false);
    }
  }

  async function changeStatus() {
    setChangingStatus(true);
    setError(null);

    try {
      const response = await fetch(
        `/api/academics/levels/${level.id}/status`,
        {
          method: "PATCH",
          headers: {
            "Content-Type":
              "application/json",
          },
          body: JSON.stringify({
            isActive: !level.isActive,
          }),
        }
      );

      const result = await response.json();

      if (!response.ok) {
        window.alert(
          result.error ??
            "Unable to change level status."
        );

        return;
      }

      router.refresh();
    } catch {
      window.alert(
        "Unable to connect to the server."
      );
    } finally {
      setChangingStatus(false);
    }
  }

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger
          render={
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              aria-label={`Actions for ${level.name}`}
            >
              <MoreVertical className="h-4 w-4" />
            </Button>
          }
        />

        <DropdownMenuContent align="end">
          <DropdownMenuItem
            onClick={() =>
              setEditOpen(true)
            }
          >
            <Pencil className="mr-2 h-4 w-4" />
            Edit Level
          </DropdownMenuItem>

          <DropdownMenuItem
            disabled={changingStatus}
            onClick={changeStatus}
          >
            {level.isActive ? (
              <>
                <CircleOff className="mr-2 h-4 w-4" />
                Deactivate
              </>
            ) : (
              <>
                <CirclePlay className="mr-2 h-4 w-4" />
                Activate
              </>
            )}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <Dialog
        open={editOpen}
        onOpenChange={setEditOpen}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              Edit Academic Level
            </DialogTitle>

            <DialogDescription>
              Update the name, category or
              display order for this level.
            </DialogDescription>
          </DialogHeader>

          <form
            onSubmit={updateLevel}
            className="space-y-5 pt-2"
          >
            <div className="space-y-2">
              <label
                htmlFor={`level-name-${level.id}`}
                className="text-sm font-medium"
              >
                Level Name
              </label>

              <Input
                id={`level-name-${level.id}`}
                value={name}
                onChange={(event) =>
                  setName(event.target.value)
                }
                required
              />
            </div>

            <div className="space-y-2">
              <label
                htmlFor={`level-category-${level.id}`}
                className="text-sm font-medium"
              >
                Category
              </label>

              <select
                id={`level-category-${level.id}`}
                value={category}
                onChange={(event) =>
                  setCategory(
                    event.target.value
                  )
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
                htmlFor={`level-order-${level.id}`}
                className="text-sm font-medium"
              >
                Display Order
              </label>

              <Input
                id={`level-order-${level.id}`}
                type="number"
                min={1}
                value={sortOrder}
                onChange={(event) =>
                  setSortOrder(
                    Number(
                      event.target.value
                    )
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
                  setEditOpen(false)
                }
              >
                Cancel
              </Button>

              <Button
                type="submit"
                disabled={saving}
                className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
              >
                {saving && (
                  <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
                )}

                Save Changes
              </Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}
