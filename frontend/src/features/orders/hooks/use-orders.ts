"use client";

import {
  keepPreviousData,
  queryOptions,
  useQuery,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { BACKEND_PATHS } from "@/constants/routes";
import type {
  OrderFilters,
  OrderPage,
  OrdersQuery,
} from "@/features/orders/types";
import { backendJson } from "@/lib/api/bff-client";

export function ordersListQueryOptions(
  filters: OrderFilters = {},
  locale: string,
) {
  const normalized: OrdersQuery = {
    q: filters.q || undefined,
    contactId: filters.contactId || undefined,
    companyId: filters.companyId || undefined,
    orderedFrom: filters.orderedFrom || undefined,
    orderedTo: filters.orderedTo || undefined,
    sort: filters.sort || "ordered",
    order: filters.order || "desc",
    after: filters.after || undefined,
    limit: filters.limit ?? LIMITS.ordersPageSize,
  };

  return queryOptions({
    queryKey: queryKeys.ordersList(normalized),
    queryFn: () =>
      backendJson<OrderPage>(BACKEND_PATHS.orders, {
        locale,
        query: normalized,
      }),
    placeholderData: keepPreviousData,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function useOrders(
  filters: OrderFilters = {},
  options?: { enabled?: boolean },
) {
  const locale = useLocale();
  return useQuery({
    ...ordersListQueryOptions(filters, locale),
    ...(options?.enabled !== undefined ? { enabled: options.enabled } : {}),
  });
}
