"use client";

import { useTranslations } from "next-intl";

import { Can } from "@/features/session/components/can";
import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
import { Link, usePathname } from "@/i18n/navigation";
import { cn } from "@/lib/utils";

const ITEMS = [
  { href: ROUTES.settingsProfile, key: "profile", permission: null },
  { href: ROUTES.settingsOrganization, key: "organization", permission: null },
  {
    href: ROUTES.settingsMembers,
    key: "members",
    permission: PERMISSIONS.usersManage,
  },
  {
    href: ROUTES.settingsTeams,
    key: "teams",
    permission: PERMISSIONS.teamsManage,
  },
  {
    href: ROUTES.settingsAudit,
    key: "audit",
    permission: PERMISSIONS.auditRead,
  },
] as const;

function NavItem({ href, label }: { href: string; label: string }) {
  const pathname = usePathname();
  const active = pathname === href;
  return (
    <li>
      <Link
        href={href}
        aria-current={active ? "page" : undefined}
        className={cn(
          "block rounded-[var(--radius-control)] px-3 py-2 text-sm transition-colors",
          active
            ? "bg-muted text-foreground font-semibold"
            : "text-muted-foreground hover:bg-muted hover:text-foreground",
        )}
      >
        {label}
      </Link>
    </li>
  );
}

export function SettingsNav() {
  const t = useTranslations("settings.nav");
  return (
    <nav aria-label={t("label")}>
      <ul className="flex flex-wrap gap-1 md:flex-col">
        {ITEMS.map((item) =>
          item.permission ? (
            <Can key={item.key} permission={item.permission}>
              <NavItem href={item.href} label={t(item.key)} />
            </Can>
          ) : (
            <NavItem key={item.key} href={item.href} label={t(item.key)} />
          ),
        )}
      </ul>
    </nav>
  );
}
