"use client";

import { useRef, type KeyboardEvent, type ReactNode } from "react";
import { cn } from "@/lib/utils";

export interface TabItem<K extends string = string> {
  key: K;
  label: string;
  content: ReactNode;
}

interface Props<K extends string = string> {
  tabs: TabItem<K>[];
  activeTab: K;
  onTabChange: (key: K) => void;
  className?: string;
}

export function RecordTabs<K extends string = string>({
  tabs,
  activeTab,
  onTabChange,
  className,
}: Props<K>) {
  const tabListRef = useRef<HTMLDivElement>(null);

  function handleKeyDown(
    e: KeyboardEvent<HTMLButtonElement>,
    currentIndex: number,
  ) {
    let targetIndex = -1;
    if (e.key === "ArrowRight") {
      targetIndex = (currentIndex + 1) % tabs.length;
    } else if (e.key === "ArrowLeft") {
      targetIndex = (currentIndex - 1 + tabs.length) % tabs.length;
    } else if (e.key === "Home") {
      targetIndex = 0;
    } else if (e.key === "End") {
      targetIndex = tabs.length - 1;
    }

    if (targetIndex >= 0) {
      e.preventDefault();
      const targetTab = tabs[targetIndex];
      onTabChange(targetTab.key);
      const tabElements =
        tabListRef.current?.querySelectorAll<HTMLButtonElement>('[role="tab"]');
      tabElements?.[targetIndex]?.focus();
    }
  }

  const currentTab = tabs.find((t) => t.key === activeTab) ?? tabs[0];

  return (
    <div className={cn("space-y-4", className)}>
      <div
        ref={tabListRef}
        role="tablist"
        aria-orientation="horizontal"
        className="flex border-b border-border gap-1 overflow-x-auto"
      >
        {tabs.map((tab, idx) => {
          const isSelected = tab.key === activeTab;
          return (
            <button
              key={tab.key}
              role="tab"
              id={`tab-${tab.key}`}
              aria-controls={`panel-${tab.key}`}
              aria-selected={isSelected}
              tabIndex={isSelected ? 0 : -1}
              type="button"
              onClick={() => onTabChange(tab.key)}
              onKeyDown={(e) => handleKeyDown(e, idx)}
              className={cn(
                "px-4 py-2.5 text-xs font-medium border-b-2 transition-colors cursor-pointer whitespace-nowrap focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring",
                isSelected
                  ? "border-primary text-foreground font-semibold"
                  : "border-transparent text-muted-foreground hover:text-foreground",
              )}
            >
              {tab.label}
            </button>
          );
        })}
      </div>

      {currentTab && (
        <div
          role="tabpanel"
          id={`panel-${currentTab.key}`}
          aria-labelledby={`tab-${currentTab.key}`}
          tabIndex={0}
          className="focus-visible:outline-none"
        >
          {currentTab.content}
        </div>
      )}
    </div>
  );
}
