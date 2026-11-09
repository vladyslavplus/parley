import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { queryKeys } from "@/constants/query-keys";

const mockBackendVoid = vi.fn();
vi.mock("@/lib/api/bff-client", () => ({
  backendVoid: (...args: unknown[]) => mockBackendVoid(...args),
}));

const mockToastSuccess = vi.fn();
vi.mock("@/lib/toasts", () => ({
  showSuccessToast: (...args: unknown[]) => mockToastSuccess(...args),
  showErrorToast: vi.fn(),
  showInfoToast: vi.fn(),
  showWarningToast: vi.fn(),
  useUndoToast: () => vi.fn(),
}));

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

vi.mock("next-themes", () => ({
  useTheme: () => ({ theme: "light", setTheme: vi.fn() }),
}));

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => "/app/settings/profile",
  useRouter: () => ({ replace: vi.fn() }),
}));

const mockProfile = {
  id: "user-1",
  email: "alex@example.com",
  displayName: "Alex",
  locale: "en",
  timeZone: "UTC",
  theme: "light",
};

vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({
    data: { user: mockProfile },
    isPending: false,
    isError: false,
  }),
}));

const { ProfileForm } = await import("./profile-form");

function renderWithClient(ui: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return {
    queryClient,
    ...render(
      <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>,
    ),
  };
}

describe("ProfileForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(cleanup);

  it("disables save button when form is not dirty, enables on change, and invalidates dependent query keys after save", async () => {
    mockBackendVoid.mockResolvedValueOnce(undefined);
    const { queryClient } = renderWithClient(<ProfileForm />);
    const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");

    const submitBtn = screen.getByRole("button", { name: "submit" });
    expect(submitBtn).toBeDisabled();

    const nameInput = screen.getByLabelText("displayName");
    expect(nameInput).toHaveValue("Alex");

    fireEvent.change(nameInput, { target: { value: "Alexander" } });
    expect(submitBtn).toBeEnabled();

    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mockBackendVoid).toHaveBeenCalledTimes(1);
    });

    await waitFor(() => {
      expect(mockToastSuccess).toHaveBeenCalledWith("saved");
      expect(submitBtn).toBeDisabled();
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: queryKeys.me(),
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: queryKeys.members(),
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: queryKeys.owners(),
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: queryKeys.contactsAll(),
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: queryKeys.companiesAll(),
      });
    });
  });
});
