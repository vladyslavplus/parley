import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { describe, expect, it, vi, beforeEach } from "vitest";

import { PipelineSettingsPanel } from "./pipeline-settings-panel";
import type { Pipeline } from "@/features/deals/types";

const mockReorderStages = vi.fn();
const mockDeleteStage = vi.fn();
const mockCreateStage = vi.fn();
const mockUpdateStage = vi.fn();
const mockRenamePipeline = vi.fn();

const mockPipeline: Pipeline = {
  id: "pipe-1",
  name: "Sales Pipeline",
  isDefault: true,
  version: 1,
  stages: [
    {
      id: "stage-1",
      name: "Lead",
      position: 1,
      kind: "open",
      probabilityBps: 2000,
      colorIndex: 0,
    },
    {
      id: "stage-2",
      name: "Qualified",
      position: 2,
      kind: "open",
      probabilityBps: 4000,
      colorIndex: 1,
    },
    {
      id: "stage-3",
      name: "Won",
      position: 3,
      kind: "won",
      probabilityBps: 10000,
      colorIndex: 4,
    },
  ],
};

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string, params?: Record<string, unknown>) => {
    if (params?.name) return `${key} ${params.name}`;
    return key;
  },
}));

vi.mock("@/features/session/components/can", () => ({
  useCan: () => true,
  Can: ({ children }: { children: ReactNode }) => <>{children}</>,
}));

vi.mock("@/features/deals/hooks/use-pipelines", () => ({
  useDefaultPipeline: () => ({
    pipeline: mockPipeline,
    isLoading: false,
    refetch: vi.fn(),
  }),
}));

vi.mock("@/features/deals/hooks/use-pipeline-mutations", () => ({
  usePipelineMutations: () => ({
    renamePipeline: mockRenamePipeline,
    createStage: mockCreateStage,
    updateStage: mockUpdateStage,
    deleteStage: mockDeleteStage,
    reorderStages: mockReorderStages,
  }),
}));

describe("PipelineSettingsPanel", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient();
  });

  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  it("renders pipeline stages and triggers reorderStages when moving a stage down", async () => {
    render(<PipelineSettingsPanel />, { wrapper });

    expect(screen.getByText("Lead")).toBeInTheDocument();
    expect(screen.getByText("Qualified")).toBeInTheDocument();
    expect(screen.getByText("Won")).toBeInTheDocument();

    const moveDownButtons = screen.getAllByLabelText("moveDown");
    expect(moveDownButtons.length).toBeGreaterThan(0);

    // Click moveDown on the first stage (Lead)
    fireEvent.click(moveDownButtons[0]!);

    await waitFor(() => {
      expect(mockReorderStages).toHaveBeenCalledWith("pipe-1", [
        "stage-2",
        "stage-1",
      ]);
    });
  });

  it("opens confirm dialog on delete stage and calls deleteStage when confirmed", async () => {
    render(<PipelineSettingsPanel />, { wrapper });

    const deleteButtons = screen.getAllByLabelText("deleteStage");
    expect(deleteButtons.length).toBeGreaterThan(0);

    fireEvent.click(deleteButtons[0]!);

    expect(screen.getByText("deleteStageTitle")).toBeInTheDocument();

    const confirmBtn = screen.getByText("deleteStage");
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(mockDeleteStage).toHaveBeenCalledWith("pipe-1", "stage-1");
    });
  });
});
