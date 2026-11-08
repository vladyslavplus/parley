import { IntlErrorCode } from "next-intl";
import { getRequestConfig } from "next-intl/server";

import { loadMessages } from "./messages";
import { isAppLocale, routing } from "./routing";
import { reportMissingMessage, messageFallback } from "./missing";

export default getRequestConfig(async ({ requestLocale }) => {
  const requested = await requestLocale;
  const locale = isAppLocale(requested) ? requested : routing.defaultLocale;

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
