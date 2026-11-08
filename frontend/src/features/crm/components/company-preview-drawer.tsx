"use client";

import { ExternalLink, Globe, MapPin, User } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { Drawer } from "@/components/ui/drawer";
import { companyRecordRoute } from "@/constants/routes";
import type { Company } from "@/features/crm/types";
import { Link } from "@/i18n/navigation";
import { formatDate } from "@/lib/formatters";

interface Props {
  company: Company | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CompanyPreviewDrawer({ company, open, onOpenChange }: Props) {
  const t = useTranslations("crm.companies");
  const locale = useLocale();

  if (!company) return null;

  const websiteUrl = company.domain
    ? company.domain.startsWith("http")
      ? company.domain
      : `https://${company.domain}`
    : null;

  return (
    <Drawer
      open={open}
      onOpenChange={onOpenChange}
      title={company.name}
      description={company.industry ?? undefined}
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
            href={companyRecordRoute(company.id)}
            className="inline-flex items-center justify-center gap-1.5 rounded-[var(--radius-control)] bg-primary text-primary-foreground px-4 py-2 text-xs font-medium hover:bg-primary/90 transition-colors shrink-0"
          >
            <span>{t("preview.openRecord")}</span>
            <ExternalLink className="h-3.5 w-3.5" />
          </Link>
        </div>
      }
    >
      <div className="space-y-6 text-sm">
        <div className="space-y-3 rounded-[var(--radius-control)] border border-border bg-card/60 p-4">
          {websiteUrl && (
            <div className="flex items-center gap-2.5 text-xs">
              <Globe className="h-4 w-4 text-muted-foreground shrink-0" />
              <a
                href={websiteUrl}
                target="_blank"
                rel="noreferrer"
                className="text-info hover:underline truncate"
              >
                {company.domain}
              </a>
            </div>
          )}

          {company.country && (
            <div className="flex items-center gap-2.5 text-xs text-muted-foreground">
              <MapPin className="h-4 w-4 shrink-0" />
              <span>{company.country}</span>
            </div>
          )}

          <div className="flex items-center gap-2.5 text-xs text-muted-foreground">
            <User className="h-4 w-4 shrink-0" />
            <span>
              {company.ownerUserId
                ? t("record.fields.owner")
                : t("record.fields.unassigned")}
            </span>
          </div>
        </div>

        <div className="pt-2 border-t border-border text-xs text-muted-foreground">
          <span>{t("columns.created")}: </span>
          <span>{formatDate(company.createdAt, locale)}</span>
        </div>
      </div>
    </Drawer>
  );
}
