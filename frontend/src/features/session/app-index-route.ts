import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";

export function resolveAppIndexRoute(
  permissions: ReadonlyArray<{ permission: string }> | undefined,
  locale: string,
): string {
  const canReadContacts =
    permissions?.some((p) => p.permission === PERMISSIONS.contactsRead) ??
    false;
  return `/${locale}${canReadContacts ? ROUTES.contacts : ROUTES.settingsProfile}`;
}
