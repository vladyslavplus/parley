// How far the caller's tasks permission reaches, from the `scope` of the
// `tasks.read` grant. The dashboard counts tasks over this scope, so the card
// names it and the link opens the list that matches it.
export const TASKS_SCOPES = ["own", "team", "org"] as const;
export type TasksScope = (typeof TASKS_SCOPES)[number];

export function tasksScopeOf(scope: string | null | undefined): TasksScope {
  if (scope === "own" || scope === "team") return scope;
  // `org`, `organization` and a grant without a scope are organization-wide.
  return scope === undefined ? "own" : "org";
}
