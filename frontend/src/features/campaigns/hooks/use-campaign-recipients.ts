"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import {
  recipientsQuery,
  type RecipientFilterValues,
  type RecipientPaging,
} from "@/features/campaigns/recipients/recipients-query";
import type { CampaignRecipientPage } from "@/features/campaigns/types";
import { backendJson } from "@/lib/api/bff-client";

// One keyset page of recipients. The previous page stays on screen while a
// filter or the page changes, so the table does not flash.
export function useCampaignRecipients(
  campaignId: string,
  values: RecipientFilterValues,
  paging: RecipientPaging,
  enabled: boolean,
) {
  const locale = useLocale();
  const query = recipientsQuery(values, paging);
  return useQuery({
    queryKey: queryKeys.campaignRecipients(campaignId, query),
    queryFn: () =>
      backendJson<CampaignRecipientPage>(
        "/campaigns/{id}/recipients",
        { locale, query },
        `/campaigns/${encodeURIComponent(campaignId)}/recipients`,
      ),
    enabled,
    placeholderData: keepPreviousData,
  });
}
