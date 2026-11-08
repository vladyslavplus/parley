"use client";

import type { ReactNode } from "react";

import type { Permission } from "@/constants/permissions";
import { useMe } from "@/lib/api/hooks/use-api";

export function useCan(permission: Permission): boolean {
  const { data } = useMe();
  return (
    data?.permissions.some((grant) => grant.permission === permission) ?? false
  );
}

// Hides UI the user cannot use. The server decides access on every request.
export function Can({
  permission,
  children,
  fallback = null,
}: {
  permission: Permission;
  children: ReactNode;
  fallback?: ReactNode;
}) {
  return useCan(permission) ? children : fallback;
}
