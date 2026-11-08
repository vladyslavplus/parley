import { cookies } from "next/headers";

import { NotFoundView } from "@/components/ui/not-found-view";
import { SESSION_COOKIES } from "@/constants/session";

export default async function NotFound() {
  const cookieStore = await cookies();
  const hasAccess = Boolean(cookieStore.get(SESSION_COOKIES.access)?.value);
  const hasRefresh = Boolean(cookieStore.get(SESSION_COOKIES.refresh)?.value);
  const isAuthenticated = hasAccess || hasRefresh;

  return <NotFoundView inApp={false} isAuthenticated={isAuthenticated} />;
}
