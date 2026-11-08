import { notFound } from "next/navigation";
import { IntlErrorCode } from "next-intl";
import { getRequestConfig } from "next-intl/server";
import { locale as rootLocale } from "next/root-params";

import { loadMessages } from "./messages";
import { isAppLocale } from "./routing";
import { reportMissingMessage, messageFallback } from "./missing";

export default getRequestConfig(async ({ locale: explicitLocale }) => {
  let locale = explicitLocale;
  if (!locale) {
    const v = await rootLocale();
    locale = isAppLocale(v) ? v : notFound();
  }

  return {
    locale,
    messages: await loadMessages(locale),
    onError(error) {
      if (error.code === IntlErrorCode.MISSING_MESSAGE) {
        reportMissingMessage(error);
        return;
      }
      throw error;
    },
    getMessageFallback: messageFallback,
  };
});
