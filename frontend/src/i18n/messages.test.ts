import { readFileSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

import { MESSAGE_NAMESPACES } from "./messages";
import { LOCALES } from "./routing";

type Tree = { [key: string]: string | Tree };

function load(locale: string, namespace: string): Tree {
  const file = path.join(
    process.cwd(),
    "messages",
    locale,
    `${namespace}.json`,
  );
  return JSON.parse(readFileSync(file, "utf8")) as Tree;
}

function flatten(tree: Tree, prefix = ""): Map<string, string> {
  const result = new Map<string, string>();
  for (const [key, value] of Object.entries(tree)) {
    const full = prefix ? `${prefix}.${key}` : key;
    if (typeof value === "string") result.set(full, value);
    else for (const [k, v] of flatten(value, full)) result.set(k, v);
  }
  return result;
}

const placeholders = (text: string) =>
  [...text.matchAll(/\{(\w+)\}/g)].map((match) => match[1]).sort();

describe("message files", () => {
  it.each(MESSAGE_NAMESPACES)(
    "%s has identical keys and placeholders in every locale",
    (namespace) => {
      const [first, ...rest] = LOCALES.map((locale) =>
        flatten(load(locale, namespace)),
      );
      for (const other of rest) {
        expect([...other.keys()].sort()).toEqual([...first.keys()].sort());
        for (const [key, text] of first) {
          expect(placeholders(other.get(key) ?? "")).toEqual(
            placeholders(text),
          );
        }
      }
    },
  );
});
