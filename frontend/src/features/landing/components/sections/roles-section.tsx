import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import {
  LANDING_BAND,
  LANDING_BODY,
  LANDING_CONTAINER,
  LANDING_HEADING,
} from "../layout/landing-container";

const ROLES = [
  "admin",
  "salesManager",
  "salesRep",
  "marketer",
  "viewer",
] as const;

/** No mock here: the roles themselves, set as type. */
export function RolesSection() {
  const t = useTranslations("landing.roles");

  return (
    <section
      aria-labelledby="landing-roles-title"
      className={cn(LANDING_CONTAINER, LANDING_BAND)}
    >
      <div className="max-w-3xl">
        <h2 id="landing-roles-title" className={LANDING_HEADING}>
          {t("title")}
        </h2>
        <p className={cn(LANDING_BODY, "mt-6")}>{t("body")}</p>
      </div>
      <ul
        aria-label={t("listLabel")}
        className="mt-12 flex flex-wrap items-baseline gap-x-3 gap-y-2 font-display text-[clamp(1.5rem,3.2vw,2.5rem)] leading-tight text-foreground"
      >
        {ROLES.map((role, index) => (
          <li key={role} className="flex items-baseline gap-3">
            <span className={index === 0 ? "" : "text-muted-foreground"}>
              {t(role)}
            </span>
            {index < ROLES.length - 1 ? (
              <span aria-hidden="true" className="text-border">
                /
              </span>
            ) : null}
          </li>
        ))}
      </ul>
    </section>
  );
}
