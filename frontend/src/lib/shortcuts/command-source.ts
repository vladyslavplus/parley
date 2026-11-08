import { useSyncExternalStore } from "react";

export interface CommandItem {
  id: string;
  title: string;
  subtitle?: string;
  group: "navigation" | "actions" | "settings" | "help";
  keywords?: string[];
  shortcut?: string;
  onSelect: () => void;
}

export type CommandSource = () => CommandItem[];

const commandSources = new Set<CommandSource>();
const listeners = new Set<() => void>();
let cachedCommands: CommandItem[] = [];

function updateCache() {
  const items: CommandItem[] = [];
  for (const source of commandSources) {
    items.push(...source());
  }
  cachedCommands = items;
}

function notify() {
  updateCache();
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
  commandSources.add(source);
  notify();
  return () => {
    commandSources.delete(source);
    notify();
  };
}

export function clearRegisteredCommandSources(): void {
  commandSources.clear();
  notify();
}

export function getRegisteredCommands(): CommandItem[] {
  return cachedCommands;
}

export function useRegisteredCommands(): CommandItem[] {
  return useSyncExternalStore(
    subscribe,
    getRegisteredCommands,
    getRegisteredCommands,
  );
}
