import { act, cleanup, renderHook } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { useDirtyGuard } from "./use-dirty-guard";

describe("useDirtyGuard", () => {
  afterEach(cleanup);
  it("executes action immediately when form is clean", () => {
    const { result } = renderHook(() => useDirtyGuard(false));
    const action = vi.fn();

    act(() => {
      result.current.guardedAction(action);
    });

    expect(action).toHaveBeenCalledTimes(1);
    expect(result.current.showPrompt).toBe(false);
  });

  it("prompts before executing action when form is dirty", () => {
    const { result } = renderHook(() => useDirtyGuard(true));
    const action = vi.fn();

    act(() => {
      result.current.guardedAction(action);
    });

    expect(action).not.toHaveBeenCalled();
    expect(result.current.showPrompt).toBe(true);

    // Confirm discard
    act(() => {
      result.current.confirmDiscard();
    });

    expect(action).toHaveBeenCalledTimes(1);
    expect(result.current.showPrompt).toBe(false);
  });

  it("aborts action on cancelDiscard", () => {
    const { result } = renderHook(() => useDirtyGuard(true));
    const action = vi.fn();

    act(() => {
      result.current.guardedAction(action);
    });

    act(() => {
      result.current.cancelDiscard();
    });

    expect(action).not.toHaveBeenCalled();
    expect(result.current.showPrompt).toBe(false);
  });

  it("prevents default on beforeunload when dirty", () => {
    renderHook(() => useDirtyGuard(true));
    const event = new Event("beforeunload", { cancelable: true });
    const preventDefaultSpy = vi.spyOn(event, "preventDefault");
    window.dispatchEvent(event);
    expect(preventDefaultSpy).toHaveBeenCalled();
  });

  it("does not prevent default on beforeunload when clean", () => {
    renderHook(() => useDirtyGuard(false));
    const event = new Event("beforeunload", { cancelable: true });
    const preventDefaultSpy = vi.spyOn(event, "preventDefault");
    window.dispatchEvent(event);
    expect(preventDefaultSpy).not.toHaveBeenCalled();
  });
});
