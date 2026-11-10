"use client";

import { Mail, Send } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { ERROR_CODES } from "@/constants/error-codes";
import { PERMISSIONS } from "@/constants/permissions";
import { useCampaignReview } from "@/features/campaigns/hooks/use-campaign-api";
import { useCampaignSending } from "@/features/campaigns/hooks/use-campaign-delivery";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { AttemptKey } from "@/features/campaigns/lib/attempt-key";
import { CAMPAIGN_STATUSES, toNumber } from "@/features/campaigns/types";
import { useCan } from "@/features/session/components/can";
import { toApiError } from "@/lib/errors";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";

// "Start sending" and the test email of a campaign that is ready. Both need
// `campaigns.send`; the server decides, this only hides what cannot work.
export function SendControls({
  campaignId,
  status,
}: {
  campaignId: string;
  status: string;
}) {
  const t = useTranslations("campaigns.send");
  const errorText = useErrorText();
  const canSend = useCan(PERMISSIONS.campaignsSend);
  const sending = useCampaignSending(campaignId);
  const review = useCampaignReview(campaignId, status === "ready", false);
  const [attempt] = useState(() => new AttemptKey());
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [testPending, setTestPending] = useState(false);

  if (!canSend || status !== CAMPAIGN_STATUSES.ready) return null;

  const recipients = review.data
    ? toNumber(review.data.treatment.targetedCount)
    : null;

  async function sendTest() {
    setTestPending(true);
    try {
      await sending.sendTest();
      showSuccessToast(t("testSent"));
    } catch (error) {
      showErrorToast(
        toApiError(error).code === ERROR_CODES.testSendRateLimited
          ? t("testRateLimited")
          : errorText(error),
      );
    } finally {
      setTestPending(false);
    }
  }

  async function confirmStart() {
    // A failed try keeps its key, so confirming again is a retry of the same
    // attempt; success or closing the dialog ends the attempt.
    await sending.start(attempt.next());
    attempt.finish();
    showSuccessToast(t("started"));
  }

  return (
    <>
      <div className="flex flex-wrap items-center gap-2">
        <Button
          type="button"
          variant="primary"
          className="gap-1.5 px-3 text-xs"
          onClick={() => setConfirmOpen(true)}
        >
          <Send className="h-4 w-4" />
          <span>{t("start")}</span>
        </Button>
        <Button
          type="button"
          variant="secondary"
          className="gap-1.5 px-3 text-xs"
          disabled={testPending}
          onClick={() => void sendTest()}
        >
          <Mail className="h-4 w-4" />
          <span>{testPending ? t("testSending") : t("test")}</span>
        </Button>
      </div>
      <p className="text-xs text-muted-foreground">{t("testHint")}</p>

      <ConfirmDialog
        open={confirmOpen}
        onOpenChange={(open) => {
          if (!open) attempt.finish();
          setConfirmOpen(open);
        }}
        destructive={false}
        title={t("confirm.title")}
        description={
          recipients === null
            ? t("confirm.descriptionUnknown")
            : t("confirm.description", { count: recipients })
        }
        confirmLabel={t("confirm.action")}
        onConfirm={confirmStart}
      />
    </>
  );
}
