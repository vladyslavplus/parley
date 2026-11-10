import type { SegmentFormValues } from "@/features/campaigns/schemas/segment-schema";
import type { SegmentDefinition } from "@/features/campaigns/types";

// Every field present, so it fits both the request body and the read model.
export type CompleteDefinition = Required<
  Omit<SegmentDefinition, "daysSinceLastOrder">
> & { daysSinceLastOrder: number | null };

export function definitionFromValues(
  values: Pick<
    SegmentFormValues,
    "ownerUserIds" | "countryCodes" | "tagIds" | "daysSinceLastOrder"
  >,
  rfmSegments: readonly string[] = [],
): CompleteDefinition {
  const days = values.daysSinceLastOrder.trim();
  return {
    ownerUserIds: values.ownerUserIds,
    countryCodes: values.countryCodes,
    tagIds: values.tagIds,
    rfmSegments: [...rfmSegments],
    daysSinceLastOrder: days === "" ? null : Number(days),
  };
}

export function valuesFromDefinition(
  name: string,
  definition: SegmentDefinition | undefined,
): SegmentFormValues {
  return {
    name,
    ownerUserIds: definition?.ownerUserIds ?? [],
    countryCodes: definition?.countryCodes ?? [],
    tagIds: definition?.tagIds ?? [],
    daysSinceLastOrder:
      definition?.daysSinceLastOrder === null ||
      definition?.daysSinceLastOrder === undefined
        ? ""
        : String(definition.daysSinceLastOrder),
  };
}

export function normalizeCountryCode(input: string): string | null {
  const code = input.trim().toUpperCase();
  return /^[A-Z]{2}$/.test(code) ? code : null;
}
