"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useTranslations, useLocale } from "next-intl";
import { useRouter } from "next/navigation";

import { Button } from "@/components/ui/button";
import { API_ROUTES, ROUTES } from "@/constants/routes";
import { useAction, useMe, useMyOrganizations } from "@/lib/api/hooks/use-api";
import { bffFetch } from "@/lib/api/bff-client";

import { OrganizationSwitcher } from "@/features/organization/components/organization-switcher";

export function AppHeader() {
  const t = useTranslations("common");
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { data: me } = useMe();
  const { data: organizations } = useMyOrganizations();
  const { pending, run } = useAction();

  async function signOut() {
    await run(async () => {
      await bffFetch(API_ROUTES.logout, { method: "POST", locale });
    });
    // Drop every cached response so the next user never sees this one's data.
    queryClient.clear();
    router.replace(`/${locale}${ROUTES.login}`);
    router.refresh();
  }

  return (
    <header className="flex flex-wrap items-center justify-between gap-3 border-b border-neutral-200 px-4 py-3">
      <span className="text-base font-semibold">{t("appName")}</span>
      <div className="flex flex-wrap items-center gap-3">
        {me ? (
          <>
            <OrganizationSwitcher
              current={me.organization.id}
              options={organizations ?? []}
            />
            <span className="text-sm text-neutral-600">
              {me.user.displayName ?? me.user.email}
            </span>
          </>
        ) : null}
        <Button variant="secondary" onClick={signOut} disabled={pending}>
          {t("actions.signOut")}
        </Button>
      </div>
    </header>
  );
}
