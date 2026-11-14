"use client";

import {
  motion,
  useScroll,
  useTransform,
  type MotionValue,
} from "motion/react";
import { createContext, use, useRef, type ReactNode } from "react";

import { useMotionAllowed } from "../../hooks/use-motion-allowed";
import {
  HERO_LAYERS,
  HERO_SCROLL_OFFSET,
  type ScrollMap,
} from "../../lib/hero-scroll";

interface HeroMotion {
  progress: MotionValue<number>;
  enabled: boolean;
}

const HeroMotionContext = createContext<HeroMotion | null>(null);

function useScrollMap(progress: MotionValue<number>, map: ScrollMap) {
  return useTransform(progress, [...map.input], [...map.output]);
}

/**
 * The hero mock as an exploded stack: on scroll the list tips back, the
 * contact record and the holdout note float off it at different speeds.
 * Progress 0 is the identity, so the static render is the first frame.
 */
export function HeroStage({
  copy,
  list,
  record,
  note,
}: {
  copy: ReactNode;
  list: ReactNode;
  record: ReactNode;
  note: ReactNode;
}) {
  const sectionRef = useRef<HTMLElement>(null);
  const enabled = useMotionAllowed();
  const { scrollYProgress: progress } = useScroll({
    target: sectionRef,
    offset: HERO_SCROLL_OFFSET,
  });

  const baseY = useScrollMap(progress, HERO_LAYERS.base.y);
  const baseRotateX = useScrollMap(progress, HERO_LAYERS.base.rotateX);
  const baseScale = useScrollMap(progress, HERO_LAYERS.base.scale);
  const recordX = useScrollMap(progress, HERO_LAYERS.record.x);
  const recordY = useScrollMap(progress, HERO_LAYERS.record.y);
  const noteX = useScrollMap(progress, HERO_LAYERS.note.x);
  const noteY = useScrollMap(progress, HERO_LAYERS.note.y);

  return (
    <section
      ref={sectionRef}
      aria-labelledby="landing-hero-title"
      className="relative overflow-x-clip"
    >
      {copy}
      <HeroMotionContext value={{ progress, enabled }}>
        <div
          aria-hidden="true"
          className="relative mx-auto mt-14 w-full max-w-5xl px-4 pb-24 [perspective:1400px] sm:mt-16 sm:px-6 lg:px-8"
        >
          <motion.div
            className="origin-top"
            style={
              enabled
                ? { y: baseY, rotateX: baseRotateX, scale: baseScale }
                : undefined
            }
          >
            {list}
          </motion.div>
          <motion.div
            className="relative z-10 -mt-10 ml-auto w-[min(22rem,90%)] lg:absolute lg:top-24 lg:right-8 lg:mt-0"
            style={enabled ? { x: recordX, y: recordY } : undefined}
          >
            {record}
          </motion.div>
          <motion.div
            className="absolute bottom-4 left-2 z-10 hidden w-60 lg:block"
            style={enabled ? { x: noteX, y: noteY } : undefined}
          >
            {note}
          </motion.div>
        </div>
      </HeroMotionContext>
    </section>
  );
}

/** A list row that is not persuadable: it steps back as the visitor scrolls. */
export function HeroQuietRow({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  const heroMotion = use(HeroMotionContext);
  if (!heroMotion) throw new Error("HeroQuietRow must render inside HeroStage");
  const opacity = useScrollMap(
    heroMotion.progress,
    HERO_LAYERS.quietRow.opacity,
  );

  return (
    <motion.div
      className={className}
      style={heroMotion.enabled ? { opacity } : undefined}
    >
      {children}
    </motion.div>
  );
}
