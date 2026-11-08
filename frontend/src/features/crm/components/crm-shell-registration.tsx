"use client";

import { useLocale, useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";

import { LIMITS } from "@/constants/limits";
import { PALETTE_GROUPS } from "@/constants/palette-groups";
import { PERMISSIONS } from "@/constants/permissions";
import {
  companyRecordRoute,
  contactRecordRoute,
  ROUTES,
} from "@/constants/routes";
import { CompanyQuickCreateDialog } from "@/features/crm/components/company-quick-create-dialog";
import { ContactQuickCreateDialog } from "@/features/crm/components/contact-quick-create-dialog";
import { useCan } from "@/features/session/components/can";
import { useRouter } from "@/i18n/navigation";
import { backendJson } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import {
  registerCommandSource,
  registerSearchSource,
  type CommandItem,
} from "@/lib/shortcuts/command-source";
import { registerCreateTarget } from "@/lib/shortcuts/create-target";

export function CrmShellRegistration() {
  const router = useRouter();
  const locale = useLocale();
  const t = useTranslations("shell");
  const [contactOpen, setContactOpen] = useState(false);
  const [companyOpen, setCompanyOpen] = useState(false);
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const canReadContacts = useCan(PERMISSIONS.contactsRead);
  const canWriteContacts = useCan(PERMISSIONS.contactsWrite);
  const canReadCompanies = useCan(PERMISSIONS.companiesRead);
  const canWriteCompanies = useCan(PERMISSIONS.companiesWrite);

  // Register Quick Create targets
  useEffect(() => {
    const unregisterFns: (() => void)[] = [];

    if (canWriteContacts) {
      unregisterFns.push(
        registerCreateTarget({
          id: "contact",
          titleKey: "contact",
          action: () => setContactOpen(true),
        }),
      );
    }

    if (canWriteCompanies) {
      unregisterFns.push(
        registerCreateTarget({
          id: "company",
          titleKey: "company",
          action: () => setCompanyOpen(true),
        }),
      );
    }

    return () => {
      unregisterFns.forEach((fn) => fn());
    };
  }, [canWriteContacts, canWriteCompanies]);

  // Register Static Commands in Palette
  useEffect(() => {
    const unregister = registerCommandSource(() => {
      const items: CommandItem[] = [];

      if (canReadContacts) {
        items.push({
          id: "crm-go-contacts",
          title: t("commandPalette.goToContacts"),
          group: PALETTE_GROUPS.navigation,
          onSelect: () => router.push(ROUTES.contacts),
        });
      }

      if (canReadCompanies) {
        items.push({
          id: "crm-go-companies",
          title: t("commandPalette.goToCompanies"),
          group: PALETTE_GROUPS.navigation,
          onSelect: () => router.push(ROUTES.companies),
        });
      }

      if (canWriteContacts) {
        items.push({
          id: "crm-create-contact",
          title: t("commandPalette.createContact"),
          group: PALETTE_GROUPS.actions,
          shortcut: "C",
          onSelect: () => setContactOpen(true),
        });
      }

      if (canWriteCompanies) {
        items.push({
          id: "crm-create-company",
          title: t("commandPalette.createCompany"),
          group: PALETTE_GROUPS.actions,
          onSelect: () => setCompanyOpen(true),
        });
      }

      return items;
    });

    return unregister;
  }, [
    canReadContacts,
    canReadCompanies,
    canWriteContacts,
    canWriteCompanies,
    router,
    t,
  ]);

  // Register Async Search Source in Palette
  useEffect(() => {
    const unregister = registerSearchSource(
      async (query: string, signal: AbortSignal): Promise<CommandItem[]> => {
        const trimmed = query.trim();
        if (trimmed.length < LIMITS.searchMinQueryLength) {
          return [];
        }

        if (!canReadContacts && !canReadCompanies) {
          return [];
        }

        // Debounce search
        await new Promise<void>((resolve, reject) => {
          if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
          const onAbort = () => {
            if (debounceTimerRef.current)
              clearTimeout(debounceTimerRef.current);
            reject(new DOMException("Aborted", "AbortError"));
          };
          signal.addEventListener("abort", onAbort, { once: true });
          debounceTimerRef.current = setTimeout(() => {
            signal.removeEventListener("abort", onAbort);
            resolve();
          }, LIMITS.searchDebounceMs);
        });

        const res = await backendJson<Schemas["SearchResponse"]>("/search", {
          method: "GET",
          query: { q: trimmed },
          locale,
          signal,
        });

        const items: CommandItem[] = [];
        for (const hit of res.items) {
          if (hit.kind === "contact" && canReadContacts) {
            items.push({
              id: `search-contact-${hit.id}`,
              title: hit.title,
              subtitle: hit.subtitle,
              group: PALETTE_GROUPS.contacts,
              onSelect: () => router.push(contactRecordRoute(hit.id)),
            });
          } else if (hit.kind === "company" && canReadCompanies) {
            items.push({
              id: `search-company-${hit.id}`,
              title: hit.title,
              subtitle: hit.subtitle,
              group: PALETTE_GROUPS.companies,
              onSelect: () => router.push(companyRecordRoute(hit.id)),
            });
          }
        }
        return items;
      },
    );

    return () => {
      if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
      unregister();
    };
  }, [canReadContacts, canReadCompanies, router, locale]);

  return (
    <>
      <ContactQuickCreateDialog
        open={contactOpen}
        onOpenChange={setContactOpen}
        onCreated={(id) => router.push(contactRecordRoute(id))}
      />
      <CompanyQuickCreateDialog
        open={companyOpen}
        onOpenChange={setCompanyOpen}
        onCreated={(id) => router.push(companyRecordRoute(id))}
      />
    </>
  );
}
