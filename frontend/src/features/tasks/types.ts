import type { TaskType } from "@/features/tasks/lib/task-types";
import type { Schemas } from "@/lib/api/schemas";

export type Task = Schemas["TaskView"];
export type TaskPage = Schemas["KeysetCursorPageOfTaskView"];
export type TaskOverdueCount = Schemas["TaskOverdueCountView"];
export type TaskStatus = "open" | "completed" | "overdue";

// What the overdue count shares with the list: the search and the links. The
// status, sort and paging of the list never apply to a count.
export type OverdueCountFilters = Pick<
  TaskFilters,
  "q" | "assigneeUserId" | "contactId" | "dealId" | "companyId" | "type"
>;

export interface TaskFilters {
  q?: string;
  assigneeUserId?: string;
  contactId?: string;
  dealId?: string;
  companyId?: string;
  status?: TaskStatus;
  type?: TaskType;
  sort?: "due" | "title" | "created";
  order?: "asc" | "desc";
  limit?: number;
  after?: string | null;
}
