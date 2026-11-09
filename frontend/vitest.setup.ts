import "@testing-library/jest-dom/vitest";
import { vi } from "vitest";

vi.mock("next/font/google", () => ({
  Onest: () => ({ variable: "--font-onest", className: "font-onest" }),
  Source_Serif_4: () => ({
    variable: "--font-source-serif",
    className: "font-serif",
  }),
  JetBrains_Mono: () => ({
    variable: "--font-jetbrains-mono",
    className: "font-mono",
  }),
}));

// jsdom lacks the pointer-capture and scroll APIs Radix Select relies on.
if (typeof window !== "undefined") {
  const proto = window.HTMLElement.prototype;
  proto.hasPointerCapture ??= () => false;
  proto.setPointerCapture ??= () => {};
  proto.releasePointerCapture ??= () => {};
  proto.scrollIntoView ??= () => {};
}
