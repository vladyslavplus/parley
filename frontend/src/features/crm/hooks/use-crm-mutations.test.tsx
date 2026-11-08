import { renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { ERROR_CODES } from "@/constants/error-codes";
import { queryKeys } from "@/constants/query-keys";
import { ApiError } from "@/lib/errors";
import { useCrmMutations } from "./use-crm-mutations";

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
  useUndoToast: () => vi.fn(),
}));

const mockBackendJson = vi.fn();
const mockBackendVoid = vi.fn();

vi.mock("@/lib/api/bff-client", () => ({
  backendJson: (...args: unknown[]) => mockBackendJson(...args),
  backendVoid: (...args: unknown[]) => mockBackendVoid(...args),
}));

describe("useCrmMutations", () => {
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

  it("invalidates filtered list queries on bulkDeleteContacts using a real QueryClient", async () => {
    mockBackendVoid.mockResolvedValueOnce(undefined);

    const listKey = queryKeys.contactList({ q: "a" });
    queryClient.setQueryData(listKey, { items: [], nextCursor: null });
    expect(queryClient.getQueryState(listKey)?.isInvalidated).toBe(false);

    const { result } = renderHook(() => useCrmMutations(), { wrapper });

    await result.current.bulkDeleteContacts(["c-1", "c-2"]);

    expect(queryClient.getQueryState(listKey)?.isInvalidated).toBe(true);
  });

  it("updates contact successfully, updates query cache and invalidates lists", async () => {
    const updatedContact = {
      id: "c-1",
      firstName: "Jane",
      version: 2,
    };
    mockBackendJson.mockResolvedValueOnce(updatedContact);

    const listKey = queryKeys.contactList();
    queryClient.setQueryData(listKey, { items: [], nextCursor: null });

    const { result } = renderHook(() => useCrmMutations(), { wrapper });

    const res = await result.current.updateContact("c-1", {
      version: 1,
      firstName: "Jane",
    });

    expect(res).toEqual(updatedContact);
    expect(queryClient.getQueryData(queryKeys.contact("c-1"))).toEqual(
      updatedContact,
    );
    expect(queryClient.getQueryState(listKey)?.isInvalidated).toBe(true);
  });

  it("handles 409 concurrency conflict by showing reload toast and invalidating cache", async () => {
    const conflictError = new ApiError({
      kind: "conflict",
      status: 409,
      code: ERROR_CODES.concurrencyConflict,
      detail: "Conflict",
      traceId: null,
      fieldErrors: {},
    });
    mockBackendJson.mockRejectedValueOnce(conflictError);

    const { result } = renderHook(() => useCrmMutations(), { wrapper });

    await expect(
      result.current.updateContact("c-1", {
        version: 1,
        firstName: "Stale",
      }),
    ).rejects.toEqual(conflictError);

    expect(mockToastError).toHaveBeenCalledWith(
      "conflict.title",
      expect.objectContaining({
        description: "conflict.description",
        action: expect.objectContaining({
          label: "conflict.reload",
        }),
      }),
    );
  });

  it("does not show reload toast for 409 email_taken error", async () => {
    const emailTakenError = new ApiError({
      kind: "conflict",
      status: 409,
      code: ERROR_CODES.contactEmailTaken,
      detail: "Email is already taken",
      traceId: null,
      fieldErrors: {},
    });
    mockBackendJson.mockRejectedValueOnce(emailTakenError);

    const { result } = renderHook(() => useCrmMutations(), { wrapper });

    await expect(
      result.current.updateContact("c-1", {
        version: 1,
        email: "taken@example.com",
      }),
    ).rejects.toEqual(emailTakenError);

    expect(mockToastError).not.toHaveBeenCalledWith(
      "conflict.title",
      expect.anything(),
    );
  });
});
