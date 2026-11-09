"use client";

import { ArrowLeft, RefreshCw, Trash2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { InlineEdit } from "@/components/ui/inline-edit";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { APP_NAME } from "@/constants/app";
import { PERMISSIONS } from "@/constants/permissions";
import { dealRecordRoute, ROUTES } from "@/constants/routes";
import { CompanyPicker } from "@/features/crm/components/company-picker";
import { CrmCustomFieldsRender } from "@/features/crm/components/crm-custom-fields-render";
import { OwnerPicker } from "@/features/crm/components/owner-picker";
import { RecordTabs } from "@/features/crm/components/record-tabs";
import { useCustomFields } from "@/features/crm/hooks/use-custom-fields";
import { useSoftDeleteWithUndo } from "@/features/crm/hooks/use-soft-delete-with-undo";
import { ContactPicker } from "@/features/deals/components/contact-picker";
import { DealWonLostDialog } from "@/features/deals/components/deal-won-lost-dialog";
import { useDealMutations } from "@/features/deals/hooks/use-deal-mutations";
import { useDeal } from "@/features/deals/hooks/use-deals";
import { useOrganizationCurrency } from "@/features/deals/hooks/use-organization-currency";
import { usePipeline } from "@/features/deals/hooks/use-pipelines";
import type { Deal, PipelineStage } from "@/features/deals/types";
import { TasksPanel } from "@/features/tasks/components/tasks-panel";
import { ActivityTimeline } from "@/features/timeline/components/activity-timeline";
import { useCan } from "@/features/session/components/can";
import { Link, useRouter } from "@/i18n/navigation";
import { toApiError } from "@/lib/errors";
import { formatCurrency } from "@/lib/formatters";
import { showSuccessToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

type TabKey = "overview" | "timeline" | "tasks";

export function DealRecordView({ id }: { id: string }) {
  const t = useTranslations("crm.deals.record");
  const tCrm = useTranslations("crm");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const router = useRouter();
  const orgCurrency = useOrganizationCurrency();

  const canWrite = useCan(PERMISSIONS.dealsWrite);
  const canDelete = useCan(PERMISSIONS.dealsDelete);

  const [activeTab, setActiveTab] = useState<TabKey>("overview");
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [terminalModalOpen, setTerminalModalOpen] = useState(false);
  const [pendingTargetStage, setPendingTargetStage] =
    useState<PipelineStage | null>(null);

  const dealQuery = useDeal(id);
  const deal = dealQuery.data;

  const pipelineQuery = usePipeline(deal?.pipelineId);
  const stages = pipelineQuery.data?.stages ?? [];

  const customFieldsQuery = useCustomFields("deal");
  const customFieldDefs = customFieldsQuery.data ?? [];

  const { updateDeal, deleteDeal, moveDeal, restoreDeal } = useDealMutations();

  const { handleDelete } = useSoftDeleteWithUndo({
    deleteEntity: () => (deal ? deleteDeal(deal.id) : Promise.resolve()),
    restoreEntity: () =>
      deal ? restoreDeal(deal.id) : Promise.resolve(null as unknown as Deal),
    deletedMessage: deal ? t("dealDeleted", { title: deal.title }) : "",
    onDeleted: () => {
      setDeleteConfirmOpen(false);
      router.push(ROUTES.deals);
    },
    onRestored: () => {
      if (deal) router.push(dealRecordRoute(deal.id));
    },
  });

  useEffect(() => {
    if (deal?.title) {
      document.title = `${deal.title} · ${APP_NAME}`;
    }
  }, [deal?.title]);

  if (dealQuery.isPending) {
    return (
      <div className="space-y-6">
        <Skeleton className="w-40" />
        <div className="rounded-[var(--radius-card)] border border-border bg-card p-6 space-y-4">
          <Skeleton className="h-7 w-64" />
          <Skeleton className="h-4 w-48" />
          <div className="grid grid-cols-2 gap-4 pt-4">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </div>
        </div>
      </div>
    );
  }

  if (dealQuery.isError || !deal) {
    return (
      <div className="rounded-[var(--radius-card)] border border-border bg-card p-8 text-center space-y-3">
        <p className="text-sm text-muted-foreground">{t("notFound")}</p>
        <Button
          variant="secondary"
          onClick={() => router.push(ROUTES.deals)}
          className="px-3 text-xs"
        >
          {t("back")}
        </Button>
      </div>
    );
  }

  const isClosed = deal.status === "won" || deal.status === "lost";

  async function handleTitleSave(newTitle: string) {
    if (!deal) return;
    try {
      await updateDeal(deal, {
        version: deal.version,
        title: newTitle,
      });
      showSuccessToast(t("saved"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  async function handleAmountSave(newAmountStr: string) {
    if (!deal) return;
    const num = parseFloat(newAmountStr);
    if (isNaN(num) || num < 0) return;
    try {
      await updateDeal(deal, {
        version: deal.version,
        amount: num,
      });
      showSuccessToast(t("saved"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  async function handleExpectedCloseSave(dateStr: string) {
    if (!deal) return;
    try {
      await updateDeal(deal, {
        version: deal.version,
        expectedCloseOn: dateStr || null,
      });
      showSuccessToast(t("saved"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  async function handleCompanySave(companyId: string | null) {
    if (!deal) return;
    try {
      await updateDeal(deal, {
        version: deal.version,
        companyId,
      });
      showSuccessToast(t("saved"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  async function handleContactSave(contactId: string | null) {
    if (!deal) return;
    try {
      await updateDeal(deal, {
        version: deal.version,
        contactId,
      });
      showSuccessToast(t("saved"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  async function handleOwnerSave(ownerUserId: string | null) {
    if (!deal) return;
    try {
      await updateDeal(deal, {
        version: deal.version,
        ownerUserId,
      });
      showSuccessToast(t("saved"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  async function handleStageChange(targetStageId: string) {
    if (!deal || targetStageId === deal.stageId) return;
    const targetStage = stages.find((s) => s.id === targetStageId);
    if (!targetStage) return;

    if (targetStage.kind === "won" || targetStage.kind === "lost") {
      setPendingTargetStage(targetStage);
      setTerminalModalOpen(true);
      return;
    }

    try {
      await moveDeal({
        id: deal.id,
        stageId: targetStage.id,
        position: 0,
        version: deal.version,
      });
      showSuccessToast(t("stageChanged"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  async function handleTerminalConfirm(payload: {
    dealId: string;
    stageId: string;
    position: number;
    version: number | string;
    lostReason?: string | null;
  }) {
    if (!deal || !pendingTargetStage) return;
    try {
      await moveDeal({
        id: deal.id,
        stageId: pendingTargetStage.id,
        position: 0,
        version: deal.version,
        lostReason: payload.lostReason,
      });
      showSuccessToast(t("stageChanged"));
      setPendingTargetStage(null);
    } catch (err) {
      throw toApiError(err);
    }
  }

  function handleReopen() {
    if (!deal) return;
    const firstOpenStage = stages.find((s) => s.kind === "open");
    if (!firstOpenStage) return;
    void moveDeal({
      id: deal.id,
      stageId: firstOpenStage.id,
      position: 0,
      version: deal.version,
    });
  }

  async function handleCustomFieldSave(key: string, value: unknown) {
    if (!deal) return;
    const activeDefs = customFieldDefs.filter((d) => !d.archivedAt);
    const current = (deal.customFields as Record<string, unknown>) ?? {};
    const nextCustomFields: Record<string, unknown> = {};
    for (const d of activeDefs) {
      nextCustomFields[d.key] =
        d.key === key ? value : (current[d.key] ?? null);
    }

    try {
      await updateDeal(deal, {
        version: deal.version,
        customFields: nextCustomFields,
      });
      showSuccessToast(tCrm("customFields.updated"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  const tabItems = [
    {
      key: "overview" as const,
      label: t("sections.overview"),
      content: (
        <div className="space-y-4">
          <h3 className="text-sm font-semibold text-foreground">
            {t("sections.customFields")}
          </h3>
          <CrmCustomFieldsRender
            definitions={customFieldDefs}
            values={deal.customFields as Record<string, unknown>}
            onSave={handleCustomFieldSave}
            disabled={!canWrite}
          />
        </div>
      ),
    },
    {
      key: "timeline" as const,
      label: t("sections.timeline"),
      content: <ActivityTimeline subject={{ dealId: deal.id }} />,
    },
    {
      key: "tasks" as const,
      label: t("sections.tasks"),
      content: (
        <TasksPanel
          subject={{
            dealId: deal.id,
            dealTitle: deal.title,
            companyId: deal.companyId ?? undefined,
            companyName: deal.companyName,
            contactId: deal.contactId ?? undefined,
            contactName: deal.contactName,
          }}
        />
      ),
    },
  ];

  const amountDisplay = formatCurrency(
    typeof deal.amount === "number"
      ? deal.amount
      : parseFloat(deal.amount || "0"),
    deal.currency || orgCurrency,
    locale,
  );

  return (
    <div className="space-y-6">
      {/* Navigation header */}
      <div className="flex items-center justify-between">
        <Link
          href={ROUTES.deals}
          className="inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          <span>{t("back")}</span>
        </Link>

        <div className="flex items-center gap-2">
          {isClosed && canWrite && (
            <Button
              variant="secondary"
              onClick={handleReopen}
              className="gap-1.5 text-xs px-2.5"
            >
              <RefreshCw className="h-3.5 w-3.5" />
              <span>{t("reopen")}</span>
            </Button>
          )}

          {canDelete && (
            <Button
              variant="secondary"
              onClick={() => setDeleteConfirmOpen(true)}
              className="gap-1.5 text-xs px-2.5 text-destructive hover:bg-destructive/10 hover:text-destructive"
            >
              <Trash2 className="h-3.5 w-3.5" />
              <span>{t("delete")}</span>
            </Button>
          )}
        </div>
      </div>

      {/* Main card */}
      <div className="rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-xs">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="space-y-1 flex-1 min-w-[240px]">
            <div className="flex items-center gap-2">
              <InlineEdit
                value={deal.title}
                onSave={handleTitleSave}
                placeholder={t("namePlaceholder")}
                disabled={!canWrite}
                className="text-xl font-bold text-foreground"
              />
              <span
                className={cn(
                  "shrink-0 rounded-xs px-2 py-0.5 text-xs font-semibold uppercase tracking-wider",
                  deal.status === "won"
                    ? "bg-success/15 text-success"
                    : deal.status === "lost"
                      ? "bg-destructive/15 text-destructive"
                      : "bg-muted text-muted-foreground",
                )}
              >
                {deal.status}
              </span>
            </div>
            {deal.lostReason && (
              <p className="text-xs text-destructive">
                {t("lostReason", { reason: deal.lostReason })}
              </p>
            )}
          </div>

          <div className="text-right">
            <div className="text-2xl font-bold tabular-nums text-foreground">
              {amountDisplay}
            </div>
            <div className="text-[11px] text-muted-foreground">
              {deal.currency}
            </div>
          </div>
        </div>

        {/* Key Fields Grid */}
        <div className="mt-6 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 pt-6 border-t border-border">
          {/* Stage selector */}
          <div className="space-y-1.5">
            <label className="text-xs font-medium text-muted-foreground">
              {t("fields.stage")}
            </label>
            <Select
              value={deal.stageId}
              onValueChange={(next) => void handleStageChange(next)}
              disabled={!canWrite}
            >
              <SelectTrigger aria-label={t("fields.stage")}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {stages.map((stg) => (
                  <SelectItem key={stg.id} value={stg.id}>
                    {stg.name} ({stg.kind})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Amount Edit */}
          <div className="space-y-1.5">
            <label className="text-xs font-medium text-muted-foreground">
              {t("fields.amount")}
            </label>
            <InlineEdit
              value={String(deal.amount)}
              onSave={handleAmountSave}
              placeholder="0.00"
              disabled={!canWrite}
              className="text-xs"
            />
          </div>

          {/* Expected Close Date */}
          <div className="space-y-1.5">
            <label className="text-xs font-medium text-muted-foreground">
              {t("fields.expectedCloseDate")}
            </label>
            <Input
              type="date"
              value={deal.expectedCloseOn ?? ""}
              onChange={(e) => void handleExpectedCloseSave(e.target.value)}
              disabled={!canWrite}
              className="text-xs"
            />
          </div>

          {/* Company Picker */}
          <div className="space-y-1.5">
            <label className="text-xs font-medium text-muted-foreground">
              {t("fields.company")}
            </label>
            <CompanyPicker
              value={deal.companyId}
              valueName={deal.companyName}
              onChange={handleCompanySave}
              disabled={!canWrite}
              placeholder={t("fields.noCompany")}
            />
          </div>

          {/* Contact Picker */}
          <div className="space-y-1.5">
            <label className="text-xs font-medium text-muted-foreground">
              {t("fields.contact")}
            </label>
            <ContactPicker
              value={deal.contactId}
              valueName={deal.contactName}
              companyId={deal.companyId}
              onChange={handleContactSave}
              disabled={!canWrite}
              placeholder={t("fields.noContact")}
            />
          </div>

          {/* Owner Picker */}
          <div className="space-y-1.5">
            <label className="text-xs font-medium text-muted-foreground">
              {t("fields.owner")}
            </label>
            <OwnerPicker
              value={deal.ownerUserId}
              onChange={handleOwnerSave}
              disabled={!canWrite}
              placeholder={t("fields.unassigned")}
            />
          </div>
        </div>
      </div>

      {/* Tabs */}
      <RecordTabs
        tabs={tabItems}
        activeTab={activeTab}
        onTabChange={(tab) => setActiveTab(tab as TabKey)}
      />

      {/* Delete Confirmation */}
      <ConfirmDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title={t("deleteConfirmTitle")}
        description={t("deleteConfirmDescription", { title: deal.title })}
        confirmLabel={t("delete")}
        cancelLabel={tCommon("cancel")}
        onConfirm={handleDelete}
        destructive
      />

      {/* Terminal Won/Lost Dialog */}
      <DealWonLostDialog
        open={terminalModalOpen}
        onOpenChange={(open) => {
          setTerminalModalOpen(open);
          if (!open) setPendingTargetStage(null);
        }}
        deal={deal}
        targetStage={pendingTargetStage}
        position={1}
        onConfirm={handleTerminalConfirm}
      />
    </div>
  );
}
