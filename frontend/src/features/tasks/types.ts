import type { Schemas } from "@/lib/api/schemas";

export type Task = Schemas["TaskView"];
export type TaskPage = Schemas["KeysetCursorPageOfTaskView"];
export type TaskStatus = "open" | "completed" | "overdue";

export interface TaskFilters {
  q?: string;
  assigneeUserId?: string;
  contactId?: string;
  dealId?: string;
  companyId?: string;
  status?: TaskStatus;
  sort?: "due" | "title" | "created";
  order?: "asc" | "desc";
  limit?: number;
  after?: string | null;
}
