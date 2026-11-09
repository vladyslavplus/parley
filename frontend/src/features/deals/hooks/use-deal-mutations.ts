"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";

import { ERROR_CODES } from "@/constants/error-codes";
import { queryKeys } from "@/constants/query-keys";
import type { Deal } from "@/features/deals/types";
import { backendJson, backendVoid } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import { describeError, toApiError } from "@/lib/errors";
import { showErrorToast } from "@/lib/toasts";

export interface CreateDealInput {
  title: string;
  amount: number | string;
  currency: string;
  stageId?: string | null;
  companyId?: string | null;
  contactId?: string | null;
  ownerUserId?: string | null;
  expectedCloseOn?: string | null;
  customFields?: Record<string, unknown> | null;
}

export interface UpdateDealInput {
  version?: number | string;
  title?: string | null;
  amount?: number | string | null;
  currency?: string | null;
  contactId?: string | null;
  companyId?: string | null;
  ownerUserId?: string | null;
  expectedCloseOn?: string | null;
  customFields?: Record<string, unknown> | null;
}

export interface MoveDealInput {
  id: string;
  stageId: string;
  position: number;
  version: number | string;
  lostReason?: string | null;
}

export function useDealMutations() {
  const locale = useLocale();
  const queryClient = useQueryClient();
  const tCrm = useTranslations("crm");
  const tErrors = useTranslations("errors.kinds");

  function handleConflict(entityId: string, queryKey: readonly unknown[]) {
    showErrorToast(tCrm("conflict.title"), {
      description: tCrm("conflict.description"),
      action: {
        label: tCrm("conflict.reload"),
        onClick: () => {
          void queryClient.invalidateQueries({ queryKey });
        },
      },
    });
    void queryClient.invalidateQueries({ queryKey });
  }

  async function createDeal(input: CreateDealInput): Promise<Deal> {
    const body: Schemas["DealRequest"] = {
      title: input.title,
      amount: input.amount,
      currency: input.currency,
      stageId: input.stageId ?? null,
      companyId: input.companyId ?? null,
      contactId: input.contactId ?? null,
      ownerUserId: input.ownerUserId ?? null,
      expectedCloseOn: input.expectedCloseOn ?? null,
      customFields: (input.customFields as Record<string, never>) ?? null,
    };

    const created = await backendJson<Deal>("/deals", {
      method: "POST",
      body,
      locale,
    });

    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.dealsAll() }),
      queryClient.invalidateQueries({ queryKey: queryKeys.pipelinesAll() }),
      queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
    ]);

    return created;
  }

  async function updateDeal(
    currentDeal: Deal,
    input: UpdateDealInput,
  ): Promise<Deal> {
    const currentAmount =
      typeof currentDeal.amount === "number"
        ? currentDeal.amount
        : currentDeal.amount
          ? parseFloat(currentDeal.amount)
          : 0;

    const parsedAmount =
      typeof input.amount === "string"
        ? parseFloat(input.amount)
        : typeof input.amount === "number"
          ? input.amount
          : currentAmount;

    const body: Schemas["UpdateDealRequest"] = {
      version: input.version ?? currentDeal.version,
      title: input.title !== undefined ? input.title : currentDeal.title,
      amount: input.amount !== undefined ? parsedAmount : currentAmount,
      currency:
        input.currency !== undefined ? input.currency : currentDeal.currency,
      contactId:
        input.contactId !== undefined ? input.contactId : currentDeal.contactId,
      companyId:
        input.companyId !== undefined ? input.companyId : currentDeal.companyId,
      ownerUserId:
        input.ownerUserId !== undefined
          ? input.ownerUserId
          : currentDeal.ownerUserId,
      expectedCloseOn:
        input.expectedCloseOn !== undefined
          ? input.expectedCloseOn
          : currentDeal.expectedCloseOn,
      customFields:
        input.customFields !== undefined
          ? (input.customFields as Record<string, never>)
          : ((currentDeal.customFields as Record<string, never>) ?? null),
    };

    try {
      const updated = await backendJson<Deal>(
        "/deals/{id}",
        {
          method: "PUT",
          body,
          locale,
        },
        `/deals/${encodeURIComponent(currentDeal.id)}`,
      );

      queryClient.setQueryData(queryKeys.deal(currentDeal.id), updated);

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.deal(currentDeal.id),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.dealLists() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.pipelinesAll() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
      ]);

      return updated;
    } catch (err) {
      const apiErr = toApiError(err);
      if (
        apiErr.status === 409 ||
        apiErr.code === ERROR_CODES.concurrencyConflict
      ) {
        handleConflict(currentDeal.id, queryKeys.deal(currentDeal.id));
      }
      throw apiErr;
    }
  }

  async function moveDeal(input: MoveDealInput): Promise<Deal> {
    const body: Schemas["MoveDealRequest"] = {
      stageId: input.stageId,
      position: input.position,
      version: input.version,
      lostReason: input.lostReason ?? null,
    };

    try {
      const moved = await backendJson<Deal>(
        "/deals/{id}/move",
        {
          method: "POST",
          body,
          locale,
        },
        `/deals/${encodeURIComponent(input.id)}/move`,
      );

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.deal(input.id) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.dealLists() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.pipelinesAll() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
      ]);

      return moved;
    } catch (err) {
      const apiErr = toApiError(err);
      if (
        apiErr.status === 409 ||
        apiErr.code === ERROR_CODES.concurrencyConflict
      ) {
        handleConflict(input.id, queryKeys.dealLists());
      } else {
        showErrorToast(
          describeError(apiErr, (kind) =>
            tErrors(kind as Parameters<typeof tErrors>[0]),
          ),
        );
      }
      throw apiErr;
    }
  }

  async function deleteDeal(id: string): Promise<void> {
    await backendVoid(
      "/deals/{id}",
      {
        method: "DELETE",
        locale,
      },
      `/deals/${encodeURIComponent(id)}`,
    );

    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.deal(id) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.dealLists() }),
      queryClient.invalidateQueries({ queryKey: queryKeys.pipelinesAll() }),
      queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
    ]);
  }

  async function restoreDeal(id: string): Promise<Deal> {
    const restored = await backendJson<Deal>(
      "/deals/{id}/restore",
      {
        method: "POST",
        locale,
      },
      `/deals/${encodeURIComponent(id)}/restore`,
    );

    await Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.deal(id) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.dealLists() }),
      queryClient.invalidateQueries({ queryKey: queryKeys.pipelinesAll() }),
      queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
    ]);

    return restored;
  }

  return {
    createDeal,
    updateDeal,
    moveDeal,
    deleteDeal,
    restoreDeal,
  };
}
