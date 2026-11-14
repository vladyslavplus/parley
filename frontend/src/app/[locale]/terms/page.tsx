import type { Metadata } from "next";
import { notFound } from "next/navigation";

import { LegalPage } from "@/features/landing/components/legal/legal-page";
import { publicPageMetadata } from "@/features/landing/lib/landing-metadata";
import { isAppLocale } from "@/i18n/routing";

type Params = Promise<{ locale: string }>;

export async function generateMetadata({
  params,
}: {
  params: Params;
}): Promise<Metadata> {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  return publicPageMetadata(locale, "terms");
}

export default async function TermsPage({ params }: { params: Params }) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  return <LegalPage document="terms" />;
}
