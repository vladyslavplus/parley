"use client";

import { Calendar } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { Skeleton } from "@/components/ui/skeleton";
import { PERMISSIONS } from "@/constants/permissions";
import { contactRecordRoute } from "@/constants/routes";
import { TaskTypeLabel } from "@/features/tasks/components/task-type-label";
import { TaskStatusCheckbox } from "@/features/tasks/components/task-status-checkbox";
import { useDueContactTasks } from "@/features/tasks/hooks/use-tasks";
import {
  DEFAULT_TASK_TYPE,
  TASK_TYPE_LABEL_KEYS,
  taskTypeOf,
} from "@/features/tasks/lib/task-types";
import type { PriorityItem } from "@/features/insights/types";
import { Can } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";
import { useMe } from "@/lib/api/hooks/use-api";
import { formatDateTime, formatRelativeAge } from "@/lib/formatters";
import { displayTimeZone } from "@/lib/time-zones";

// The tasks behind a "Follow up" step: the contact's open tasks that are due or
// late, with a way to complete one right here. The list follows the viewer's own
// data scope while the priority fact counts any assignee, so an empty answer may
// mean a colleague's task, and says so.
export function DueTasks({ item }: { item: PriorityItem }) {
  const t = useTranslations("insights.home.priority");
  const tTasks = useTranslations("insights.home.priority.nextStep");
  const locale = useLocale();
  const { data: me } = useMe();
  const timeZone = displayTimeZone(me);
  const query = useDueContactTasks(item.contactId, true);
  const tTask = useTranslations("crm.tasks");

  // "Call: Confirm order". A to-do says nothing more than its title.
  function typedTitle(task: { type: string; title: string }): string {
    const code = taskTypeOf(task.type);
    return code === DEFAULT_TASK_TYPE
      ? task.title
      : tTask("typeTitle", {
          type: tTask(TASK_TYPE_LABEL_KEYS[code]),
          title: task.title,
        });
  }

  let body;
  if (query.isPending) {
    body = (
      <div className="space-y-2" aria-hidden="true">
        <Skeleton className="h-8 w-full" />
      </div>
    );
  } else if (!query.data) {
    body = <p className="text-muted-foreground">{tTasks("tasksLoadFailed")}</p>;
  } else if (query.data.items.length === 0) {
    body = (
      <div className="space-y-1">
        <p className="text-foreground">{tTasks("tasksEmpty")}</p>
        <p className="text-muted-foreground">{tTasks("tasksColleague")}</p>
        <Link
          href={contactRecordRoute(item.contactId)}
          className="inline-block rounded-sm text-info underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
        >
          {t("openContact")}
        </Link>
      </div>
    );
  } else {
    body = (
      <ul className="space-y-2">
        {query.data.items.map((task) => {
          const colleague =
            task.assigneeUserId !== me?.user.id && task.assigneeDisplayName;
          return (
            <li key={task.id} className="flex items-start gap-2">
              <Can permission={PERMISSIONS.tasksWrite}>
                <TaskStatusCheckbox task={task} className="mt-0.5" />
              </Can>
              <div className="min-w-0 flex-1 space-y-0.5">
                <p className="flex items-start gap-1.5 break-words font-medium text-foreground">
                  <TaskTypeLabel type={task.type} iconOnly className="mt-0.5" />
                  <span className="min-w-0">{typedTitle(task)}</span>
                </p>
                {task.dueAt ? (
                  <p
                    className="flex items-center gap-1 text-[11px] tabular-nums text-destructive"
                    title={formatDateTime(task.dueAt, locale, timeZone)}
                  >
                    <Calendar className="h-3 w-3 shrink-0" aria-hidden="true" />
                    {formatRelativeAge(task.dueAt, locale)}
                  </p>
                ) : null}
                {colleague ? (
                  <p className="text-[11px] text-muted-foreground">
                    {tTasks("assignedTo", { name: colleague })}
                  </p>
                ) : null}
              </div>
            </li>
          );
        })}
      </ul>
    );
  }

  return (
    <div className="space-y-1.5">
      <p className="font-medium text-foreground">{tTasks("dueTasks")}</p>
      {body}
    </div>
  );
}
