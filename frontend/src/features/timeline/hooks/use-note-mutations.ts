"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import type { Note, SubjectRef } from "@/features/timeline/types";
import { backendJson, backendVoid } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import { describeError, toApiError } from "@/lib/errors";
import { showErrorToast } from "@/lib/toasts";

export function useNoteMutations() {
  const queryClient = useQueryClient();
  const locale = useLocale();
  const tErrors = useTranslations("errors");

  async function createNote(
    body: Schemas["NoteRequest"],
    subject: SubjectRef,
  ): Promise<Note> {
    try {
      const created = await backendJson<Note>("/notes", {
        method: "POST",
        body,
        locale,
      });

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.notesList(subject as Record<string, unknown>),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
      ]);

      return created;
    } catch (err) {
      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  async function updateNote(
    id: string,
    body: Schemas["UpdateNoteRequest"],
    subject: SubjectRef,
  ): Promise<Note> {
    try {
      const updated = await backendJson<Note>(
        "/notes/{id}",
        {
          method: "PUT",
          body,
          locale,
        },
        `/notes/${encodeURIComponent(id)}`,
      );

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.note(id) }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.notesList(subject as Record<string, unknown>),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
      ]);

      return updated;
    } catch (err) {
      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  async function deleteNote(id: string, subject: SubjectRef): Promise<void> {
    try {
      await backendVoid(
        "/notes/{id}",
        {
          method: "DELETE",
          locale,
        },
        `/notes/${encodeURIComponent(id)}`,
      );

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.note(id) }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.notesList(subject as Record<string, unknown>),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
      ]);
    } catch (err) {
      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  return {
    createNote,
    updateNote,
    deleteNote,
  };
}
