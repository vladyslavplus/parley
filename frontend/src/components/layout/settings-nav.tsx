"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";

import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
import { customFieldsListQueryOptions } from "@/features/crm/hooks/use-custom-fields";
import { Can } from "@/features/session/components/can";
import { Link, usePathname } from "@/i18n/navigation";
import {
  auditListQueryOptions,
  invitationsListQueryOptions,
  membersListQueryOptions,
  meQueryOptions,
  teamsListQueryOptions,
} from "@/lib/api/hooks/use-api";
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
  {
    href: ROUTES.settingsCustomFields,
    key: "customFields",
    permission: PERMISSIONS.customFieldsManage,
  },
] as const;

type SettingsNavKey = (typeof ITEMS)[number]["key"];

function NavItem({
  href,
  itemKey,
  label,
}: {
  href: string;
  itemKey: SettingsNavKey;
  label: string;
}) {
  const pathname = usePathname();
  const locale = useLocale();
  const queryClient = useQueryClient();
  const active = pathname === href;

  function handlePrefetchIntent() {
    if (itemKey === "profile" || itemKey === "organization") {
      void queryClient.query(meQueryOptions(locale)).catch(() => undefined);
      return;
    }

    if (itemKey === "members") {
      void queryClient
        .query(membersListQueryOptions({}, locale))
        .catch(() => undefined);
      void queryClient
        .query(invitationsListQueryOptions({}, locale))
        .catch(() => undefined);
      return;
    }

    if (itemKey === "teams") {
      void queryClient
        .query(teamsListQueryOptions({}, locale))
        .catch(() => undefined);
      return;
    }

    if (itemKey === "audit") {
      void queryClient
        .query(auditListQueryOptions({}, locale))
        .catch(() => undefined);
      return;
    }

    if (itemKey === "customFields") {
      void queryClient
        .query(customFieldsListQueryOptions({}, locale))
        .catch(() => undefined);
    }
  }

  return (
    <li>
      <Link
        href={href}
        aria-current={active ? "page" : undefined}
        onMouseEnter={handlePrefetchIntent}
        onFocus={handlePrefetchIntent}
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
              <NavItem
                href={item.href}
                itemKey={item.key}
                label={t(item.key)}
              />
            </Can>
          ) : (
            <NavItem
              key={item.key}
              href={item.href}
              itemKey={item.key}
              label={t(item.key)}
            />
          ),
        )}
      </ul>
    </nav>
  );
}
