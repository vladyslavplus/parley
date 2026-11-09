import { renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { describe, expect, it, vi, beforeEach } from "vitest";

import { ERROR_CODES } from "@/constants/error-codes";
import { queryKeys } from "@/constants/query-keys";
import type { Deal } from "@/features/deals/types";
import { ApiError } from "@/lib/errors";
import { useDealMutations } from "./use-deal-mutations";

const mockToastError = vi.fn();

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/lib/toasts", () => ({
  showErrorToast: (...args: unknown[]) => mockToastError(...args),
  showSuccessToast: vi.fn(),
  showInfoToast: vi.fn(),
  showWarningToast: vi.fn(),
}));

const mockBackendJson = vi.fn();
const mockBackendVoid = vi.fn();

vi.mock("@/lib/api/bff-client", () => ({
  backendJson: (...args: unknown[]) => mockBackendJson(...args),
  backendVoid: (...args: unknown[]) => mockBackendVoid(...args),
}));

describe("useDealMutations", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  it("invalidates deal and board queries on moveDeal", async () => {
    const movedDeal = {
      id: "deal-1",
      stageId: "stage-2",
      position: 1,
      version: 2,
    };
    mockBackendJson.mockResolvedValueOnce(movedDeal);

    const listKey = queryKeys.dealLists();
    queryClient.setQueryData(listKey, { items: [], nextCursor: null });
    expect(queryClient.getQueryState(listKey)?.isInvalidated).toBe(false);

    const { result } = renderHook(() => useDealMutations(), { wrapper });

    await result.current.moveDeal({
      id: "deal-1",
      stageId: "stage-2",
      position: 1,
      version: 1,
    });

    expect(queryClient.getQueryState(listKey)?.isInvalidated).toBe(true);
  });

  it("handles 409 concurrency conflict on moveDeal with reload toast and cache invalidation", async () => {
    const conflictError = new ApiError({
      kind: "conflict",
      status: 409,
      code: ERROR_CODES.concurrencyConflict,
      detail: "Concurrent move conflict",
      traceId: null,
      fieldErrors: {},
    });
    mockBackendJson.mockRejectedValueOnce(conflictError);

    const listKey = queryKeys.dealLists();
    queryClient.setQueryData(listKey, { items: [], nextCursor: null });

    const { result } = renderHook(() => useDealMutations(), { wrapper });

    await expect(
      result.current.moveDeal({
        id: "deal-1",
        stageId: "stage-2",
        position: 1,
        version: 1,
      }),
    ).rejects.toThrow();

    expect(mockToastError).toHaveBeenCalledWith(
      "conflict.title",
      expect.objectContaining({
        description: "conflict.description",
        action: expect.objectContaining({
          label: "conflict.reload",
        }),
      }),
    );
    expect(queryClient.getQueryState(listKey)?.isInvalidated).toBe(true);
  });

  it("updates deal preserving cached fields when partial body is passed", async () => {
    const cachedDeal = {
      id: "deal-1",
      title: "Enterprise Deal",
      amount: 15000,
      currency: "EUR",
      version: 4,
      contactId: "contact-1",
      companyId: "company-1",
      ownerUserId: "user-1",
      expectedCloseOn: "2026-12-31",
      customFields: { priority: "high" },
    };
    queryClient.setQueryData(queryKeys.deal("deal-1"), cachedDeal);
    queryClient.setQueryData(queryKeys.activitiesAll(), []);

    const updatedDeal = { ...cachedDeal, title: "Renamed Deal", version: 5 };
    mockBackendJson.mockResolvedValueOnce(updatedDeal);

    const { result } = renderHook(() => useDealMutations(), { wrapper });

    // Pass the current deal directly as argument
    await result.current.updateDeal(cachedDeal as unknown as Deal, {
      title: "Renamed Deal",
    });

    expect(mockBackendJson).toHaveBeenCalledWith(
      "/deals/{id}",
      {
        method: "PUT",
        locale: "en",
        body: {
          version: 4,
          title: "Renamed Deal",
          amount: 15000,
          currency: "EUR",
          contactId: "contact-1",
          companyId: "company-1",
          ownerUserId: "user-1",
          expectedCloseOn: "2026-12-31",
          customFields: { priority: "high" },
        },
      },
      "/deals/deal-1",
    );

    expect(
      queryClient.getQueryState(queryKeys.activitiesAll())?.isInvalidated,
    ).toBe(true);
  });

  it("creates deal and invalidates deal and pipeline queries", async () => {
    const createdDeal = {
      id: "deal-new",
      title: "New Deal",
      amount: 5000,
      currency: "USD",
      version: 1,
    };
    mockBackendJson.mockResolvedValueOnce(createdDeal);

    const allDealsKey = queryKeys.dealsAll();
    queryClient.setQueryData(allDealsKey, []);

    const { result } = renderHook(() => useDealMutations(), { wrapper });

    const res = await result.current.createDeal({
      title: "New Deal",
      amount: 5000,
      currency: "USD",
    });

    expect(res).toEqual(createdDeal);
    expect(queryClient.getQueryState(allDealsKey)?.isInvalidated).toBe(true);
  });

  it("deletes deal and invalidates all deal and pipeline caches", async () => {
    mockBackendVoid.mockResolvedValueOnce(undefined);

    const dealKey = queryKeys.deal("deal-del");
    queryClient.setQueryData(dealKey, { id: "deal-del" });

    const { result } = renderHook(() => useDealMutations(), { wrapper });

    await result.current.deleteDeal("deal-del");

    expect(mockBackendVoid).toHaveBeenCalledWith(
      "/deals/{id}",
      {
        method: "DELETE",
        locale: "en",
      },
      "/deals/deal-del",
    );
    expect(queryClient.getQueryState(dealKey)?.isInvalidated).toBe(true);
  });

  it("restores deal and invalidates caches", async () => {
    const restoredDeal = {
      id: "deal-restore",
      title: "Restored Deal",
      amount: 1000,
      version: 2,
    };
    mockBackendJson.mockResolvedValueOnce(restoredDeal);

    const dealKey = queryKeys.deal("deal-restore");
    const dealListsKey = queryKeys.dealLists();
    queryClient.setQueryData(dealKey, { id: "deal-restore" });
    queryClient.setQueryData(dealListsKey, []);

    const { result } = renderHook(() => useDealMutations(), { wrapper });

    const res = await result.current.restoreDeal("deal-restore");

    expect(mockBackendJson).toHaveBeenCalledWith(
      "/deals/{id}/restore",
      {
        method: "POST",
        locale: "en",
      },
      "/deals/deal-restore/restore",
    );
    expect(res).toEqual(restoredDeal);
    expect(queryClient.getQueryState(dealKey)?.isInvalidated).toBe(true);
    expect(queryClient.getQueryState(dealListsKey)?.isInvalidated).toBe(true);
  });
});
