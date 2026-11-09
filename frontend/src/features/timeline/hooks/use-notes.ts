"use client";

import {
  keepPreviousData,
  queryOptions,
  useQuery,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import type { Note, NotePage, SubjectRef } from "@/features/timeline/types";
import { backendJson } from "@/lib/api/bff-client";

export function notesListQueryOptions(subject: SubjectRef, locale: string) {
  return queryOptions({
    queryKey: queryKeys.notesList(subject as Record<string, unknown>),
    queryFn: () =>
      backendJson<NotePage>("/notes", {
        locale,
        query: {
          contactId: subject.contactId || undefined,
          dealId: subject.dealId || undefined,
          companyId: subject.companyId || undefined,
          limit: LIMITS.maxNotesPageSize,
          order: "desc",
        },
      }),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function noteQueryOptions(id: string, locale: string) {
  return queryOptions({
    queryKey: queryKeys.note(id),
    queryFn: () =>
      backendJson<Note>(
        "/notes/{id}",
        {
          locale,
        },
        `/notes/${encodeURIComponent(id)}`,
      ),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function useNotes(subject: SubjectRef, options?: { enabled?: boolean }) {
  const locale = useLocale();
  const hasSubject = Boolean(
    subject.contactId || subject.dealId || subject.companyId,
  );
  return useQuery({
    ...notesListQueryOptions(subject, locale),
    enabled: (options?.enabled ?? true) && hasSubject,
  });
}
