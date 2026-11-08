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
  settingsProfile: "/app/settings/profile",
  settingsOrganization: "/app/settings/organization",
  settingsMembers: "/app/settings/members",
  settingsTeams: "/app/settings/teams",
  settingsAudit: "/app/settings/audit",
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

export const ROUTE_PARAMS = {
  returnUrl: "returnUrl",
  email: "email",
  userId: "userId",
  token: "token",
} as const;

export const APP_AREA_SEGMENT = "app";
