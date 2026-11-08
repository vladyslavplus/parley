export const PERMISSIONS = {
  contactsRead: "contacts.read",
  contactsWrite: "contacts.write",
  contactsDelete: "contacts.delete",
  contactsExport: "contacts.export",
  companiesRead: "companies.read",
  companiesWrite: "companies.write",
  companiesDelete: "companies.delete",
  dealsRead: "deals.read",
  dealsWrite: "deals.write",
  dealsDelete: "deals.delete",
  tasksRead: "tasks.read",
  tasksWrite: "tasks.write",
  tasksDelete: "tasks.delete",
  activitiesRead: "activities.read",
  activitiesWrite: "activities.write",
  ordersRead: "orders.read",
  pipelinesManage: "pipelines.manage",
  customFieldsManage: "customfields.manage",
  tagsManage: "tags.manage",
  campaignsRead: "campaigns.read",
  campaignsManage: "campaigns.manage",
  campaignsSend: "campaigns.send",
  dashboardsView: "dashboards.view",
  insightsView: "insights.view",
  insightsMlView: "insights.ml.view",
  insightsMlRun: "insights.ml.run",
  insightsMlManage: "insights.ml.manage",
  usersManage: "users.manage",
  teamsManage: "teams.manage",
  orgManage: "org.manage",
  auditRead: "audit.read",
} as const;

export type Permission = (typeof PERMISSIONS)[keyof typeof PERMISSIONS];

export const SYSTEM_ROLES = [
  "admin",
  "sales_manager",
  "sales_rep",
  "marketer",
  "viewer",
] as const;

export type SystemRole = (typeof SYSTEM_ROLES)[number];

export const MEMBERSHIP_STATUSES = {
  active: "active",
  deactivated: "deactivated",
} as const;
