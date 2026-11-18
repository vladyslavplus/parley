// A page change should land on the first row, not where the pagination
// buttons were. The buttons sit below the table, so the user is at the bottom
// of the page and of the table's own scroller when pressing Next.

const TABLE_ROOT_ATTRIBUTE = "data-table-root";
const TABLE_SCROLL_ATTRIBUTE = "data-table-scroll";

// Spread onto the table's outer element and its inner scroller.
export const TABLE_ROOT_PROPS = { [TABLE_ROOT_ATTRIBUTE]: "" };
export const TABLE_SCROLL_PROPS = { [TABLE_SCROLL_ATTRIBUTE]: "" };

// The sticky top bar covers this much of the viewport; a table whose top edge
// is above it needs scrolling back into view.
const STICKY_TOP_OFFSET_PX = 56;

function prefersReducedMotion(): boolean {
  return (
    typeof window !== "undefined" &&
    typeof window.matchMedia === "function" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}

/**
 * Scrolls the table that owns `origin` (the pagination controls) to its top:
 * resets the table's inner scroller and, when the table starts above the
 * viewport, scrolls the page back to it. Smooth unless the user asked for
 * reduced motion. A list without a `DataTable` anchors on the parent element.
 */
export function scrollToTableTop(origin: HTMLElement | null): void {
  if (!origin) return;
  const root =
    origin.closest<HTMLElement>(`[${TABLE_ROOT_ATTRIBUTE}]`) ??
    origin.parentElement;
  if (!root) return;

  root
    .querySelectorAll<HTMLElement>(`[${TABLE_SCROLL_ATTRIBUTE}]`)
    .forEach((scroller) => {
      scroller.scrollTop = 0;
    });

  if (root.getBoundingClientRect().top >= STICKY_TOP_OFFSET_PX) return;
  root.scrollIntoView({
    block: "start",
    behavior: prefersReducedMotion() ? "auto" : "smooth",
  });
}
