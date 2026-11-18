"use client";

import { parseAsBoolean, useQueryState } from "nuqs";

export const CONTACT_CREATE_PARAM = "new";

/**
 * One definition for both directions: the contacts page reads the dialog state
 * with this parser, and `contactCreateHref` writes it with the same parser.
 * nuqs booleans are `true`/`false` in the URL, so a hand-written `?new=1` would
 * read as closed and the deep link would silently do nothing.
 */
export const contactCreateParser = parseAsBoolean.withDefault(false);

/**
 * Open state of the create-contact dialog, kept in the URL so a deep link opens
 * it. Closing the dialog writes the default value, which clears the parameter.
 */
export function useContactCreateParam() {
  return useQueryState(CONTACT_CREATE_PARAM, contactCreateParser);
}

/** Contacts-list URL with the create dialog already open. */
export function contactCreateHref(base: string): string {
  const value = contactCreateParser.serialize(true);
  return `${base}?${CONTACT_CREATE_PARAM}=${value}`;
}
