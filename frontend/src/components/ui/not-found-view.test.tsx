import {
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import commonEn from "../../../messages/en/common.json";
import errorsEn from "../../../messages/en/errors.json";
import errorsUk from "../../../messages/uk/errors.json";
import { ApiError } from "@/lib/errors";
import { NotFoundView } from "./not-found-view";

describe("NotFoundView", () => {
  const originalLocation = window.location;
  let mockAssign: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    mockAssign = vi.fn();
    Object.defineProperty(window, "location", {
      value: {
        ...originalLocation,
        assign: mockAssign,
      },
      writable: true,
      configurable: true,
    });
  });

  afterEach(() => {
    cleanup();
    Object.defineProperty(window, "location", {
      value: originalLocation,
      writable: true,
      configurable: true,
    });
  });

  it("renders full-page 404 for unauthenticated user in English without workspace link", () => {
    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, errors: errorsEn }}
      >
        <NotFoundView inApp={false} isAuthenticated={false} />
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("404")).toBeDefined();
    expect(screen.getByText("Page not found")).toBeDefined();
    expect(
      screen.getByText(
        "The page you are looking for does not exist or has been moved.",
      ),
    ).toBeDefined();

    // Primary action for unauthenticated is returnHome only
    const loginLink = screen.getByRole("link", { name: "Return to sign in" });
    expect(loginLink.getAttribute("href")).toBe("/en/login");

    // Signed out users must NEVER be offered a workspace link
    expect(screen.queryByRole("link", { name: /workspace/i })).toBeNull();
  });

  it("does not query /me or trigger QueryClient 401 redirect to login", () => {
    // Simulate QueryClient configured with onError redirecting to /login on 401
    const queryClient = new QueryClient({
      queryCache: new QueryCache({
        onError: (error) => {
          if (error instanceof ApiError && error.kind === "unauthorized") {
            // eslint-disable-next-line @next/next/no-location-assign-relative-destination
            window.location.assign("/en/login?returnUrl=/en/some-typo");
          }
        },
      }),
    });

    render(
      <QueryClientProvider client={queryClient}>
        <NextIntlClientProvider
          locale="en"
          messages={{ common: commonEn, errors: errorsEn }}
        >
          <NotFoundView inApp={false} isAuthenticated={false} />
        </NextIntlClientProvider>
      </QueryClientProvider>,
    );

    // Verify no redirect was initiated
    expect(mockAssign).not.toHaveBeenCalled();
    // Rendered 404 content
    expect(screen.getByText("Page not found")).toBeDefined();
  });

  it("renders full-page 404 for authenticated user in Ukrainian with workspace link", () => {
    render(
      <NextIntlClientProvider
        locale="uk"
        messages={{ common: commonEn, errors: errorsUk }}
      >
        <NotFoundView inApp={false} isAuthenticated={true} />
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("404")).toBeDefined();
    expect(screen.getByText("Сторінку не знайдено")).toBeDefined();
    expect(
      screen.getByText(
        "Сторінка, яку ви шукаєте, не існує або була переміщена.",
      ),
    ).toBeDefined();

    // Primary action for authenticated is returnApp
    const appLink = screen.getByRole("link", {
      name: /Перейти до робочого простору/i,
    });
    expect(appLink.getAttribute("href")).toBe("/uk/app/settings/profile");

    // Secondary action is returnHome
    const homeLink = screen.getByRole("link", {
      name: /Повернутися до входу/i,
    });
    expect(homeLink.getAttribute("href")).toBe("/uk/login");
  });

  it("renders in-app 404 inside shell section", () => {
    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ common: commonEn, errors: errorsEn }}
      >
        <NotFoundView inApp={true} />
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("Section not found")).toBeDefined();
    expect(
      screen.getByText(
        "This section does not exist or may have been relocated.",
      ),
    ).toBeDefined();

    const appLink = screen.getByRole("link", {
      name: /Go to workspace/i,
    });
    expect(appLink.getAttribute("href")).toBe("/en/app/settings/profile");
  });
});
