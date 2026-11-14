import { HeroSection } from "./hero/hero-section";
import { LandingFooter } from "./layout/landing-footer";
import { LandingHeader } from "./layout/landing-header";
import { LandingSkipLink } from "./layout/skip-link";
import { CampaignSection } from "./sections/campaign-section";
import { CtaSection } from "./sections/cta-section";
import { FieldSection } from "./sections/field-section";
import { HonestSection } from "./sections/honest-section";
import { PrioritySection } from "./sections/priority-section";
import { ResultsSection } from "./sections/results-section";
import { RolesSection } from "./sections/roles-section";
import { SameEmailSection } from "./sections/same-email-section";
import { TelegramSection } from "./sections/telegram-section";

export const LANDING_MAIN_ID = "landing-main";

export function LandingPage({ signedIn }: { signedIn: boolean }) {
  return (
    <div className="selection:bg-highlight selection:text-highlight-foreground">
      <LandingSkipLink targetId={LANDING_MAIN_ID} />
      <LandingHeader signedIn={signedIn} />
      <main id={LANDING_MAIN_ID} tabIndex={-1} className="outline-none">
        <HeroSection signedIn={signedIn} />
        <SameEmailSection />
        <FieldSection />
        <PrioritySection />
        <CampaignSection />
        <ResultsSection />
        <TelegramSection />
        <RolesSection />
        <HonestSection />
        <CtaSection signedIn={signedIn} />
      </main>
      <LandingFooter />
    </div>
  );
}
