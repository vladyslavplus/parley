import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { LIMITS } from "@/constants/limits";
import { customFieldsListQueryOptions } from "@/features/crm/hooks/use-custom-fields";
import {
  auditListQueryOptions,
  invitationsListQueryOptions,
  membersListQueryOptions,
  meQueryOptions,
  teamsListQueryOptions,
} from "@/lib/api/hooks/use-api";

const mockBackendJson = vi
  .fn()
  .mockResolvedValue({ items: [], nextCursor: null });
vi.mock("@/lib/api/bff-client", () => ({
  backendJson: (...args: unknown[]) => mockBackendJson(...args),
}));

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/i18n/navigation", () => ({
  usePathname: () => "/app/settings/profile",
  Link: ({
    children,
    href,
    onMouseEnter,
    onFocus,
    ...rest
  }: {
    children: ReactNode;
    href: string;
    onMouseEnter?: () => void;
    onFocus?: () => void;
  }) => (
    <a href={href} onMouseEnter={onMouseEnter} onFocus={onFocus} {...rest}>
      {children}
    </a>
  ),
}));

vi.mock("@/features/session/components/can", () => ({
  Can: ({ children }: { children: ReactNode }) => <>{children}</>,
}));

const { SettingsNav } = await import("./settings-nav");

describe("SettingsNav", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(cleanup);

  it("prefetches settings queries with the exact queryKeys used by each panel's default state", () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const querySpy = vi.spyOn(queryClient, "query");

    render(
      <QueryClientProvider client={queryClient}>
        <SettingsNav />
      </QueryClientProvider>,
    );

    // Profile / Organization -> meQueryOptions
    fireEvent.mouseEnter(screen.getByRole("link", { name: "profile" }));
    expect(querySpy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: meQueryOptions("en").queryKey,
      }),
    );

    // Members -> membersListQueryOptions & invitationsListQueryOptions
    fireEvent.mouseEnter(screen.getByRole("link", { name: "members" }));
    const panelMembersOptions = membersListQueryOptions(
      {
        after: "",
        limit: LIMITS.defaultTablePageSize,
        q: undefined,
        roleCode: undefined,
        teamId: undefined,
        status: "active",
        sort: "name",
        order: "asc",
      },
      "en",
    );
    const panelInvitationsOptions = invitationsListQueryOptions(
      {
        after: "",
        limit: LIMITS.defaultTablePageSize,
        q: undefined,
        roleCode: undefined,
        sort: "createdAt",
        order: "desc",
      },
      "en",
    );
    expect(querySpy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: panelMembersOptions.queryKey,
      }),
    );
    expect(querySpy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: panelInvitationsOptions.queryKey,
      }),
    );

    // Teams -> teamsListQueryOptions
    fireEvent.focus(screen.getByRole("link", { name: "teams" }));
    const panelTeamsOptions = teamsListQueryOptions(
      {
        after: "",
        limit: LIMITS.defaultTablePageSize,
        q: undefined,
        sort: "name",
        order: "asc",
      },
      "en",
    );
    expect(querySpy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: panelTeamsOptions.queryKey,
      }),
    );

    // Audit -> auditListQueryOptions
    fireEvent.mouseEnter(screen.getByRole("link", { name: "audit" }));
    const panelAuditOptions = auditListQueryOptions(
      {
        after: "",
        limit: LIMITS.defaultTablePageSize,
        q: undefined,
        action: undefined,
        actorUserId: undefined,
        from: undefined,
        to: undefined,
        sort: "occurredAt",
        order: "desc",
      },
      "en",
    );
    expect(querySpy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: panelAuditOptions.queryKey,
      }),
    );

    // Custom fields -> customFieldsListQueryOptions
    fireEvent.mouseEnter(screen.getByRole("link", { name: "customFields" }));
    const panelCustomFieldsOptions = customFieldsListQueryOptions(
      {
        after: "",
        limit: LIMITS.defaultTablePageSize,
        q: undefined,
        entityType: "contact",
        fieldType: undefined,
        sort: "name",
        order: "asc",
      },
      "en",
    );
    expect(querySpy).toHaveBeenCalledWith(
      expect.objectContaining({
        queryKey: panelCustomFieldsOptions.queryKey,
      }),
    );
  });
});
