"use client";

import * as Popover from "@radix-ui/react-popover";
import { Info } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useId, useRef, useState, type ReactNode } from "react";

import { cn } from "@/lib/utils";

// Leaving the trigger closes the bubble after this pause, so the pointer can
// cross the gap into the bubble to scroll or select its text.
const HOVER_CLOSE_DELAY_MS = 150;

interface InfoHintProps {
  /** The short explanation, from the `hints` namespace. */
  children: ReactNode;
  className?: string;
  /** `wide` gives a list of terms room to sit on one line each; a tall hint scrolls instead of running off the screen. */
  size?: "default" | "wide";
}

/**
 * The structure of a hint: one lead sentence, then short paragraphs or a list of
 * terms, then a muted note. Terms are bold and spelled like the label in the UI,
 * and no term sits inside quote marks.
 */
export function HintLead({ children }: { children: ReactNode }) {
  return <p className="text-foreground">{children}</p>;
}

export function HintParagraph({ children }: { children: ReactNode }) {
  return <p className="mt-2">{children}</p>;
}

export function HintGroup({ children }: { children: ReactNode }) {
  return (
    <p className="mt-3 text-[11px] font-semibold text-muted-foreground">
      {children}
    </p>
  );
}

export interface HintItem {
  /** The label the interface shows for this thing. */
  term: string;
  text: string;
}

export function HintList({ items }: { items: readonly HintItem[] }) {
  return (
    <dl className="mt-1.5 space-y-1">
      {items.map((item) => (
        <div key={item.term}>
          <dt className="inline font-semibold text-foreground">{item.term}</dt>{" "}
          <dd className="inline">{item.text}</dd>
        </div>
      ))}
    </dl>
  );
}

export function HintNote({ children }: { children: ReactNode }) {
  return <p className="mt-2 text-muted-foreground">{children}</p>;
}

/**
 * The "what is this?" affordance next to a term or a field. Hover, keyboard
 * focus and a tap open it; Escape or an outside click closes it.
 *
 * The bubble never takes focus. Radix focuses a popover on open and hands focus
 * back to the trigger on close, and this trigger opens on focus as well, so the
 * two would trade focus forever: every cycle reopened the bubble and pulled the
 * page back to it, which also closed any other popover or menu that dismisses
 * once focus leaves it. The explanation is announced through `aria-describedby`
 * on the trigger instead, which is the tooltip pattern.
 */
export function InfoHint({
  children,
  className,
  size = "default",
}: InfoHintProps) {
  const t = useTranslations("hints");
  const [open, setOpen] = useState(false);
  const contentId = useId();
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  function cancelClose() {
    if (closeTimer.current) clearTimeout(closeTimer.current);
    closeTimer.current = null;
  }
  function closeSoon() {
    cancelClose();
    closeTimer.current = setTimeout(() => setOpen(false), HOVER_CLOSE_DELAY_MS);
  }
  useEffect(() => cancelClose, []);

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        <button
          type="button"
          aria-label={t("whatIsThis")}
          aria-describedby={open ? contentId : undefined}
          className={cn(
            "relative inline-flex h-6 w-6 shrink-0 cursor-pointer items-center justify-center rounded-full text-muted-foreground transition-colors after:absolute after:-inset-1 after:content-[''] hover:bg-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
            className,
          )}
          onPointerEnter={(event) => {
            // A touch tap also reports a matching leave, so hovering is for
            // pointers that can hover; a tap opens through the click handler.
            if (event.pointerType === "touch") return;
            cancelClose();
            setOpen(true);
          }}
          onPointerLeave={(event) => {
            if (event.pointerType === "touch") return;
            closeSoon();
          }}
          onFocus={() => setOpen(true)}
          onClick={(event) => {
            // A tap always shows the hint; closing is Escape or an outside tap.
            event.preventDefault();
            setOpen(true);
          }}
        >
          <Info className="h-3.5 w-3.5" aria-hidden="true" />
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          id={contentId}
          role="tooltip"
          sideOffset={6}
          collisionPadding={12}
          // Focus stays on the trigger: the bubble is a description, not a
          // place to type, and moving focus is what used to bounce the state.
          onOpenAutoFocus={(event) => event.preventDefault()}
          onCloseAutoFocus={(event) => event.preventDefault()}
          onPointerEnter={(event) => {
            if (event.pointerType !== "touch") cancelClose();
          }}
          onPointerLeave={(event) => {
            if (event.pointerType !== "touch") closeSoon();
          }}
          className={cn(
            "z-50 max-h-[var(--radix-popover-content-available-height)] overflow-y-auto rounded-[var(--radius-card)] border border-border bg-popover p-3 text-xs leading-relaxed text-popover-foreground shadow-[var(--shadow-floating)] focus:outline-none",
            size === "wide"
              ? "max-w-[min(32rem,calc(100vw-2rem))]"
              : "max-w-xs",
          )}
        >
          {children}
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
