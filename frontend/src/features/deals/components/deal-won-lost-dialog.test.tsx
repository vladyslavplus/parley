import {
  render,
  screen,
  fireEvent,
  waitFor,
  cleanup,
} from "@testing-library/react";
import { describe, expect, it, vi, afterEach } from "vitest";

import { DealWonLostDialog } from "./deal-won-lost-dialog";
import type { Deal, PipelineBoardStage } from "@/features/deals/types";

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string, params?: Record<string, unknown>) => {
    if (params?.title) return `${key} ${params.title}`;
    return key;
  },
}));

describe("DealWonLostDialog", () => {
  afterEach(cleanup);
  const mockDeal: Deal = {
    id: "deal-1",
    pipelineId: "pipe-1",
    stageId: "stage-1",
    stageName: "Proposal",
    status: "open",
    title: "Enterprise Deal",
    amount: 10000,
    currency: "USD",
    contactId: null,
    contactName: null,
    companyId: null,
    companyName: null,
    ownerUserId: "user-1",
    ownerDisplayName: "John Doe",
    ownerEmail: "john@example.com",
    expectedCloseOn: null,
    closedAt: null,
    lostReason: null,
    boardPosition: 1,
    customFields: {},
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    version: 1,
  };

  const wonStage: PipelineBoardStage = {
    id: "stage-won",
    name: "Won",
    position: 4,
    kind: "won",
    probabilityBps: 10000,
    colorIndex: 4,
    dealCount: 5,
    totalAmount: 50000,
  };

  const lostStage: PipelineBoardStage = {
    id: "stage-lost",
    name: "Lost",
    position: 5,
    kind: "lost",
    probabilityBps: 0,
    colorIndex: 0,
    dealCount: 2,
    totalAmount: 15000,
  };

  it("renders won dialog and calls onConfirm with null lostReason when confirmed", async () => {
    const onConfirm = vi.fn().mockResolvedValue(undefined);
    const onOpenChange = vi.fn();

    render(
      <DealWonLostDialog
        open={true}
        onOpenChange={onOpenChange}
        deal={mockDeal}
        targetStage={wonStage}
        position={1}
        onConfirm={onConfirm}
      />,
    );

    expect(screen.getByText("wonTitle")).toBeInTheDocument();
    expect(screen.queryByLabelText("lostReasonLabel")).not.toBeInTheDocument();

    const confirmBtn = screen.getByText("confirmWon");
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(onConfirm).toHaveBeenCalledWith({
        dealId: "deal-1",
        stageId: "stage-won",
        position: 1,
        version: 1,
        lostReason: null,
      });
      expect(onOpenChange).toHaveBeenCalledWith(false);
    });
  });

  it("renders lost dialog with reason textarea and calls onConfirm with typed reason", async () => {
    const onConfirm = vi.fn().mockResolvedValue(undefined);
    const onOpenChange = vi.fn();

    render(
      <DealWonLostDialog
        open={true}
        onOpenChange={onOpenChange}
        deal={mockDeal}
        targetStage={lostStage}
        position={2}
        onConfirm={onConfirm}
      />,
    );

    expect(screen.getByText("lostTitle")).toBeInTheDocument();
    const textarea = screen.getByPlaceholderText("lostReasonPlaceholder");
    expect(textarea).toBeInTheDocument();

    fireEvent.change(textarea, {
      target: { value: "Customer chose competitor" },
    });

    const confirmBtn = screen.getByText("confirmLost");
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(onConfirm).toHaveBeenCalledWith({
        dealId: "deal-1",
        stageId: "stage-lost",
        position: 2,
        version: 1,
        lostReason: "Customer chose competitor",
      });
      expect(onOpenChange).toHaveBeenCalledWith(false);
    });
  });

  it("calls onOpenChange(false) when cancelled", () => {
    const onConfirm = vi.fn();
    const onOpenChange = vi.fn();

    render(
      <DealWonLostDialog
        open={true}
        onOpenChange={onOpenChange}
        deal={mockDeal}
        targetStage={wonStage}
        position={1}
        onConfirm={onConfirm}
      />,
    );

    const cancelBtn = screen.getByText("cancel");
    fireEvent.click(cancelBtn);

    expect(onOpenChange).toHaveBeenCalledWith(false);
    expect(onConfirm).not.toHaveBeenCalled();
  });
});
