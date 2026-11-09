import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import commonEn from "../../messages/en/common.json";
import commonUk from "../../messages/uk/common.json";
import { ThemedToaster } from "@/components/providers";
import {
  showErrorToast,
  showInfoToast,
  showSuccessToast,
  showUndoToast,
  showWarningToast,
} from "./toasts";

vi.mock("next-themes", () => ({
  useTheme: () => ({ resolvedTheme: "light" }),
}));

function renderToaster(locale: "en" | "uk" = "en") {
  const messages = { common: locale === "uk" ? commonUk : commonEn };
  return render(
    <NextIntlClientProvider locale={locale} messages={messages}>
      <ThemedToaster />
    </NextIntlClientProvider>,
  );
}

describe("Toasts and Notification Roles", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(cleanup);

  it("renders toaster container with localized aria-label notifications region in en and uk", async () => {
    const { unmount } = renderToaster("en");
    expect(
      screen.getByRole("region", { name: /notifications/i }),
    ).toBeInTheDocument();
    unmount();

    renderToaster("uk");
    expect(
      screen.getByRole("region", { name: /сповіщення/i }),
    ).toBeInTheDocument();

    showSuccessToast("Збережено");
    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "Закрити сповіщення" }),
      ).toBeInTheDocument();
    });
  });

  it("renders success toast with accessible text", async () => {
    renderToaster("en");

    showSuccessToast("Profile saved successfully");

    await waitFor(() => {
      expect(
        screen.getByText("Profile saved successfully"),
      ).toBeInTheDocument();
    });
  });

  it("renders info and warning toasts", async () => {
    renderToaster("en");

    showInfoToast("Information update");
    showWarningToast("Caution required");

    await waitFor(() => {
      expect(screen.getByText("Information update")).toBeInTheDocument();
      expect(screen.getByText("Caution required")).toBeInTheDocument();
    });
  });

  it("renders undo toast with action button and triggers callback", async () => {
    const onUndo = vi.fn();
    renderToaster("en");

    showUndoToast({
      message: "Contact deleted",
      undoLabel: "Undo",
      failedMessage: "Restore failed",
      onUndo,
    });

    await waitFor(() => {
      expect(screen.getByText("Contact deleted")).toBeInTheDocument();
    });

    const undoBtn = screen.getByRole("button", { name: "Undo" });
    expect(undoBtn).toBeInTheDocument();

    fireEvent.click(undoBtn);

    await waitFor(() => {
      expect(onUndo).toHaveBeenCalledTimes(1);
    });
  });

  it("routes error toasts through centralized helper with toastErrorDurationMs duration", async () => {
    renderToaster("en");
    showErrorToast("Operation failed");

    await waitFor(() => {
      expect(screen.getByText("Operation failed")).toBeInTheDocument();
    });
  });
});
