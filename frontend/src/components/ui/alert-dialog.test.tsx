import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import commonEn from "../../../messages/en/common.json";
import errorsEn from "../../../messages/en/errors.json";
import formsEn from "../../../messages/en/forms.json";
import { ApiError } from "@/lib/errors";
import { AlertDialog } from "./alert-dialog";

describe("AlertDialog", () => {
  afterEach(cleanup);

  it("renders title, description and action buttons", () => {
    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn }}
      >
        <AlertDialog
          open={true}
          onOpenChange={() => {}}
          title="Delete selected item?"
          description="This action cannot be undone."
          onConfirm={() => {}}
        />
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("Delete selected item?")).toBeDefined();
    expect(screen.getByText("This action cannot be undone.")).toBeDefined();
    expect(screen.getByRole("button", { name: "Confirm" })).toBeDefined();
    expect(screen.getByRole("button", { name: "Cancel" })).toBeDefined();
  });

  it("calls onConfirm when clicking confirm button", async () => {
    const handleConfirm = vi.fn();
    const handleOpenChange = vi.fn();

    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn }}
      >
        <AlertDialog
          open={true}
          onOpenChange={handleOpenChange}
          title="Delete record?"
          description="Confirm deletion."
          onConfirm={handleConfirm}
        />
      </NextIntlClientProvider>,
    );

    const confirmBtn = screen.getByRole("button", { name: "Confirm" });
    fireEvent.click(confirmBtn);

    expect(handleConfirm).toHaveBeenCalledTimes(1);
  });

  it("calls onOpenChange(false) when cancel button is clicked", () => {
    const handleOpenChange = vi.fn();

    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, forms: formsEn }}
      >
        <AlertDialog
          open={true}
          onOpenChange={handleOpenChange}
          title="Cancel check"
          description="Check cancel"
          onConfirm={() => {}}
        />
      </NextIntlClientProvider>,
    );

    const cancelBtn = screen.getByRole("button", { name: "Cancel" });
    fireEvent.click(cancelBtn);

    expect(handleOpenChange).toHaveBeenCalledWith(false);
  });

  it("maps error on failure using error kind messages", async () => {
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
        <AlertDialog
          open={true}
          onOpenChange={() => {}}
          title="Delete item?"
          description="Confirm action"
          onConfirm={handleConfirm}
        />
      </NextIntlClientProvider>,
    );

    const confirmBtn = screen.getByRole("button", { name: "Confirm" });
    fireEvent.click(confirmBtn);

    expect(await screen.findByText(errorsEn.kinds.forbidden)).toBeDefined();
  });
});
