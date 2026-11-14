import type { Metadata } from "next";
import { cookies } from "next/headers";
import { notFound } from "next/navigation";

import { SESSION_COOKIES } from "@/constants/session";
import { LandingPage } from "@/features/landing/components/landing-page";
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
  return publicPageMetadata(locale, "home");
}

export default async function HomePage({ params }: { params: Params }) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const jar = await cookies();
  const signedIn =
    jar.has(SESSION_COOKIES.access) || jar.has(SESSION_COOKIES.refresh);

  return <LandingPage signedIn={signedIn} />;
}
