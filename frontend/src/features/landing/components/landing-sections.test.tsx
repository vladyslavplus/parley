import { cleanup, render, screen, within } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import type { ReactElement } from "react";
import { afterEach, beforeAll, describe, expect, it } from "vitest";

import landingEn from "../../../../messages/en/landing.json";
import { HeroSection } from "./hero/hero-section";
import { CampaignSection } from "./sections/campaign-section";
import { CtaSection } from "./sections/cta-section";
import { FieldSection } from "./sections/field-section";
import { HonestSection } from "./sections/honest-section";
import { PrioritySection } from "./sections/priority-section";
import { ResultsSection } from "./sections/results-section";
import { RolesSection } from "./sections/roles-section";
import { SameEmailSection } from "./sections/same-email-section";
import { TelegramSection } from "./sections/telegram-section";

const messages = { landing: landingEn };

// Reduced motion: every section must render its static scene.
beforeAll(() => {
  window.matchMedia = ((query: string) => ({
    matches: true,
    media: query,
    addEventListener: () => {},
    removeEventListener: () => {},
  })) as unknown as typeof window.matchMedia;
});

afterEach(cleanup);

function show(element: ReactElement) {
  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      {element}
    </NextIntlClientProvider>,
  );
}

describe("landing sections, static scene", () => {
  it("hero offers sign-up and sign-in to visitors, the app to members", () => {
    show(<HeroSection signedIn={false} />);
    expect(
      screen.getByRole("heading", { level: 1, name: landingEn.hero.title }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: landingEn.nav.createWorkspace }),
    ).toHaveAttribute("href", "/en/register");
    expect(
      screen.getByRole("link", { name: landingEn.nav.signIn }),
    ).toHaveAttribute("href", "/en/login");
    cleanup();

    show(<HeroSection signedIn />);
    expect(
      screen.getByRole("link", { name: landingEn.nav.openApp }),
    ).toHaveAttribute("href", "/en/app");
    expect(
      screen.queryByRole("link", { name: landingEn.nav.signIn }),
    ).toBeNull();
  });

  it("field shows the settled lit scene, with a text alternative, and no canvas", () => {
    const { container } = show(<FieldSection />);
    expect(screen.getByText(landingEn.field.alt)).toBeInTheDocument();
    expect(container.querySelector("canvas")).toBeNull();
    expect(
      container.querySelectorAll("svg path.fill-highlight").length,
    ).toBeGreaterThan(0);
    expect(
      screen.getByText(landingEn.field.legend.holdout.term),
    ).toBeInTheDocument();
  });

  it.each([
    ["same email", SameEmailSection, landingEn.sameEmail.title],
    ["priority", PrioritySection, landingEn.priority.title],
    ["campaign", CampaignSection, landingEn.campaign.title],
    ["results", ResultsSection, landingEn.results.title],
    ["telegram", TelegramSection, landingEn.telegram.title],
    ["roles", RolesSection, landingEn.roles.title],
    ["honest", HonestSection, landingEn.honest.title],
  ] as const)("%s section renders its heading", (_, Section, title) => {
    show(<Section />);
    const heading = screen.getByRole("heading", { level: 2, name: title });
    expect(within(heading.closest("section")!).getByText(title)).toBe(heading);
  });

  it("roles lists the five system roles only", () => {
    show(<RolesSection />);
    const list = screen.getByRole("list", { name: landingEn.roles.listLabel });
    expect(within(list).getAllByRole("listitem")).toHaveLength(5);
    for (const name of [
      landingEn.roles.admin,
      landingEn.roles.salesManager,
      landingEn.roles.salesRep,
      landingEn.roles.marketer,
      landingEn.roles.viewer,
    ]) {
      expect(within(list).getByText(name)).toBeInTheDocument();
    }
  });

  it("telegram reply mirrors the bot: next actions and links, no contact names", () => {
    show(<TelegramSection />);
    const chat = screen.getByRole("figure", {
      name: landingEn.telegram.chatLabel,
    });
    expect(within(chat).getAllByRole("listitem")).toHaveLength(3);
    expect(
      within(chat).getByText(landingEn.telegram.items.call),
    ).toBeInTheDocument();
    expect(
      within(chat).getAllByText(landingEn.telegram.openRecord),
    ).toHaveLength(3);
    expect(
      within(chat).getByText(landingEn.telegram.openInsights),
    ).toBeInTheDocument();
    expect(chat).not.toHaveTextContent(landingEn.mock.rows.first.name);
  });

  it("final call to action follows the session", () => {
    show(<CtaSection signedIn />);
    expect(screen.getByText(landingEn.cta.signedInBody)).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: landingEn.nav.openApp }),
    ).toBeInTheDocument();
  });
});
