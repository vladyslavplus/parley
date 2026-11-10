"use client";

import { useTranslations } from "next-intl";

import { buttonVariants } from "@/components/ui/button";
import { ROUTES } from "@/constants/routes";
import { Link } from "@/i18n/navigation";

// Shown when the organization has no contacts yet: the list has nothing to
// rank, so the empty state offers the actions that start it.
export function NewOrganizationEmpty() {
  const t = useTranslations("insights.home.newOrganization");
  return (
    <div className="rounded-[var(--radius-card)] border border-dashed border-border px-6 py-10 text-center">
      <h3 className="text-sm font-semibold text-foreground">{t("title")}</h3>
      <p className="mx-auto mt-1 max-w-md text-xs text-muted-foreground">
        {t("description")}
      </p>
      <div className="mt-4 flex flex-wrap items-center justify-center gap-2">
        <Link
          href={ROUTES.contacts}
          className={buttonVariants({
            variant: "primary",
            className: "px-3 text-xs",
          })}
        >
          {t("addContact")}
        </Link>
        <Link
          href={ROUTES.deals}
          className={buttonVariants({
            variant: "secondary",
            className: "px-3 text-xs",
          })}
        >
          {t("openDeals")}
        </Link>
      </div>
    </div>
  );
}
