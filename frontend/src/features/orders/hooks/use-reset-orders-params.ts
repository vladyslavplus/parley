"use client";

import { parseAsString, useQueryStates } from "nuqs";

// URL params owned by the orders panel; cleared when its tab is left.
const ORDERS_URL_PARAMS = {
  q: parseAsString,
  sort: parseAsString,
  order: parseAsString,
  after: parseAsString,
  size: parseAsString,
  orderedFrom: parseAsString,
  orderedTo: parseAsString,
};

export function useResetOrdersParams() {
  const [, setParams] = useQueryStates(ORDERS_URL_PARAMS);
  return () =>
    setParams({
      q: null,
      sort: null,
      order: null,
      after: null,
      size: null,
      orderedFrom: null,
      orderedTo: null,
    });
}
