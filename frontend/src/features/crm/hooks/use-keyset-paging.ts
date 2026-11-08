"use client";

import { parseAsString, useQueryState } from "nuqs";
import { useState } from "react";

export function useKeysetPaging() {
  const [after, setAfter] = useQueryState(
    "after",
    parseAsString.withDefault(""),
  );
  const [cursorHistory, setCursorHistory] = useState<string[]>([]);

  function handleNextPage(nextCursor: string | null) {
    if (!nextCursor) return;
    setCursorHistory((prev) => [...prev, after]);
    void setAfter(nextCursor);
  }

  function handlePreviousPage() {
    if (cursorHistory.length === 0) {
      void setAfter("");
      return;
    }
    const prevCursor = cursorHistory[cursorHistory.length - 1];
    setCursorHistory((prev) => prev.slice(0, -1));
    void setAfter(prevCursor);
  }

  function resetPaging() {
    void setAfter("");
    setCursorHistory([]);
  }

  return {
    after,
    setAfter,
    cursorHistory,
    hasPreviousPage: cursorHistory.length > 0 || Boolean(after),
    handleNextPage,
    handlePreviousPage,
    resetPaging,
  };
}
