export const AUDIT_ACTIONS = [
  "identity.member.role_changed",
  "identity.member.team_changed",
  "identity.member.deactivated",
  "identity.member.reactivated",
  "identity.invitation.created",
  "identity.invitation.accepted",
  "identity.invitation.revoked",
  "identity.team.created",
  "identity.team.renamed",
  "identity.team.deleted",
  "identity.organization.settings_changed",
  "identity.session.sign_in_failed",
  "identity.session.account_locked",
  "crm.contact.deleted",
  "crm.contact.restored",
  "crm.company.deleted",
  "crm.company.restored",
  "crm.contacts.exported",
] as const;

export type AuditActionCode = (typeof AUDIT_ACTIONS)[number];

export function auditActionMessageKey(code: string) {
  return code.replaceAll(".", "_");
}
