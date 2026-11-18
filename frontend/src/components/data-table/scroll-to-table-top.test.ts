import { afterEach, describe, expect, it, vi } from "vitest";

import {
  scrollToTableTop,
  TABLE_ROOT_PROPS,
  TABLE_SCROLL_PROPS,
} from "./scroll-to-table-top";

function buildTable(top: number) {
  const root = document.createElement("div");
  Object.entries(TABLE_ROOT_PROPS).forEach(([k, v]) => root.setAttribute(k, v));
  const scroller = document.createElement("div");
  Object.entries(TABLE_SCROLL_PROPS).forEach(([k, v]) =>
    scroller.setAttribute(k, v),
  );
  scroller.scrollTop = 300;
  const pagination = document.createElement("div");
  root.append(scroller, pagination);
  document.body.append(root);
  root.getBoundingClientRect = () => ({ top }) as DOMRect;
  root.scrollIntoView = vi.fn();
  return { root, scroller, pagination };
}

function mockReducedMotion(reduced: boolean) {
  window.matchMedia = vi.fn().mockReturnValue({ matches: reduced });
}

afterEach(() => {
  document.body.innerHTML = "";
});

describe("scrollToTableTop", () => {
  it("resets the inner scroller and scrolls a table above the viewport into view smoothly", () => {
    mockReducedMotion(false);
    const { root, scroller, pagination } = buildTable(-400);
    scrollToTableTop(pagination);
    expect(scroller.scrollTop).toBe(0);
    expect(root.scrollIntoView).toHaveBeenCalledWith({
      block: "start",
      behavior: "smooth",
    });
  });

  it("jumps instantly under reduced motion", () => {
    mockReducedMotion(true);
    const { root, pagination } = buildTable(-400);
    scrollToTableTop(pagination);
    expect(root.scrollIntoView).toHaveBeenCalledWith({
      block: "start",
      behavior: "auto",
    });
  });

  it("leaves the page alone when the table is already in view", () => {
    mockReducedMotion(false);
    const { root, scroller, pagination } = buildTable(120);
    scrollToTableTop(pagination);
    expect(scroller.scrollTop).toBe(0);
    expect(root.scrollIntoView).not.toHaveBeenCalled();
  });
});
