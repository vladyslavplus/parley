"use client";

import { useLocale, useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";

import { LIMITS } from "@/constants/limits";
import { PALETTE_GROUPS } from "@/constants/palette-groups";
import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
import { TaskModal } from "@/features/tasks/components/task-modal";
import type { TaskPage } from "@/features/tasks/types";
import { useCan } from "@/features/session/components/can";
import { useRouter } from "@/i18n/navigation";
import { backendJson } from "@/lib/api/bff-client";
import {
  registerCommandSource,
  registerSearchSource,
  type CommandItem,
} from "@/lib/shortcuts/command-source";
import { registerCreateTarget } from "@/lib/shortcuts/create-target";

export function TaskShellRegistration() {
  const router = useRouter();
  const locale = useLocale();
  const t = useTranslations("shell");
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const canReadTasks = useCan(PERMISSIONS.tasksRead);
  const canWriteTasks = useCan(PERMISSIONS.tasksWrite);

  // Quick create target registration
  useEffect(() => {
    if (!canWriteTasks) return;

    const unregister = registerCreateTarget({
      id: "task",
      titleKey: "task",
      action: () => setCreateModalOpen(true),
    });

    return unregister;
  }, [canWriteTasks]);

  // Static commands in Command Palette
  useEffect(() => {
    const unregister = registerCommandSource(() => {
      const items: CommandItem[] = [];

      items.push({
        id: "crm-go-tasks",
        title: t("commandPalette.goToTasks"),
        group: PALETTE_GROUPS.navigation,
        onSelect: () => router.push(ROUTES.tasks),
      });

      if (canWriteTasks) {
        items.push({
          id: "crm-create-task",
          title: t("commandPalette.createTask"),
          group: PALETTE_GROUPS.actions,
          onSelect: () => setCreateModalOpen(true),
        });
      }

      return items;
    });

    return unregister;
  }, [canWriteTasks, router, t]);

  // Async search source for tasks in Palette
  useEffect(() => {
    const unregister = registerSearchSource(
      async (query: string, signal: AbortSignal): Promise<CommandItem[]> => {
        const trimmed = query.trim();
        if (trimmed.length < LIMITS.searchMinQueryLength) {
          return [];
        }

        await new Promise<void>((resolve, reject) => {
          if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
          const onAbort = () => {
            if (debounceTimerRef.current)
              clearTimeout(debounceTimerRef.current);
            reject(new DOMException("Aborted", "AbortError"));
          };
          signal.addEventListener("abort", onAbort, { once: true });
          debounceTimerRef.current = setTimeout(() => {
            signal.removeEventListener("abort", onAbort);
            resolve();
          }, LIMITS.searchDebounceMs);
        });

        try {
          const res = await backendJson<TaskPage>(
            canReadTasks ? "/tasks" : "/tasks/mine",
            {
              method: "GET",
              query: { q: trimmed, limit: 5 },
              locale,
              signal,
            },
          );

          return res.items.map((task) => ({
            id: `search-task-${task.id}`,
            title: task.title,
            subtitle:
              `${task.assigneeDisplayName ?? ""} ${task.isCompleted ? "(completed)" : ""}`.trim(),
            group: PALETTE_GROUPS.tasks,
            onSelect: () =>
              router.push(
                `${ROUTES.tasks}?q=${encodeURIComponent(task.title)}`,
              ),
          }));
        } catch {
          return [];
        }
      },
    );

    return () => {
      if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
      unregister();
    };
  }, [canReadTasks, router, locale]);

  return (
    <TaskModal
      open={createModalOpen}
      onOpenChange={setCreateModalOpen}
      onSaved={() => router.push(ROUTES.tasks)}
    />
  );
}
