import type { paths } from "@/lib/api/schema";
import type { Schemas } from "@/lib/api/schemas";

export type Order = Schemas["OrderView"];
export type OrderPage = Schemas["KeysetCursorPageOfOrderView"];

export const ORDER_SORTS = ["ordered", "amount"] as const;
export type OrderSort = (typeof ORDER_SORTS)[number];

// Query parameters of `GET /orders` as the generated API contract defines them.
export type OrdersQuery = NonNullable<
  paths["/orders"]["get"]["parameters"]["query"]
>;

export interface OrderFilters extends Omit<
  OrdersQuery,
  "sort" | "order" | "after" | "limit"
> {
  sort?: OrderSort;
  order?: "asc" | "desc";
  after?: string | null;
  limit?: number;
}
