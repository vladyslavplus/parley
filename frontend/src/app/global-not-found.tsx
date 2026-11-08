import type { Metadata } from "next";

import { GlobalStatusPage } from "@/components/ui/global-status-page";
import errorsEn from "../../messages/en/errors.json";

export const metadata: Metadata = {
  title: `${errorsEn.notFound.badge} - ${errorsEn.notFound.title}`,
  description: errorsEn.notFound.description,
};

export default function GlobalNotFound() {
  return <GlobalStatusPage status="404" />;
}
