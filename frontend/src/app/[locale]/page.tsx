import { cookies } from "next/headers";
import { redirect } from "next/navigation";

import { ROUTES } from "@/constants/routes";
import { SESSION_COOKIES } from "@/constants/session";
import type { AppLocale } from "@/i18n/routing";

export default async function HomePage({
  params,
}: {
  params: Promise<{ locale: AppLocale }>;
}) {
  const { locale } = await params;
  const jar = await cookies();
  const signedIn =
    jar.has(SESSION_COOKIES.access) || jar.has(SESSION_COOKIES.refresh);
  redirect(`/${locale}${signedIn ? ROUTES.settingsProfile : ROUTES.login}`);
}
