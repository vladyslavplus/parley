import { defineConfig, globalIgnores } from "eslint/config";
import nextTs from "eslint-config-next/typescript";
import nextVitals from "eslint-config-next/core-web-vitals";
import globals from "globals";

export default defineConfig([
  ...nextVitals,
  ...nextTs,
  {
    files: [
      "scripts/**/*.mjs",
      "*.config.ts",
      "*.config.mts",
      "vitest.setup.ts",
    ],
    languageOptions: {
      globals: { ...globals.node },
    },
  },
  {
    files: ["src/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          paths: [
            {
              name: "react",
              importNames: ["useMemo", "useCallback", "memo"],
              message:
                "React Compiler memoizes automatically; compute values directly in render.",
            },
          ],
        },
      ],
      "no-restricted-syntax": [
        "error",
        {
          selector:
            "MemberExpression[object.name='React'][property.name=/^(useMemo|useCallback|memo)$/]",
          message:
            "React Compiler memoizes automatically; compute values directly in render.",
        },
      ],
    },
  },
  globalIgnores([
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
    "src/lib/api/schema.d.ts",
  ]),
]);
