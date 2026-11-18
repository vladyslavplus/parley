import { ListTodo, Mail, Phone, Users, type LucideIcon } from "lucide-react";

// The task types the API knows. The OpenAPI schema types the code as a plain
// string, so the list lives here; an unknown code reads as a to-do.
export const TASK_TYPES = ["call", "email", "meeting", "todo"] as const;
export type TaskType = (typeof TASK_TYPES)[number];

export const DEFAULT_TASK_TYPE: TaskType = "todo";

export function isTaskType(value: unknown): value is TaskType {
  return TASK_TYPES.some((type) => type === value);
}

export function taskTypeOf(value: string | null | undefined): TaskType {
  return isTaskType(value) ? value : DEFAULT_TASK_TYPE;
}

export const TASK_TYPE_ICONS: Record<TaskType, LucideIcon> = {
  call: Phone,
  email: Mail,
  meeting: Users,
  todo: ListTodo,
};

export const TASK_TYPE_LABEL_KEYS = {
  call: "types.call",
  email: "types.email",
  meeting: "types.meeting",
  todo: "types.todo",
} as const;
