"use client";

import {
  keepPreviousData,
  queryOptions,
  useInfiniteQuery,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import { describeError, toApiError } from "@/lib/errors";

export function meQueryOptions(locale: string) {
  return queryOptions({
    queryKey: queryKeys.me(),
    queryFn: () => backendJson<Schemas["CurrentUserView"]>("/me", { locale }),
    staleTime: LIMITS.meStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function useMe() {
  const locale = useLocale();
  return useQuery(meQueryOptions(locale));
}

export function useMyOrganizations() {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.myOrganizations(),
    queryFn: () =>
      backendJson<Schemas["MyOrganizationView"][]>("/me/organizations", {
        locale,
      }),
    staleTime: LIMITS.meStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

function usePagedList<
  TPage extends { items: unknown[]; nextCursor: string | null },
>(
  queryKey: readonly unknown[],
  path: "/members" | "/teams" | "/invitations" | "/audit-events",
  enabled: boolean,
) {
  const locale = useLocale();
  return useInfiniteQuery({
    queryKey,
    enabled,
    initialPageParam: null as string | null,
    queryFn: ({ pageParam }) =>
      backendJson<TPage>(path, {
        locale,
        query: { after: pageParam, limit: LIMITS.defaultPageSize },
      }),
    getNextPageParam: (last) => last.nextCursor,
  });
}

export const useMembers = (enabled = true) =>
  usePagedList<Schemas["KeysetCursorPageOfMemberView"]>(
    queryKeys.members(),
    "/members",
    enabled,
  );
export const useInvitations = (enabled = true) =>
  usePagedList<Schemas["KeysetCursorPageOfInvitationView"]>(
    queryKeys.invitations(),
    "/invitations",
    enabled,
  );
export const useTeams = (enabled = true) =>
  usePagedList<Schemas["KeysetCursorPageOfTeamView"]>(
    queryKeys.teams(),
    "/teams",
    enabled,
  );
export const useAuditEvents = (enabled = true) =>
  usePagedList<Schemas["KeysetCursorPageOfAuditEventView"]>(
    queryKeys.audit(),
    "/audit-events",
    enabled,
  );

export function membersListQueryOptions(
  params: {
    after?: string;
    limit?: number;
    q?: string;
    roleCode?: string;
    teamId?: string;
    status?: "active" | "deactivated" | "all";
    sort?: "name" | "email" | "role" | "joinedAt";
    order?: "asc" | "desc";
  } = {},
  locale: string,
) {
  const normalized = {
    after: params.after || undefined,
    limit: params.limit ?? LIMITS.defaultTablePageSize,
    q: params.q || undefined,
    roleCode: params.roleCode || undefined,
    teamId: params.teamId || undefined,
    status: params.status || ("active" as const),
    sort: params.sort || ("name" as const),
    order: params.order || ("asc" as const),
  };

  return queryOptions({
    queryKey: queryKeys.membersList(normalized),
    queryFn: () =>
      backendJson<Schemas["KeysetCursorPageOfMemberView"]>("/members", {
        locale,
        query: normalized,
      }),
    placeholderData: keepPreviousData,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function useMembersPage(
  params: Parameters<typeof membersListQueryOptions>[0] = {},
  enabled = true,
) {
  const locale = useLocale();
  return useQuery({
    ...membersListQueryOptions(params, locale),
    enabled,
  });
}

export function invitationsListQueryOptions(
  params: {
    after?: string;
    limit?: number;
    q?: string;
    roleCode?: string;
    sort?: "createdAt" | "email" | "expiresAt";
    order?: "asc" | "desc";
  } = {},
  locale: string,
) {
  const normalized = {
    after: params.after || undefined,
    limit: params.limit ?? LIMITS.defaultTablePageSize,
    q: params.q || undefined,
    roleCode: params.roleCode || undefined,
    sort: params.sort || ("createdAt" as const),
    order: params.order || ("desc" as const),
  };

  return queryOptions({
    queryKey: queryKeys.invitationsList(normalized),
    queryFn: () =>
      backendJson<Schemas["KeysetCursorPageOfInvitationView"]>("/invitations", {
        locale,
        query: normalized,
      }),
    placeholderData: keepPreviousData,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function useInvitationsPage(
  params: Parameters<typeof invitationsListQueryOptions>[0] = {},
  enabled = true,
) {
  const locale = useLocale();
  return useQuery({
    ...invitationsListQueryOptions(params, locale),
    enabled,
  });
}

export function teamsListQueryOptions(
  params: {
    after?: string;
    limit?: number;
    q?: string;
    sort?: "name" | "createdAt";
    order?: "asc" | "desc";
  } = {},
  locale: string,
) {
  const normalized = {
    after: params.after || undefined,
    limit: params.limit ?? LIMITS.defaultTablePageSize,
    q: params.q || undefined,
    sort: params.sort || ("name" as const),
    order: params.order || ("asc" as const),
  };

  return queryOptions({
    queryKey: queryKeys.teamsList(normalized),
    queryFn: () =>
      backendJson<Schemas["KeysetCursorPageOfTeamView"]>("/teams", {
        locale,
        query: normalized,
      }),
    placeholderData: keepPreviousData,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function useTeamsPage(
  params: Parameters<typeof teamsListQueryOptions>[0] = {},
  enabled = true,
) {
  const locale = useLocale();
  return useQuery({
    ...teamsListQueryOptions(params, locale),
    enabled,
  });
}

export function auditListQueryOptions(
  params: {
    after?: string;
    limit?: number;
    q?: string;
    action?: string;
    actorUserId?: string;
    from?: string;
    to?: string;
    sort?: "occurredAt";
    order?: "asc" | "desc";
  } = {},
  locale: string,
) {
  const normalized = {
    after: params.after || undefined,
    limit: params.limit ?? LIMITS.defaultTablePageSize,
    q: params.q || undefined,
    action: params.action || undefined,
    actorUserId: params.actorUserId || undefined,
    from: params.from || undefined,
    to: params.to || undefined,
    sort: params.sort || ("occurredAt" as const),
    order: params.order || ("desc" as const),
  };

  return queryOptions({
    queryKey: queryKeys.auditList(normalized),
    queryFn: () =>
      backendJson<Schemas["KeysetCursorPageOfAuditEventView"]>(
        "/audit-events",
        {
          locale,
          query: normalized,
        },
      ),
    placeholderData: keepPreviousData,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function useAuditEventsPage(
  params: Parameters<typeof auditListQueryOptions>[0] = {},
  enabled = true,
) {
  const locale = useLocale();
  return useQuery({
    ...auditListQueryOptions(params, locale),
    enabled,
  });
}

export function useInvalidate() {
  const client = useQueryClient();
  return (key: readonly unknown[]) =>
    client.invalidateQueries({ queryKey: key });
}

// Runs a mutation, keeping a single localized error message for the panel.
export function useAction() {
  const tKinds = useTranslations("errors.kinds");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function run(action: () => Promise<void>): Promise<boolean> {
    setPending(true);
    setError(null);
    try {
      await action();
      return true;
    } catch (caught) {
      setError(describeError(toApiError(caught), (kind) => tKinds(kind)));
      return false;
    } finally {
      setPending(false);
    }
  }

  return { error, pending, run };
}
