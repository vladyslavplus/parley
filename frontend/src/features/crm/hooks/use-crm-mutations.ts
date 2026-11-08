"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { toast } from "sonner";

import { ERROR_CODES } from "@/constants/error-codes";
import { queryKeys } from "@/constants/query-keys";
import { backendJson, backendVoid } from "@/lib/api/bff-client";
import { describeError, toApiError, type ApiError } from "@/lib/errors";
import type { Company, Contact, Tag } from "@/features/crm/types";
import type { Schemas } from "@/lib/api/schemas";

export interface CreateContactInput {
  firstName: string;
  lastName?: string | null;
  email?: string | null;
  phone?: string | null;
  jobTitle?: string | null;
  country?: string | null;
  city?: string | null;
  companyId?: string | null;
  ownerUserId?: string | null;
  customFields?: Record<string, unknown> | null;
}

export interface UpdateContactInput {
  version: number | string;
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
  phone?: string | null;
  jobTitle?: string | null;
  country?: string | null;
  city?: string | null;
  companyId?: string | null;
  customFields?: Record<string, unknown> | null;
}

export interface CreateCompanyInput {
  name: string;
  domain?: string | null;
  country?: string | null;
  industry?: string | null;
  ownerUserId?: string | null;
  customFields?: Record<string, unknown> | null;
}

export interface UpdateCompanyInput {
  version: number | string;
  name?: string | null;
  domain?: string | null;
  country?: string | null;
  industry?: string | null;
  customFields?: Record<string, unknown> | null;
}

export function useCrmMutations() {
  const locale = useLocale();
  const queryClient = useQueryClient();
  const tCrm = useTranslations("crm");
  const tErrors = useTranslations("errors.kinds");

  function handleConflict(entityId: string, queryKey: readonly unknown[]) {
    toast.error(tCrm("conflict.title"), {
      description: tCrm("conflict.description"),
      action: {
        label: tCrm("conflict.reload"),
        onClick: () => {
          queryClient.invalidateQueries({ queryKey });
        },
      },
    });
    void queryClient.invalidateQueries({ queryKey });
  }

  async function createContact(data: CreateContactInput): Promise<Contact> {
    const body: Schemas["ContactRequest"] = {
      firstName: data.firstName,
      lastName: data.lastName ?? null,
      email: data.email ?? null,
      phone: data.phone ?? null,
      jobTitle: data.jobTitle ?? null,
      country: data.country ?? null,
      city: data.city ?? null,
      companyId: data.companyId ?? null,
      ownerUserId: data.ownerUserId ?? null,
      customFields: (data.customFields as Record<string, never>) ?? null,
    };
    const created = await backendJson<Contact>("/contacts", {
      method: "POST",
      body,
      locale,
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.contactLists() });
    return created;
  }

  async function updateContact(
    id: string,
    data: UpdateContactInput,
  ): Promise<Contact> {
    try {
      const body: Schemas["UpdateContactRequest"] = {
        version: data.version,
        firstName: data.firstName ?? null,
        lastName: data.lastName ?? null,
        email: data.email ?? null,
        phone: data.phone ?? null,
        jobTitle: data.jobTitle ?? null,
        country: data.country ?? null,
        city: data.city ?? null,
        companyId: data.companyId ?? null,
        customFields: (data.customFields as Record<string, never>) ?? null,
      };
      const updated = await backendJson<Contact>(
        "/contacts/{id}",
        {
          method: "PUT",
          body,
          locale,
        },
        `/contacts/${encodeURIComponent(id)}`,
      );
      queryClient.setQueryData(queryKeys.contact(id), updated);
      await queryClient.invalidateQueries({
        queryKey: queryKeys.contactLists(),
      });
      return updated;
    } catch (err) {
      const apiErr = toApiError(err);
      if (apiErr.code === ERROR_CODES.concurrencyConflict) {
        handleConflict(id, queryKeys.contact(id));
      } else {
        toast.error(describeError(apiErr, (k) => tErrors(k)));
      }
      throw apiErr;
    }
  }

  async function deleteContact(id: string): Promise<void> {
    await backendVoid(
      "/contacts/{id}",
      { method: "DELETE", locale },
      `/contacts/${encodeURIComponent(id)}`,
    );
    await queryClient.invalidateQueries({ queryKey: queryKeys.contactLists() });
    await queryClient.invalidateQueries({ queryKey: queryKeys.contact(id) });
  }

  async function restoreContact(id: string): Promise<Contact> {
    const restored = await backendJson<Contact>(
      "/contacts/{id}/restore",
      { method: "POST", locale },
      `/contacts/${encodeURIComponent(id)}/restore`,
    );
    queryClient.setQueryData(queryKeys.contact(id), restored);
    await queryClient.invalidateQueries({ queryKey: queryKeys.contactLists() });
    return restored;
  }

  async function bulkAssignContactOwner(
    ids: string[],
    ownerUserId: string,
  ): Promise<void> {
    await backendVoid("/contacts/bulk-owner", {
      method: "POST",
      body: { ids, ownerUserId },
      locale,
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.contactLists() });
    await queryClient.invalidateQueries({ queryKey: queryKeys.contactsAll() });
  }

  async function bulkDeleteContacts(ids: string[]): Promise<void> {
    await backendVoid("/contacts/bulk-delete", {
      method: "POST",
      body: { ids },
      locale,
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.contactLists() });
    await queryClient.invalidateQueries({ queryKey: queryKeys.contactsAll() });
  }

  async function attachContactTags(
    id: string,
    tagIds: string[],
  ): Promise<Contact> {
    const updated = await backendJson<Contact>(
      "/contacts/{id}/tags",
      {
        method: "POST",
        body: { tagIds },
        locale,
      },
      `/contacts/${encodeURIComponent(id)}/tags`,
    );
    queryClient.setQueryData(queryKeys.contact(id), updated);
    await queryClient.invalidateQueries({ queryKey: queryKeys.contactLists() });
    return updated;
  }

  async function detachContactTag(id: string, tagId: string): Promise<void> {
    await backendVoid(
      "/contacts/{id}/tags/{tagId}",
      { method: "DELETE", locale },
      `/contacts/${encodeURIComponent(id)}/tags/${encodeURIComponent(tagId)}`,
    );
    await queryClient.invalidateQueries({ queryKey: queryKeys.contact(id) });
    await queryClient.invalidateQueries({ queryKey: queryKeys.contactLists() });
  }

  async function createTag(name: string, colorIndex = 0): Promise<Tag> {
    const body: Schemas["TagRequest"] = {
      name,
      colorIndex,
    };
    const created = await backendJson<Tag>("/tags", {
      method: "POST",
      body,
      locale,
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.tags() });
    return created;
  }

  async function createCompany(data: CreateCompanyInput): Promise<Company> {
    const body: Schemas["CompanyRequest"] = {
      name: data.name,
      domain: data.domain ?? null,
      country: data.country ?? null,
      industry: data.industry ?? null,
      ownerUserId: data.ownerUserId ?? null,
      customFields: (data.customFields as Record<string, never>) ?? null,
    };
    const created = await backendJson<Company>("/companies", {
      method: "POST",
      body,
      locale,
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.companyLists() });
    return created;
  }

  async function updateCompany(
    id: string,
    data: UpdateCompanyInput,
  ): Promise<Company> {
    try {
      const body: Schemas["UpdateCompanyRequest"] = {
        version: data.version,
        name: data.name ?? null,
        domain: data.domain ?? null,
        country: data.country ?? null,
        industry: data.industry ?? null,
        customFields: (data.customFields as Record<string, never>) ?? null,
      };
      const updated = await backendJson<Company>(
        "/companies/{id}",
        {
          method: "PUT",
          body,
          locale,
        },
        `/companies/${encodeURIComponent(id)}`,
      );
      queryClient.setQueryData(queryKeys.company(id), updated);
      await queryClient.invalidateQueries({
        queryKey: queryKeys.companyLists(),
      });
      return updated;
    } catch (err) {
      const apiErr = toApiError(err);
      if (apiErr.code === ERROR_CODES.concurrencyConflict) {
        handleConflict(id, queryKeys.company(id));
      } else {
        toast.error(describeError(apiErr, (k) => tErrors(k)));
      }
      throw apiErr;
    }
  }

  async function deleteCompany(id: string): Promise<void> {
    await backendVoid(
      "/companies/{id}",
      { method: "DELETE", locale },
      `/companies/${encodeURIComponent(id)}`,
    );
    await queryClient.invalidateQueries({ queryKey: queryKeys.companyLists() });
    await queryClient.invalidateQueries({ queryKey: queryKeys.company(id) });
  }

  async function restoreCompany(id: string): Promise<Company> {
    const restored = await backendJson<Company>(
      "/companies/{id}/restore",
      { method: "POST", locale },
      `/companies/${encodeURIComponent(id)}/restore`,
    );
    queryClient.setQueryData(queryKeys.company(id), restored);
    await queryClient.invalidateQueries({ queryKey: queryKeys.companyLists() });
    return restored;
  }

  async function bulkAssignCompanyOwner(
    ids: string[],
    ownerUserId: string,
  ): Promise<void> {
    await backendVoid("/companies/bulk-owner", {
      method: "POST",
      body: { ids, ownerUserId },
      locale,
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.companyLists() });
    await queryClient.invalidateQueries({ queryKey: queryKeys.companiesAll() });
  }

  async function bulkDeleteCompanies(ids: string[]): Promise<void> {
    await backendVoid("/companies/bulk-delete", {
      method: "POST",
      body: { ids },
      locale,
    });
    await queryClient.invalidateQueries({ queryKey: queryKeys.companyLists() });
    await queryClient.invalidateQueries({ queryKey: queryKeys.companiesAll() });
  }

  return {
    createContact,
    updateContact,
    deleteContact,
    restoreContact,
    bulkAssignContactOwner,
    bulkDeleteContacts,
    attachContactTags,
    detachContactTag,
    createTag,
    createCompany,
    updateCompany,
    deleteCompany,
    restoreCompany,
    bulkAssignCompanyOwner,
    bulkDeleteCompanies,
  };
}

export type { ApiError };
