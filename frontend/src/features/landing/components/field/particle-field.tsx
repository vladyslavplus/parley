"use client";

import { useEffect, useRef, useState } from "react";

import { cn } from "@/lib/utils";

import { useMotionAllowed } from "../../hooks/use-motion-allowed";
import { createFieldRenderer } from "../../lib/field-renderer";
import { StaticField } from "./static-field";

const VIEW_MARGIN = "160px 0px";

/**
 * The customer field. The server renders the settled scene as SVG; with
 * motion allowed, a canvas takes over once the field nears the viewport and
 * runs only while it stays visible.
 */
export function ParticleField({ className }: { className?: string }) {
  const frameRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [canvasReady, setCanvasReady] = useState(false);
  const animated = useMotionAllowed();

  useEffect(() => {
    const frame = frameRef.current;
    const canvas = canvasRef.current;
    if (!animated || !frame || !canvas) return;

    const renderer = createFieldRenderer(canvas, () => setCanvasReady(true));
    if (!renderer) return;

    let inView = false;
    const update = () => {
      if (inView && document.visibilityState === "visible") renderer.start();
      else renderer.stop();
    };

    const resizeObserver = new ResizeObserver(([entry]) => {
      const { width, height } = entry.contentRect;
      renderer.resize(width, height);
      update();
    });
    const viewObserver = new IntersectionObserver(
      ([entry]) => {
        inView = entry.isIntersecting;
        update();
      },
      { rootMargin: VIEW_MARGIN },
    );
    const themeObserver = new MutationObserver(() => renderer.refreshColors());

    resizeObserver.observe(frame);
    viewObserver.observe(frame);
    themeObserver.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ["class"],
    });
    document.addEventListener("visibilitychange", update);

    return () => {
      document.removeEventListener("visibilitychange", update);
      themeObserver.disconnect();
      viewObserver.disconnect();
      resizeObserver.disconnect();
      renderer.destroy();
      setCanvasReady(false);
    };
  }, [animated]);

  return (
    <div
      ref={frameRef}
      aria-hidden="true"
      className={cn("relative overflow-hidden", className)}
    >
      <StaticField
        className={cn(
          "absolute inset-0 h-full w-full transition-opacity duration-[var(--duration-slow)]",
          canvasReady && "opacity-0",
        )}
      />
      {animated ? (
        <canvas
          ref={canvasRef}
          className={cn(
            "absolute inset-0 h-full w-full opacity-0 transition-opacity duration-[var(--duration-slow)]",
            canvasReady && "opacity-100",
          )}
        />
      ) : null}
    </div>
  );
}
