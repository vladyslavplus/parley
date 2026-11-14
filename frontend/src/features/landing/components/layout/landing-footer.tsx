import { useTranslations } from "next-intl";

import { Logo } from "@/components/brand/logo";
import { LocaleToggle } from "@/components/layout/locale-toggle";
import { ROUTES } from "@/constants/routes";
import { Link } from "@/i18n/navigation";

import { LANDING_CONTAINER } from "./landing-container";

const LINK =
  "rounded-[var(--radius-control)] text-sm text-muted-foreground underline-offset-4 hover:text-foreground hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring";

export function LandingFooter() {
  const t = useTranslations("landing.footer");

  return (
    <footer className="border-t border-border">
      <div
        className={`${LANDING_CONTAINER} flex flex-col gap-8 py-12 md:flex-row md:items-start md:justify-between`}
      >
        <div className="flex flex-col gap-2">
          <Logo size={24} />
          <p className="text-sm text-muted-foreground">{t("note")}</p>
        </div>
        <nav aria-label={t("label")}>
          <ul className="flex flex-wrap items-center gap-x-6 gap-y-3">
            <li>
              <Link href={ROUTES.home} className={LINK}>
                {t("home")}
              </Link>
            </li>
            <li>
              <Link href={ROUTES.privacy} className={LINK}>
                {t("privacy")}
              </Link>
            </li>
            <li>
              <Link href={ROUTES.terms} className={LINK}>
                {t("terms")}
              </Link>
            </li>
            <li>
              <Link href={ROUTES.login} className={LINK}>
                {t("signIn")}
              </Link>
            </li>
            <li className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground">
                {t("language")}
              </span>
              <LocaleToggle />
            </li>
          </ul>
        </nav>
      </div>
    </footer>
  );
}
