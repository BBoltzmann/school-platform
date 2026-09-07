"use client";

import {
  FormEvent,
  useMemo,
  useState,
} from "react";
import { useRouter } from "next/navigation";
import {
  CalendarClock,
  Copy,
  LoaderCircle,
  Plus,
  Save,
  Trash2,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

import type {
  TimetableBlockInput,
  TimetableDayInput,
  TimetableSettings,
} from "@/types/timetable";

const WEEK_DAYS = [
  {
    dayOfWeek: 1,
    label: "Monday",
  },
  {
    dayOfWeek: 2,
    label: "Tuesday",
  },
  {
    dayOfWeek: 3,
    label: "Wednesday",
  },
  {
    dayOfWeek: 4,
    label: "Thursday",
  },
  {
    dayOfWeek: 5,
    label: "Friday",
  },
  {
    dayOfWeek: 6,
    label: "Saturday",
  },
  {
    dayOfWeek: 0,
    label: "Sunday",
  },
];

function normalizeDay(
  value: number | string
) {
  if (typeof value === "number") {
    return value;
  }

  const numeric =
    Number(value);

  if (!Number.isNaN(numeric)) {
    return numeric;
  }

  const mapping: Record<string, number> = {
    sunday: 0,
    monday: 1,
    tuesday: 2,
    wednesday: 3,
    thursday: 4,
    friday: 5,
    saturday: 6,
  };

  return mapping[
    value.toLowerCase()
  ] ?? -1;
}

function shortTime(
  value: string | undefined
) {
  if (!value) {
    return "";
  }

  return value.slice(0, 5);
}

function newClientId() {
  return `${Date.now()}-${Math.random()}`;
}

function createDays(
  settings: TimetableSettings | null
): TimetableDayInput[] {
  return WEEK_DAYS.map(
    (weekday) => {
      const existing =
        settings?.days.find(
          (day) =>
            normalizeDay(
              day.dayOfWeek
            ) ===
            weekday.dayOfWeek
        );

      return {
        dayOfWeek:
          weekday.dayOfWeek,
        label:
          weekday.label,
        enabled:
          Boolean(existing),
        startTime:
          shortTime(
            existing?.startTime
          ),
        endTime:
          shortTime(
            existing?.endTime
          ),
        blocks:
          existing?.nonTeachingBlocks.map(
            (block) => ({
              clientId:
                block.id,
              name:
                block.name,
              startTime:
                shortTime(
                  block.startTime
                ),
              endTime:
                shortTime(
                  block.endTime
                ),
            })
          ) ?? [],
      };
    }
  );
}

function minutesBetween(
  start: string,
  end: string
) {
  if (!start || !end) {
    return 0;
  }

  const [
    startHour,
    startMinute,
  ] = start
    .split(":")
    .map(Number);

  const [
    endHour,
    endMinute,
  ] = end
    .split(":")
    .map(Number);

  return (
    endHour * 60 +
    endMinute -
    (startHour * 60 +
      startMinute)
  );
}

function teachingMinutes(
  day: TimetableDayInput
) {
  if (
    !day.enabled ||
    !day.startTime ||
    !day.endTime
  ) {
    return 0;
  }

  const schoolMinutes =
    minutesBetween(
      day.startTime,
      day.endTime
    );

  const blockedMinutes =
    day.blocks.reduce(
      (total, block) =>
        total +
        Math.max(
          0,
          minutesBetween(
            block.startTime,
            block.endTime
          )
        ),
      0
    );

  return Math.max(
    0,
    schoolMinutes -
      blockedMinutes
  );
}

export function TimetableSettingsEditor({
  settings,
  sessionName,
}: {
  settings: TimetableSettings | null;
  sessionName: string;
}) {
  const router = useRouter();

  const [
    periodDuration,
    setPeriodDuration,
  ] = useState(
    settings?.periodDurationMinutes ??
      40
  );

  const [days, setDays] =
    useState<
      TimetableDayInput[]
    >(() =>
      createDays(settings)
    );

  const [saving, setSaving] =
    useState(false);

  const [error, setError] =
    useState<string | null>(
      null
    );

  const [success, setSuccess] =
    useState<string | null>(
      null
    );

  const weeklyCapacity =
    useMemo(() => {
      if (
        periodDuration <= 0
      ) {
        return 0;
      }

      return days.reduce(
        (total, day) =>
          total +
          Math.floor(
            teachingMinutes(day) /
              periodDuration
          ),
        0
      );
    }, [
      days,
      periodDuration,
    ]);

  function updateDay(
    dayOfWeek: number,
    changes: Partial<TimetableDayInput>
  ) {
    setDays((current) =>
      current.map((day) =>
        day.dayOfWeek ===
        dayOfWeek
          ? {
              ...day,
              ...changes,
            }
          : day
      )
    );

    setSuccess(null);
  }

  function addBlock(
    dayOfWeek: number
  ) {
    setDays((current) =>
      current.map((day) =>
        day.dayOfWeek ===
        dayOfWeek
          ? {
              ...day,
              blocks: [
                ...day.blocks,
                {
                  clientId:
                    newClientId(),
                  name: "Break",
                  startTime: "",
                  endTime: "",
                },
              ],
            }
          : day
      )
    );

    setSuccess(null);
  }

  function updateBlock(
    dayOfWeek: number,
    clientId: string,
    changes: Partial<TimetableBlockInput>
  ) {
    setDays((current) =>
      current.map((day) =>
        day.dayOfWeek ===
        dayOfWeek
          ? {
              ...day,
              blocks:
                day.blocks.map(
                  (block) =>
                    block.clientId ===
                    clientId
                      ? {
                          ...block,
                          ...changes,
                        }
                      : block
                ),
            }
          : day
      )
    );

    setSuccess(null);
  }

  function removeBlock(
    dayOfWeek: number,
    clientId: string
  ) {
    setDays((current) =>
      current.map((day) =>
        day.dayOfWeek ===
        dayOfWeek
          ? {
              ...day,
              blocks:
                day.blocks.filter(
                  (block) =>
                    block.clientId !==
                    clientId
                ),
            }
          : day
      )
    );

    setSuccess(null);
  }

  function copyMondayToWeekdays() {
    const monday =
      days.find(
        (day) =>
          day.dayOfWeek === 1
      );

    if (
      !monday ||
      !monday.enabled ||
      !monday.startTime ||
      !monday.endTime
    ) {
      setError(
        "Configure Monday before copying its schedule."
      );

      return;
    }

    setDays((current) =>
      current.map((day) => {
        if (
          day.dayOfWeek < 1 ||
          day.dayOfWeek > 5
        ) {
          return day;
        }

        if (
          day.dayOfWeek === 1
        ) {
          return day;
        }

        return {
          ...day,
          enabled: true,
          startTime:
            monday.startTime,
          endTime:
            monday.endTime,
          blocks:
            monday.blocks.map(
              (block) => ({
                ...block,
                clientId:
                  newClientId(),
              })
            ),
        };
      })
    );

    setError(null);
    setSuccess(
      "Monday's structure was copied to Tuesday–Friday. You can still edit any day individually."
    );
  }

  function validate() {
    if (
      periodDuration < 10 ||
      periodDuration > 180
    ) {
      return "Period duration must be between 10 and 180 minutes.";
    }

    const enabledDays =
      days.filter(
        (day) => day.enabled
      );

    if (
      enabledDays.length === 0
    ) {
      return "Select at least one school day.";
    }

    for (const day of enabledDays) {
      if (
        !day.startTime ||
        !day.endTime
      ) {
        return `${day.label}: enter both the school start and closing time.`;
      }

      if (
        day.startTime >=
        day.endTime
      ) {
        return `${day.label}: school start time must be earlier than closing time.`;
      }

      for (
        const block
        of day.blocks
      ) {
        if (
          !block.name.trim()
        ) {
          return `${day.label}: every non-teaching block needs a name.`;
        }

        if (
          !block.startTime ||
          !block.endTime
        ) {
          return `${day.label} — ${block.name}: enter both start and end time.`;
        }

        if (
          block.startTime >=
          block.endTime
        ) {
          return `${day.label} — ${block.name}: start time must be earlier than end time.`;
        }
      }
    }

    return null;
  }

  async function save(
    event: FormEvent<HTMLFormElement>
  ) {
    event.preventDefault();

    const validationError =
      validate();

    if (validationError) {
      setError(
        validationError
      );
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(null);

    const payload = {
      periodDurationMinutes:
        periodDuration,

      days: days
        .filter(
          (day) =>
            day.enabled
        )
        .map((day) => ({
          dayOfWeek:
            day.dayOfWeek,

          startTime:
            `${day.startTime}:00`,

          endTime:
            `${day.endTime}:00`,

          nonTeachingBlocks:
            day.blocks.map(
              (
                block,
                index
              ) => ({
                name:
                  block.name.trim(),

                startTime:
                  `${block.startTime}:00`,

                endTime:
                  `${block.endTime}:00`,

                sortOrder:
                  index + 1,
              })
            ),
        })),
    };

    try {
      const response =
        await fetch(
          "/api/timetable/settings",
          {
            method: "PUT",
            headers: {
              "Content-Type":
                "application/json",
            },
            body: JSON.stringify(
              payload
            ),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        setError(
          result.error ??
            "Unable to save timetable settings."
        );

        return;
      }

      setSuccess(
        "Timetable structure saved."
      );

      router.refresh();
    } catch {
      setError(
        "Unable to connect to the server."
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className="flex items-start gap-3 border-b px-5 py-4">
        <CalendarClock className="mt-0.5 h-5 w-5" />

        <div>
          <h2 className="font-semibold">
            Timetable Structure
          </h2>

          <p className="mt-1 text-xs text-muted-foreground">
            {sessionName}. Configure
            teaching periods, school
            hours and any number of
            breaks or other
            non-teaching blocks.
          </p>
        </div>
      </div>

      <form
        onSubmit={save}
        className="space-y-7 p-5"
      >
        <div className="grid gap-4 lg:grid-cols-3">
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Period Duration
            </label>

            <div className="flex items-center gap-2">
              <Input
                type="number"
                min={10}
                max={180}
                value={
                  periodDuration
                }
                onChange={(event) =>
                  setPeriodDuration(
                    Number(
                      event.target
                        .value
                    )
                  )
                }
                className="max-w-32"
                required
              />

              <span className="text-sm text-muted-foreground">
                minutes
              </span>
            </div>

            <p className="text-xs text-muted-foreground">
              Currently starting at
              40 minutes, but this is
              fully configurable.
            </p>
          </div>

          <div className="rounded-lg border p-4 lg:col-span-2">
            <div className="text-xs text-muted-foreground">
              Weekly teaching capacity
            </div>

            <div className="mt-1 text-2xl font-bold">
              {weeklyCapacity}{" "}
              <span className="text-sm font-normal text-muted-foreground">
                periods per class
              </span>
            </div>

            <p className="mt-1 text-xs text-muted-foreground">
              Calculated from enabled
              days, school hours,
              non-teaching blocks and
              period duration.
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-6">
          <div>
            <h3 className="font-semibold">
              School Week
            </h3>

            <p className="mt-1 text-xs text-muted-foreground">
              Each day can have its own
              opening, closing and break
              structure.
            </p>
          </div>

          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={
              copyMondayToWeekdays
            }
          >
            <Copy className="mr-2 h-4 w-4" />
            Copy Monday to Tue–Fri
          </Button>
        </div>

        <div className="space-y-4">
          {days.map((day) => (
            <div
              key={day.dayOfWeek}
              className={
                day.enabled
                  ? "rounded-xl border"
                  : "rounded-xl border bg-muted/20"
              }
            >
              <div className="flex flex-col gap-4 p-4 lg:flex-row lg:items-center lg:justify-between">
                <label className="flex items-center gap-3">
                  <input
                    type="checkbox"
                    checked={
                      day.enabled
                    }
                    onChange={(
                      event
                    ) =>
                      updateDay(
                        day.dayOfWeek,
                        {
                          enabled:
                            event.target
                              .checked,
                        }
                      )
                    }
                    className="h-4 w-4 accent-yellow-400"
                  />

                  <span className="font-medium">
                    {day.label}
                  </span>
                </label>

                {day.enabled && (
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-xs text-muted-foreground">
                      School hours
                    </span>

                    <input
                      type="time"
                      value={
                        day.startTime
                      }
                      onChange={(
                        event
                      ) =>
                        updateDay(
                          day.dayOfWeek,
                          {
                            startTime:
                              event.target
                                .value,
                          }
                        )
                      }
                      className="h-9 rounded-md border bg-background px-3 text-sm"
                      required
                    />

                    <span className="text-muted-foreground">
                      →
                    </span>

                    <input
                      type="time"
                      value={
                        day.endTime
                      }
                      onChange={(
                        event
                      ) =>
                        updateDay(
                          day.dayOfWeek,
                          {
                            endTime:
                              event.target
                                .value,
                          }
                        )
                      }
                      className="h-9 rounded-md border bg-background px-3 text-sm"
                      required
                    />
                  </div>
                )}
              </div>

              {day.enabled && (
                <div className="border-t p-4">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <div className="text-sm font-medium">
                        Non-Teaching
                        Blocks
                      </div>

                      <p className="mt-1 text-xs text-muted-foreground">
                        Optional. Add
                        breaks, lunch,
                        assembly or any
                        other blocked
                        time.
                      </p>
                    </div>

                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      onClick={() =>
                        addBlock(
                          day.dayOfWeek
                        )
                      }
                    >
                      <Plus className="mr-2 h-4 w-4" />
                      Add Block
                    </Button>
                  </div>

                  {day.blocks.length ===
                  0 ? (
                    <div className="mt-4 rounded-lg border border-dashed p-4 text-center text-xs text-muted-foreground">
                      No breaks or
                      non-teaching blocks
                      configured.
                    </div>
                  ) : (
                    <div className="mt-4 space-y-3">
                      {day.blocks.map(
                        (block) => (
                          <div
                            key={
                              block.clientId
                            }
                            className="grid gap-3 rounded-lg border p-3 md:grid-cols-[1fr_140px_20px_140px_40px] md:items-center"
                          >
                            <Input
                              value={
                                block.name
                              }
                              onChange={(
                                event
                              ) =>
                                updateBlock(
                                  day.dayOfWeek,
                                  block.clientId,
                                  {
                                    name:
                                      event.target
                                        .value,
                                  }
                                )
                              }
                              placeholder="e.g. Break, Lunch, Assembly"
                              required
                            />

                            <input
                              type="time"
                              value={
                                block.startTime
                              }
                              onChange={(
                                event
                              ) =>
                                updateBlock(
                                  day.dayOfWeek,
                                  block.clientId,
                                  {
                                    startTime:
                                      event.target
                                        .value,
                                  }
                                )
                              }
                              className="h-10 rounded-md border bg-background px-3 text-sm"
                              required
                            />

                            <span className="hidden text-center text-muted-foreground md:block">
                              →
                            </span>

                            <input
                              type="time"
                              value={
                                block.endTime
                              }
                              onChange={(
                                event
                              ) =>
                                updateBlock(
                                  day.dayOfWeek,
                                  block.clientId,
                                  {
                                    endTime:
                                      event.target
                                        .value,
                                  }
                                )
                              }
                              className="h-10 rounded-md border bg-background px-3 text-sm"
                              required
                            />

                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() =>
                                removeBlock(
                                  day.dayOfWeek,
                                  block.clientId
                                )
                              }
                            >
                              <Trash2 className="h-4 w-4" />

                              <span className="sr-only">
                                Remove
                              </span>
                            </Button>
                          </div>
                        )
                      )}
                    </div>
                  )}

                  <div className="mt-4 text-xs text-muted-foreground">
                    Estimated teaching
                    time:{" "}
                    {teachingMinutes(
                      day
                    )}{" "}
                    minutes
                    {periodDuration >
                      0 && (
                      <>
                        {" · "}
                        {Math.floor(
                          teachingMinutes(
                            day
                          ) /
                            periodDuration
                        )}{" "}
                        complete periods
                      </>
                    )}
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>

        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {error}
          </div>
        )}

        {success && (
          <div className="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-700">
            {success}
          </div>
        )}

        <div className="flex justify-end border-t pt-5">
          <Button
            type="submit"
            disabled={saving}
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            {saving ? (
              <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Save className="mr-2 h-4 w-4" />
            )}

            Save Timetable Structure
          </Button>
        </div>
      </form>
    </section>
  );
}
