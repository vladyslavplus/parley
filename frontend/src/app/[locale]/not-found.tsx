import type { Metadata } from "next";
import { cookies } from "next/headers";
import { getTranslations } from "next-intl/server";

import { NotFoundView } from "@/components/ui/not-found-view";
import { SESSION_COOKIES } from "@/constants/session";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("errors.notFound");
  return {
    title: t("title"),
  };
}

export default async function NotFound() {
  const cookieStore = await cookies();
  const hasAccess = Boolean(cookieStore.get(SESSION_COOKIES.access)?.value);
  const hasRefresh = Boolean(cookieStore.get(SESSION_COOKIES.refresh)?.value);
  const isAuthenticated = hasAccess || hasRefresh;

  return <NotFoundView inApp={false} isAuthenticated={isAuthenticated} />;
}
