import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import { LandingActions } from "../layout/landing-actions";
import { LANDING_CONTAINER } from "../layout/landing-container";
import { HeroQuietRow, HeroStage } from "./hero-stage";

const ROWS = [
  { key: "first", persuadable: true },
  { key: "second", persuadable: false },
  { key: "third", persuadable: true },
  { key: "fourth", persuadable: false },
  { key: "fifth", persuadable: true },
] as const;

// On wide screens the rows leave room on the right for the floating record.
const ROW_GRID =
  "lg:pr-[25rem] grid grid-cols-[minmax(0,1fr)_auto] gap-x-4 gap-y-1 px-4 py-3 sm:grid-cols-[11rem_minmax(0,1fr)_auto] sm:items-center sm:px-5";

export function HeroSection({ signedIn }: { signedIn: boolean }) {
  const t = useTranslations("landing");

  const copy = (
    <div className={cn(LANDING_CONTAINER, "pt-12 sm:pt-20")}>
      <h1
        id="landing-hero-title"
        className="max-w-[20ch] font-display text-[clamp(2.25rem,5.2vw,4rem)] font-medium leading-[1.02] tracking-[-0.025em] text-balance text-foreground"
      >
        {t("hero.title")}
      </h1>
      <p className="mt-6 max-w-[58ch] text-lg leading-relaxed text-pretty text-muted-foreground sm:text-xl">
        {t("hero.lead")}
      </p>
      <LandingActions
        signedIn={signedIn}
        labels={{
          openApp: t("nav.openApp"),
          createWorkspace: t("nav.createWorkspace"),
          signIn: t("nav.signIn"),
        }}
        className="mt-9"
      />
    </div>
  );

  const list = (
    <div className="overflow-hidden rounded-[var(--radius-landing)] border border-border bg-background shadow-floating">
      <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1 border-b border-border px-4 py-4 sm:px-5">
        <span className="font-display text-xl font-medium text-foreground">
          {t("mock.list.title")}
        </span>
        <span className="text-sm text-muted-foreground">
          {t("mock.list.subtitle")}
        </span>
        <span className="ml-auto rounded-full border border-border bg-card px-2.5 py-0.5 text-xs text-muted-foreground">
          {t("mock.list.sortedBy")}
        </span>
      </div>
      <div
        className={cn(
          ROW_GRID,
          "hidden bg-card/60 py-2 text-xs font-medium text-muted-foreground sm:grid",
        )}
      >
        <span>{t("mock.list.columnContact")}</span>
        <span>{t("mock.list.columnWhy")}</span>
        <span>{t("mock.list.columnNext")}</span>
      </div>
      <ul className="divide-y divide-border">
        {ROWS.map((row) => {
          const content = (
            <div className={ROW_GRID}>
              <span className="flex min-w-0 items-center gap-2.5 text-sm font-medium text-foreground">
                <span
                  className={cn(
                    "h-2 w-2 shrink-0 rounded-full",
                    row.persuadable
                      ? "bg-highlight ring-1 ring-landing-lit-edge"
                      : "bg-muted-foreground/30",
                  )}
                />
                <span className="truncate">
                  {t(`mock.rows.${row.key}.name`)}
                </span>
              </span>
              <span className="col-start-1 truncate text-sm text-muted-foreground sm:col-start-auto">
                {t(`mock.rows.${row.key}.why`)}
              </span>
              <span className="col-start-2 row-span-2 row-start-1 self-center justify-self-end whitespace-nowrap rounded-[var(--radius-control)] border border-border px-2 py-1 text-xs text-foreground sm:col-start-auto sm:row-span-1 sm:row-start-auto sm:justify-self-start">
                {t(`mock.rows.${row.key}.next`)}
              </span>
            </div>
          );
          return (
            <li key={row.key}>
              {row.persuadable ? (
                content
              ) : (
                <HeroQuietRow>{content}</HeroQuietRow>
              )}
            </li>
          );
        })}
      </ul>
      <p className="border-t border-border px-4 py-3 text-right text-xs text-muted-foreground sm:px-5">
        {t("mock.list.updated")}
      </p>
    </div>
  );

  const record = (
    <div className="rounded-[var(--radius-landing)] border border-border bg-popover p-5 shadow-floating">
      <div className="flex items-center gap-3">
        <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-tag-1 font-display text-base font-medium text-tag-1-foreground">
          {t("mock.rows.first.name").charAt(0)}
        </span>
        <div className="min-w-0">
          <p className="text-xs text-muted-foreground">
            {t("mock.record.label")}
          </p>
          <p className="truncate text-base font-medium text-foreground">
            {t("mock.rows.first.name")}
          </p>
        </div>
      </div>
      <p className="mt-4 text-xs font-medium text-muted-foreground">
        {t("mock.record.why")}
      </p>
      <ul className="mt-2 space-y-1.5 text-sm text-foreground">
        {(["reasonOne", "reasonTwo", "reasonThree"] as const).map((key) => (
          <li key={key} className="flex items-start gap-2">
            <span className="mt-2 h-1 w-1 shrink-0 rounded-full bg-muted-foreground" />
            {t(`mock.record.${key}`)}
          </li>
        ))}
      </ul>
      <p className="mt-4 rounded-[var(--radius-control)] bg-primary px-3 py-2 text-center text-sm font-medium text-primary-foreground">
        {t("mock.record.next")}
      </p>
    </div>
  );

  const note = (
    <div className="rounded-[var(--radius-card)] border border-border bg-popover p-4 shadow-floating">
      <p className="flex items-center gap-2 text-sm font-medium text-foreground">
        <span className="h-2.5 w-2.5 rounded-full border border-muted-foreground" />
        {t("mock.note.title")}
      </p>
      <p className="mt-1 text-xs leading-relaxed text-muted-foreground">
        {t("mock.note.body")}
      </p>
    </div>
  );

  return <HeroStage copy={copy} list={list} record={record} note={note} />;
}
