import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { RequirePermission } from "@/features/session/components/require-permission";

const replace = vi.fn();
let status: "pending" | "allowed" | "denied" = "denied";

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));
vi.mock("@/i18n/navigation", () => ({ useRouter: () => ({ replace }) }));
vi.mock("@/features/session/components/can", () => ({
  useCanStatus: () => status,
}));

describe("RequirePermission", () => {
  afterEach(() => {
    cleanup();
    replace.mockClear();
  });

  it("redirects a user without the permission and never renders the screen", () => {
    status = "denied";
    render(
      <RequirePermission
        permission="campaigns.manage"
        redirectTo="/app/campaigns"
      >
        <p>builder</p>
      </RequirePermission>,
    );
    expect(replace).toHaveBeenCalledWith("/app/campaigns");
    expect(screen.queryByText("builder")).toBeNull();
  });

  it("renders the screen for an allowed user and waits while unknown", () => {
    status = "pending";
    const { rerender } = render(
      <RequirePermission permission="campaigns.manage" redirectTo="/x">
        <p>builder</p>
      </RequirePermission>,
    );
    expect(replace).not.toHaveBeenCalled();
    expect(screen.queryByText("builder")).toBeNull();

    status = "allowed";
    rerender(
      <RequirePermission permission="campaigns.manage" redirectTo="/x">
        <p>builder</p>
      </RequirePermission>,
    );
    expect(screen.getByText("builder")).toBeInTheDocument();
  });
});
