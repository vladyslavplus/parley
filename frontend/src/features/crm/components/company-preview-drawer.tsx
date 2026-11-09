"use client";

import { ExternalLink, Trash2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { Drawer } from "@/components/ui/drawer";
import { PERMISSIONS } from "@/constants/permissions";
import { companyRecordRoute } from "@/constants/routes";
import { useCrmMutations } from "@/features/crm/hooks/use-crm-mutations";
import { useCustomFields } from "@/features/crm/hooks/use-custom-fields";
import { useSoftDeleteWithUndo } from "@/features/crm/hooks/use-soft-delete-with-undo";
import type { Company } from "@/features/crm/types";
import { Can } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";
import { formatDate } from "@/lib/formatters";

interface CompanyPreviewDrawerProps {
  company: Company | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CompanyPreviewDrawer({
  company,
  open,
  onOpenChange,
}: CompanyPreviewDrawerProps) {
  const t = useTranslations("crm.companies");
  const tCrm = useTranslations("crm");
  const locale = useLocale();
  const customFieldsQuery = useCustomFields("company");
  const { deleteCompany, restoreCompany } = useCrmMutations();

  const { handleDelete } = useSoftDeleteWithUndo({
    deleteEntity: () =>
      company ? deleteCompany(company.id) : Promise.resolve(),
    restoreEntity: () =>
      company ? restoreCompany(company.id) : Promise.resolve(),
    deletedMessage: t("record.deleted"),
    onDeleted: () => {
      onOpenChange(false);
    },
  });

  if (!company) return null;

  const websiteUrl = company.domain
    ? company.domain.startsWith("http")
      ? company.domain
      : `https://${company.domain}`
    : null;

  const customFieldDefs = customFieldsQuery.data ?? [];
  const customFieldValues =
    (company.customFields as Record<string, unknown>) ?? {};

  const activeFieldEntries = customFieldDefs
    .map((def) => {
      const val = customFieldValues[def.key];
      if (val === undefined || val === null || val === "") return null;
      let displayVal = String(val);
      if (def.fieldType === "boolean") {
        displayVal = val ? tCrm("customFields.yes") : tCrm("customFields.no");
      }
      return { key: def.key, label: def.label, value: displayVal };
    })
    .filter((entry): entry is { key: string; label: string; value: string } =>
      Boolean(entry),
    );

  const hasCompanyDetails = Boolean(
    websiteUrl || company.industry || company.country,
  );
  const hasOwner = Boolean(company.ownerDisplayName || company.ownerEmail);
  const hasDetails = Boolean(
    activeFieldEntries.length > 0 || company.createdAt,
  );

  return (
    <Drawer
      open={open}
      onOpenChange={onOpenChange}
      caption={t("columns.name")}
      title={company.name}
      description={company.industry ?? undefined}
      footer={
        <div className="flex items-center justify-between gap-2 w-full">
          <Can permission={PERMISSIONS.companiesDelete}>
            <Button
              variant="secondary"
              className="text-xs text-destructive border-border hover:bg-destructive/10 hover:border-destructive/40 gap-1.5 px-2.5 whitespace-nowrap shrink-0"
              onClick={handleDelete}
            >
              <Trash2 className="h-3.5 w-3.5 shrink-0" />
              <span>{t("preview.delete")}</span>
            </Button>
          </Can>
          <div className="flex items-center gap-2 ml-auto shrink-0">
            <Button
              variant="secondary"
              className="text-xs px-3 whitespace-nowrap shrink-0"
              onClick={() => onOpenChange(false)}
            >
              {t("preview.back")}
            </Button>
            <Link
              href={companyRecordRoute(company.id)}
              className="inline-flex items-center justify-center gap-1.5 rounded-[var(--radius-control)] bg-primary text-primary-foreground px-3 text-xs font-medium hover:bg-primary/90 transition-colors whitespace-nowrap shrink-0"
            >
              <span>{t("preview.openRecord")}</span>
              <ExternalLink className="h-3.5 w-3.5 shrink-0" />
            </Link>
          </div>
        </div>
      }
    >
      <div className="space-y-5 text-sm">
        {/* Group 1: Company details */}
        {hasCompanyDetails && (
          <section className="space-y-2">
            <h4 className="text-xs font-semibold text-muted-foreground">
              {t("preview.groups.companyDetails")}
            </h4>
            <dl className="divide-y divide-border/60 rounded-[var(--radius-control)] border border-border bg-card/60 px-3.5 py-1">
              {websiteUrl && (
                <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0">
                    {t("record.fields.domain")}
                  </dt>
                  <dd className="min-w-0 text-right">
                    <a
                      href={websiteUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="font-mono text-info hover:underline truncate block"
                    >
                      {company.domain}
                    </a>
                  </dd>
                </div>
              )}

              {company.industry && (
                <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0">
                    {t("record.fields.industry")}
                  </dt>
                  <dd className="font-medium text-foreground truncate text-right">
                    {company.industry}
                  </dd>
                </div>
              )}

              {company.country && (
                <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0">
                    {t("record.fields.country")}
                  </dt>
                  <dd className="font-mono font-medium text-foreground uppercase truncate text-right">
                    {company.country}
                  </dd>
                </div>
              )}
            </dl>
          </section>
        )}

        {/* Group 2: Ownership */}
        {hasOwner && (
          <section className="space-y-2">
            <h4 className="text-xs font-semibold text-muted-foreground">
              {t("preview.groups.ownership")}
            </h4>
            <dl className="divide-y divide-border/60 rounded-[var(--radius-control)] border border-border bg-card/60 px-3.5 py-1">
              <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                <dt className="text-muted-foreground shrink-0">
                  {t("record.fields.owner")}
                </dt>
                <dd className="min-w-0 text-right">
                  {company.ownerDisplayName ? (
                    <div className="flex flex-col items-end min-w-0">
                      <span className="font-medium text-foreground truncate max-w-full">
                        {company.ownerDisplayName}
                      </span>
                      {company.ownerEmail &&
                        company.ownerEmail !== company.ownerDisplayName && (
                          <span className="font-mono text-[11px] text-muted-foreground truncate max-w-full">
                            {company.ownerEmail}
                          </span>
                        )}
                    </div>
                  ) : (
                    <span className="font-mono text-foreground truncate block">
                      {company.ownerEmail}
                    </span>
                  )}
                </dd>
              </div>
            </dl>
          </section>
        )}

        {/* Group 3: Details (Custom fields, Created) */}
        {hasDetails && (
          <section className="space-y-2">
            <h4 className="text-xs font-semibold text-muted-foreground">
              {t("preview.groups.details")}
            </h4>
            <dl className="divide-y divide-border/60 rounded-[var(--radius-control)] border border-border bg-card/60 px-3.5 py-1">
              {activeFieldEntries.map((entry) => (
                <div
                  key={entry.key}
                  className="flex items-baseline justify-between gap-3 py-2 text-xs"
                >
                  <dt className="text-muted-foreground shrink-0">
                    {entry.label}
                  </dt>
                  <dd className="font-medium text-foreground truncate text-right">
                    {entry.value}
                  </dd>
                </div>
              ))}

              {company.createdAt && (
                <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0">
                    {t("columns.created")}
                  </dt>
                  <dd className="font-medium text-foreground tabular-nums text-right">
                    {formatDate(company.createdAt, locale)}
                  </dd>
                </div>
              )}
            </dl>
          </section>
        )}
      </div>
    </Drawer>
  );
}
