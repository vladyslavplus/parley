"use client";

import { useTranslations } from "next-intl";

interface Props {
  subject: string;
  body: string;
}

// The body is author-written HTML. An empty `sandbox` attribute gives the frame
// a unique origin with scripts, forms, popups and top navigation disabled, so
// nothing in a template can run in (or read from) the app origin. The frame
// always renders as a light page, like a mail client would.
export function TemplatePreviewFrame({ subject, body }: Props) {
  const t = useTranslations("campaigns.templates.preview");
  return (
    <div className="space-y-2">
      <p className="truncate text-xs text-muted-foreground">
        <span className="font-medium text-foreground">{t("subject")}: </span>
        {subject}
      </p>
      <iframe
        sandbox=""
        srcDoc={body}
        title={t("frameTitle")}
        referrerPolicy="no-referrer"
        loading="lazy"
        style={{ colorScheme: "light", backgroundColor: "Canvas" }}
        className="h-96 w-full rounded-[var(--radius-control)] border border-border"
      />
    </div>
  );
}
