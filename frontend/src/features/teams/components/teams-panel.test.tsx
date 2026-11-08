import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { NuqsTestingAdapter } from "nuqs/adapters/testing";
import { afterEach, describe, expect, it, vi } from "vitest";

const mockBackendVoid = vi.fn();
vi.mock("@/lib/api/bff-client", () => ({
  backendVoid: (...args: unknown[]) => mockBackendVoid(...args),
}));

const mockInvalidate = vi.fn();
vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string, params?: Record<string, unknown>) => {
    if (params?.name) return `${key} ${params.name}`;
    return key;
  },
}));

vi.mock("@/lib/toasts", () => ({
  showSuccessToast: vi.fn(),
  showErrorToast: vi.fn(),
}));

vi.mock("@/lib/api/hooks/use-api", () => ({
  useTeamsPage: () => ({
    isPending: false,
    isError: false,
    isFetching: false,
    data: {
      items: [{ id: "team-1", name: "Enterprise Sales" }],
      nextCursor: null,
    },
  }),
  useInvalidate: () => mockInvalidate,
}));

const { TeamsPanel } = await import("./teams-panel");

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("TeamsPanel", () => {
  it("opens Create modal, Edit modal (Save disabled while clean), and Delete ConfirmDialog, invalidating teams and members", async () => {
    mockBackendVoid.mockResolvedValue(undefined);
    mockInvalidate.mockResolvedValue(undefined);

    render(
      <NuqsTestingAdapter>
        <TeamsPanel />
      </NuqsTestingAdapter>,
    );

    expect(screen.getByText("Enterprise Sales")).toBeInTheDocument();

    // Open Edit modal via icon button
    fireEvent.click(
      screen.getByRole("button", { name: "editTeam Enterprise Sales" }),
    );
    const editDialog = screen.getByRole("dialog");
    const saveBtn = within(editDialog).getByRole("button", { name: "save" });
    expect(saveBtn).toBeDisabled();

    fireEvent.click(within(editDialog).getByRole("button", { name: "cancel" }));

    // Open Delete ConfirmDialog via icon button
    fireEvent.click(
      screen.getByRole("button", { name: "deleteTeam Enterprise Sales" }),
    );
    const alertDialog = screen.getByRole("alertdialog");
    fireEvent.click(
      within(alertDialog).getByRole("button", { name: "delete" }),
    );

    await waitFor(() => {
      expect(mockBackendVoid).toHaveBeenCalledWith(
        "/teams/{id}",
        expect.objectContaining({ method: "DELETE" }),
        "/teams/team-1",
      );
      expect(mockInvalidate).toHaveBeenCalledWith(["api", "teams"]);
      expect(mockInvalidate).toHaveBeenCalledWith(["api", "members"]);
    });
  });
});
