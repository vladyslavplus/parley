import type { Metadata } from "next";
import { NextIntlClientProvider, hasLocale } from "next-intl";
import { getMessages, getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";
import type { ReactNode } from "react";

import { fontDisplay, fontMono, fontSans } from "@/app/fonts";
import { Providers } from "@/components/providers";
import { THEME_STORAGE_KEY } from "@/constants/theme";
import { routing } from "@/i18n/routing";
import { cn } from "@/lib/utils";

import "../globals.css";

const THEME_INIT_SCRIPT = `(()=>{try{var k=${JSON.stringify(THEME_STORAGE_KEY)},apply=()=>{var e=document.documentElement,t=localStorage.getItem(k),d=t==="dark"||((!t||t==="system")&&window.matchMedia("(prefers-color-scheme: dark)").matches),c=d?"dark":"light";if(!e.classList.contains(c)){e.classList.remove(d?"light":"dark");e.classList.add(c);}if(e.style.colorScheme!==c){e.style.colorScheme=c;}};window.__parleyApplyTheme=apply;apply();if(window.__parleyThemeObserver){window.__parleyThemeObserver.disconnect();}window.__parleyThemeObserver=new MutationObserver(()=>{var e=document.documentElement;if(!e.classList.contains("dark")&&!e.classList.contains("light")){apply();}});window.__parleyThemeObserver.observe(document.documentElement,{attributes:true,attributeFilter:["class"]});}catch{}})();`;

export function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({
    locale: locale as never,
    namespace: "common",
  });
  return {
    title: {
      default: t("appName"),
      template: `%s · ${t("appName")}`,
    },
    description: t("appDescription"),
  };
}

export default async function LocaleLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  const messages = await getMessages();

  return (
    <html
      lang={locale}
      className={cn(fontSans.variable, fontDisplay.variable, fontMono.variable)}
      suppressHydrationWarning
    >
      <head>
        <meta name="color-scheme" content="light dark" />
        <script dangerouslySetInnerHTML={{ __html: THEME_INIT_SCRIPT }} />
      </head>
      <body className="min-h-screen bg-background font-sans text-foreground antialiased">
        <NextIntlClientProvider locale={locale} messages={messages}>
          <Providers>{children}</Providers>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
