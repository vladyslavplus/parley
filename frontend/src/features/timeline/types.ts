import type { Schemas } from "@/lib/api/schemas";

export type Activity = Schemas["ActivityView"];
export type ActivityPage = Schemas["KeysetCursorPageOfActivityView"];
export type Note = Schemas["NoteView"];
export type NotePage = Schemas["KeysetCursorPageOfNoteView"];

export type ActivityType =
  | "contact_created"
  | "deal_stage_changed"
  | "deal_closed"
  | "note_added"
  | "task_completed"
  | "order_recorded";

export interface SubjectRef {
  contactId?: string;
  dealId?: string;
  companyId?: string;
}

export interface ActivityFilters extends SubjectRef {
  type?: string;
  q?: string;
  order?: "asc" | "desc";
  limit?: number;
  after?: string | null;
}
