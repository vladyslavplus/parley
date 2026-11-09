"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { Trash2, UserPlus } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import type { Permission } from "@/constants/permissions";
import { OwnerPicker } from "@/features/crm/components/owner-picker";
import { Can } from "@/features/session/components/can";
import { describeError, toApiError } from "@/lib/errors";

interface Props {
  selectedCount: number;
  writePermission: Permission;
  deletePermission: Permission;
  onAssignOwner: (ownerUserId: string) => Promise<void>;
  onDelete: () => Promise<void>;
  clearSelection: () => void;
}

export function CrmBulkBar({
  selectedCount,
  writePermission,
  deletePermission,
  onAssignOwner,
  onDelete,
  clearSelection,
}: Props) {
  const t = useTranslations("crm.bulk");
  const tErrors = useTranslations("errors.kinds");
  const tCommon = useTranslations("common");

  const [assignOpen, setAssignOpen] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [selectedOwner, setSelectedOwner] = useState("");
  const [isPending, setIsPending] = useState(false);
  const [assignError, setAssignError] = useState<string | null>(null);

  async function handleAssign() {
    if (!selectedOwner) return;
    setIsPending(true);
    setAssignError(null);
    try {
      await onAssignOwner(selectedOwner);
      setAssignOpen(false);
      setSelectedOwner("");
      clearSelection();
    } catch (err) {
      const apiErr = toApiError(err);
      setAssignError(describeError(apiErr, (k) => tErrors(k)));
      // Keep selection on failure
    } finally {
      setIsPending(false);
    }
  }

  async function handleDelete() {
    setIsPending(true);
    try {
      await onDelete();
      setDeleteConfirmOpen(false);
      clearSelection();
    } finally {
      setIsPending(false);
    }
  }

  return (
    <div className="flex items-center gap-2">
      {/* Assign Owner button */}
      <Can permission={writePermission}>
        <Button
          variant="secondary"
          onClick={() => {
            setAssignError(null);
            setSelectedOwner("");
            setAssignOpen(true);
          }}
          className="h-8 gap-1.5 text-xs px-2.5"
        >
          <UserPlus className="h-3.5 w-3.5" />
          <span>{t("assignOwner")}</span>
        </Button>
      </Can>

      {/* Delete button (Strictly permission-gated: sales reps won't see it!) */}
      <Can permission={deletePermission}>
        <Button
          variant="secondary"
          onClick={() => setDeleteConfirmOpen(true)}
          className="h-8 gap-1.5 text-xs px-2.5 text-destructive hover:bg-destructive/10"
        >
          <Trash2 className="h-3.5 w-3.5" />
          <span>{t("delete")}</span>
        </Button>
      </Can>

      {/* Assign Owner Dialog with shared OwnerPicker */}
      <Dialog.Root
        open={assignOpen}
        onOpenChange={(nextOpen) => {
          setAssignOpen(nextOpen);
          if (!nextOpen) {
            setAssignError(null);
            setSelectedOwner("");
          }
        }}
      >
        <Dialog.Portal>
          <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-sm animate-in fade-in-0 duration-150" />
          <Dialog.Content className="fixed left-[50%] top-[50%] z-50 w-full max-w-sm translate-x-[-50%] translate-y-[-50%] rounded-[var(--radius-card)] border border-border bg-card p-5 shadow-floating space-y-4 focus:outline-none">
            <Dialog.Title className="text-sm font-semibold text-foreground">
              {t("assignOwnerDialogTitle")}
            </Dialog.Title>
            <Dialog.Description className="sr-only">
              {t("selectOwner")}
            </Dialog.Description>

            {assignError && <Alert tone="error">{assignError}</Alert>}

            <OwnerPicker
              variant="control"
              allowClear={false}
              value={selectedOwner || null}
              placeholder={t("selectOwner")}
              ariaLabel={t("selectOwner")}
              onChange={(nextOwnerId) => setSelectedOwner(nextOwnerId ?? "")}
            />

            <div className="flex justify-end gap-2">
              <Button
                variant="secondary"
                onClick={() => setAssignOpen(false)}
                disabled={isPending}
                className="h-8 px-3 text-xs"
              >
                {tCommon("cancel")}
              </Button>
              <Button
                variant="primary"
                onClick={handleAssign}
                disabled={!selectedOwner || isPending}
                className="h-8 px-3 text-xs"
              >
                {t("assign")}
              </Button>
            </div>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>

      {/* Delete Confirmation Dialog */}
      <ConfirmDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title={t("deleteConfirmTitle")}
        description={t("deleteConfirmDescription", { count: selectedCount })}
        confirmLabel={t("delete")}
        onConfirm={handleDelete}
        destructive
      />
    </div>
  );
}
