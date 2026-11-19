"use client";

import { useSelectedLayoutSegments } from "next/navigation";
import { ViewTransition, type ReactNode } from "react";

import { VIEW_TRANSITION_CLASSES } from "@/constants/view-transitions";

interface Props {
  children: ReactNode;
  /**
   * How many route segments below the layout that renders this boundary decide
   * when the content changes. Leave it out to follow every segment.
   */
  depth?: number;
}

/**
 * The content below a layout crossfades when the route it shows changes.
 *
 * The key is the route that is actually rendered (its segments, with the real
 * ids), not the address bar. The boundary therefore changes only for a route
 * change: filtering, sorting or paging a list keeps the same route, and the
 * campaign builder, which rewrites the address to the new campaign's id while
 * it stays on screen, is not remounted by that rewrite.
 *
 * The app shell uses `depth={1}`: only a move between sections (Home, Deals,
 * Settings) fades the whole page area, and a layout with its own navigation
 * places a boundary around its own content, so the navigation stays mounted and
 * still. The crossfade does not move anything: the main navigation is lateral,
 * and a back or forward step in the browser history carries no direction.
 * `default="none"` keeps unrelated transitions (a Suspense reveal, a deferred
 * value) from cross-fading the whole page.
 */
export function RouteTransition({ children, depth }: Props) {
  const route = useSelectedLayoutSegments().slice(0, depth).join("/");
  return (
    <ViewTransition
      key={route}
      enter={VIEW_TRANSITION_CLASSES.routeEnter}
      exit={VIEW_TRANSITION_CLASSES.routeExit}
      default="none"
    >
      {children}
    </ViewTransition>
  );
}
