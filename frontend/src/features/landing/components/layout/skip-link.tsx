import { useTranslations } from "next-intl";

/**
 * First stop for keyboard users on every public page: it stays out of the
 * layout until focused, then jumps past the header to the main content.
 */
export function LandingSkipLink({ targetId }: { targetId: string }) {
  const t = useTranslations("landing.nav");

  return (
    <a
      href={`#${targetId}`}
      className="sr-only rounded-[var(--radius-control)] bg-primary px-3 py-2 text-sm text-primary-foreground focus:not-sr-only focus:absolute focus:top-3 focus:left-3 focus:z-50"
    >
      {t("skip")}
    </a>
  );
}
