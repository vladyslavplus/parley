import type { ReactNode } from "react";

import { Logo } from "@/components/brand/logo";
import { ROUTES } from "@/constants/routes";
import { Link } from "@/i18n/navigation";

export default function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <main className="mx-auto flex min-h-screen w-full max-w-md flex-col justify-center px-4 py-10">
      <Link
        href={ROUTES.home}
        className="mb-10 self-start rounded-[var(--radius-control)] focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-ring"
      >
        <Logo size={28} />
      </Link>
      {children}
    </main>
  );
}
