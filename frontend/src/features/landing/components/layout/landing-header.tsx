import { useTranslations } from "next-intl";

import { Logo } from "@/components/brand/logo";
import { LocaleToggle } from "@/components/layout/locale-toggle";
import { ThemeToggle } from "@/components/layout/theme-toggle";
import { buttonVariants } from "@/components/ui/button";
import { ROUTES } from "@/constants/routes";
import { Link } from "@/i18n/navigation";

import { LANDING_CONTAINER } from "./landing-container";

export function LandingHeader({ signedIn }: { signedIn: boolean }) {
  const t = useTranslations("landing.nav");

  return (
    <header className={`${LANDING_CONTAINER} flex items-center gap-3 py-5`}>
      <Link
        href={ROUTES.home}
        aria-label={t("home")}
        className="mr-auto rounded-[var(--radius-control)] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-ring"
      >
        <Logo size={28} />
      </Link>
      <nav aria-label={t("label")} className="flex items-center gap-2">
        <div className="hidden sm:block">
          <LocaleToggle />
        </div>
        <ThemeToggle />
        {signedIn ? (
          <Link
            href={ROUTES.app}
            className={buttonVariants({ className: "h-8 text-xs sm:text-sm" })}
          >
            {t("openApp")}
          </Link>
        ) : (
          <Link
            href={ROUTES.login}
            className={buttonVariants({
              variant: "secondary",
              className: "h-8 text-xs sm:text-sm",
            })}
          >
            {t("signIn")}
          </Link>
        )}
      </nav>
    </header>
  );
}
