export const ROUTES = {
  home: "/",
  login: "/login",
  register: "/register",
  checkEmail: "/check-email",
  confirmEmail: "/confirm-email",
  forgotPassword: "/forgot-password",
  resetPassword: "/reset-password",
  acceptInvitation: "/accept-invitation",
  app: "/app",
  contacts: "/app/contacts",
  companies: "/app/companies",
  deals: "/app/deals",
  tasks: "/app/tasks",
  settingsProfile: "/app/settings/profile",
  settingsOrganization: "/app/settings/organization",
  settingsMembers: "/app/settings/members",
  settingsTeams: "/app/settings/teams",
  settingsPipeline: "/app/settings/pipeline",
  settingsAudit: "/app/settings/audit",
  settingsCustomFields: "/app/settings/custom-fields",
  devTokens: "/dev/tokens",
} as const;

export type Route = (typeof ROUTES)[keyof typeof ROUTES];

export const API_ROUTES = {
  register: "/api/auth/register",
  confirmEmail: "/api/auth/confirm-email",
  resendConfirmation: "/api/auth/resend-confirmation",
  login: "/api/auth/login",
  logout: "/api/auth/logout",
  refresh: "/api/auth/refresh",
  forgotPassword: "/api/auth/forgot-password",
  resetPassword: "/api/auth/reset-password",
  switchOrganization: "/api/auth/switch-organization",
  acceptInvitation: "/api/auth/accept-invitation",
  backend: "/api/backend",
} as const;

export const BACKEND_PATHS = {
  me: "/me",
  contacts: "/contacts",
  companies: "/companies",
  deals: "/deals",
  pipelines: "/pipelines",
  tasks: "/tasks",
  notes: "/notes",
  activities: "/activities",
  orders: "/orders",
  tags: "/tags",
  customFields: "/custom-fields",
} as const;

export const ROUTE_PARAMS = {
  returnUrl: "returnUrl",
  email: "email",
  userId: "userId",
  token: "token",
} as const;

export const APP_AREA_SEGMENT = "app";

export function contactRecordRoute(id: string): string {
  return `${ROUTES.contacts}/${id}`;
}

export function companyRecordRoute(id: string): string {
  return `${ROUTES.companies}/${id}`;
}

export function dealRecordRoute(id: string): string {
  return `${ROUTES.deals}/${id}`;
}

export function contactExportDownloadRoute(id: string): string {
  return `${API_ROUTES.backend}/contacts/exports/${encodeURIComponent(id)}/download`;
}
