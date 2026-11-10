"use client";

import { useTranslations } from "next-intl";
import { useEffect, type ReactNode } from "react";

import { LoadingBlock } from "@/components/layout/loading-block";
import type { Permission } from "@/constants/permissions";
import { useCanStatus } from "@/features/session/components/can";
import { useRouter } from "@/i18n/navigation";

interface Props {
  permission: Permission;
  // Where a user without the permission is sent. The server still rejects the
  // calls; this only keeps a viewer out of screens they cannot use.
  redirectTo: string;
  children: ReactNode;
}

export function RequirePermission({ permission, redirectTo, children }: Props) {
  const status = useCanStatus(permission);
  const router = useRouter();
  const tCommon = useTranslations("common");

  useEffect(() => {
    if (status === "denied") router.replace(redirectTo);
  }, [status, redirectTo, router]);

  if (status === "pending") return <LoadingBlock />;
  if (status === "denied") {
    return (
      <p role="alert" className="text-sm text-muted-foreground">
        {tCommon("forbidden")}
      </p>
    );
  }
  return children;
}
