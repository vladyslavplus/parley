import {
  Building2,
  Contact,
  Palette,
  Settings,
  type LucideIcon,
} from "lucide-react";

import { PERMISSIONS, type Permission } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";

export interface NavItemConfig {
  id: string;
  href: string;
  titleKey: string;
  icon: LucideIcon;
  permission?: Permission | null;
  section: "main" | "settings" | "dev";
}

export const MAIN_NAV_ITEMS: NavItemConfig[] = [
  {
    id: "contacts",
    href: ROUTES.contacts,
    titleKey: "sidebar.items.contacts",
    icon: Contact,
    permission: PERMISSIONS.contactsRead,
    section: "main",
  },
  {
    id: "companies",
    href: ROUTES.companies,
    titleKey: "sidebar.items.companies",
    icon: Building2,
    permission: PERMISSIONS.companiesRead,
    section: "main",
  },
];

export const SETTINGS_NAV_ITEMS: NavItemConfig[] = [
  {
    id: "settings",
    href: ROUTES.settingsProfile,
    titleKey: "sidebar.items.settings",
    icon: Settings,
    section: "settings",
  },
];

export const DEV_NAV_ITEMS: NavItemConfig[] = [
  {
    id: "tokens",
    href: ROUTES.devTokens,
    titleKey: "sidebar.items.tokens",
    icon: Palette,
    section: "dev",
  },
];
