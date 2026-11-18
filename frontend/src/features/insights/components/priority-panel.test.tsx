import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { NuqsTestingAdapter } from "nuqs/adapters/testing";
import { afterEach, describe, expect, it, vi } from "vitest";

import crmEn from "../../../../messages/en/crm.json";
import hintsEn from "../../../../messages/en/hints.json";
import insightsEn from "../../../../messages/en/insights.json";
import tableEn from "../../../../messages/en/table.json";

const state = vi.hoisted(() => ({
  mode: "ml",
  canViewNumbers: true,
  canWriteTasks: true,
  action: "action.call",
  dueTasks: [
    {
      id: "t-1",
      title: "Check invoice",
      type: "call",
      dueAt: "2020-01-01T10:00:00Z",
      isCompleted: false,
      version: 1,
      assigneeUserId: "u-2",
      assigneeDisplayName: "Maria Lopez",
    },
  ] as unknown[],
}));

vi.mock("@/features/session/components/can", () => ({
  useCan: () => state.canViewNumbers,
  Can: ({ children }: { children: React.ReactNode }) =>
    state.canWriteTasks ? children : null,
}));
vi.mock("@/features/insights/hooks/use-scoring", () => ({
  useContactScores: () => ({
    isPending: false,
    data: {
      scores: [{ kind: "uplift", pTreated: 0.3, pControl: 0.2 }],
    },
  }),
  useScoringModels: () => ({
    data: [
      {
        kind: "uplift",
        isActive: true,
        segmentRules: {
          do_not_disturb_uplift_below: 0,
          persuadable_uplift_at_least: 0.1,
          sure_thing_control_rate_at_least: 0.35,
        },
      },
    ],
  }),
}));
vi.mock("@/features/crm/hooks/use-contacts", () => ({
  useContacts: () => ({ data: { items: [{ id: "c-1" }] } }),
}));
vi.mock("@/features/crm/hooks/use-contact", () => ({
  useContact: () => ({
    isPending: false,
    data: { phone: "+380 50 111 22 33", email: null },
  }),
}));
vi.mock("@/features/tasks/hooks/use-tasks", () => ({
  useDueContactTasks: () => ({
    isPending: false,
    data: { items: state.dueTasks },
  }),
}));
vi.mock("@/features/tasks/components/task-status-checkbox", () => ({
  TaskStatusCheckbox: ({ task }: { task: { title: string } }) => (
    <span role="checkbox" aria-checked="false" aria-label={task.title} />
  ),
}));
vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({
    data: {
      user: { id: "u-1", timeZone: "UTC" },
    },
  }),
}));
vi.mock("@/i18n/navigation", () => ({
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));
vi.mock("@/features/insights/hooks/use-insights", () => ({
  usePriority: () => ({
    data: {
      items: [
        {
          contactId: "c-1",
          displayName: "Sarah Connor",
          companyName: null,
          ownerUserId: "u-1",
          rank: 1,
          value: 41.9,
          mode: state.mode,
          segment: "champions",
          upliftValue: state.mode === "ml" ? 0.123 : null,
          upliftSegment: state.mode === "ml" ? "persuadable" : null,
          reasonCodes: ["reason.uplift_high"],
          suggestedAction: state.action,
        },
      ],
      mode: state.mode,
      mlStatus: "ready",
      modelProvenance: "semi_synthetic",
      nextCursor: null,
      upliftHighThreshold: 0.2,
      staleDealDays: 14,
    },
    isPending: false,
    isFetching: false,
    isError: false,
  }),
  usePrioritySegments: () => ({
    data: {
      asOf: "2026-11-15",
      rfmAsOf: "2026-11-15",
      upliftAsOf: "2026-11-15",
      rfm: [{ segment: "champions", onList: 4, total: 827 }],
      uplift: [{ segment: "persuadable", onList: 585, total: 1690 }],
    },
  }),
}));

const { PriorityPanel } =
  await import("@/features/insights/components/priority-panel");

function renderPanel(onUrlUpdate?: (query: string) => void) {
  return render(
    <NextIntlClientProvider
      locale="en"
      messages={{
        insights: insightsEn,
        hints: hintsEn,
        crm: crmEn,
        table: tableEn,
      }}
    >
      <NuqsTestingAdapter
        onUrlUpdate={(event) => onUrlUpdate?.(event.queryString)}
      >
        <PriorityPanel />
      </NuqsTestingAdapter>
    </NextIntlClientProvider>,
  );
}

afterEach(() => {
  cleanup();
  state.mode = "ml";
  state.canViewNumbers = true;
  state.canWriteTasks = true;
  state.action = "action.call";
  state.dueTasks = [
    {
      id: "t-1",
      title: "Check invoice",
      type: "call",
      dueAt: "2020-01-01T10:00:00Z",
      isCompleted: false,
      version: 1,
      assigneeUserId: "u-2",
      assigneeDisplayName: "Maria Lopez",
    },
  ];
});

describe("priority panel segments and numbers", () => {
  it("shows the uplift badge first and the RFM segment as the secondary label in ML mode", () => {
    renderPanel();

    const row = screen.getByText("Sarah Connor").closest("tr");
    expect(row).not.toBeNull();
    expect(
      within(row as HTMLElement).getByText("Likely to respond"),
    ).toBeInTheDocument();
    expect(
      within(row as HTMLElement).getByText("RFM: Champions"),
    ).toBeInTheDocument();
    // The number is percentage points, signed; the unit explains itself on hover.
    expect(row?.textContent).toContain("+12.3 pp");
    expect(within(row as HTMLElement).getByText("pp")).toHaveAttribute(
      "title",
      "percentage points",
    );
  });

  it("shows only the RFM label in RFM mode, with no uplift column", () => {
    state.mode = "rfm";
    renderPanel();

    const row = screen.getByText("Sarah Connor").closest("tr");
    expect(
      within(row as HTMLElement).getByText("Champions"),
    ).toBeInTheDocument();
    expect(
      within(row as HTMLElement).queryByText("Likely to respond"),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("columnheader", {
        name: insightsEn.home.priority.upliftColumn,
      }),
    ).not.toBeInTheDocument();
  });

  it("keeps the chips but hides the uplift column from a caller without the ML view", () => {
    state.canViewNumbers = false;
    renderPanel();

    const row = screen.getByText("Sarah Connor").closest("tr");
    expect(
      within(row as HTMLElement).getByText("Likely to respond"),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("columnheader", {
        name: insightsEn.home.priority.upliftColumn,
      }),
    ).not.toBeInTheDocument();
    expect(row?.textContent).not.toContain("+12.3");
  });

  it("opens the follow-up step into the due task, a tel link and the missing email", () => {
    renderPanel();

    fireEvent.click(screen.getByRole("button", { name: "Follow up" }));

    expect(screen.getByText("Call: Check invoice")).toBeInTheDocument();
    expect(screen.getByText("Assigned to Maria Lopez")).toBeInTheDocument();
    expect(
      screen.getByRole("checkbox", { name: "Check invoice" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: "+380 50 111 22 33" }),
    ).toHaveAttribute("href", "tel:+380501112233");
    expect(screen.getByText("No email address")).toBeInTheDocument();
  });

  it("says so when no due task is visible, and offers the contact", () => {
    state.dueTasks = [];
    renderPanel();

    fireEvent.click(screen.getByRole("button", { name: "Follow up" }));

    expect(
      screen.getByText("No open task is due any more."),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        "A colleague's task may be hidden from you. Open the contact to check.",
      ),
    ).toBeInTheDocument();
  });

  it("explains a win-back step in one line instead of listing tasks", () => {
    state.action = "action.win_back";
    renderPanel();

    fireEvent.click(screen.getByRole("button", { name: "Win back" }));

    expect(
      screen.getByText(
        "Used to buy, but not lately. Check in or make an offer.",
      ),
    ).toBeInTheDocument();
    expect(screen.queryByText("Check invoice")).not.toBeInTheDocument();
  });

  it("shows the chance without and with contact on a click of the uplift value", () => {
    renderPanel();

    fireEvent.click(
      screen.getByRole("button", { name: "Show the chances for Sarah Connor" }),
    );

    expect(screen.getByText("Without contact")).toBeInTheDocument();
    expect(screen.getByText("20 %")).toBeInTheDocument();
    expect(screen.getByText("30 %")).toBeInTheDocument();
    expect(screen.getByText("+10.0 pp from contacting.")).toBeInTheDocument();
  });

  it("explains every buying-pattern segment and the live thresholds in the column hints", () => {
    renderPanel();

    const headerHint = (column: string) =>
      within(
        screen.getByRole("columnheader", { name: new RegExp(`^${column}`) }),
      ).getByRole("button", { name: "What is this?" });

    fireEvent.focus(headerHint("Segment"));
    const segmentTip = screen.getByRole("tooltip");
    expect(segmentTip).toHaveTextContent("Buying pattern");
    expect(segmentTip).toHaveTextContent(
      "At risk Used to buy often, but not lately.",
    );
    expect(segmentTip).toHaveTextContent(
      "Contacting raises the chance of a response by 10 pp or more.",
    );
    fireEvent.keyDown(segmentTip, { key: "Escape" });

    fireEvent.focus(headerHint("Uplift"));
    const upliftTip = screen.getByRole("tooltip");
    expect(upliftTip).toHaveTextContent("Do not disturb below 0 pp");
    expect(upliftTip).toHaveTextContent("High uplift reason starts at 20 pp");
  });

  it("names the stale-deal days from the list response in the Why hint", () => {
    renderPanel();

    fireEvent.focus(
      within(screen.getByRole("columnheader", { name: /^Why/ })).getByRole(
        "button",
        { name: "What is this?" },
      ),
    );

    expect(screen.getByRole("tooltip")).toHaveTextContent(
      "An open deal has not moved for 14 days.",
    );
  });

  it("hides Create task from a caller without tasks.write", () => {
    state.canWriteTasks = false;
    renderPanel();

    expect(
      screen.queryByRole("button", { name: /Create task/ }),
    ).not.toBeInTheDocument();
  });

  it("offers both segment dimensions, labels them N of M and writes the choice to the URL", async () => {
    const onUrlUpdate = vi.fn();
    renderPanel(onUrlUpdate);

    fireEvent.click(screen.getByRole("button", { name: crmEn.filters.button }));
    fireEvent.click(screen.getByRole("button", { name: /Uplift segment/ }));

    const options = screen.getAllByRole("option");
    expect(
      options.some((option) =>
        option.textContent?.includes("Likely to respond · 585 of 1,690"),
      ),
    ).toBe(true);

    fireEvent.click(screen.getByRole("option", { name: /Likely to respond/ }));

    await waitFor(() => {
      expect(onUrlUpdate).toHaveBeenCalled();
    });
    expect(onUrlUpdate.mock.calls.at(-1)?.[0]).toContain(
      "upliftSegment=persuadable",
    );
  });
});
