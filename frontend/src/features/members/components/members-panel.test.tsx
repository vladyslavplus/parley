import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { NuqsTestingAdapter } from "nuqs/adapters/testing";
import { afterEach, describe, expect, it, vi } from "vitest";

import { queryKeys } from "@/constants/query-keys";
import { ApiError } from "@/lib/errors";

const TEAM_ID = "3f2b8c1e-7d4a-4c1f-9b6e-0a1b2c3d4e5f";
const TEAM_2_ID = "99999999-7d4a-4c1f-9b6e-0a1b2c3d4e5f";

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string, params?: Record<string, unknown>) => {
    if (params?.name) return `${key} ${params.name}`;
    return key;
  },
}));

let canTeams = false;
vi.mock("@/features/session/components/can", () => ({
  useCan: () => canTeams,
}));

const backendVoidMock = vi.fn();
vi.mock("@/lib/api/bff-client", () => ({
  backendVoid: (...args: unknown[]) => backendVoidMock(...args),
  backendJson: vi.fn(),
}));

const invalidateMock = vi.fn().mockResolvedValue(undefined);
let teamsItems: Array<{ id: string; name: string }> = [];

const paged = (items: unknown[]) => ({
  isPending: false,
  isError: false,
  data: { pages: [{ items, nextCursor: null }] },
  hasNextPage: false,
});

vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({ data: { user: { id: "me" } } }),
  useMembersPage: () => ({
    isPending: false,
    isError: false,
    isFetching: false,
    data: {
      items: [
        {
          id: "m1",
          userId: "u1",
          email: "a@example.com",
          displayName: "Alice",
          roleCode: "sales_rep",
          teamId: TEAM_ID,
          status: "active",
          joinedAt: "2026-01-01T00:00:00Z",
        },
        {
          id: "m2",
          userId: "u2",
          email: "b@example.com",
          displayName: "Bob",
          roleCode: "owner",
          teamId: null,
          status: "active",
          joinedAt: "2026-01-02T00:00:00Z",
        },
      ],
      nextCursor: null,
    },
  }),
  useInvitationsPage: () => ({
    isPending: false,
    isError: false,
    isFetching: false,
    data: {
      items: [
        {
          id: "inv-1",
          email: "pending@example.com",
          roleCode: "sales_rep",
          teamId: null,
          expiresAt: "2026-12-01T00:00:00Z",
        },
      ],
      nextCursor: null,
    },
  }),
  useTeamsPage: () => ({
    isPending: false,
    isError: false,
    isFetching: false,
    data: {
      items: teamsItems,
      nextCursor: null,
    },
  }),
  useMembers: () => paged([]),
  useInvitations: () => paged([]),
  useTeams: () => ({ data: undefined }),
  useInvalidate: () => invalidateMock,
  useAction: () => ({ error: null, pending: false, run: vi.fn() }),
}));

const { MembersPanel } = await import("./members-panel");

afterEach(() => {
  cleanup();
  backendVoidMock.mockReset();
  invalidateMock.mockClear();
  teamsItems = [];
});

describe("MembersPanel", () => {
  it("does not claim 'no team' for a member with a team when teams are not loaded, and renders role as text badge without inline selects", () => {
    canTeams = false;
    render(
      <NuqsTestingAdapter>
        <MembersPanel />
      </NuqsTestingAdapter>,
    );
    const row = screen.getByText("a@example.com").closest("tr");
    expect(row).not.toBeNull();
    expect(within(row as HTMLElement).queryByText("noTeam")).toBeNull();
    expect(within(row as HTMLElement).getByText("inTeam")).toBeInTheDocument();
    expect(
      within(row as HTMLElement).queryByRole("combobox"),
    ).not.toBeInTheDocument();
  });

  it("opens Invite member modal, Edit member modal, and Deactivate ConfirmDialog (even when only one owner is on the current page)", () => {
    canTeams = false;
    render(
      <NuqsTestingAdapter>
        <MembersPanel />
      </NuqsTestingAdapter>,
    );

    // Invite member button opens modal
    fireEvent.click(screen.getByRole("button", { name: "invite.title" }));
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "cancel" }));

    // Edit member icon button opens edit modal
    fireEvent.click(screen.getByRole("button", { name: "editMember Alice" }));
    const editDialog = screen.getByRole("dialog");
    expect(within(editDialog).getByText("editModal.title")).toBeInTheDocument();
    expect(
      within(editDialog).getByRole("button", { name: "save" }),
    ).toBeDisabled();
    fireEvent.click(within(editDialog).getByRole("button", { name: "cancel" }));

    // Bob is the only owner on this page, and still has a Deactivate button (server enforces last-owner on 409)
    expect(
      screen.getByRole("button", { name: "deactivateMember Bob" }),
    ).toBeInTheDocument();

    fireEvent.click(
      screen.getByRole("button", { name: "deactivateMember Alice" }),
    );
    expect(screen.getByRole("alertdialog")).toBeInTheDocument();
  });

  it("invalidates queries on partial failure when role change succeeds but team change fails, and clears error when opening another member", async () => {
    canTeams = true;
    teamsItems = [
      { id: TEAM_ID, name: "Enterprise" },
      { id: TEAM_2_ID, name: "SMB" },
    ];
    backendVoidMock
      .mockResolvedValueOnce(undefined) // role PUT succeeds
      .mockRejectedValueOnce(
        new ApiError({
          status: 403,
          code: "forbidden",
          detail: "Team assignment failed",
          kind: "forbidden",
          traceId: null,
          fieldErrors: {},
        }),
      ); // team PUT fails

    render(
      <NuqsTestingAdapter>
        <MembersPanel />
      </NuqsTestingAdapter>,
    );

    fireEvent.click(screen.getByRole("button", { name: "editMember Alice" }));
    const editDialog = screen.getByRole("dialog");

    const roleSelect = within(editDialog).getByLabelText("columns.role");
    const teamSelect = within(editDialog).getByLabelText("columns.team");

    fireEvent.keyDown(roleSelect, { key: "Enter" });
    fireEvent.click(await screen.findByRole("option", { name: "admin" }));
    fireEvent.keyDown(teamSelect, { key: "Enter" });
    fireEvent.click(await screen.findByRole("option", { name: "SMB" }));

    const saveBtn = within(editDialog).getByRole("button", { name: "save" });
    expect(saveBtn).not.toBeDisabled();
    fireEvent.click(saveBtn);

    await waitFor(() => {
      expect(within(editDialog).getByRole("alert")).toHaveTextContent(
        "Team assignment failed",
      );
    });

    // Even though team change failed, role change succeeded so members/teams/owners/me were invalidated
    expect(invalidateMock).toHaveBeenCalledWith(queryKeys.members());
    expect(invalidateMock).toHaveBeenCalledWith(queryKeys.teams());
    expect(invalidateMock).toHaveBeenCalledWith(queryKeys.owners());
    expect(invalidateMock).toHaveBeenCalledWith(queryKeys.me());

    // Close modal and open another member — error is cleared
    fireEvent.click(within(editDialog).getByRole("button", { name: "cancel" }));
    fireEvent.click(screen.getByRole("button", { name: "editMember Bob" }));
    await waitFor(() => {
      expect(
        within(screen.getByRole("dialog")).queryByRole("alert"),
      ).toBeNull();
    });
  });
});
