import { toNumber } from "@/features/campaigns/types";
import type { MlSettings } from "@/features/insights/types";

export interface MlConflictResolution {
  // The admin's own choice: kept, never overwritten by the reload.
  desiredEnabled: boolean;
  // The version to send with the next save: the one the server holds now.
  version: number;
  // The state the server holds now, to explain what changed elsewhere.
  serverEnabled: boolean;
  // The server's state differs from the choice the admin made.
  changedElsewhere: boolean;
}

// Resolves a 409 version conflict on the ML switch. The reloaded version
// replaces the stale one, while the admin's chosen state stays in place so the
// input is not lost; the admin then saves again on purpose.
export function resolveMlConflict(
  desiredEnabled: boolean,
  server: MlSettings,
): MlConflictResolution {
  return {
    desiredEnabled,
    version: toNumber(server.version),
    serverEnabled: server.mlEnabled,
    changedElsewhere: server.mlEnabled !== desiredEnabled,
  };
}
