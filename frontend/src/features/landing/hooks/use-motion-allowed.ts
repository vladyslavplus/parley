import { useSyncExternalStore } from "react";

const REDUCED_MOTION_QUERY = "(prefers-reduced-motion: reduce)";

function subscribe(onChange: () => void) {
  const query = window.matchMedia(REDUCED_MOTION_QUERY);
  query.addEventListener("change", onChange);
  return () => query.removeEventListener("change", onChange);
}

const readClient = () => !window.matchMedia(REDUCED_MOTION_QUERY).matches;
// The server and the hydration pass render the static scene; motion starts
// on the next render, so the markup never mismatches.
const readServer = () => false;

/** True when the visitor has not asked for reduced motion. */
export function useMotionAllowed(): boolean {
  return useSyncExternalStore(subscribe, readClient, readServer);
}
