"use client";

import {
  Building2,
  ExternalLink,
  Mail,
  MapPin,
  Phone,
  User,
} from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { Drawer } from "@/components/ui/drawer";
import { companyRecordRoute, contactRecordRoute } from "@/constants/routes";
import { CrmTagBadge } from "@/features/crm/components/crm-tag-badge";
import { useTags } from "@/features/crm/hooks/use-tags";
import type { Contact } from "@/features/crm/types";
import { Link } from "@/i18n/navigation";
import { formatDate } from "@/lib/formatters";

interface Props {
  contact: Contact | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ContactPreviewDrawer({ contact, open, onOpenChange }: Props) {
  const t = useTranslations("crm.contacts");
  const locale = useLocale();
  const tagsQuery = useTags();

  if (!contact) return null;

  const fullName = `${contact.firstName} ${contact.lastName ?? ""}`.trim();
  const allTags = tagsQuery.data?.items ?? [];
  const tagMap = new Map(allTags.map((tg) => [tg.id, tg]));

  return (
    <Drawer
      open={open}
      onOpenChange={onOpenChange}
      title={fullName}
      description={contact.jobTitle ?? undefined}
      footer={
        <div className="flex items-center justify-between gap-3">
          <Button
            variant="secondary"
            className="w-full text-xs"
            onClick={() => onOpenChange(false)}
          >
            {t("record.back")}
          </Button>
          <Link
            href={contactRecordRoute(contact.id)}
            className="inline-flex items-center justify-center gap-1.5 rounded-[var(--radius-control)] bg-primary text-primary-foreground px-4 py-2 text-xs font-medium hover:bg-primary/90 transition-colors shrink-0"
          >
            <span>{t("preview.openRecord")}</span>
            <ExternalLink className="h-3.5 w-3.5" />
          </Link>
        </div>
      }
    >
      <div className="space-y-6 text-sm">
        {/* Contact info list */}
        <div className="space-y-3 rounded-[var(--radius-control)] border border-border bg-card/60 p-4">
          {contact.email && (
            <div className="flex items-center gap-2.5 text-xs">
              <Mail className="h-4 w-4 text-muted-foreground shrink-0" />
              <a
                href={`mailto:${contact.email}`}
                className="text-info hover:underline truncate"
              >
                {contact.email}
              </a>
            </div>
          )}

          {contact.phone && (
            <div className="flex items-center gap-2.5 text-xs">
              <Phone className="h-4 w-4 text-muted-foreground shrink-0" />
              <a
                href={`tel:${contact.phone}`}
                className="text-foreground hover:underline"
              >
                {contact.phone}
              </a>
            </div>
          )}

          {contact.companyId && (
            <div className="flex items-center gap-2.5 text-xs">
              <Building2 className="h-4 w-4 text-muted-foreground shrink-0" />
              <Link
                href={companyRecordRoute(contact.companyId)}
                className="text-info hover:underline"
              >
                {t("preview.viewCompany")}
              </Link>
            </div>
          )}

          {(contact.city || contact.country) && (
            <div className="flex items-center gap-2.5 text-xs text-muted-foreground">
              <MapPin className="h-4 w-4 shrink-0" />
              <span>
                {[contact.city, contact.country].filter(Boolean).join(", ")}
              </span>
            </div>
          )}

          <div className="flex items-center gap-2.5 text-xs text-muted-foreground">
            <User className="h-4 w-4 shrink-0" />
            <span>
              {contact.ownerUserId
                ? t("record.fields.owner")
                : t("record.fields.unassigned")}
            </span>
          </div>
        </div>

        {/* Tags section */}
        {contact.tagIds && contact.tagIds.length > 0 && (
          <div className="space-y-2">
            <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              {t("record.sections.tags")}
            </h4>
            <div className="flex flex-wrap gap-1.5">
              {contact.tagIds.map((tagId) => {
                const tg = tagMap.get(tagId);
                if (!tg) return null;
                return (
                  <CrmTagBadge
                    key={tg.id}
                    name={tg.name}
                    colorIndex={tg.colorIndex}
                  />
                );
              })}
            </div>
          </div>
        )}

        {/* Created date */}
        <div className="pt-2 border-t border-border text-xs text-muted-foreground">
          <span>{t("columns.created")}: </span>
          <span>{formatDate(contact.createdAt, locale)}</span>
        </div>
      </div>
    </Drawer>
  );
}
