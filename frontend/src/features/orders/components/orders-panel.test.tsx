import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { NuqsTestingAdapter } from "nuqs/adapters/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import crmEn from "../../../../messages/en/crm.json";
import tableEn from "../../../../messages/en/table.json";

const mockBackendJson = vi.fn();
vi.mock("@/lib/api/bff-client", () => ({
  backendJson: (...args: unknown[]) => mockBackendJson(...args),
}));

let mockTimeZone = "UTC";
vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({ data: { user: { timeZone: mockTimeZone } } }),
}));

const { OrdersPanel } = await import("./orders-panel");

const PAGE = {
  items: [
    {
      id: "o1",
      contactId: "c1",
      externalRef: "INV-1001",
      orderedAt: "2026-03-05T10:00:00Z",
      totalAmount: 1234.5,
      currency: "USD",
      lineCount: 3,
      isCancellation: false,
      source: "demo-seed",
    },
    {
      id: "o2",
      contactId: "c1",
      externalRef: "C-1002",
      orderedAt: "2026-03-06T10:00:00Z",
      totalAmount: -80,
      currency: "USD",
      lineCount: 1,
      isCancellation: true,
      source: "demo-seed",
    },
  ],
  nextCursor: "cursor-2",
};

function renderPanel(
  client: QueryClient,
  onUrlUpdate?: (update: { queryString: string }) => void,
  searchParams?: string,
) {
  return render(
    <NuqsTestingAdapter onUrlUpdate={onUrlUpdate} searchParams={searchParams}>
      <QueryClientProvider client={client}>
        <NextIntlClientProvider
          locale="en"
          messages={{ crm: crmEn, table: tableEn }}
          timeZone="UTC"
        >
          <OrdersPanel contactId="c1" />
        </NextIntlClientProvider>
      </QueryClientProvider>
    </NuqsTestingAdapter>,
  );
}

function newClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } });
}

beforeEach(() => {
  mockBackendJson.mockReset();
  mockBackendJson.mockResolvedValue(PAGE);
  mockTimeZone = "UTC";
});

afterEach(cleanup);

describe("orders panel", () => {
  it("lists orders with localized money and flags cancellations", async () => {
    renderPanel(newClient());

    expect(await screen.findByText("INV-1001")).toBeInTheDocument();
    expect(screen.getByText("$1,234.50")).toBeInTheDocument();
    expect(screen.getByText("Cancellation")).toBeInTheDocument();
    expect(mockBackendJson).toHaveBeenCalledWith(
      "/orders",
      expect.objectContaining({
        query: expect.objectContaining({
          contactId: "c1",
          sort: "ordered",
          order: "desc",
        }),
      }),
    );
  });

  it("keeps sort and the keyset cursor in the URL", async () => {
    const updates: string[] = [];
    renderPanel(newClient(), ({ queryString }) => updates.push(queryString));
    await screen.findByText("INV-1001");

    fireEvent.click(screen.getByRole("button", { name: /total/i }));
    await waitFor(() =>
      expect(updates.some((q) => q.includes("sort=amount"))).toBe(true),
    );

    fireEvent.click(screen.getByRole("button", { name: /next/i }));
    await waitFor(() =>
      expect(updates.some((q) => q.includes("after=cursor-2"))).toBe(true),
    );
  });

  it("shows the empty state when the contact has no orders", async () => {
    mockBackendJson.mockResolvedValue({ items: [], nextCursor: null });
    renderPanel(newClient());

    expect(await screen.findByText("No orders yet")).toBeInTheDocument();
  });

  it("does not show a skeleton again when revisiting with cached data", async () => {
    const client = newClient();
    const first = renderPanel(client);
    await screen.findByText("INV-1001");
    first.unmount();

    const second = renderPanel(client);
    expect(screen.getByText("INV-1001")).toBeInTheDocument();
    expect(second.container.querySelector(".animate-pulse")).toBeNull();
  });

  it("keeps cents-level rounding for amounts with more than two decimals", async () => {
    mockBackendJson.mockResolvedValue({
      items: [
        { ...PAGE.items[0], totalAmount: 1234.5678 },
        { ...PAGE.items[1], totalAmount: "-0.1234" },
      ],
      nextCursor: null,
    });
    renderPanel(newClient());

    expect(await screen.findByText("$1,234.57")).toBeInTheDocument();
    expect(screen.getByText("-$0.12")).toBeInTheDocument();
  });

  it("sends the picked days as UTC instants of the display time zone", async () => {
    mockTimeZone = "Europe/Kyiv";
    renderPanel(
      newClient(),
      undefined,
      "?orderedFrom=2026-03-06&orderedTo=2026-03-06",
    );
    await screen.findByText("INV-1001");

    // An order at 22:30 UTC on 5 March is 6 March in Kyiv: the 6th finds it.
    expect(mockBackendJson).toHaveBeenCalledWith(
      "/orders",
      expect.objectContaining({
        query: expect.objectContaining({
          orderedFrom: "2026-03-05T22:00:00.000Z",
          orderedTo: "2026-03-06T22:00:00.000Z",
        }),
      }),
    );
  });

  it("covers the 23-hour spring-forward day", async () => {
    mockTimeZone = "Europe/Kyiv";
    renderPanel(
      newClient(),
      undefined,
      "?orderedFrom=2026-03-29&orderedTo=2026-03-29",
    );
    await screen.findByText("INV-1001");

    expect(mockBackendJson).toHaveBeenCalledWith(
      "/orders",
      expect.objectContaining({
        query: expect.objectContaining({
          orderedFrom: "2026-03-28T22:00:00.000Z",
          orderedTo: "2026-03-29T21:00:00.000Z",
        }),
      }),
    );
  });

  it("does not query when the start day is after the end day", async () => {
    renderPanel(
      newClient(),
      undefined,
      "?orderedFrom=2026-04-02&orderedTo=2026-04-01",
    );

    expect(
      await screen.findByText(crmEn.orders.invalidDateRange),
    ).toBeInTheDocument();
    expect(mockBackendJson).not.toHaveBeenCalled();
  });
});
