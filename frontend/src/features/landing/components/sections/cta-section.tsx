import { useTranslations } from "next-intl";

import { cn } from "@/lib/utils";

import { LandingActions } from "../layout/landing-actions";
import { LANDING_CONTAINER } from "../layout/landing-container";

export function CtaSection({ signedIn }: { signedIn: boolean }) {
  const t = useTranslations("landing");

  return (
    <section
      aria-labelledby="landing-cta-title"
      className={cn(LANDING_CONTAINER, "pt-8 pb-24 sm:pb-32")}
    >
      <div className="rounded-[var(--radius-landing-lg)] bg-card px-6 py-14 sm:px-12 sm:py-20">
        <h2
          id="landing-cta-title"
          className="max-w-[20ch] font-display text-[clamp(2rem,5vw,3.75rem)] font-medium leading-[1.05] tracking-[-0.02em] text-balance text-foreground"
        >
          {t("cta.title")}
        </h2>
        <p className="mt-6 max-w-[52ch] text-lg leading-relaxed text-pretty text-muted-foreground">
          {signedIn ? t("cta.signedInBody") : t("cta.body")}
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
    </section>
  );
}
