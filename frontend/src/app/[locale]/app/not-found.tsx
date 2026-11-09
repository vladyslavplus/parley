import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { NotFoundView } from "@/components/ui/not-found-view";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("errors.notFound");
  return {
    title: t("title"),
  };
}

export default function AppNotFound() {
  return <NotFoundView inApp={true} />;
}
