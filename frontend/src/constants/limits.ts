export const LIMITS = {
  defaultPageSize: 20,
  searchDebounceMs: 300,
  passwordMinLength: 8,
  passwordMaxLength: 256,
  emailMaxLength: 256,
  organizationNameMaxLength: 200,
  displayNameMaxLength: 100,
  teamNameMaxLength: 100,
  currencyCodeLength: 3,
  meStaleTimeMs: 60_000,
  tablePageSizes: [10, 25, 50, 100] as const,
  defaultTablePageSize: 25,
  toastDurationMs: 5000,
} as const;

export type Limits = (typeof LIMITS)[keyof typeof LIMITS];
