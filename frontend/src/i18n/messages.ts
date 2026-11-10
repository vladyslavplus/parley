import type { AppLocale } from "./routing";

export const MESSAGE_NAMESPACES = [
  "common",
  "errors",
  "validation",
  "auth",
  "settings",
  "dev",
  "shell",
  "table",
  "forms",
  "crm",
  "campaigns",
  "insights",
] as const;

export type MessageNamespace = (typeof MESSAGE_NAMESPACES)[number];

export async function loadMessages(locale: AppLocale) {
  const entries = await Promise.all(
    MESSAGE_NAMESPACES.map(
      async (namespace) =>
        [
          namespace,
          (await import(`../../messages/${locale}/${namespace}.json`)).default,
        ] as const,
    ),
  );
  return Object.fromEntries(entries) as Messages;
}

import type common from "../../messages/en/common.json";
import type errors from "../../messages/en/errors.json";
import type validation from "../../messages/en/validation.json";
import type auth from "../../messages/en/auth.json";
import type settings from "../../messages/en/settings.json";
import type dev from "../../messages/en/dev.json";
import type shell from "../../messages/en/shell.json";
import type table from "../../messages/en/table.json";
import type forms from "../../messages/en/forms.json";
import type crm from "../../messages/en/crm.json";
import type campaigns from "../../messages/en/campaigns.json";
import type insights from "../../messages/en/insights.json";

export type Messages = {
  common: typeof common;
  errors: typeof errors;
  validation: typeof validation;
  auth: typeof auth;
  settings: typeof settings;
  dev: typeof dev;
  shell: typeof shell;
  table: typeof table;
  forms: typeof forms;
  crm: typeof crm;
  campaigns: typeof campaigns;
  insights: typeof insights;
};
