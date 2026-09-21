import {
  AlertTriangle,
  CalendarDays,
} from "lucide-react";

import { ClassSubjectRequirementsEditor } from "@/components/timetable/class-subject-requirements-editor";
import { TimetableMvpPanel } from "@/components/timetable/timetable-mvp-panel";
import { TimetableSettingsEditor } from "@/components/timetable/timetable-settings-editor";
import { tenantDisplayName } from "@/lib/tenant-display-name";
import { getSessionContext } from "@/lib/auth/session";

import {
  getTimetableReadiness,
  getTimetableSetup,
  getTimetableTerms,
} from "@/lib/api/timetable";

import type {
  TimetableSettings,
} from "@/types/timetable";

function minutesBetween(
  start: string,
  end: string
) {
  const [
    startHour,
    startMinute,
  ] = start
    .slice(0, 5)
    .split(":")
    .map(Number);

  const [
    endHour,
    endMinute,
  ] = end
    .slice(0, 5)
    .split(":")
    .map(Number);

  return (
    endHour * 60 +
    endMinute -
    (startHour * 60 +
      startMinute)
  );
}

function weeklyCapacity(
  settings: TimetableSettings | null
) {
  if (
    !settings ||
    settings.periodDurationMinutes <=
      0
  ) {
    return null;
  }

  return settings.days.reduce(
    (total, day) => {
      const schoolMinutes =
        minutesBetween(
          day.startTime,
          day.endTime
        );

      const blockedMinutes =
        day.nonTeachingBlocks.reduce(
          (blocked, block) =>
            blocked +
            Math.max(
              0,
              minutesBetween(
                block.startTime,
                block.endTime
              )
            ),
          0
        );

      return (
        total +
        Math.floor(
          Math.max(
            0,
            schoolMinutes -
              blockedMinutes
          ) /
            settings.periodDurationMinutes
        )
      );
    },
    0
  );
}

type TimetablePageProps = {
  params: Promise<{
    tenantSlug: string;
  }>;
};

export default async function TimetablePage({
  params,
}: TimetablePageProps) {
  const { tenantSlug } = await params;
  const session = await getSessionContext();

  const setup =
    await getTimetableSetup();

  if (!setup.currentSession) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            Timetable
          </h1>

          <p className="mt-1 text-sm text-muted-foreground">
            Configure and generate
            termly school timetables.
          </p>
        </div>

        <div className="flex gap-3 rounded-xl border border-amber-200 bg-amber-50 p-5 text-amber-800">
          <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" />

          <div>
            <div className="font-semibold">
              Current academic session
              required
            </div>

            <p className="mt-1 text-sm">
              Set a current academic
              session before configuring
              the timetable.
            </p>
          </div>
        </div>
      </div>
    );
  }

  const [
    readiness,
    terms,
  ] = await Promise.all([
    getTimetableReadiness(),
    getTimetableTerms(),
  ]);

  const capacity =
    weeklyCapacity(
      setup.settings
    );

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            Timetable
          </h1>

          <p className="mt-1 text-sm text-muted-foreground">
            Plan school periods,
            validate teacher coverage
            and generate the timetable
            for each academic term.
          </p>
        </div>

        <div className="flex items-center gap-2 rounded-lg border bg-card px-4 py-2 text-sm">
          <CalendarDays className="h-4 w-4 text-muted-foreground" />

          <span className="text-muted-foreground">
            Session:
          </span>

          <span className="font-medium">
            {
              setup.currentSession.name
            }
          </span>
        </div>
      </div>

      <TimetableMvpPanel
        readiness={readiness}
        terms={terms}
        settings={setup.settings}
        schoolName={session?.tenantName ?? tenantDisplayName(tenantSlug)}
      />

      <TimetableSettingsEditor
        settings={setup.settings}
        sessionName={
          setup.currentSession.name
        }
      />

      <ClassSubjectRequirementsEditor
        classes={setup.classes}
        subjects={setup.subjects}
        sessionName={
          setup.currentSession.name
        }
        weeklyCapacity={
          capacity
        }
      />
    </div>
  );
}
