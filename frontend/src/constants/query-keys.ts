export const queryKeys = {
  all: ["api"] as const,
  insightsPriority: (filters: Record<string, unknown>) =>
    [...queryKeys.all, "insights", "priority", filters] as const,
  insightsRfmSegments: () =>
    [...queryKeys.all, "insights", "rfm-segments"] as const,
  notificationsList: (filters: Record<string, unknown>) =>
    [...queryKeys.all, "notifications", "list", filters] as const,
  notificationsUnreadCount: () =>
    [...queryKeys.all, "notifications", "unread-count"] as const,
  notificationsPreferences: () =>
    [...queryKeys.all, "notifications", "preferences"] as const,
  insightsMlStatus: () => [...queryKeys.all, "insights", "ml-status"] as const,
  insightsModels: () => [...queryKeys.all, "insights", "models"] as const,
  insightsScoringRun: (id: string) =>
    [...queryKeys.all, "insights", "scoring-run", id] as const,
  contactScores: (contactId: string) =>
    [...queryKeys.all, "contacts", "scores", contactId] as const,
  telegramStatus: () => [...queryKeys.all, "telegram", "status"] as const,
  telegramLink: () => [...queryKeys.all, "telegram", "link"] as const,
  insightsMlSettings: () =>
    [...queryKeys.all, "insights", "ml-settings"] as const,
  dashboardOverview: () =>
    [...queryKeys.all, "dashboards", "overview"] as const,
  health: () => [...queryKeys.all, "health"] as const,
  me: () => [...queryKeys.all, "me"] as const,
  myOrganizations: () => [...queryKeys.all, "me", "organizations"] as const,
  organization: (id: string) => [...queryKeys.all, "organization", id] as const,
  members: () => [...queryKeys.all, "members"] as const,
  membersList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "members", "list", filters] as const,
  invitations: () => [...queryKeys.all, "invitations"] as const,
  invitationsList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "invitations", "list", filters] as const,
  teams: () => [...queryKeys.all, "teams"] as const,
  teamsList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "teams", "list", filters] as const,
  audit: () => [...queryKeys.all, "audit"] as const,
  auditList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "audit", "list", filters] as const,
  owners: () => [...queryKeys.all, "owners"] as const,
  ownersList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "owners", "list", filters] as const,
  contactsAll: () => [...queryKeys.all, "contacts"] as const,
  contactLists: () => [...queryKeys.all, "contacts", "list"] as const,
  contactList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "contacts", "list", filters] as const,
  contact: (id: string) =>
    [...queryKeys.all, "contacts", "detail", id] as const,
  companiesAll: () => [...queryKeys.all, "companies"] as const,
  companyLists: () => [...queryKeys.all, "companies", "list"] as const,
  companyList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "companies", "list", filters] as const,
  company: (id: string) =>
    [...queryKeys.all, "companies", "detail", id] as const,
  tags: () => [...queryKeys.all, "tags"] as const,
  tagsList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "tags", "list", filters] as const,
  customFieldsAll: () => [...queryKeys.all, "custom-fields"] as const,
  customFields: (entityType?: string) =>
    entityType !== undefined
      ? ([...queryKeys.all, "custom-fields", entityType] as const)
      : ([...queryKeys.all, "custom-fields"] as const),
  customFieldsList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "custom-fields", "list", filters] as const,
  pipelinesAll: () => [...queryKeys.all, "pipelines"] as const,
  pipelinesList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "pipelines", "list", filters] as const,
  pipeline: (id: string) =>
    [...queryKeys.all, "pipelines", "detail", id] as const,
  pipelineBoard: (id: string) =>
    [...queryKeys.all, "pipelines", "board", id] as const,
  dealsAll: () => [...queryKeys.all, "deals"] as const,
  dealLists: () => [...queryKeys.all, "deals", "list"] as const,
  dealList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "deals", "list", filters] as const,
  deal: (id: string) => [...queryKeys.all, "deals", "detail", id] as const,
  tasksAll: () => [...queryKeys.all, "tasks"] as const,
  tasksLists: () => [...queryKeys.all, "tasks", "list"] as const,
  tasksList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "tasks", "list", filters] as const,
  tasksMine: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "tasks", "mine", filters] as const,
  task: (id: string) => [...queryKeys.all, "tasks", "detail", id] as const,
  tasksOverdueCount: () =>
    [...queryKeys.all, "tasks", "overdue-count"] as const,
  notesAll: () => [...queryKeys.all, "notes"] as const,
  notesList: (subject: Record<string, unknown>) =>
    [...queryKeys.all, "notes", "list", subject] as const,
  note: (id: string) => [...queryKeys.all, "notes", "detail", id] as const,
  templatesAll: () => [...queryKeys.all, "templates"] as const,
  templatesList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "templates", "list", filters] as const,
  segmentsAll: () => [...queryKeys.all, "segments"] as const,
  segmentsList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "segments", "list", filters] as const,
  segmentPreview: (definition: unknown) =>
    [...queryKeys.all, "segments", "preview", definition] as const,
  templatePreview: (id: string, version: number | string) =>
    [...queryKeys.all, "templates", "preview", id, version] as const,
  campaignsAll: () => [...queryKeys.all, "campaigns"] as const,
  campaignsList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "campaigns", "list", filters] as const,
  campaign: (id: string) =>
    [...queryKeys.all, "campaigns", "detail", id] as const,
  campaignProgress: (id: string) =>
    [...queryKeys.all, "campaigns", "progress", id] as const,
  campaignResults: (id: string) =>
    [...queryKeys.all, "campaigns", "results", id] as const,
  campaignRecipients: (id: string, filters: Record<string, unknown>) =>
    [...queryKeys.all, "campaigns", "recipients", id, filters] as const,
  campaignReview: (id: string) =>
    [...queryKeys.all, "campaigns", "review", id] as const,
  ordersAll: () => [...queryKeys.all, "orders"] as const,
  ordersList: (filters?: Record<string, unknown>) =>
    [...queryKeys.all, "orders", "list", filters] as const,
  order: (id: string) => [...queryKeys.all, "orders", "detail", id] as const,
  activitiesAll: () => [...queryKeys.all, "activities"] as const,
  activitiesList: (subject: Record<string, unknown>) =>
    [...queryKeys.all, "activities", "list", subject] as const,
};
