export const LIMITS = {
  defaultPageSize: 20,
  searchDebounceMs: 200,
  searchMinQueryLength: 2,
  searchMaxQueryLength: 100,
  exportPollIntervalMs: 1500,
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
  customFieldKeyMinLength: 2,
  customFieldKeyMaxLength: 40,
  customFieldLabelMaxLength: 100,
  contactFirstNameMaxLength: 100,
  contactLastNameMaxLength: 100,
  contactPhoneMaxLength: 50,
  companyNameMaxLength: 200,
  companyDomainMaxLength: 255,
  companyCountryMaxLength: 2,
  companyIndustryMaxLength: 100,
  crmListDefaultLimit: 25,
  crmLookupLimit: 100,
  tagColorCount: 5,
} as const;

export const CUSTOM_FIELD_KEY_REGEX = /^[a-z][a-z0-9_]*$/;

export const SENTINELS = {
  all: "__all__",
} as const;

export type Limits = (typeof LIMITS)[keyof typeof LIMITS];
