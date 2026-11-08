import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

const mockCreateCustomField = vi.fn();
const mockUpdateCustomField = vi.fn();
const mockArchiveCustomField = vi.fn();

let mockCanStatus: "pending" | "allowed" | "denied" = "allowed";

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string, params?: Record<string, unknown>) => {
    if (params?.label) return `${key} ${params.label}`;
    return key;
  },
}));

vi.mock("@/features/session/components/can", () => ({
  Can: ({ children }: { children: React.ReactNode }) => children,
  useCan: () => mockCanStatus === "allowed",
  useCanStatus: () => mockCanStatus,
}));

const mockDefinitions = [
  {
    id: "cf-1",
    entityType: "contact",
    key: "lead_source",
    label: "Lead Source",
    fieldType: "text",
    options: null,
    isRequired: false,
    position: 0,
    archivedAt: null,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
  },
  {
    id: "cf-2",
    entityType: "contact",
    key: "external_code",
    label: "External Code",
    fieldType: "text",
    options: null,
    isRequired: false,
    position: 1,
    archivedAt: "2026-01-02T00:00:00Z",
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-02T00:00:00Z",
  },
];

vi.mock("@/features/crm/hooks/use-custom-fields", () => ({
  useCustomFields: () => ({
    data: mockDefinitions,
    isPending: false,
    isError: false,
  }),
}));

vi.mock("@/features/crm/hooks/use-custom-field-mutations", () => ({
  useCustomFieldMutations: () => ({
    createCustomField: mockCreateCustomField,
    updateCustomField: mockUpdateCustomField,
    archiveCustomField: mockArchiveCustomField,
  }),
}));

const { CustomFieldsSettingsPanel } =
  await import("./custom-fields-settings-panel");

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  mockCanStatus = "allowed";
});

describe("CustomFieldsSettingsPanel", () => {
  it("renders active and archived custom fields", () => {
    render(<CustomFieldsSettingsPanel />);

    expect(screen.getByText("Lead Source")).toBeInTheDocument();
    expect(screen.getByText("lead_source")).toBeInTheDocument();
    expect(screen.getByText("External Code")).toBeInTheDocument();
    expect(screen.getByText("archived")).toBeInTheDocument();
  });

  it("opens archive confirmation dialog and triggers archiveCustomField", async () => {
    render(<CustomFieldsSettingsPanel />);

    const archiveBtn = screen.getByRole("button", {
      name: "archiveField Lead Source",
    });
    fireEvent.click(archiveBtn);

    const confirmBtn = screen.getByRole("button", {
      name: "archive",
    });
    fireEvent.click(confirmBtn);

    expect(mockArchiveCustomField).toHaveBeenCalledWith("cf-1");
  });

  it("renders forbidden message only when manage permission is denied", () => {
    mockCanStatus = "denied";
    render(<CustomFieldsSettingsPanel />);

    expect(screen.getByText("forbidden")).toBeInTheDocument();
    expect(screen.queryByText("Lead Source")).toBeNull();
  });

  it("renders loading skeleton without forbidden flash while permission check is pending", () => {
    mockCanStatus = "pending";
    const { container } = render(<CustomFieldsSettingsPanel />);

    expect(screen.queryByText("forbidden")).toBeNull();
    expect(screen.queryByText("Lead Source")).toBeNull();
    expect(container.querySelectorAll(".animate-pulse").length).toBeGreaterThan(
      0,
    );
  });
});
