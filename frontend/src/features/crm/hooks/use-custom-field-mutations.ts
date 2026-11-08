"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { toast } from "sonner";

import { queryKeys } from "@/constants/query-keys";
import { backendJson, backendVoid } from "@/lib/api/bff-client";
import { describeError, toApiError } from "@/lib/errors";
import type { CustomFieldDefinition } from "@/features/crm/types";
import type { Schemas } from "@/lib/api/schemas";

export interface CreateCustomFieldInput {
  entityType: "contact" | "company";
  key: string;
  label: string;
  fieldType: "text" | "number" | "boolean" | "select" | "date";
  options?: string[] | null;
  isRequired?: boolean;
  position?: number;
}

export interface UpdateCustomFieldInput {
  label: string;
  options?: string[] | null;
  isRequired?: boolean;
  position?: number;
}

export function useCustomFieldMutations() {
  const locale = useLocale();
  const queryClient = useQueryClient();
  const tErrors = useTranslations("errors.kinds");

  async function createCustomField(
    data: CreateCustomFieldInput,
  ): Promise<CustomFieldDefinition> {
    try {
      const body: Schemas["CustomFieldRequest"] = {
        entityType: data.entityType,
        key: data.key,
        label: data.label,
        fieldType: data.fieldType,
        options: data.options ?? null,
        isRequired: data.isRequired ?? false,
        position: data.position ?? 0,
      };

      const created = await backendJson<CustomFieldDefinition>(
        "/custom-fields",
        {
          method: "POST",
          body,
          locale,
        },
      );

      await queryClient.invalidateQueries({
        queryKey: queryKeys.customFieldsAll(),
      });
      return created;
    } catch (err) {
      const apiErr = toApiError(err);
      toast.error(describeError(apiErr, (k) => tErrors(k)));
      throw apiErr;
    }
  }

  async function updateCustomField(
    id: string,
    data: UpdateCustomFieldInput,
  ): Promise<CustomFieldDefinition> {
    try {
      const body: Schemas["UpdateCustomFieldRequest"] = {
        label: data.label,
        options: data.options ?? null,
        isRequired: data.isRequired ?? false,
        position: data.position ?? 0,
      };

      const updated = await backendJson<CustomFieldDefinition>(
        "/custom-fields/{id}",
        {
          method: "PUT",
          body,
          locale,
        },
        `/custom-fields/${encodeURIComponent(id)}`,
      );

      await queryClient.invalidateQueries({
        queryKey: queryKeys.customFieldsAll(),
      });
      return updated;
    } catch (err) {
      const apiErr = toApiError(err);
      toast.error(describeError(apiErr, (k) => tErrors(k)));
      throw apiErr;
    }
  }

  async function archiveCustomField(id: string): Promise<void> {
    try {
      await backendVoid(
        "/custom-fields/{id}/archive",
        { method: "POST", locale },
        `/custom-fields/${encodeURIComponent(id)}/archive`,
      );

      await queryClient.invalidateQueries({
        queryKey: queryKeys.customFieldsAll(),
      });
    } catch (err) {
      const apiErr = toApiError(err);
      toast.error(describeError(apiErr, (k) => tErrors(k)));
      throw apiErr;
    }
  }

  return {
    createCustomField,
    updateCustomField,
    archiveCustomField,
  };
}
