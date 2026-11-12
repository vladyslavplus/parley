"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import type { Segment, Template } from "@/features/campaigns/types";
import { backendJson, backendVoid } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";

// Errors are thrown as ApiError so the calling form can map codes and field
// errors; the library lists are refreshed after every successful change.
export function useLibraryMutations() {
  const queryClient = useQueryClient();
  const locale = useLocale();

  async function refreshTemplates() {
    await queryClient.invalidateQueries({
      queryKey: queryKeys.templatesAll(),
    });
  }

  async function refreshSegments() {
    await queryClient.invalidateQueries({ queryKey: queryKeys.segmentsAll() });
  }

  async function createTemplate(
    body: Schemas["TemplateRequest"],
  ): Promise<Template> {
    const created = await backendJson<Template>("/templates", {
      method: "POST",
      body,
      locale,
    });
    await refreshTemplates();
    return created;
  }

  async function updateTemplate(
    id: string,
    body: Schemas["TemplateRequest"],
  ): Promise<Template> {
    const updated = await backendJson<Template>(
      "/templates/{id}",
      { method: "PUT", body, locale },
      `/templates/${encodeURIComponent(id)}`,
    );
    await refreshTemplates();
    return updated;
  }

  async function archiveTemplate(id: string): Promise<void> {
    await backendVoid(
      "/templates/{id}",
      { method: "DELETE", locale },
      `/templates/${encodeURIComponent(id)}`,
    );
    await refreshTemplates();
  }

  async function createSegment(
    body: Schemas["SegmentRequest"],
  ): Promise<Segment> {
    const created = await backendJson<Segment>("/segments", {
      method: "POST",
      body,
      locale,
    });
    await refreshSegments();
    return created;
  }

  async function updateSegment(
    id: string,
    body: Schemas["SegmentRequest"],
  ): Promise<Segment> {
    const updated = await backendJson<Segment>(
      "/segments/{id}",
      { method: "PUT", body, locale },
      `/segments/${encodeURIComponent(id)}`,
    );
    await refreshSegments();
    return updated;
  }

  async function archiveSegment(id: string): Promise<void> {
    await backendVoid(
      "/segments/{id}",
      { method: "DELETE", locale },
      `/segments/${encodeURIComponent(id)}`,
    );
    await refreshSegments();
  }

  return {
    createTemplate,
    updateTemplate,
    archiveTemplate,
    createSegment,
    updateSegment,
    archiveSegment,
  };
}
