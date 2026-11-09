"use client";

import {
  Activity,
  CheckSquare,
  DollarSign,
  Package,
  TrendingUp,
} from "lucide-react";
import { useTranslations } from "next-intl";

import { EmptyState } from "@/components/ui/empty-state";

export function TimelinePlaceholder() {
  const t = useTranslations("crm.contacts.record.placeholders.timeline");
  return (
    <EmptyState
      icon={Activity}
      title={t("title")}
      description={t("description")}
    />
  );
}

export function TasksPlaceholder() {
  const t = useTranslations("crm.contacts.record.placeholders.tasks");
  return (
    <EmptyState
      icon={CheckSquare}
      title={t("title")}
      description={t("description")}
    />
  );
}

export function DealsPlaceholder() {
  const t = useTranslations("crm.contacts.record.placeholders.deals");
  return (
    <EmptyState
      icon={DollarSign}
      title={t("title")}
      description={t("description")}
    />
  );
}

export function OrdersPlaceholder() {
  const t = useTranslations("crm.contacts.record.placeholders.orders");
  return (
    <EmptyState
      icon={Package}
      title={t("title")}
      description={t("description")}
    />
  );
}

export function ScoresPlaceholder() {
  const t = useTranslations("crm.contacts.record.placeholders.scores");
  return (
    <EmptyState
      icon={TrendingUp}
      title={t("title")}
      description={t("description")}
    />
  );
}
