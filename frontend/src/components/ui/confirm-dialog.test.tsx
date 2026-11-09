import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import commonEn from "../../../messages/en/common.json";
import errorsEn from "../../../messages/en/errors.json";
import formsEn from "../../../messages/en/forms.json";
import { ApiError } from "@/lib/errors";
import { ConfirmDialog } from "./confirm-dialog";

afterEach(cleanup);

describe("ConfirmDialog", () => {
  it("focuses the Cancel button on open and does NOT close on Escape or outside click", async () => {
    const onOpenChange = vi.fn();
    const onConfirm = vi.fn();

    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn, errors: errorsEn }}
      >
        <ConfirmDialog
          open
          onOpenChange={onOpenChange}
          title="Delete record"
          description="This record will be permanently removed."
          confirmLabel="Delete"
          cancelLabel="Cancel"
          onConfirm={onConfirm}
          destructive
        />
      </NextIntlClientProvider>,
    );

    const cancelBtn = screen.getByRole("button", { name: "Cancel" });
    await waitFor(() => {
      expect(document.activeElement).toBe(cancelBtn);
    });

    fireEvent.keyDown(document.activeElement ?? document.body, {
      key: "Escape",
      code: "Escape",
    });
    expect(onOpenChange).not.toHaveBeenCalled();

    fireEvent.click(cancelBtn);
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it("executes async onConfirm and closes on success", async () => {
    const onOpenChange = vi.fn();
    const onConfirm = vi.fn().mockResolvedValue(undefined);

    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn, errors: errorsEn }}
      >
        <ConfirmDialog
          open
          onOpenChange={onOpenChange}
          title="Deactivate member"
          description="They will lose access immediately."
          confirmLabel="Deactivate"
          cancelLabel="Cancel"
          onConfirm={onConfirm}
          destructive
        />
      </NextIntlClientProvider>,
    );

    fireEvent.click(screen.getByRole("button", { name: "Deactivate" }));

    await waitFor(() => {
      expect(onConfirm).toHaveBeenCalledTimes(1);
      expect(onOpenChange).toHaveBeenCalledWith(false);
    });
  });

  it("maps error on failure using localized error messages", async () => {
    const handleConfirm = vi.fn().mockRejectedValue(
      new ApiError({
        status: 403,
        code: "forbidden",
        detail: null,
        kind: "forbidden",
        traceId: null,
        fieldErrors: {},
      }),
    );

    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn, errors: errorsEn }}
      >
        <ConfirmDialog
          open
          onOpenChange={() => {}}
          title="Delete item?"
          description="Confirm action"
          onConfirm={handleConfirm}
        />
      </NextIntlClientProvider>,
    );

    fireEvent.click(screen.getByRole("button", { name: "Confirm" }));

    expect(await screen.findByText(errorsEn.kinds.forbidden)).toBeDefined();
  });
});
