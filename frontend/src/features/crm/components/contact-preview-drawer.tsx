"use client";

import { ExternalLink, Trash2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { Drawer } from "@/components/ui/drawer";
import { PERMISSIONS } from "@/constants/permissions";
import { companyRecordRoute, contactRecordRoute } from "@/constants/routes";
import { CrmTagBadge } from "@/features/crm/components/crm-tag-badge";
import { useCrmMutations } from "@/features/crm/hooks/use-crm-mutations";
import { useCustomFields } from "@/features/crm/hooks/use-custom-fields";
import { useSoftDeleteWithUndo } from "@/features/crm/hooks/use-soft-delete-with-undo";
import { useTags } from "@/features/crm/hooks/use-tags";
import type { Contact } from "@/features/crm/types";
import { Can } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";
import { formatDate } from "@/lib/formatters";

interface ContactPreviewDrawerProps {
  contact: Contact | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ContactPreviewDrawer({
  contact,
  open,
  onOpenChange,
}: ContactPreviewDrawerProps) {
  const t = useTranslations("crm.contacts");
  const tCrm = useTranslations("crm");
  const locale = useLocale();
  const tagsQuery = useTags();
  const customFieldsQuery = useCustomFields("contact");
  const { deleteContact, restoreContact } = useCrmMutations();

  const { handleDelete } = useSoftDeleteWithUndo({
    deleteEntity: () =>
      contact ? deleteContact(contact.id) : Promise.resolve(),
    restoreEntity: () =>
      contact ? restoreContact(contact.id) : Promise.resolve(),
    deletedMessage: t("record.deleted"),
    onDeleted: () => {
      onOpenChange(false);
    },
  });

  if (!contact) return null;

  const fullName = `${contact.firstName} ${contact.lastName ?? ""}`.trim();
  const allTags = tagsQuery.data?.items ?? [];
  const tagMap = new Map(allTags.map((tg) => [tg.id, tg]));
  const resolvedTags = (contact.tagIds ?? [])
    .map((tagId) => tagMap.get(tagId))
    .filter((tg): tg is NonNullable<typeof tg> => Boolean(tg));

  const customFieldDefs = customFieldsQuery.data ?? [];
  const customFieldValues =
    (contact.customFields as Record<string, unknown>) ?? {};

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

  const locationValue = [contact.city, contact.country]
    .filter(Boolean)
    .join(", ");
  const hasContactDetails = Boolean(
    contact.email ||
    contact.phone ||
    contact.companyName ||
    contact.companyId ||
    contact.jobTitle ||
    locationValue,
  );
  const hasOwner = Boolean(contact.ownerDisplayName || contact.ownerEmail);
  const hasDetails = Boolean(
    resolvedTags.length > 0 ||
    activeFieldEntries.length > 0 ||
    contact.createdAt,
  );

  return (
    <Drawer
      open={open}
      onOpenChange={onOpenChange}
      caption={t("columns.name")}
      title={fullName}
      description={contact.jobTitle ?? undefined}
      footer={
        <div className="flex items-center justify-between gap-2 w-full">
          <Can permission={PERMISSIONS.contactsDelete}>
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
              href={contactRecordRoute(contact.id)}
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
        {/* Group 1: Contact details */}
        {hasContactDetails && (
          <section className="space-y-2">
            <h4 className="text-xs font-semibold text-muted-foreground">
              {t("preview.groups.contactDetails")}
            </h4>
            <dl className="divide-y divide-border/60 rounded-[var(--radius-control)] border border-border bg-card/60 px-3.5 py-1">
              {contact.email && (
                <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0">
                    {t("record.fields.email")}
                  </dt>
                  <dd className="min-w-0 text-right">
                    <a
                      href={`mailto:${contact.email}`}
                      className="font-mono text-info hover:underline truncate block"
                    >
                      {contact.email}
                    </a>
                  </dd>
                </div>
              )}

              {contact.phone && (
                <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0">
                    {t("record.fields.phone")}
                  </dt>
                  <dd className="min-w-0 text-right">
                    <a
                      href={`tel:${contact.phone}`}
                      className="font-mono text-foreground hover:underline truncate block"
                    >
                      {contact.phone}
                    </a>
                  </dd>
                </div>
              )}

              {(contact.companyName || contact.companyId) && (
                <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0">
                    {t("record.fields.company")}
                  </dt>
                  <dd className="min-w-0 text-right">
                    {contact.companyId ? (
                      <Link
                        href={companyRecordRoute(contact.companyId)}
                        className="font-medium text-info hover:underline truncate block"
                      >
                        {contact.companyName || t("preview.viewCompany")}
                      </Link>
                    ) : (
                      <span className="font-medium text-foreground truncate block">
                        {contact.companyName}
                      </span>
                    )}
                  </dd>
                </div>
              )}

              {contact.jobTitle && (
                <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0">
                    {t("record.fields.jobTitle")}
                  </dt>
                  <dd className="font-medium text-foreground truncate text-right">
                    {contact.jobTitle}
                  </dd>
                </div>
              )}

              {locationValue && (
                <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0">
                    {t("preview.fields.location")}
                  </dt>
                  <dd className="font-medium text-foreground truncate text-right">
                    {locationValue}
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
                  {contact.ownerDisplayName ? (
                    <div className="flex flex-col items-end min-w-0">
                      <span className="font-medium text-foreground truncate max-w-full">
                        {contact.ownerDisplayName}
                      </span>
                      {contact.ownerEmail &&
                        contact.ownerEmail !== contact.ownerDisplayName && (
                          <span className="font-mono text-[11px] text-muted-foreground truncate max-w-full">
                            {contact.ownerEmail}
                          </span>
                        )}
                    </div>
                  ) : (
                    <span className="font-mono text-foreground truncate block">
                      {contact.ownerEmail}
                    </span>
                  )}
                </dd>
              </div>
            </dl>
          </section>
        )}

        {/* Group 3: Details (Tags, Custom fields, Created) */}
        {hasDetails && (
          <section className="space-y-2">
            <h4 className="text-xs font-semibold text-muted-foreground">
              {t("preview.groups.details")}
            </h4>
            <dl className="divide-y divide-border/60 rounded-[var(--radius-control)] border border-border bg-card/60 px-3.5 py-1">
              {resolvedTags.length > 0 && (
                <div className="flex items-start justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0 pt-0.5">
                    {t("record.sections.tags")}
                  </dt>
                  <dd className="flex flex-wrap justify-end gap-1.5">
                    {resolvedTags.map((tg) => (
                      <CrmTagBadge
                        key={tg.id}
                        name={tg.name}
                        colorIndex={tg.colorIndex}
                      />
                    ))}
                  </dd>
                </div>
              )}

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

              {contact.createdAt && (
                <div className="flex items-baseline justify-between gap-3 py-2 text-xs">
                  <dt className="text-muted-foreground shrink-0">
                    {t("columns.created")}
                  </dt>
                  <dd className="font-medium text-foreground tabular-nums text-right">
                    {formatDate(contact.createdAt, locale)}
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
