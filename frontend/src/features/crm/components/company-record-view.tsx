"use client";

import {
  ArrowLeft,
  Calendar,
  ExternalLink,
  Globe,
  MapPin,
  RefreshCw,
  Trash2,
  User,
} from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { AlertDialog } from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { InlineEdit } from "@/components/ui/inline-edit";
import { Skeleton } from "@/components/ui/skeleton";
import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
import { CrmCustomFieldsRender } from "@/features/crm/components/crm-custom-fields-render";
import {
  DealsPlaceholder,
  TimelinePlaceholder,
} from "@/features/crm/components/crm-placeholders";
import { RecordTabs } from "@/features/crm/components/record-tabs";
import { useCompany } from "@/features/crm/hooks/use-company";
import { useCrmMutations } from "@/features/crm/hooks/use-crm-mutations";
import { useCustomFields } from "@/features/crm/hooks/use-custom-fields";
import { useSoftDeleteWithUndo } from "@/features/crm/hooks/use-soft-delete-with-undo";
import { Can, useCan } from "@/features/session/components/can";
import { Link, useRouter } from "@/i18n/navigation";
import { useMembers } from "@/lib/api/hooks/use-api";
import { toApiError } from "@/lib/errors";
import { formatDate } from "@/lib/formatters";

type TabKey = "overview" | "timeline" | "deals" | "contacts";

export function CompanyRecordView({ id }: { id: string }) {
  const t = useTranslations("crm.companies");
  const tCrm = useTranslations("crm");
  const locale = useLocale();
  const router = useRouter();

  const canWrite = useCan(PERMISSIONS.companiesWrite);

  const [activeTab, setActiveTab] = useState<TabKey>("overview");
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);

  const companyQuery = useCompany(id);
  const customFieldsQuery = useCustomFields("company");
  const membersQuery = useMembers();

  const { updateCompany, deleteCompany, restoreCompany } = useCrmMutations();

  const { handleDelete } = useSoftDeleteWithUndo({
    deleteEntity: () => deleteCompany(id),
    restoreEntity: () => restoreCompany(id),
    deletedMessage: t("record.deleted"),
    onDeleted: () => {
      setDeleteConfirmOpen(false);
      router.push(ROUTES.companies);
    },
    onRestored: () => {
      router.push(`${ROUTES.companies}/${id}`);
    },
  });

  if (companyQuery.isPending) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-40" />
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

  if (companyQuery.isError || !companyQuery.data) {
    const apiError = companyQuery.error ? toApiError(companyQuery.error) : null;
    const is404 = apiError?.status === 404;

    return (
      <div className="space-y-4">
        <Link
          href={ROUTES.companies}
          className="inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          <span>{t("record.back")}</span>
        </Link>
        <div className="rounded-[var(--radius-card)] border border-border bg-card p-8 text-center space-y-3">
          <p className="text-sm text-muted-foreground">
            {is404 ? tCrm("record.notFound") : tCrm("record.loadError")}
          </p>
          {!is404 && (
            <Button
              variant="secondary"
              onClick={() => companyQuery.refetch()}
              className="h-8 gap-1.5 text-xs px-3"
            >
              <RefreshCw className="h-3.5 w-3.5" />
              <span>{tCrm("record.retry")}</span>
            </Button>
          )}
        </div>
      </div>
    );
  }

  const company = companyQuery.data;
  const customFieldDefs = customFieldsQuery.data ?? [];
  const members = membersQuery.data?.pages.flatMap((p) => p.items) ?? [];
  const memberMap = new Map(members.map((m) => [m.userId, m]));
  const currentOwner = company.ownerUserId
    ? memberMap.get(company.ownerUserId)
    : null;

  async function handleFieldSave(
    field: "name" | "domain" | "country" | "industry",
    value: string | null,
  ) {
    await updateCompany(company.id, {
      version: company.version,
      name: field === "name" ? value : company.name,
      domain: field === "domain" ? value : company.domain,
      country: field === "country" ? value : company.country,
      industry: field === "industry" ? value : company.industry,
    });
    toast.success(tCrm("customFields.updated"));
  }

  async function handleCustomFieldSave(key: string, value: unknown) {
    // Archived keys are rejected by the API, so submit active definitions only
    const activeDefs = customFieldDefs.filter((d) => !d.archivedAt);
    const current = (company.customFields as Record<string, unknown>) ?? {};
    const nextCustomFields: Record<string, unknown> = {};
    for (const d of activeDefs) {
      nextCustomFields[d.key] =
        d.key === key ? value : (current[d.key] ?? null);
    }

    await updateCompany(company.id, {
      version: company.version,
      name: company.name,
      domain: company.domain,
      country: company.country,
      industry: company.industry,
      customFields: nextCustomFields,
    });
    toast.success(tCrm("customFields.updated"));
  }

  const websiteUrl = company.domain
    ? company.domain.startsWith("http")
      ? company.domain
      : `https://${company.domain}`
    : null;

  const tabItems = [
    {
      key: "overview" as const,
      label: t("record.sections.overview"),
      content: (
        <div className="space-y-4">
          <h3 className="text-sm font-semibold text-foreground">
            {t("record.sections.customFields")}
          </h3>
          <CrmCustomFieldsRender
            definitions={customFieldDefs}
            values={company.customFields as Record<string, unknown>}
            onSave={handleCustomFieldSave}
            disabled={!canWrite}
          />
        </div>
      ),
    },
    {
      key: "timeline" as const,
      label: t("record.sections.timeline"),
      content: <TimelinePlaceholder />,
    },
    {
      key: "deals" as const,
      label: t("record.sections.deals"),
      content: <DealsPlaceholder />,
    },
    {
      key: "contacts" as const,
      label: t("record.sections.contacts"),
      content: (
        <div className="rounded-[var(--radius-card)] border border-dashed border-border p-8 text-center bg-card/40">
          <p className="text-sm font-semibold text-foreground">
            {t("record.placeholders.contacts.title")}
          </p>
          <p className="mt-1 text-xs text-muted-foreground">
            {t("record.placeholders.contacts.description")}
          </p>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Back button */}
      <div>
        <Link
          href={ROUTES.companies}
          className="inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          <span>{t("record.back")}</span>
        </Link>
      </div>

      {/* Main Header Card */}
      <div className="rounded-[var(--radius-card)] border border-border bg-card p-6 space-y-5 shadow-sm">
        <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
          <div className="space-y-1">
            <h1 className="text-xl font-bold tracking-tight text-foreground">
              <InlineEdit
                disabled={!canWrite}
                value={company.name}
                onSave={(v) => handleFieldSave("name", v)}
                placeholder={t("record.fields.name")}
                ariaLabel={t("record.fields.name")}
                className="text-xl font-bold tracking-tight px-1 py-0.5"
              />
            </h1>

            <div className="text-xs text-muted-foreground">
              <InlineEdit
                disabled={!canWrite}
                value={company.industry ?? ""}
                onSave={(v) => handleFieldSave("industry", v || null)}
                placeholder={t("record.fields.industry")}
                ariaLabel={t("record.fields.industry")}
              />
            </div>
          </div>

          <Can permission={PERMISSIONS.companiesDelete}>
            <Button
              variant="secondary"
              onClick={() => setDeleteConfirmOpen(true)}
              className="h-8 gap-1.5 text-xs px-3 text-destructive hover:bg-destructive/10 self-start sm:self-auto"
            >
              <Trash2 className="h-3.5 w-3.5" />
              <span>{t("record.delete")}</span>
            </Button>
          </Can>
        </div>

        {/* Info Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4 pt-3 border-t border-border/60">
          {/* Domain */}
          <div className="space-y-1">
            <span className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
              <Globe className="h-3.5 w-3.5" />
              <span>{t("record.fields.domain")}</span>
            </span>
            <div className="flex items-center gap-2">
              <InlineEdit
                disabled={!canWrite}
                value={company.domain ?? ""}
                onSave={(v) => handleFieldSave("domain", v || null)}
                placeholder={t("record.fields.domain")}
                ariaLabel={t("record.fields.domain")}
              />
              {websiteUrl && (
                <a
                  href={websiteUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="text-info hover:underline"
                  aria-label={t("preview.visitWebsite")}
                >
                  <ExternalLink className="h-3.5 w-3.5" />
                </a>
              )}
            </div>
          </div>

          {/* Country */}
          <div className="space-y-1">
            <span className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
              <MapPin className="h-3.5 w-3.5" />
              <span>{t("record.fields.country")}</span>
            </span>
            <InlineEdit
              disabled={!canWrite}
              value={company.country ?? ""}
              onSave={(v) =>
                handleFieldSave("country", v ? v.toUpperCase() : null)
              }
              placeholder={t("record.fields.country")}
              ariaLabel={t("record.fields.country")}
            />
          </div>

          {/* Owner */}
          <div className="space-y-1">
            <span className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
              <User className="h-3.5 w-3.5" />
              <span>{t("record.fields.owner")}</span>
            </span>
            <span className="text-xs text-foreground inline-block pt-1">
              {currentOwner
                ? (currentOwner.displayName ?? currentOwner.email)
                : company.ownerUserId
                  ? t("record.fields.assigned")
                  : t("record.fields.unassigned")}
            </span>
          </div>
        </div>

        {/* Created date */}
        <div className="pt-2 border-t border-border/40 text-xs text-muted-foreground flex items-center gap-1.5">
          <Calendar className="h-3.5 w-3.5" />
          <span>
            {tCrm("record.createdWithDate", {
              date: formatDate(company.createdAt, locale),
            })}
          </span>
        </div>
      </div>

      {/* Accessible RecordTabs */}
      <RecordTabs
        tabs={tabItems}
        activeTab={activeTab}
        onTabChange={setActiveTab}
      />

      {/* Delete confirmation dialog */}
      <AlertDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title={t("record.deleteConfirm")}
        description={t("record.deleteConfirmDescription")}
        confirmLabel={t("record.delete")}
        onConfirm={handleDelete}
        destructive
      />
    </div>
  );
}
