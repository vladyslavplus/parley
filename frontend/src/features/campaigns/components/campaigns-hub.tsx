"use client";

import { Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import { parseAsString, parseAsStringLiteral, useQueryState } from "nuqs";

import { buttonVariants } from "@/components/ui/button";
import {
  SegmentedControl,
  type SegmentedOption,
} from "@/components/ui/segmented-control";
import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
import { CampaignsList } from "@/features/campaigns/components/campaigns-list";
import { SegmentsLibrary } from "@/features/campaigns/components/segments-library";
import { TemplatesLibrary } from "@/features/campaigns/components/templates-library";
import { Can } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";

const VIEWS = ["campaigns", "templates", "segments"] as const;
type HubView = (typeof VIEWS)[number];

export function CampaignsHub() {
  const t = useTranslations("campaigns");
  const [view, setView] = useQueryState(
    "view",
    parseAsStringLiteral(VIEWS).withDefault("campaigns"),
  );

  // Search text and the page cursor belong to one library; a tab switch starts
  // the other one clean.
  const [, setStatus] = useQueryState("status", parseAsString.withDefault(""));
  const [, setQ] = useQueryState("q", parseAsString.withDefault(""));
  const [, setAfter] = useQueryState("after", parseAsString.withDefault(""));

  const options: SegmentedOption<HubView>[] = [
    { value: "campaigns", label: t("hub.campaigns") },
    { value: "templates", label: t("hub.templates") },
    { value: "segments", label: t("hub.segments") },
  ];

  return (
    <div className="space-y-4">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            {t("title")}
          </h1>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {t("subtitle")}
          </p>
        </div>
        <Can permission={PERMISSIONS.campaignsManage}>
          <Link
            href={ROUTES.campaignNew}
            className={buttonVariants({
              variant: "primary",
              className: "gap-1.5 self-start px-3 text-xs sm:self-auto",
            })}
          >
            <Plus className="h-4 w-4" />
            <span>{t("hub.newCampaign")}</span>
          </Link>
        </Can>
      </div>

      <SegmentedControl
        value={view}
        options={options}
        ariaLabel={t("hub.viewLabel")}
        onChange={(next) => {
          void setView(next);
          void setQ("");
          void setStatus(null);
          void setAfter("");
        }}
      />

      {view === "campaigns" ? <CampaignsList /> : null}
      {view === "templates" ? <TemplatesLibrary /> : null}
      {view === "segments" ? <SegmentsLibrary /> : null}
    </div>
  );
}
