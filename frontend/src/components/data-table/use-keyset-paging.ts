"use client";

import { parseAsNumberLiteral, parseAsString, useQueryState } from "nuqs";
import { useState } from "react";

import { LIMITS } from "@/constants/limits";

type ValidPageSize = (typeof LIMITS.tablePageSizes)[number];

function isValidPageSize(size: number): size is ValidPageSize {
  return (LIMITS.tablePageSizes as readonly number[]).includes(size);
}

export function useKeysetPaging(paramName = "after") {
  const [cursor, setCursor] = useQueryState(
    paramName,
    parseAsString.withDefault(""),
  );
  const [pageSize, setPageSizeState] = useQueryState(
    "size",
    parseAsNumberLiteral(LIMITS.tablePageSizes).withDefault(
      LIMITS.defaultTablePageSize,
    ),
  );
  const [cursorHistory, setCursorHistory] = useState<string[]>([]);

  function setPageSize(newSize: number) {
    void setCursor("");
    setCursorHistory([]);
    const validSize: ValidPageSize = isValidPageSize(newSize)
      ? newSize
      : LIMITS.defaultTablePageSize;
    void setPageSizeState(validSize);
  }

  function handleNextPage(nextCursor: string | null) {
    if (!nextCursor) return;
    setCursorHistory((prev) => [...prev, cursor]);
    void setCursor(nextCursor);
  }

  function handlePreviousPage() {
    if (cursorHistory.length === 0) {
      void setCursor("");
      return;
    }
    const prevCursor = cursorHistory[cursorHistory.length - 1];
    setCursorHistory((prev) => prev.slice(0, -1));
    void setCursor(prevCursor);
  }

  function resetPaging() {
    void setCursor("");
    setCursorHistory([]);
  }

  return {
    after: cursor,
    cursor,
    setAfter: setCursor,
    setCursor,
    pageSize,
    setPageSize,
    cursorHistory,
    hasPreviousPage: cursorHistory.length > 0 || Boolean(cursor),
    handleNextPage,
    handlePreviousPage,
    resetPaging,
  };
}
