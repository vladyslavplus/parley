import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";

export function resolveAppIndexRoute(
  permissions: ReadonlyArray<{ permission: string }> | undefined,
  locale: string,
): string {
  const canReadContacts =
    permissions?.some((p) => p.permission === PERMISSIONS.contactsRead) ??
    false;
  if (canReadContacts) {
    return `/${locale}${ROUTES.contacts}`;
  }
  const canReadDeals =
    permissions?.some((p) => p.permission === PERMISSIONS.dealsRead) ?? false;
  if (canReadDeals) {
    return `/${locale}${ROUTES.deals}`;
  }
  const canReadTasks =
    permissions?.some((p) => p.permission === PERMISSIONS.tasksRead) ?? false;
  if (canReadTasks) {
    return `/${locale}${ROUTES.tasks}`;
  }
  return `/${locale}${ROUTES.settingsProfile}`;
}
