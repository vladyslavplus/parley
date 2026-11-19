// The sidebar remembers an explicit choice in a cookie, so the server renders
// the right width on the first paint. Without a choice the rail follows the
// screen width.
export const SIDEBAR_PREFERENCE = {
  cookie: "parley_sidebar",
  collapsed: "collapsed",
  expanded: "expanded",
  maxAgeSeconds: 365 * 24 * 60 * 60,
} as const;

// The values of `data-sidebar` on the sidebar; the `rail` variant in
// `globals.css` reads them.
export const SIDEBAR_MODES = {
  auto: "auto",
  collapsed: "collapsed",
  expanded: "expanded",
} as const;

// Below this width the sidebar is a rail unless the person chose otherwise.
// Keep it equal to the media query of the `rail` variant in `globals.css`.
export const SIDEBAR_RAIL_QUERY = "(max-width: 1024px)";
