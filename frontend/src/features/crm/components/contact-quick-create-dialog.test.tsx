import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { ERROR_CODES } from "@/constants/error-codes";
import { ApiError } from "@/lib/errors";
import { ContactQuickCreateDialog } from "./contact-quick-create-dialog";

const mockCreateContact = vi.fn();

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({
    data: { user: { id: "u-me" } },
  }),
}));

vi.mock("@/features/crm/hooks/use-companies", () => ({
  useCompanies: () => ({
    data: { items: [] },
  }),
}));

vi.mock("@/features/crm/hooks/use-crm-mutations", () => ({
  useCrmMutations: () => ({
    createContact: mockCreateContact,
  }),
}));

vi.mock("@/lib/toasts", () => ({
  showSuccessToast: vi.fn(),
  showErrorToast: vi.fn(),
  showInfoToast: vi.fn(),
  showWarningToast: vi.fn(),
  useUndoToast: () => vi.fn(),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("ContactQuickCreateDialog", () => {
  it("maps 409 email_taken error onto the email field", async () => {
    const emailConflictError = new ApiError({
      kind: "conflict",
      status: 409,
      code: ERROR_CODES.contactEmailTaken,
      detail: "This email address is already in use",
      traceId: null,
      fieldErrors: {},
    });

    mockCreateContact.mockRejectedValueOnce(emailConflictError);

    render(
      <ContactQuickCreateDialog
        open={true}
        onOpenChange={vi.fn()}
        onCreated={vi.fn()}
      />,
    );

    const firstNameInput = screen.getByLabelText(/firstName/i);
    const emailInput = screen.getByLabelText(/email/i);

    fireEvent.change(firstNameInput, { target: { value: "John" } });
    fireEvent.change(emailInput, { target: { value: "john@example.com" } });

    fireEvent.click(screen.getByRole("button", { name: "submit" }));

    await waitFor(() => {
      expect(
        screen.getByText("This email address is already in use"),
      ).toBeInTheDocument();
    });

    // It should NOT show a form-level error alert, but the field error
    expect(screen.queryByRole("alert")).toBeNull();
  });
});
