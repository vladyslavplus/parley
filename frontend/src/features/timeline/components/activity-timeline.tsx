"use client";

import { useQueryClient } from "@tanstack/react-query";
import {
  CheckCircle2,
  FileText,
  Kanban,
  Loader2,
  MessageSquare,
  Package,
  Send,
  UserPlus,
} from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/input";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { LIMITS } from "@/constants/limits";
import { PERMISSIONS } from "@/constants/permissions";
import { queryKeys } from "@/constants/query-keys";
import { NoteModal } from "@/features/timeline/components/note-modal";
import { useActivities } from "@/features/timeline/hooks/use-activities";
import { useNoteMutations } from "@/features/timeline/hooks/use-note-mutations";
import { readOrderRecordedPayload } from "@/features/timeline/order-recorded-payload";
import type { Activity, Note, SubjectRef } from "@/features/timeline/types";
import { useMe } from "@/lib/api/hooks/use-api";
import {
  formatCurrency,
  formatDateTime,
  formatRelativeAge,
} from "@/lib/formatters";
import { displayTimeZone } from "@/lib/time-zones";
import { showSuccessToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

interface Props {
  subject: SubjectRef;
}

const FILTER_TYPES = [
  "all",
  "note_added",
  "task_completed",
  "deal_stage_changed",
  "deal_closed",
  "contact_created",
  "order_recorded",
] as const;
type FilterType = (typeof FILTER_TYPES)[number];

export function ActivityTimeline({ subject }: Props) {
  const t = useTranslations("crm.timeline");
  const tNotes = useTranslations("crm.notes");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const queryClient = useQueryClient();
  const { data: me } = useMe();
  const timeZone = displayTimeZone(me);
  const activitiesWriteGrant = me?.permissions.find(
    (grant) => grant.permission === PERMISSIONS.activitiesWrite,
  );
  const hasOrgActivitiesWrite = Boolean(
    activitiesWriteGrant &&
    (activitiesWriteGrant.scope === "org" ||
      activitiesWriteGrant.scope === "organization" ||
      activitiesWriteGrant.scope === null),
  );
  const { createNote, deleteNote } = useNoteMutations();

  const [typeFilter, setTypeFilter] = useState<FilterType>("all");
  const [quickNoteText, setQuickNoteText] = useState("");
  const [quickNoteSubmitting, setQuickNoteSubmitting] = useState(false);

  const [editingNote, setEditingNote] = useState<Note | null>(null);
  const [noteModalOpen, setNoteModalOpen] = useState(false);
  const [deletingNoteId, setDeletingNoteId] = useState<string | null>(null);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);

  const {
    activities,
    hasMore,
    loadMore,
    isLoadingMore,
    isLoading,
    isFetching,
    refetch,
  } = useActivities({
    ...subject,
    type: typeFilter === "all" ? undefined : typeFilter,
  });

  async function handleQuickNoteSubmit(e: React.SyntheticEvent) {
    e.preventDefault();
    if (!quickNoteText.trim() || quickNoteSubmitting) return;

    setQuickNoteSubmitting(true);
    try {
      await createNote(
        {
          body: quickNoteText.trim(),
          contactId: subject.contactId || null,
          dealId: subject.dealId || null,
          companyId: subject.companyId || null,
        },
        subject,
      );
      setQuickNoteText("");
      showSuccessToast(tNotes("noteCreated"));
    } catch {
      // Handled by toast in mutation hook
    } finally {
      setQuickNoteSubmitting(false);
    }
  }

  async function handleDeleteNoteConfirm() {
    if (!deletingNoteId) return;
    await deleteNote(deletingNoteId, subject);
    showSuccessToast(tNotes("noteDeleted"));
    setDeleteConfirmOpen(false);
    setDeletingNoteId(null);
    await refetch();
    await queryClient.invalidateQueries({
      queryKey: queryKeys.activitiesAll(),
    });
  }

  function renderActivityIcon(activity: Activity) {
    switch (activity.type) {
      case "contact_created":
        return <UserPlus className="h-3.5 w-3.5 text-primary" />;
      case "deal_stage_changed":
      case "deal_closed":
        return <Kanban className="h-3.5 w-3.5 text-warning" />;
      case "note_added":
        return <MessageSquare className="h-3.5 w-3.5 text-info" />;
      case "task_completed":
        return <CheckCircle2 className="h-3.5 w-3.5 text-success" />;
      case "order_recorded":
        return readOrderRecordedPayload(activity.payload).isCancellation ? (
          <Package className="h-3.5 w-3.5 text-destructive" />
        ) : (
          <Package className="h-3.5 w-3.5 text-success" />
        );
      default:
        return <FileText className="h-3.5 w-3.5 text-muted-foreground" />;
    }
  }

  function renderActivityTitle(activity: Activity) {
    const actor =
      activity.actorDisplayName ??
      (activity.actorUserId ? t("unknownUser") : t("system"));
    switch (activity.type) {
      case "contact_created":
        return t("events.contactCreated", { actor });
      case "deal_stage_changed": {
        const payload = activity.payload as Record<string, unknown> | null;
        const stage =
          (payload?.toStageName as string) ||
          (payload?.stageName as string) ||
          t("events.aStage");
        return t("events.dealStageChanged", {
          actor,
          deal: activity.dealTitle ?? t("deal"),
          stage,
        });
      }
      case "deal_closed": {
        const payload = activity.payload as Record<string, unknown> | null;
        const statusRaw = (payload?.status as string) ?? "closed";
        let status = t("events.statusClosed");
        if (statusRaw === "won") status = t("events.statusWon");
        else if (statusRaw === "lost") status = t("events.statusLost");
        else if (statusRaw === "open") status = t("events.statusOpen");
        return t("events.dealClosed", {
          actor,
          deal: activity.dealTitle ?? t("deal"),
          status,
        });
      }
      case "note_added":
        return t("events.noteAdded", { actor });
      case "task_completed":
        return t("events.taskCompleted", {
          actor,
          task: activity.taskTitle ?? t("task"),
        });
      case "order_recorded": {
        const order = readOrderRecordedPayload(activity.payload);
        const amount =
          order.totalAmount === null
            ? null
            : formatCurrency(
                order.totalAmount,
                order.currency ?? LIMITS.defaultCurrency,
                locale,
              );
        if (order.invoiceNo && amount) {
          return t("events.orderRecorded", {
            invoice: order.invoiceNo,
            amount,
          });
        }
        if (order.invoiceNo) {
          return t("events.orderRecordedInvoice", { invoice: order.invoiceNo });
        }
        if (amount) {
          return t("events.orderRecordedAmount", { amount });
        }
        return t("events.orderRecordedNeutral");
      }
      default:
        return activity.type;
    }
  }

  return (
    <div className="space-y-4">
      {/* Quick Add Note box */}
      <form
        onSubmit={handleQuickNoteSubmit}
        className="rounded-[var(--radius-card)] border border-border bg-card p-3 shadow-xs space-y-2"
      >
        <Textarea
          value={quickNoteText}
          onChange={(e) => setQuickNoteText(e.target.value)}
          rows={2}
          maxLength={LIMITS.noteBodyMaxLength}
          placeholder={tNotes("form.quickAddPlaceholder")}
          aria-label={tNotes("form.quickAddPlaceholder")}
          className="min-h-16 text-xs"
        />
        <div className="flex items-center justify-between pt-1 border-t border-border/40">
          <span className="text-[11px] text-muted-foreground">
            {quickNoteText.length} / {LIMITS.noteBodyMaxLength}
          </span>
          <Button
            type="submit"
            variant="primary"
            disabled={!quickNoteText.trim() || quickNoteSubmitting}
            className="h-7 gap-1 px-2.5 text-xs"
          >
            {quickNoteSubmitting ? (
              <Loader2 className="h-3 w-3 animate-spin" />
            ) : (
              <Send className="h-3 w-3" />
            )}
            <span>{tNotes("addNote")}</span>
          </Button>
        </div>
      </form>

      {/* Filter Tabs */}
      <div className="flex items-center justify-between border-b border-border pb-2 overflow-x-auto">
        <div className="flex items-center gap-1 min-w-max">
          {FILTER_TYPES.map((ft) => (
            <button
              key={ft}
              type="button"
              onClick={() => setTypeFilter(ft)}
              className={cn(
                "rounded-xs px-2 py-1 text-xs font-medium transition-colors",
                typeFilter === ft
                  ? "bg-muted text-foreground font-semibold"
                  : "text-muted-foreground hover:text-foreground",
              )}
            >
              {t(`filters.${ft}` as Parameters<typeof t>[0])}
            </button>
          ))}
        </div>
      </div>

      <TableProgressBar active={isFetching} />

      {/* Timeline items */}
      {isLoading ? (
        <div className="space-y-3 py-2">
          {Array.from({ length: 3 }).map((_, i) => (
            <div
              key={i}
              className="h-16 w-full animate-pulse rounded-[var(--radius-card)] bg-muted/40"
            />
          ))}
        </div>
      ) : activities.length === 0 ? (
        <div className="rounded-[var(--radius-card)] border border-border bg-card p-6 text-center">
          <p className="text-xs text-muted-foreground">{t("empty")}</p>
        </div>
      ) : (
        <div className="relative pl-6 space-y-4 before:absolute before:left-2.5 before:top-2 before:bottom-2 before:w-px before:bg-border">
          {activities.map((activity) => {
            const isAuthor = Boolean(
              activity.actorUserId &&
              me?.user?.id &&
              activity.actorUserId === me.user.id,
            );
            const canEditOrDelete = isAuthor || hasOrgActivitiesWrite;

            return (
              <div
                key={activity.id}
                className="relative group rounded-[var(--radius-card)] border border-border bg-card p-3 shadow-xs space-y-2"
              >
                {/* Node dot on the timeline track */}
                <div className="absolute -left-[1.8rem] top-3.5 flex h-4 w-4 items-center justify-center rounded-full bg-card border border-border shadow-xs">
                  {renderActivityIcon(activity)}
                </div>

                {/* Entry header */}
                <div className="flex items-start justify-between gap-2">
                  <span className="flex flex-wrap items-center gap-2 text-xs font-medium text-foreground leading-snug">
                    {renderActivityTitle(activity)}
                    {activity.type === "order_recorded" &&
                    readOrderRecordedPayload(activity.payload)
                      .isCancellation ? (
                      <span className="inline-flex items-center rounded px-1.5 py-0.5 text-[11px] font-medium bg-destructive/10 text-destructive">
                        {t("events.orderCancellation")}
                      </span>
                    ) : null}
                  </span>
                  <span
                    className="shrink-0 text-[11px] text-muted-foreground tabular-nums whitespace-nowrap"
                    title={formatDateTime(
                      activity.occurredAt,
                      locale,
                      timeZone,
                    )}
                  >
                    {formatRelativeAge(activity.occurredAt, locale)}
                  </span>
                </div>

                {/* Note body or details */}
                {activity.noteBody && (
                  <div className="rounded-xs bg-muted/50 p-2 text-foreground text-xs whitespace-pre-wrap font-sans">
                    {activity.noteBody}
                  </div>
                )}

                {/* Action buttons on Note */}
                {activity.type === "note_added" &&
                  activity.noteId &&
                  canEditOrDelete && (
                    <div className="flex justify-end gap-2 pt-1 border-t border-border/40 opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 [@media(hover:none)]:opacity-100 transition-opacity">
                      {activity.noteVersion !== null &&
                        activity.noteVersion !== undefined && (
                          <button
                            type="button"
                            onClick={() => {
                              setEditingNote({
                                id: activity.noteId!,
                                body: activity.noteBody ?? "",
                                authorUserId: activity.actorUserId ?? "",
                                authorDisplayName: activity.actorDisplayName,
                                authorEmail: null,
                                contactId: activity.contactId,
                                dealId: activity.dealId,
                                companyId: activity.companyId,
                                createdAt: activity.occurredAt,
                                updatedAt: activity.occurredAt,
                                version: Number(activity.noteVersion),
                              });
                              setNoteModalOpen(true);
                            }}
                            className="text-[11px] text-muted-foreground hover:text-foreground"
                          >
                            {tNotes("editNote")}
                          </button>
                        )}
                      <button
                        type="button"
                        onClick={() => {
                          setDeletingNoteId(activity.noteId!);
                          setDeleteConfirmOpen(true);
                        }}
                        className="text-[11px] text-destructive hover:underline"
                      >
                        {tNotes("deleteNote")}
                      </button>
                    </div>
                  )}
              </div>
            );
          })}

          {/* Show More */}
          {hasMore && (
            <div className="pt-2 text-center">
              <Button
                type="button"
                variant="secondary"
                onClick={() => void loadMore()}
                disabled={isLoadingMore}
                className="h-7 px-3 text-xs text-muted-foreground hover:text-foreground"
              >
                {isLoadingMore ? (
                  <Loader2 className="mr-1 h-3 w-3 animate-spin" />
                ) : null}
                {t("showMore")}
              </Button>
            </div>
          )}
        </div>
      )}

      {/* Note Edit Modal */}
      <NoteModal
        open={noteModalOpen}
        onOpenChange={setNoteModalOpen}
        note={editingNote}
        subject={subject}
        onSaved={async () => {
          await refetch();
          await queryClient.invalidateQueries({
            queryKey: queryKeys.activitiesAll(),
          });
        }}
      />

      {/* Delete Note Confirm Dialog */}
      <ConfirmDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title={tNotes("deleteConfirmTitle")}
        description={tNotes("deleteConfirmDescription")}
        confirmLabel={tNotes("deleteNote")}
        cancelLabel={tCommon("cancel")}
        onConfirm={handleDeleteNoteConfirm}
        destructive
      />
    </div>
  );
}
