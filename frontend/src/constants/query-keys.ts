export const queryKeys = {
  all: ["api"] as const,
  health: () => [...queryKeys.all, "health"] as const,
  me: () => [...queryKeys.all, "me"] as const,
  myOrganizations: () => [...queryKeys.all, "me", "organizations"] as const,
  organization: (id: string) => [...queryKeys.all, "organization", id] as const,
  members: () => [...queryKeys.all, "members"] as const,
  invitations: () => [...queryKeys.all, "invitations"] as const,
  teams: () => [...queryKeys.all, "teams"] as const,
  audit: () => [...queryKeys.all, "audit"] as const,
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
  customFieldsAll: () => [...queryKeys.all, "custom-fields"] as const,
  customFields: (entityType?: string) =>
    entityType !== undefined
      ? ([...queryKeys.all, "custom-fields", entityType] as const)
      : ([...queryKeys.all, "custom-fields"] as const),
};
