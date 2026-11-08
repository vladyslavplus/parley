import { cleanup, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import { displayTimeZone } from "@/lib/time-zones";

const OCCURRED_AT = "2026-06-01T22:30:00Z";

let me: unknown;
vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({ data: me }),
  useAuditEvents: () => ({
    isPending: false,
    isError: false,
    hasNextPage: false,
    data: {
      pages: [
        {
          items: [
            {
              id: "e1",
              action: "member.role_changed",
              targetType: null,
              actorUserId: null,
              occurredAt: OCCURRED_AT,
            },
          ],
        },
      ],
    },
  }),
}));

const { AuditPanel } = await import("./audit-panel");

afterEach(cleanup);

function renderPanel() {
  render(
    <NextIntlClientProvider
      locale="en"
      messages={{}}
      timeZone="Pacific/Auckland"
    >
      <AuditPanel />
    </NextIntlClientProvider>,
  );
}

describe("audit timestamps", () => {
  it("shows the signed-in user's time zone, not the runtime zone", () => {
    me = {
      user: { timeZone: "America/New_York" },
      organization: { timeZone: "Asia/Tokyo" },
    };
    renderPanel();
    // 22:30 UTC is 18:30 in New York (summer time).
    expect(screen.getByText(/6:30:00\sPM/)).toBeInTheDocument();
  });
});

describe("displayTimeZone", () => {
  it("falls back from the user to the organization to UTC", () => {
    expect(
      displayTimeZone({
        user: { timeZone: "" },
        organization: { timeZone: "Asia/Tokyo" },
      }),
    ).toBe("Asia/Tokyo");
    expect(displayTimeZone(undefined)).toBe("UTC");
  });
});
