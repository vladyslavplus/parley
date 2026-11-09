import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import type { Activity } from "@/features/timeline/types";
import { ActivityTimeline } from "./activity-timeline";

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>,
  );
}

const mockCreateNote = vi.fn();
const mockDeleteNote = vi.fn();
const mockLoadMore = vi.fn();

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string, values?: Record<string, string>) => {
    if (values) {
      return `${key}:${JSON.stringify(values)}`;
    }
    return key;
  },
}));

let mockMeData: {
  user: { id: string; fullName: string };
  organization: { timeZone: string; currency: string };
  permissions: { permission: string; scope: string | null }[];
} = {
  user: { id: "u-1", fullName: "Test User" },
  organization: { timeZone: "America/New_York", currency: "USD" },
  permissions: [{ permission: "activities.write", scope: "org" }],
};

vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({
    data: mockMeData,
  }),
}));

vi.mock("@/features/session/components/can", () => ({
  Can: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  useCan: () => true,
}));

let mockActivities: Activity[] = [];
let mockHasMore = false;
let mockIsLoading = false;

vi.mock("@/features/timeline/hooks/use-activities", () => ({
  useActivities: () => ({
    activities: mockActivities,
    hasMore: mockHasMore,
    loadMore: mockLoadMore,
    isLoadingMore: false,
    isLoading: mockIsLoading,
    isFetching: false,
  }),
}));

vi.mock("@/features/timeline/hooks/use-note-mutations", () => ({
  useNoteMutations: () => ({
    createNote: mockCreateNote,
    deleteNote: mockDeleteNote,
    updateNote: vi.fn(),
  }),
}));

vi.mock("@/lib/toasts", () => ({
  showSuccessToast: vi.fn(),
  showErrorToast: vi.fn(),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  mockMeData = {
    user: { id: "u-1", fullName: "Test User" },
    organization: { timeZone: "America/New_York", currency: "USD" },
    permissions: [{ permission: "activities.write", scope: "org" }],
  };
  mockActivities = [];
  mockHasMore = false;
  mockIsLoading = false;
});

describe("ActivityTimeline", () => {
  const subject = { contactId: "contact-1" };

  it("renders activities with formatted labels, time zone title tooltip, and event types", () => {
    mockActivities = [
      {
        id: "act-1",
        type: "contact_created",
        actorUserId: "user-1",
        actorDisplayName: "Alice Admin",
        occurredAt: "2026-11-09T14:30:00Z",
        contactId: "contact-1",
        contactName: "John Doe",
        dealId: null,
        dealTitle: null,
        companyId: null,
        companyName: null,
        taskId: null,
        taskTitle: null,
        noteId: null,
        noteBody: null,
        noteVersion: null,
        payload: null,
      },
      {
        id: "act-2",
        type: "task_completed",
        actorUserId: "user-2",
        actorDisplayName: "Bob Sales",
        occurredAt: "2026-11-09T15:00:00Z",
        contactId: "contact-1",
        contactName: "John Doe",
        dealId: null,
        dealTitle: null,
        companyId: null,
        companyName: null,
        taskId: "task-1",
        taskTitle: "Follow-up phone call",
        noteId: null,
        noteBody: null,
        noteVersion: null,
        payload: null,
      },
      {
        id: "act-3",
        type: "note_added",
        actorUserId: "user-1",
        actorDisplayName: "Alice Admin",
        occurredAt: "2026-11-09T16:00:00Z",
        contactId: "contact-1",
        contactName: "John Doe",
        dealId: null,
        dealTitle: null,
        companyId: null,
        companyName: null,
        taskId: null,
        taskTitle: null,
        noteId: "note-1",
        noteBody: "Customer agreed to proposal",
        noteVersion: 1,
        payload: null,
      },
    ];

    renderWithClient(<ActivityTimeline subject={subject} />);

    // Verify event texts rendered
    expect(screen.getByText(/contactCreated/)).toBeDefined();
    expect(screen.getByText(/taskCompleted/)).toBeDefined();
    expect(screen.getByText(/noteAdded/)).toBeDefined();
    expect(screen.getByText("Customer agreed to proposal")).toBeDefined();

    // Verify time zone formatted tooltip title exists
    const timeElements = screen.getAllByTitle(/2026/);
    expect(timeElements.length).toBe(3);
  });

  it("submits a quick note", async () => {
    mockCreateNote.mockResolvedValueOnce({
      id: "note-new",
      body: "Quick follow up note",
      authorUserId: "u-1",
      contactId: "contact-1",
      version: 1,
      createdAt: "2026-11-09T12:00:00Z",
      updatedAt: "2026-11-09T12:00:00Z",
    });

    renderWithClient(<ActivityTimeline subject={subject} />);

    const textarea = screen.getByPlaceholderText("form.quickAddPlaceholder");
    fireEvent.change(textarea, { target: { value: "Quick follow up note" } });

    const submitBtn = screen.getByRole("button", { name: /addNote/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mockCreateNote).toHaveBeenCalledWith(
        {
          body: "Quick follow up note",
          contactId: "contact-1",
          dealId: null,
          companyId: null,
        },
        subject,
      );
    });

    expect(textarea).toHaveValue("");
  });

  it("renders show more button and calls loadMore when hasMore is true", () => {
    mockActivities = [
      {
        id: "act-1",
        type: "note_added",
        actorUserId: "user-1",
        actorDisplayName: "Alice Admin",
        occurredAt: "2026-11-09T14:30:00Z",
        contactId: "contact-1",
        contactName: "John Doe",
        dealId: null,
        dealTitle: null,
        companyId: null,
        companyName: null,
        taskId: null,
        taskTitle: null,
        noteId: "note-1",
        noteBody: "Note content",
        noteVersion: 1,
        payload: null,
      },
    ];
    mockHasMore = true;

    renderWithClient(<ActivityTimeline subject={subject} />);

    const showMoreBtn = screen.getByRole("button", { name: "showMore" });
    fireEvent.click(showMoreBtn);

    expect(mockLoadMore).toHaveBeenCalled();
  });

  it("hides edit and delete buttons on teammate's note when activities.write scope is own", () => {
    mockMeData.permissions = [{ permission: "activities.write", scope: "own" }];
    mockActivities = [
      {
        id: "act-1",
        type: "note_added",
        actorUserId: "other-user",
        actorDisplayName: "Other Teammate",
        occurredAt: "2026-11-09T14:30:00Z",
        contactId: "contact-1",
        contactName: "John Doe",
        dealId: null,
        dealTitle: null,
        companyId: null,
        companyName: null,
        taskId: null,
        taskTitle: null,
        noteId: "note-1",
        noteBody: "Teammate private note",
        noteVersion: 1,
        payload: null,
      },
    ];

    renderWithClient(<ActivityTimeline subject={subject} />);

    expect(screen.queryByRole("button", { name: "editNote" })).toBeNull();
    expect(screen.queryByRole("button", { name: "deleteNote" })).toBeNull();
  });

  it("hides edit button when noteVersion is null even for note author", () => {
    mockActivities = [
      {
        id: "act-1",
        type: "note_added",
        actorUserId: "u-1",
        actorDisplayName: "Test User",
        occurredAt: "2026-11-09T14:30:00Z",
        contactId: "contact-1",
        contactName: "John Doe",
        dealId: null,
        dealTitle: null,
        companyId: null,
        companyName: null,
        taskId: null,
        taskTitle: null,
        noteId: "note-1",
        noteBody: "Author note without version",
        noteVersion: null,
        payload: null,
      },
    ];

    renderWithClient(<ActivityTimeline subject={subject} />);

    expect(screen.queryByRole("button", { name: "editNote" })).toBeNull();
    expect(screen.getByRole("button", { name: "deleteNote" })).toBeDefined();
  });
});

function systemActivity(overrides: Partial<Activity>): Activity {
  return {
    id: "act-x",
    type: "order_recorded",
    actorUserId: null,
    actorDisplayName: null,
    occurredAt: "2026-11-09T14:30:00Z",
    contactId: "contact-1",
    contactName: "John Doe",
    dealId: null,
    dealTitle: null,
    companyId: null,
    companyName: null,
    taskId: null,
    taskTitle: null,
    noteId: null,
    noteBody: null,
    noteVersion: null,
    payload: null,
    ...overrides,
  };
}

describe("ActivityTimeline order entries", () => {
  const subject = { contactId: "contact-1" };

  it("renders the invoice number and localized amount", () => {
    mockActivities = [
      systemActivity({
        payload: {
          invoiceNo: "536001",
          totalAmount: 1234.5678,
          currency: "USD",
          isCancellation: false,
        },
      }),
    ];
    renderWithClient(<ActivityTimeline subject={subject} />);

    expect(
      screen.getByText(
        'events.orderRecorded:{"invoice":"536001","amount":"$1,234.57"}',
      ),
    ).toBeDefined();
    expect(screen.queryByText("events.orderCancellation")).toBeNull();
  });

  it("marks a cancellation with a badge", () => {
    mockActivities = [
      systemActivity({
        payload: {
          invoiceNo: "C536002",
          totalAmount: -50,
          currency: "USD",
          isCancellation: true,
        },
      }),
    ];
    renderWithClient(<ActivityTimeline subject={subject} />);

    expect(screen.getByText("events.orderCancellation")).toBeDefined();
    expect(screen.getByText(/-\$50\.00/)).toBeDefined();
  });

  it.each([
    [null, "events.orderRecordedNeutral"],
    [{}, "events.orderRecordedNeutral"],
    [{ invoiceNo: "  ", totalAmount: "x" }, "events.orderRecordedNeutral"],
    [{ invoiceNo: "A1" }, 'events.orderRecordedInvoice:{"invoice":"A1"}'],
    [
      { totalAmount: "10.5", currency: "USD" },
      'events.orderRecordedAmount:{"amount":"$10.50"}',
    ],
  ])("reads a defective payload %j defensively", (payload, expected) => {
    mockActivities = [systemActivity({ payload })];
    renderWithClient(<ActivityTimeline subject={subject} />);

    expect(screen.getByText(expected)).toBeDefined();
  });

  it("offers an orders type filter", () => {
    renderWithClient(<ActivityTimeline subject={subject} />);

    expect(
      screen.getByRole("button", { name: "filters.order_recorded" }),
    ).toBeDefined();
  });
});

describe("ActivityTimeline actor fallback", () => {
  const subject = { contactId: "contact-1" };

  it("shows the system label for an entry without an actor", () => {
    mockActivities = [systemActivity({ type: "contact_created" })];
    renderWithClient(<ActivityTimeline subject={subject} />);

    expect(screen.getByText(/contactCreated.*"actor":"system"/)).toBeDefined();
  });

  it("keeps the unknown-user label when the actor is known but unnamed", () => {
    mockActivities = [
      systemActivity({ type: "contact_created", actorUserId: "user-9" }),
    ];
    renderWithClient(<ActivityTimeline subject={subject} />);

    expect(
      screen.getByText(/contactCreated.*"actor":"unknownUser"/),
    ).toBeDefined();
  });

  it("never shows a raw closed status word for a deal", () => {
    mockActivities = [
      systemActivity({
        type: "deal_closed",
        dealTitle: "Big deal",
        payload: {},
      }),
    ];
    renderWithClient(<ActivityTimeline subject={subject} />);

    expect(screen.getByText(/"status":"events.statusClosed"/)).toBeDefined();
  });
});
