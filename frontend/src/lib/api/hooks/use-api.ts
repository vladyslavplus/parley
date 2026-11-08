"use client";

import {
  useInfiniteQuery,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";
import { useState } from "react";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import { describeError, toApiError } from "@/lib/errors";
import { useTranslations } from "next-intl";

export function useMe() {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.me(),
    queryFn: () => backendJson<Schemas["CurrentUserView"]>("/me", { locale }),
    staleTime: LIMITS.meStaleTimeMs,
  });
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
  });
}

function usePagedList<
  TPage extends { items: unknown[]; nextCursor: string | null },
>(
  queryKey: readonly unknown[],
  path: "/members" | "/teams" | "/invitations" | "/audit-events",
  cursorParam: "after" | "before",
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
        query: { [cursorParam]: pageParam, limit: LIMITS.defaultPageSize },
      }),
    getNextPageParam: (last) => last.nextCursor,
  });
}

export const useMembers = (enabled = true) =>
  usePagedList<Schemas["CursorPageOfMemberView"]>(
    queryKeys.members(),
    "/members",
    "after",
    enabled,
  );
export const useInvitations = (enabled = true) =>
  usePagedList<Schemas["CursorPageOfInvitationView"]>(
    queryKeys.invitations(),
    "/invitations",
    "after",
    enabled,
  );
export const useTeams = (enabled = true) =>
  usePagedList<Schemas["CursorPageOfTeamView"]>(
    queryKeys.teams(),
    "/teams",
    "after",
    enabled,
  );
export const useAuditEvents = (enabled = true) =>
  usePagedList<Schemas["CursorPageOfAuditEventView"]>(
    queryKeys.audit(),
    "/audit-events",
    "before",
    enabled,
  );

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
