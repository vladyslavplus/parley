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
};
