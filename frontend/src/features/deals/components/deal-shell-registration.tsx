"use client";

import { useLocale, useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";

import { LIMITS } from "@/constants/limits";
import { PALETTE_GROUPS } from "@/constants/palette-groups";
import { PERMISSIONS } from "@/constants/permissions";
import { dealRecordRoute, ROUTES } from "@/constants/routes";
import { DealQuickCreateDialog } from "@/features/deals/components/deal-quick-create-dialog";
import type { DealPage } from "@/features/deals/types";
import { useCan } from "@/features/session/components/can";
import { useRouter } from "@/i18n/navigation";
import { backendJson } from "@/lib/api/bff-client";
import {
  registerCommandSource,
  registerSearchSource,
  type CommandItem,
} from "@/lib/shortcuts/command-source";
import { registerCreateTarget } from "@/lib/shortcuts/create-target";

export function DealShellRegistration() {
  const router = useRouter();
  const locale = useLocale();
  const t = useTranslations("shell");
  const [quickCreateOpen, setQuickCreateOpen] = useState(false);
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const canReadDeals = useCan(PERMISSIONS.dealsRead);
  const canWriteDeals = useCan(PERMISSIONS.dealsWrite);

  // Quick create target registration
  useEffect(() => {
    if (!canWriteDeals) return;

    const unregister = registerCreateTarget({
      id: "deal",
      titleKey: "deal",
      action: () => setQuickCreateOpen(true),
    });

    return unregister;
  }, [canWriteDeals]);

  // Static commands in Command Palette
  useEffect(() => {
    const unregister = registerCommandSource(() => {
      const items: CommandItem[] = [];

      if (canReadDeals) {
        items.push({
          id: "crm-go-deals",
          title: t("commandPalette.goToDeals"),
          group: PALETTE_GROUPS.navigation,
          onSelect: () => router.push(ROUTES.deals),
        });
      }

      if (canWriteDeals) {
        items.push({
          id: "crm-create-deal",
          title: t("commandPalette.createDeal"),
          group: PALETTE_GROUPS.actions,
          onSelect: () => setQuickCreateOpen(true),
        });
      }

      return items;
    });

    return unregister;
  }, [canReadDeals, canWriteDeals, router, t]);

  // Async search source for deals in Palette
  useEffect(() => {
    if (!canReadDeals) return;

    const unregister = registerSearchSource(
      async (query: string, signal: AbortSignal): Promise<CommandItem[]> => {
        const trimmed = query.trim();
        if (trimmed.length < LIMITS.searchMinQueryLength) {
          return [];
        }

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

        try {
          const res = await backendJson<DealPage>("/deals", {
            method: "GET",
            query: { q: trimmed, limit: 5 },
            locale,
            signal,
          });

          return res.items.map((deal) => ({
            id: `search-deal-${deal.id}`,
            title: deal.title,
            subtitle: `${deal.stageName} · ${deal.companyName ?? ""}`.trim(),
            group: PALETTE_GROUPS.deals,
            onSelect: () => router.push(dealRecordRoute(deal.id)),
          }));
        } catch {
          return [];
        }
      },
    );

    return () => {
      if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
      unregister();
    };
  }, [canReadDeals, router, locale]);

  return (
    <DealQuickCreateDialog
      open={quickCreateOpen}
      onOpenChange={setQuickCreateOpen}
      onCreated={(id) => router.push(dealRecordRoute(id))}
    />
  );
}
