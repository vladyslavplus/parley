import { ACTION_CODES } from "@/features/insights/lib/codes";

// How a suggested next step reaches the contact. The list row carries no phone
// or email, so the popover reads them from the contact when it opens.

export type ContactChannel = "phone" | "email";

/** The channels a step offers, best first; empty when it offers none. */
export function channelsForAction(action: string): ContactChannel[] {
  switch (action) {
    case ACTION_CODES.call:
      return ["phone", "email"];
    case ACTION_CODES.winBack:
    case ACTION_CODES.reward:
    case ACTION_CODES.nurture:
      return ["email", "phone"];
    // A review opens the record, and a hold never makes contact easier: the
    // model advises against contacting the person now.
    default:
      return [];
  }
}

/** A `tel:` link keeps the digits and a leading plus; a value without digits has no link. */
export function telHref(phone: string): string | null {
  const dialable = phone.replace(/[^\d+]/g, "");
  return /\d/.test(dialable) ? `tel:${dialable}` : null;
}

export function mailtoHref(email: string): string | null {
  const trimmed = email.trim();
  return trimmed ? `mailto:${trimmed}` : null;
}
