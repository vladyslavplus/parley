export type RecipientEmail =
  { kind: "address"; address: string } | { kind: "hidden" } | { kind: "none" };

// The address is decided by `emailVisible`, never by a null address alone: a
// null address with visibility on means the contact has none, while a hidden
// address is null for callers without organization-wide contact access.
export function recipientEmail(row: {
  emailVisible: boolean;
  email: string | null;
}): RecipientEmail {
  if (!row.emailVisible) return { kind: "hidden" };
  return row.email ? { kind: "address", address: row.email } : { kind: "none" };
}
