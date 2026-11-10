"use client";

import { useTranslations } from "next-intl";
import { useState } from "react";

import { useServerTableState } from "@/components/data-table/use-server-table-state";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { PERMISSIONS } from "@/constants/permissions";
import { LibraryPanel } from "@/features/campaigns/components/library-panel";
import { SegmentEditorDialog } from "@/features/campaigns/components/segment-editor-dialog";
import { useSegments } from "@/features/campaigns/hooks/use-library";
import { useLibraryMutations } from "@/features/campaigns/hooks/use-library-mutations";
import type { SegmentSummary } from "@/features/campaigns/types";
import { useCan } from "@/features/session/components/can";
import { showSuccessToast } from "@/lib/toasts";

export function SegmentsLibrary() {
  const t = useTranslations("campaigns.segments");
  const tCommon = useTranslations("common");
  const tLibrary = useTranslations("campaigns.library");
  const canManage = useCan(PERMISSIONS.campaignsManage);
  const { archiveSegment } = useLibraryMutations();

  const tableState = useServerTableState({ defaultSort: "name" });
  const query = useSegments({
    q: tableState.q || undefined,
    after: tableState.after || null,
    limit: tableState.pageSize,
  });

  const [editorOpen, setEditorOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [archiving, setArchiving] = useState<SegmentSummary | null>(null);

  function openEditor(id: string | null) {
    setEditingId(id);
    setEditorOpen(true);
  }

  async function confirmArchive() {
    if (!archiving) return;
    await archiveSegment(archiving.id);
    showSuccessToast(t("archived"));
    setArchiving(null);
  }

  return (
    <>
      <LibraryPanel
        rows={query.data?.items ?? []}
        nextCursor={query.data?.nextCursor ?? null}
        isLoading={query.isPending && !query.data}
        isFetching={query.isFetching}
        isError={query.isError}
        onRetry={() => void query.refetch()}
        tableState={tableState}
        searchPlaceholder={t("searchPlaceholder")}
        emptyTitle={t("empty.title")}
        emptyDescription={t("empty.description")}
        createLabel={t("new")}
        canManage={canManage}
        onCreate={() => openEditor(null)}
        onEdit={(row) => openEditor(row.id)}
        onArchive={setArchiving}
        editLabel={t("edit")}
        archiveLabel={t("archive")}
        loadError={t("loadError")}
        retryLabel={tLibrary("retry")}
        nameLabel={t("columns.name")}
      />

      <SegmentEditorDialog
        open={editorOpen}
        onOpenChange={setEditorOpen}
        segmentId={editingId}
      />

      <ConfirmDialog
        open={Boolean(archiving)}
        onOpenChange={(open) => {
          if (!open) setArchiving(null);
        }}
        title={t("archiveConfirm.title")}
        description={t("archiveConfirm.description", {
          name: archiving?.name ?? "",
        })}
        confirmLabel={t("archive")}
        cancelLabel={tCommon("cancel")}
        onConfirm={confirmArchive}
        destructive
      />
    </>
  );
}
