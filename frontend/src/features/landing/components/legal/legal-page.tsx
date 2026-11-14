import { ArrowLeft } from "lucide-react";
import { useTranslations } from "next-intl";

import { Logo } from "@/components/brand/logo";
import { ROUTES } from "@/constants/routes";
import { Link } from "@/i18n/navigation";
import { cn } from "@/lib/utils";

import { LandingFooter } from "../layout/landing-footer";
import { LANDING_CONTAINER } from "../layout/landing-container";
import { LandingSkipLink } from "../layout/skip-link";

export const LEGAL_SECTIONS = {
  privacy: ["stored", "cookies", "email", "telegram", "analytics", "removal"],
  terms: ["asIs", "data", "email", "scores"],
} as const;

export type LegalDocument = keyof typeof LEGAL_SECTIONS;

const LEGAL_MAIN_ID = "legal-main";

/** Short plain-text page shared by privacy and terms. */
export function LegalPage({ document }: { document: LegalDocument }) {
  const t = useTranslations("landing.legal");

  return (
    <div className="flex min-h-screen flex-col">
      <LandingSkipLink targetId={LEGAL_MAIN_ID} />
      <header className={cn(LANDING_CONTAINER, "flex items-center py-5")}>
        <Link
          href={ROUTES.home}
          className="rounded-[var(--radius-control)] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-ring"
        >
          <Logo size={28} />
        </Link>
      </header>
      <main
        id={LEGAL_MAIN_ID}
        tabIndex={-1}
        className={cn(LANDING_CONTAINER, "flex-1 pt-10 pb-24 outline-none")}
      >
        <article className="max-w-[60ch]">
          <Link
            href={ROUTES.home}
            className="inline-flex items-center gap-1.5 rounded-[var(--radius-control)] text-sm text-muted-foreground hover:text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
          >
            <ArrowLeft className="h-4 w-4" aria-hidden="true" />
            {t("back")}
          </Link>
          <h1 className="mt-8 font-display text-[clamp(2.25rem,5vw,3.5rem)] font-medium leading-[1.05] tracking-[-0.02em] text-foreground">
            {t(`${document}.title`)}
          </h1>
          <p className="mt-6 text-lg leading-relaxed text-muted-foreground">
            {t(`${document}.intro`)}
          </p>
          {LEGAL_SECTIONS[document].map((section) => (
            <section key={section} className="mt-10">
              <h2 className="text-lg font-semibold text-foreground">
                {t(`${document}.sections.${section}.title` as never)}
              </h2>
              <p className="mt-2 text-base leading-relaxed text-muted-foreground">
                {t(`${document}.sections.${section}.body` as never)}
              </p>
            </section>
          ))}
        </article>
      </main>
      <LandingFooter />
    </div>
  );
}
