"use client";

import { useEffect, useState, useSyncExternalStore } from "react";

export interface CommandItem {
  id: string;
  title: string;
  subtitle?: string | null;
  group: string;
  keywords?: string[];
  shortcut?: string;
  onSelect: () => void;
}

export type CommandSource = () => CommandItem[];
export type SearchSource = (
  query: string,
  signal: AbortSignal,
) => Promise<CommandItem[]>;

const staticSources = new Set<CommandSource>();
const searchSources = new Set<SearchSource>();
const listeners = new Set<() => void>();
let cachedStaticCommands: CommandItem[] = [];

function updateStaticCache() {
  const items: CommandItem[] = [];
  for (const source of staticSources) {
    try {
      const res = source();
      if (Array.isArray(res)) {
        items.push(...res);
      }
    } catch {
      // Ignored for static collection
    }
  }
  cachedStaticCommands = items;
}

function notify() {
  updateStaticCache();
  for (const listener of listeners) {
    listener();
  }
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function registerCommandSource(source: CommandSource): () => void {
  staticSources.add(source);
  notify();
  return () => {
    staticSources.delete(source);
    notify();
  };
}

export function registerSearchSource(source: SearchSource): () => void {
  searchSources.add(source);
  return () => {
    searchSources.delete(source);
  };
}

export function clearRegisteredCommandSources(): void {
  staticSources.clear();
  searchSources.clear();
  notify();
}

export function getRegisteredStaticCommands(): CommandItem[] {
  return cachedStaticCommands;
}

export function useRegisteredCommands(query: string = ""): {
  commands: CommandItem[];
  loading: boolean;
  isError: boolean;
} {
  const staticCommands = useSyncExternalStore(
    subscribe,
    getRegisteredStaticCommands,
    getRegisteredStaticCommands,
  );

  const [dynamicCommands, setDynamicCommands] = useState<CommandItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [isError, setIsError] = useState(false);

  const trimmedQuery = query.trim();

  useEffect(() => {
    if (!trimmedQuery || searchSources.size === 0) {
      return;
    }

    const controller = new AbortController();
    let active = true;

    queueMicrotask(() => {
      if (active) {
        setLoading(true);
        setIsError(false);
        setDynamicCommands([]);
      }
    });

    const promises = Array.from(searchSources).map((source) =>
      source(trimmedQuery, controller.signal),
    );

    Promise.all(promises)
      .then((results) => {
        if (!active) return;
        setDynamicCommands(results.flat());
        setLoading(false);
        setIsError(false);
      })
      .catch(() => {
        if (!active || controller.signal.aborted) return;
        setLoading(false);
        setIsError(true);
      });

    return () => {
      active = false;
      controller.abort();
    };
  }, [trimmedQuery]);

  const allCommands = trimmedQuery
    ? [...staticCommands, ...dynamicCommands]
    : staticCommands;

  return {
    commands: allCommands,
    loading: trimmedQuery ? loading : false,
    isError: trimmedQuery ? isError : false,
  };
}
