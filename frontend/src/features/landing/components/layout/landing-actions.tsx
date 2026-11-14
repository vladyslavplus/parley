import { ArrowRight } from "lucide-react";

import { buttonVariants } from "@/components/ui/button";
import { ROUTES } from "@/constants/routes";
import { Link } from "@/i18n/navigation";
import { cn } from "@/lib/utils";

const SIZE = {
  compact: "",
  large: "h-11 px-5 text-base",
} as const;

export interface LandingActionLabels {
  openApp: string;
  createWorkspace: string;
  signIn: string;
}

/**
 * Signed-in visitors get one way in; signed-out visitors can create a
 * workspace or sign in.
 */
export function LandingActions({
  signedIn,
  labels,
  size = "large",
  className,
}: {
  signedIn: boolean;
  labels: LandingActionLabels;
  size?: keyof typeof SIZE;
  className?: string;
}) {
  if (signedIn) {
    return (
      <div className={cn("flex flex-wrap items-center gap-3", className)}>
        <Link
          href={ROUTES.app}
          className={buttonVariants({ className: cn("gap-2", SIZE[size]) })}
        >
          {labels.openApp}
          <ArrowRight className="h-4 w-4" aria-hidden="true" />
        </Link>
      </div>
    );
  }

  return (
    <div className={cn("flex flex-wrap items-center gap-3", className)}>
      <Link
        href={ROUTES.register}
        className={buttonVariants({ className: cn("gap-2", SIZE[size]) })}
      >
        {labels.createWorkspace}
        <ArrowRight className="h-4 w-4" aria-hidden="true" />
      </Link>
      <Link
        href={ROUTES.login}
        className={buttonVariants({
          variant: "secondary",
          className: SIZE[size],
        })}
      >
        {labels.signIn}
      </Link>
    </div>
  );
}
