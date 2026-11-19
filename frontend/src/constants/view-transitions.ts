// The class names that `<ViewTransition>` hands to the browser. The animations
// themselves live in `globals.css` under the same names.
export const VIEW_TRANSITION_CLASSES = {
  routeEnter: "route-enter",
  routeExit: "route-exit",
  revealEnter: "reveal-enter",
  revealExit: "reveal-exit",
} as const;
