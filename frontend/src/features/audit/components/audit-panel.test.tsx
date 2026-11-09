import { cleanup, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { NuqsTestingAdapter } from "nuqs/adapters/testing";
import { afterEach, describe, expect, it, vi } from "vitest";

import crmEn from "../../../../messages/en/crm.json";
import settingsEn from "../../../../messages/en/settings.json";
import tableEn from "../../../../messages/en/table.json";
import { displayTimeZone } from "@/lib/time-zones";

const OCCURRED_AT = "2026-06-01T22:30:00Z";

vi.mock("@/features/crm/hooks/use-owners", () => ({
  useOwners: () => ({ data: { items: [] }, isFetching: false }),
}));

let me: unknown;
vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({ data: me }),
  useAuditEventsPage: () => ({
    isPending: false,
    isError: false,
    isFetching: false,
    data: {
      items: [
        {
          id: "e1",
          action: "member.role_changed",
          targetType: null,
          actorUserId: null,
          occurredAt: OCCURRED_AT,
        },
      ],
      nextCursor: null,
      hasMore: false,
    },
  }),
}));

const { AuditPanel } = await import("./audit-panel");

afterEach(cleanup);

function renderPanel() {
  render(
    <NuqsTestingAdapter>
      <NextIntlClientProvider
        locale="en"
        messages={{ settings: settingsEn, table: tableEn, crm: crmEn }}
        timeZone="Pacific/Auckland"
      >
        <AuditPanel />
      </NextIntlClientProvider>
    </NuqsTestingAdapter>,
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
