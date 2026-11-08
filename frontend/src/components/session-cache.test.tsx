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

import { API_ROUTES } from "@/constants/routes";

const replace = vi.fn();
const refresh = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace, refresh }),
}));
vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

const bffFetch = vi.fn();
vi.mock("@/lib/api/bff-client", () => ({ bffFetch }));

vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({ data: undefined }),
  useMyOrganizations: () => ({ data: [] }),
  useAction: () => ({
    error: null,
    pending: false,
    run: async (action: () => Promise<void>) => {
      await action();
      return true;
    },
  }),
}));

const { AppHeader } = await import("./layout/app-header");
const { LoginForm } = await import("@/features/auth/components/login-form");
const { OrganizationSwitcher } =
  await import("@/features/organization/components/organization-switcher");

function setup(children: ReactNode) {
  const client = new QueryClient();
  client.setQueryData(["api", "me"], { user: { displayName: "User A" } });
  render(<QueryClientProvider client={client}>{children}</QueryClientProvider>);
  return client;
}

beforeEach(() => {
  bffFetch.mockReset();
  bffFetch.mockResolvedValue(new Response(null, { status: 204 }));
  replace.mockReset();
  refresh.mockReset();
});

afterEach(cleanup);

describe("session boundaries clear the query cache", () => {
  it("clears cached data on sign-out before navigating away", async () => {
    const client = setup(<AppHeader />);
    const clear = vi.spyOn(client, "clear");
    replace.mockImplementation(() => {
      expect(clear).toHaveBeenCalled();
    });

    fireEvent.click(screen.getByRole("button"));

    await waitFor(() => expect(replace).toHaveBeenCalled());
    expect(bffFetch).toHaveBeenCalledWith(
      API_ROUTES.logout,
      expect.objectContaining({ method: "POST" }),
    );
    expect(client.getQueryData(["api", "me"])).toBeUndefined();
  });

  it("clears cached data after a successful sign-in", async () => {
    const client = setup(<LoginForm returnUrl={null} />);

    fireEvent.change(screen.getByLabelText("email"), {
      target: { value: "b@example.com" },
    });
    fireEvent.change(screen.getByLabelText("password"), {
      target: { value: "Password-123" },
    });
    fireEvent.submit(screen.getByRole("button", { name: "submit" }));

    await waitFor(() => expect(replace).toHaveBeenCalled());
    expect(client.getQueryData(["api", "me"])).toBeUndefined();
  });

  it("resets cached data after an organization switch", async () => {
    const client = setup(
      <OrganizationSwitcher
        current="org-1"
        options={[
          { id: "org-1", name: "One", roleCode: "admin", isCurrent: true },
          { id: "org-2", name: "Two", roleCode: "admin", isCurrent: false },
        ]}
      />,
    );
    const reset = vi.spyOn(client, "resetQueries");

    fireEvent.change(screen.getByRole("combobox"), {
      target: { value: "org-2" },
    });

    await waitFor(() => expect(reset).toHaveBeenCalled());
    expect(refresh).toHaveBeenCalled();
  });
});
