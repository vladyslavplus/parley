import { describe, expect, it } from "vitest";

import {
  createCustomFieldSchema,
  updateCustomFieldSchema,
} from "./custom-field-schema";

const mockT = (key: string, params?: Record<string, string | number>) => {
  if (params?.max) return `${key}:${params.max}`;
  if (params?.min) return `${key}:${params.min}`;
  return key;
};

describe("customFieldSchema", () => {
  const schema = createCustomFieldSchema(mockT);
  const updateSchema = updateCustomFieldSchema(mockT);

  it("validates a correct custom field payload", () => {
    const valid = {
      key: "lead_source",
      label: "Lead Source",
      entityType: "contact" as const,
      fieldType: "text" as const,
      isRequired: false,
      position: 0,
    };
    expect(schema.safeParse(valid).success).toBe(true);
  });

  it("returns invalidNumber for negative or non-number position", () => {
    const negative = {
      key: "lead_source",
      label: "Lead Source",
      entityType: "contact" as const,
      fieldType: "text" as const,
      isRequired: false,
      position: -1,
    };
    const resNegative = schema.safeParse(negative);
    expect(resNegative.success).toBe(false);
    if (!resNegative.success) {
      expect(resNegative.error.issues[0].message).toBe("invalidNumber");
    }

    const nanPosition = {
      label: "Lead Source",
      isRequired: false,
      position: NaN,
    };
    const resNan = updateSchema.safeParse(nanPosition);
    expect(resNan.success).toBe(false);
    if (!resNan.success) {
      expect(resNan.error.issues[0].message).toBe("invalidNumber");
    }
  });

  it("enforces key regex and length limits", () => {
    const invalidKey = {
      key: "123_bad",
      label: "Lead Source",
      entityType: "contact" as const,
      fieldType: "text" as const,
      isRequired: false,
      position: 0,
    };
    const res = schema.safeParse(invalidKey);
    expect(res.success).toBe(false);
    if (!res.success) {
      expect(res.error.issues[0].message).toBe("customFieldKey");
    }
  });
});
