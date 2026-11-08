import type { ApiError } from "./api-error";

export interface FormErrorMapping<TField extends string> {
  fields: Partial<Record<TField, string>>;
  formMessage: string | null;
}

// Backend field names may differ in casing or use a dotted path; match by the
// last path segment, case-insensitive.
function normalize(field: string): string {
  const last = field.split(".").pop() ?? field;
  return last.replace(/\[\d+\]$/, "").toLowerCase();
}

export function mapFieldErrors<TField extends string>(
  error: ApiError,
  knownFields: readonly TField[],
): FormErrorMapping<TField> {
  const byName = new Map(knownFields.map((f) => [f.toLowerCase(), f]));
  const fields: Partial<Record<TField, string>> = {};
  let unmatched: string | null = null;

  for (const [field, items] of Object.entries(error.fieldErrors)) {
    const target = byName.get(normalize(field));
    const message = items[0]?.message;
    if (!message) continue;
    if (target) {
      fields[target] ??= message;
    } else {
      unmatched ??= message;
    }
  }

  const hasFields = Object.keys(fields).length > 0;
  return {
    fields,
    formMessage: hasFields && !unmatched ? null : (unmatched ?? error.detail),
  };
}
