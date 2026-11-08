import type { Schemas } from "@/lib/api/schemas";

export type Contact = Schemas["ContactView"];
export type Company = Schemas["CompanyView"];
export type Tag = Schemas["TagView"];
export type CustomFieldDefinition = Schemas["CustomFieldDefinitionView"];

export type ContactPage = Schemas["KeysetCursorPageOfContactView"];
export type CompanyPage = Schemas["KeysetCursorPageOfCompanyView"];

export interface ContactFilters {
  q?: string;
  tagId?: string;
  companyId?: string;
  ownerUserId?: string;
  createdFrom?: string;
  createdTo?: string;
  sort?: string;
  order?: "asc" | "desc";
  after?: string | null;
  limit?: number;
}

export interface CompanyFilters {
  q?: string;
  ownerUserId?: string;
  sort?: string;
  order?: "asc" | "desc";
  after?: string | null;
  limit?: number;
}
