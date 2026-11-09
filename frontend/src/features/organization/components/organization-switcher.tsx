"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { useId } from "react";

import { SimpleSelect } from "@/components/ui/select";
import { API_ROUTES } from "@/constants/routes";
import { useAction } from "@/lib/api/hooks/use-api";
import { bffFetch } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";

export function OrganizationSwitcher({
  current,
  options,
}: {
  current: string;
  options: Schemas["MyOrganizationView"][];
}) {
  const t = useTranslations("common.organizationSwitcher");
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const id = useId();
  const { error, pending, run } = useAction();

  async function switchTo(organizationId: string) {
    const switched = await run(async () => {
      await bffFetch(API_ROUTES.switchOrganization, {
        method: "POST",
        body: { organizationId },
        locale,
      });
    });
    if (switched) {
      // Another organization: every cached response is stale, not just old.
      await queryClient.resetQueries();
      router.refresh();
    }
  }

  if (options.length < 2) {
    const currentOrg = options.find((o) => o.id === current) ?? options[0];
    return (
      <span className="text-xs font-medium text-foreground px-2 py-1 truncate max-w-48">
        {currentOrg ? currentOrg.name : ""}
      </span>
    );
  }

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="sr-only">
        {t("label")}
      </label>
      <SimpleSelect
        id={id}
        value={current}
        disabled={pending}
        onValueChange={(next) => void switchTo(next)}
        options={options.map((option) => ({
          value: option.id,
          label: option.name,
        }))}
        className="text-xs"
      />
      {error ? (
        <p role="alert" className="text-xs text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}
