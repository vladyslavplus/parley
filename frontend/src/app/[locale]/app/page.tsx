import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { BACKEND_PATHS } from "@/constants/routes";
import { SESSION_COOKIES } from "@/constants/session";
import { HomeView } from "@/features/insights/components/home-view";
import {
  hasAnyHomeSection,
  homeSections,
} from "@/features/insights/lib/home-sections";
import { resolveAppIndexRoute } from "@/features/session/app-index-route";
import type { AppLocale } from "@/i18n/routing";
import type { Schemas } from "@/lib/api/schemas";
import { backendFetch } from "@/lib/bff/server";

export default async function AppIndexPage({
  params,
}: {
  params: Promise<{ locale: AppLocale }>;
}) {
  const { locale } = await params;
  const cookieStore = await cookies();
  const accessToken = cookieStore.get(SESSION_COOKIES.access)?.value;

  let user: Schemas["CurrentUserView"] | null = null;
  if (accessToken) {
    try {
      const res = await backendFetch({
        method: "GET",
        path: BACKEND_PATHS.me,
        accessToken,
        acceptLanguage: locale,
      });

      if (res.ok) {
        user = (await res.json()) as Schemas["CurrentUserView"];
      }
    } catch {
      // fallback to profile if /me is unreachable
    }
  }

  // Home: priority today and the dashboard, for users who may read either.
  // Everyone else keeps the first section they can open.
  const sections = homeSections(user?.permissions);
  if (user && hasAnyHomeSection(sections)) {
    return <HomeView sections={sections} />;
  }

  redirect(resolveAppIndexRoute(user?.permissions, locale));
}
