"use client";

import { useTranslations } from "next-intl";

import {
  HintLead,
  HintList,
  HintNote,
  HintParagraph,
  InfoHint,
  type HintItem,
} from "@/components/ui/info-hint";

// The hints of the campaign area. Every term in a list is the label the
// interface shows for that thing (read from the same messages as the table, the
// badge or the control), so a hint and its label cannot disagree. The wording
// lives in `hints.campaignHints`; this file only lays the pieces out.

function HintBody({
  lead,
  intro = [],
  items = [],
  after = [],
  notes = [],
}: {
  lead: string;
  intro?: string[];
  items?: HintItem[];
  after?: string[];
  notes?: string[];
}) {
  return (
    <InfoHint size="wide">
      <HintLead>{lead}</HintLead>
      {intro.map((text) => (
        <HintParagraph key={text}>{text}</HintParagraph>
      ))}
      {items.length > 0 ? <HintList items={items} /> : null}
      {after.map((text) => (
        <HintParagraph key={text}>{text}</HintParagraph>
      ))}
      {notes.map((text) => (
        <HintNote key={text}>{text}</HintNote>
      ))}
    </InfoHint>
  );
}

export function HoldoutHint() {
  const t = useTranslations("hints.campaignHints.holdout");
  const tUi0 = useTranslations("campaigns.recipients.arm");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: tUi0("treatment"), text: t("items.email.text") },
        { term: tUi0("holdout"), text: t("items.holdout.text") },
      ]}
      after={[t("after1")]}
      notes={[t("note1")]}
    />
  );
}

export function HoldoutSizeHint() {
  const t = useTranslations("hints.campaignHints.holdoutSize");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: t("items.smaller.term"), text: t("items.smaller.text") },
        { term: t("items.larger.term"), text: t("items.larger.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function MdeHint() {
  const t = useTranslations("hints.campaignHints.mde");
  return (
    <HintBody lead={t("lead")} intro={[t("intro1")]} notes={[t("note1")]} />
  );
}

export function SrmHint() {
  const t = useTranslations("hints.campaignHints.srm");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: t("items.ok.term"), text: t("items.ok.text") },
        { term: t("items.alarm.term"), text: t("items.alarm.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function TemplateHint() {
  const t = useTranslations("hints.campaignHints.template");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: t("items.variables.term"), text: t("items.variables.text") },
        { term: t("items.preview.term"), text: t("items.preview.text") },
        { term: t("items.test.term"), text: t("items.test.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function VariablesHint() {
  const t = useTranslations("hints.campaignHints.variables");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: t("items.firstName.term"), text: t("items.firstName.text") },
        { term: t("items.lastName.term"), text: t("items.lastName.text") },
        { term: t("items.company.term"), text: t("items.company.text") },
        {
          term: t("items.unsubscribe.term"),
          text: t("items.unsubscribe.text"),
        },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function AudienceHint() {
  const t = useTranslations("hints.campaignHints.audience");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: t("items.segment.term"), text: t("items.segment.text") },
        { term: t("items.everyone.term"), text: t("items.everyone.text") },
      ]}
      notes={[t("note1"), t("note2")]}
    />
  );
}

export function TargetingHint() {
  const t = useTranslations("hints.campaignHints.targeting");
  const tUi0 = useTranslations("campaigns.builder.targeting.modes");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: tUi0("all.title"), text: t("items.all.text") },
        { term: tUi0("top_uplift.title"), text: t("items.uplift.text") },
        { term: tUi0("top_rfm.title"), text: t("items.rfm.text") },
      ]}
      notes={[t("note1"), t("note2")]}
    />
  );
}

export function TargetedHint() {
  const t = useTranslations("hints.campaignHints.targeted");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: t("items.emailYes.term"), text: t("items.emailYes.text") },
        { term: t("items.emailNo.term"), text: t("items.emailNo.text") },
        { term: t("items.holdYes.term"), text: t("items.holdYes.text") },
        { term: t("items.holdNo.term"), text: t("items.holdNo.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function PrepareHint() {
  const t = useTranslations("hints.campaignHints.prepare");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: t("items.audience.term"), text: t("items.audience.text") },
        { term: t("items.groups.term"), text: t("items.groups.text") },
        { term: t("items.targeting.term"), text: t("items.targeting.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function StartSendingHint() {
  const t = useTranslations("hints.campaignHints.startSending");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: t("items.who.term"), text: t("items.who.text") },
        { term: t("items.unsub.term"), text: t("items.unsub.text") },
        { term: t("items.once.term"), text: t("items.once.text") },
        { term: t("items.after.term"), text: t("items.after.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function StatusHint() {
  const t = useTranslations("hints.campaignHints.status");
  const tUi0 = useTranslations("campaigns.status");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: tUi0("draft"), text: t("items.draft.text") },
        { term: tUi0("preparing"), text: t("items.preparing.text") },
        { term: tUi0("ready"), text: t("items.ready.text") },
        { term: tUi0("sending"), text: t("items.sending.text") },
        { term: tUi0("completed"), text: t("items.completed.text") },
        { term: tUi0("failed"), text: t("items.failed.text") },
        { term: tUi0("cancelled"), text: t("items.cancelled.text") },
      ]}
    />
  );
}

export function ProgressHint() {
  const t = useTranslations("hints.campaignHints.progress");
  return <HintBody lead={t("lead")} notes={[t("note1")]} />;
}

export function RecipientsHint() {
  const t = useTranslations("hints.campaignHints.recipients");
  return <HintBody lead={t("lead")} notes={[t("note1")]} />;
}

export function SendStatusHint() {
  const t = useTranslations("hints.campaignHints.sendStatus");
  const tUi0 = useTranslations("campaigns.recipients.sendStatus");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: tUi0("claimed"), text: t("items.claimed.text") },
        { term: tUi0("sent"), text: t("items.sent.text") },
        { term: tUi0("retryable_failed"), text: t("items.retry.text") },
        { term: tUi0("failed"), text: t("items.failed.text") },
        { term: tUi0("skipped_suppressed"), text: t("items.suppressed.text") },
        { term: tUi0("unknown"), text: t("items.unknown.text") },
        { term: tUi0("demo"), text: t("items.demo.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function ConversionColumnHint() {
  const t = useTranslations("hints.campaignHints.conversionColumn");
  const tUi0 = useTranslations("campaigns.recipients.conversion");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: tUi0("converted"), text: t("items.converted.text") },
        { term: tUi0("not_converted"), text: t("items.none.text") },
        { term: tUi0("pending"), text: t("items.pending.text") },
        { term: tUi0("not_targeted"), text: t("items.notTargeted.text") },
        { term: tUi0("not_visible"), text: t("items.hidden.text") },
        { term: tUi0("deleted"), text: t("items.deleted.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function ConversionRateHint({ days }: { days: number }) {
  const t = useTranslations("hints.campaignHints.conversionRate");
  const tUi0 = useTranslations("campaigns.results.table");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: tUi0("targeted"), text: t("items.people.text") },
        { term: tUi0("converted"), text: t("items.converted.text", { days }) },
        { term: tUi0("rate"), text: t("items.rate.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function DifferenceHint() {
  const t = useTranslations("hints.campaignHints.difference");
  return (
    <HintBody
      lead={t("lead")}
      intro={[t("intro1")]}
      items={[
        { term: t("items.positive.term"), text: t("items.positive.text") },
        { term: t("items.zero.term"), text: t("items.zero.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function IntervalHint() {
  const t = useTranslations("hints.campaignHints.interval");
  return (
    <HintBody
      lead={t("lead")}
      intro={[t("intro1")]}
      items={[
        { term: t("items.zero.term"), text: t("items.zero.text") },
        { term: t("items.above.term"), text: t("items.above.text") },
        { term: t("items.below.term"), text: t("items.below.text") },
      ]}
    />
  );
}

export function PValueHint() {
  const t = useTranslations("hints.campaignHints.pValue");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        { term: t("items.small.term"), text: t("items.small.text") },
        { term: t("items.large.term"), text: t("items.large.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function RevenueDifferenceHint() {
  const t = useTranslations("hints.campaignHints.revenueDifference");
  return <HintBody lead={t("lead")} notes={[t("note1")]} />;
}

export function AttributionWindowHint({ days }: { days: number }) {
  const t = useTranslations("hints.campaignHints.attributionWindow");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        {
          term: t("items.length.term"),
          text: t("items.length.text", { days }),
        },
        { term: t("items.open.term"), text: t("items.open.text") },
        { term: t("items.closed.term"), text: t("items.closed.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function ProvisionalHint() {
  const t = useTranslations("hints.campaignHints.provisional");
  return (
    <HintBody
      lead={t("lead")}
      items={[
        {
          term: t("items.provisional.term"),
          text: t("items.provisional.text"),
        },
        { term: t("items.final.term"), text: t("items.final.text") },
      ]}
      notes={[t("note1")]}
    />
  );
}

export function SimulatedHint() {
  const t = useTranslations("hints.campaignHints.simulated");
  return <HintBody lead={t("lead")} notes={[t("note1")]} />;
}
