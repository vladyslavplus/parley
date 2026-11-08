import { useSyncExternalStore } from "react";

export type QuickCreateKey = "contact" | "deal" | "task";

export interface CreateTarget {
  id: string;
  titleKey: QuickCreateKey;
  action: () => void;
}

const createTargets = new Map<string, CreateTarget>();
const listeners = new Set<() => void>();
let cachedTargets: CreateTarget[] = [];

function updateCache() {
  cachedTargets = Array.from(createTargets.values());
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

export function registerCreateTarget(target: CreateTarget): () => void {
  createTargets.set(target.id, target);
  notify();
  return () => {
    createTargets.delete(target.id);
    notify();
  };
}

export function getRegisteredCreateTargets(): CreateTarget[] {
  return cachedTargets;
}

export function clearRegisteredCreateTargets(): void {
  createTargets.clear();
  notify();
}

export function useRegisteredCreateTargets(): CreateTarget[] {
  return useSyncExternalStore(
    subscribe,
    getRegisteredCreateTargets,
    getRegisteredCreateTargets,
  );
}
