"use client";

import * as Popover from "@radix-ui/react-popover";
import {
  ArrowUpRight,
  ChevronDown,
  Copy,
  ListTodo,
  Mail,
  Phone,
} from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

import { Skeleton } from "@/components/ui/skeleton";
import { PERMISSIONS } from "@/constants/permissions";
import { contactRecordRoute } from "@/constants/routes";
import { useContact } from "@/features/crm/hooks/use-contact";
import { DueTasks } from "@/features/insights/components/due-tasks";
import { ACTION_CODES, actionLabelKey } from "@/features/insights/lib/codes";
import {
  channelsForAction,
  mailtoHref,
  telHref,
  type ContactChannel,
} from "@/features/insights/lib/contact-channels";
import type { PriorityItem } from "@/features/insights/types";
import { Can } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

const ICON_BUTTON =
  "inline-flex h-6 w-6 items-center justify-center rounded-md border border-border bg-background text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring";
const ADVICE_KEYS: Record<
  string,
  "advice.winBack" | "advice.reward" | "advice.nurture"
> = {
  [ACTION_CODES.winBack]: "advice.winBack",
  [ACTION_CODES.reward]: "advice.reward",
  [ACTION_CODES.nurture]: "advice.nurture",
};
const LABEL_TEXT = "text-xs font-medium text-foreground";
const LABEL_ACTION = cn(
  LABEL_TEXT,
  "inline-flex cursor-pointer items-center gap-1 rounded-sm underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
);

// The "Next step" cell. The label says what to do; for a call or an outreach
// step it opens the contact's phone and email without leaving the list. A
// review opens the record, and a hold offers no way to make contact, because the
// model advises against contacting the person now.
export function NextStepCell({
  item,
  onCreateTask,
}: {
  item: PriorityItem;
  onCreateTask: (item: PriorityItem) => void;
}) {
  const t = useTranslations("insights");
  const actionKey = actionLabelKey(item.suggestedAction);
  const label = actionKey ? t(actionKey) : "";
  const channels = channelsForAction(item.suggestedAction);
  const name = item.displayName;

  return (
    <div className="flex items-center justify-between gap-2">
      {channels.length > 0 ? (
        <ChannelPopover item={item} label={label} channels={channels} />
      ) : item.suggestedAction === ACTION_CODES.reviewDeal ? (
        <Link
          href={contactRecordRoute(item.contactId)}
          className={LABEL_ACTION}
        >
          {label}
        </Link>
      ) : (
        <span className={LABEL_TEXT}>{label}</span>
      )}
      <div className="flex items-center gap-1">
        <Link
          href={contactRecordRoute(item.contactId)}
          aria-label={`${t("home.priority.openContact")}: ${name}`}
          title={t("home.priority.openContact")}
          className={ICON_BUTTON}
        >
          <ArrowUpRight className="h-3.5 w-3.5" aria-hidden="true" />
        </Link>
        <Can permission={PERMISSIONS.tasksWrite}>
          <button
            type="button"
            aria-label={`${t("home.priority.createTask")}: ${name}`}
            title={t("home.priority.createTask")}
            className={cn(ICON_BUTTON, "cursor-pointer")}
            onClick={() => onCreateTask(item)}
          >
            <ListTodo className="h-3.5 w-3.5" aria-hidden="true" />
          </button>
        </Can>
      </div>
    </div>
  );
}

function ChannelPopover({
  item,
  label,
  channels,
}: {
  item: PriorityItem;
  label: string;
  channels: ContactChannel[];
}) {
  const [open, setOpen] = useState(false);
  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        <button type="button" className={LABEL_ACTION}>
          <span>{label}</span>
          <ChevronDown className="h-3 w-3" aria-hidden="true" />
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={6}
          className="z-50 w-72 max-w-[calc(100vw-2rem)] rounded-[var(--radius-card)] border border-border bg-popover p-3 text-xs text-popover-foreground shadow-[var(--shadow-floating)] focus:outline-none"
        >
          {/* Mounted only while open, so the contact and its tasks are read on demand. */}
          {open ? <PopoverBody item={item} channels={channels} /> : null}
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}

// What to do, then how to reach the person. A follow-up shows the task that is
// due; the other steps say in one line why they were suggested.
function PopoverBody({
  item,
  channels,
}: {
  item: PriorityItem;
  channels: ContactChannel[];
}) {
  const t = useTranslations("insights.home.priority.nextStep");
  const advice = ADVICE_KEYS[item.suggestedAction];
  const followUp = item.suggestedAction === ACTION_CODES.call;

  return (
    <div className="space-y-3">
      <p className="font-medium text-foreground">{item.displayName}</p>
      {followUp ? <DueTasks item={item} /> : null}
      {advice ? <p className="text-muted-foreground">{t(advice)}</p> : null}
      <div className="space-y-1.5">
        {followUp ? (
          <p className="font-medium text-foreground">{t("reachOut")}</p>
        ) : null}
        <ChannelList item={item} channels={channels} />
      </div>
    </div>
  );
}

function ChannelList({
  item,
  channels,
}: {
  item: PriorityItem;
  channels: ContactChannel[];
}) {
  const t = useTranslations("insights.home.priority.nextStep");
  const contact = useContact(item.contactId);

  if (contact.isPending) {
    return (
      <div className="space-y-2" aria-hidden="true">
        <Skeleton className="h-5 w-full" />
        <Skeleton className="h-5 w-full" />
      </div>
    );
  }
  if (!contact.data) {
    return <p className="text-muted-foreground">{t("loadFailed")}</p>;
  }

  const values: Record<ContactChannel, string | null> = {
    phone: contact.data.phone?.trim() || null,
    email: contact.data.email?.trim() || null,
  };
  const missing = channels.some((channel) => !values[channel]);

  return (
    <div className="space-y-2">
      <ul className="space-y-1.5">
        {channels.map((channel) => (
          <ChannelRow key={channel} channel={channel} value={values[channel]} />
        ))}
      </ul>
      {missing ? (
        <Link
          href={contactRecordRoute(item.contactId)}
          className="inline-block rounded-sm text-info underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
        >
          {t("addInContact")}
        </Link>
      ) : null}
    </div>
  );
}

function ChannelRow({
  channel,
  value,
}: {
  channel: ContactChannel;
  value: string | null;
}) {
  const t = useTranslations("insights.home.priority.nextStep");
  const Icon = channel === "phone" ? Phone : Mail;
  const href = value
    ? channel === "phone"
      ? telHref(value)
      : mailtoHref(value)
    : null;
  const what = t(channel === "phone" ? "phone" : "email");

  async function copy() {
    if (!value) return;
    try {
      await navigator.clipboard.writeText(value);
      showSuccessToast(t("copied", { what }));
    } catch {
      showErrorToast(t("copyFailed"));
    }
  }

  return (
    <li className="flex items-center gap-2">
      <Icon
        className="h-3.5 w-3.5 shrink-0 text-muted-foreground"
        aria-hidden="true"
      />
      {value ? (
        <>
          {href ? (
            <a
              href={href}
              className="min-w-0 flex-1 truncate rounded-sm font-medium text-foreground underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
            >
              {value}
            </a>
          ) : (
            <span className="min-w-0 flex-1 truncate">{value}</span>
          )}
          <button
            type="button"
            aria-label={t("copy", { what })}
            title={t("copy", { what })}
            className={cn(ICON_BUTTON, "shrink-0 cursor-pointer")}
            onClick={() => void copy()}
          >
            <Copy className="h-3.5 w-3.5" aria-hidden="true" />
          </button>
        </>
      ) : (
        <span className="text-muted-foreground">
          {t(channel === "phone" ? "noPhone" : "noEmail")}
        </span>
      )}
    </li>
  );
}
