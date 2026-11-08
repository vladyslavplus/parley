"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { useId } from "react";

import { Select } from "@/components/ui/input";
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

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="sr-only">
        {t("label")}
      </label>
      <Select
        id={id}
        value={current}
        disabled={pending || options.length < 2}
        onChange={(event) => void switchTo(event.target.value)}
      >
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {option.name}
          </option>
        ))}
      </Select>
      {error ? (
        <p role="alert" className="text-sm text-red-800">
          {error}
        </p>
      ) : null}
    </div>
  );
}
