export const LIMITS = {
  defaultPageSize: 20,
  // The bell polls the unread count only while the tab is visible.
  notificationsPollIntervalMs: 45_000,
  // The bell shows this many unread items as a count; above it, "99+".
  notificationsBellCountCap: 99,
  // Latest items shown in the bell dropdown.
  notificationsPopoverSize: 5,
  // A scoring run is polled this often while it is queued or running.
  scoringRunPollIntervalMs: 2_000,
  // The Telegram link is polled this often while a link code is pending.
  telegramLinkPollIntervalMs: 3_000,
  // The countdown of a link code ticks this often.
  telegramCodeTickMs: 1_000,
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
  queryStaleTimeMs: 30_000,
  queryGcTimeMs: 5 * 60_000,
  tablePageSizes: [20, 50, 100] as const,
  defaultTablePageSize: 20,
  toastDurationMs: 4000,
  toastErrorDurationMs: 8000,
  toastUndoDurationMs: 8000,
  toastMaxVisible: 3,
  toastTopOffsetPx: 64,
  toastMaxWidthPx: 356,
  ownerPickerLimit: 20,
  ownerPickerMaxLimit: 50,
  tagPickerLimit: 20,
  comboboxMaxVisibleRows: 5,
  tableProgressDelayMs: 150,
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
  crmListDefaultLimit: 20,
  crmLookupLimit: 100,
  companyPickerLimit: 20,
  contactPickerLimit: 20,
  tagColorCount: 5,
  stageColorCount: 5,
  dealTitleMaxLength: 200,
  dealAmountMax: 1_000_000_000,
  stageNameMaxLength: 100,
  pipelineNameMaxLength: 100,
  kanbanColumnPageSize: 20,
  defaultCurrency: "USD",
  taskTitleMaxLength: 200,
  taskNotesMaxLength: 2000,
  noteBodyMaxLength: 4000,
  dealPickerLimit: 20,
  tasksPageSize: 20,
  maxTasksPageSize: 100,
  activitiesPageSize: 20,
  maxNotesPageSize: 100,
  ordersPageSize: 20,
  campaignAutosaveDebounceMs: 800,
  campaignReviewPollMs: 2000,
  // A campaign still `preparing` after this long is offered a resume.
  campaignPrepareStallMs: 60_000,
  // Delivery progress and the status of a sending campaign are polled at this
  // pace until the campaign leaves `sending`.
  campaignProgressPollMs: 2000,
  // After "refresh results" the figures are polled until they are recomputed,
  // for at most this long.
  resultsRefreshPollMs: 2000,
  resultsRefreshMaxMs: 30_000,
  // Decimals shown for rates and lifts, in percent / percentage points.
  resultsPercentDigits: 1,
  // A p-value below the floor is shown as "< floor", never as 0.
  pValueFloor: 0.001,
  pValueDigits: 3,
  campaignNameMaxLength: 200,
  templateNameMaxLength: 200,
  templateSubjectMaxLength: 300,
  templateBodyMaxLength: 100_000,
  segmentNameMaxLength: 200,
  segmentDaysSinceLastOrderMax: 3650,
  segmentCountryCodeLength: 2,
  segmentPreviewDebounceMs: 400,
  libraryPageSize: 20,
  // Mirror the server options Campaigns:Holdout (bounds) and
  // Campaigns:Analysis:PreviewBaselineRateBps; change them together.
  holdoutShareMinBps: 500,
  holdoutShareMaxBps: 5000,
  holdoutShareStepBps: 100,
  holdoutShareDefaultBps: 1000,
  targetingShareMinBps: 500,
  targetingShareMaxBps: 5000,
  targetingShareStepBps: 100,
  targetingShareDefaultBps: 1000,
  mdeBaselineRateBps: 2000,
  mdeZAlpha: 1.959964,
  mdeZBeta: 0.841621,
} as const;

export const CUSTOM_FIELD_KEY_REGEX = /^[a-z][a-z0-9_]*$/;

export const SENTINELS = {
  all: "__all__",
} as const;

export type Limits = (typeof LIMITS)[keyof typeof LIMITS];
