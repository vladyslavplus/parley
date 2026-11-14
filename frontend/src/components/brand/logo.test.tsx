import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";

import { APP_NAME } from "@/constants/app";

import { Logo } from "./logo";
import { LogoMark, logoTier } from "./logo-mark";

afterEach(cleanup);

function svgOf(container: HTMLElement) {
  const svg = container.querySelector("svg");
  if (!svg) throw new Error("No svg rendered");
  return svg;
}

describe("LogoMark", () => {
  it("is decorative without a title", () => {
    const { container } = render(<LogoMark />);
    const svg = svgOf(container);
    expect(svg).toHaveAttribute("aria-hidden", "true");
    expect(svg).not.toHaveAttribute("role");
  });

  it("is an image with an accessible name when titled", () => {
    render(<LogoMark title={APP_NAME} />);
    expect(screen.getByRole("img", { name: APP_NAME })).toBeInTheDocument();
  });

  it("uses the flat tier below 24 px and the full tier from 24 px", () => {
    expect(logoTier(16)).toBe("flat");
    expect(logoTier(23)).toBe("flat");
    expect(logoTier(24)).toBe("full");
    expect(logoTier(8, "full")).toBe("full");
    expect(logoTier(64, "flat")).toBe("flat");

    const flat = render(<LogoMark size={16} />);
    expect(svgOf(flat.container)).toHaveAttribute("data-tier", "flat");
    expect(svgOf(flat.container)).toHaveAttribute("viewBox", "0 0 24 24");
    flat.unmount();

    const full = render(<LogoMark size={32} />);
    expect(svgOf(full.container)).toHaveAttribute("data-tier", "full");
    expect(svgOf(full.container)).toHaveAttribute("viewBox", "0 0 100 100");
  });

  it("gives every full mark its own gradient ids", () => {
    const { container } = render(
      <>
        <LogoMark size={32} />
        <LogoMark size={32} />
      </>,
    );
    const ids = [...container.querySelectorAll("[id]")].map((el) => el.id);
    expect(ids.length).toBeGreaterThan(0);
    expect(new Set(ids).size).toBe(ids.length);
  });
});

describe("Logo", () => {
  it("shows the live name next to a decorative mark", () => {
    const { container } = render(<Logo size={28} />);
    expect(screen.getByText(APP_NAME)).toBeInTheDocument();
    expect(svgOf(container)).toHaveAttribute("aria-hidden", "true");
    expect(svgOf(container)).toHaveAttribute("width", "28");
  });
});
