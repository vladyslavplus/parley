"use client";

import type { ReactNode } from "react";

import type { Permission } from "@/constants/permissions";
import { useMe } from "@/lib/api/hooks/use-api";
import { useMounted } from "@/lib/use-mounted";

export type CanStatus = "pending" | "allowed" | "denied";

export function useCanStatus(permission: Permission): CanStatus {
  const { data, isPending } = useMe();
  const mounted = useMounted();
  if (!mounted || isPending) return "pending";
  const hasPermission =
    data?.permissions.some((grant) => grant.permission === permission) ?? false;
  return hasPermission ? "allowed" : "denied";
}

export function useCan(permission: Permission): boolean {
  return useCanStatus(permission) === "allowed";
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
