import { cleanup, render, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

const TEAM_ID = "3f2b8c1e-7d4a-4c1f-9b6e-0a1b2c3d4e5f";

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

let canTeams = false;
vi.mock("@/features/session/components/can", () => ({
  useCan: () => canTeams,
}));

const paged = (items: unknown[]) => ({
  isPending: false,
  isError: false,
  data: { pages: [{ items, nextCursor: null }] },
  hasNextPage: false,
});
vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({ data: { user: { id: "me" } } }),
  useMembers: () =>
    paged([
      {
        id: "m1",
        userId: "u1",
        email: "a@example.com",
        displayName: "A",
        roleCode: "member",
        teamId: TEAM_ID,
        status: "active",
        joinedAt: "2026-01-01T00:00:00Z",
      },
    ]),
  useInvitations: () => paged([]),
  useTeams: () => ({ data: undefined }),
  useInvalidate: () => vi.fn(),
  useAction: () => ({ error: null, pending: false, run: vi.fn() }),
}));

const { MembersPanel } = await import("./members-panel");

afterEach(cleanup);

describe("members table team column", () => {
  it("does not claim 'no team' for a member with a team when teams are not loaded", () => {
    canTeams = false;
    render(<MembersPanel />);
    const row = screen.getByText("a@example.com").closest("tr");
    expect(row).not.toBeNull();
    expect(within(row as HTMLElement).queryByText("noTeam")).toBeNull();
    expect(within(row as HTMLElement).getByText("inTeam")).toBeInTheDocument();
  });
});
