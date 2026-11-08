import type { InputHTMLAttributes, Ref, SelectHTMLAttributes } from "react";

import { cn } from "@/lib/utils";

const CONTROL =
  "w-full rounded-md border bg-white px-3 py-2 text-sm text-neutral-900 focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-neutral-900 disabled:opacity-60";

export function Input({
  className,
  invalid,
  ref,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & {
  invalid?: boolean;
  ref?: Ref<HTMLInputElement>;
}) {
  return (
    <input
      ref={ref}
      aria-invalid={invalid || undefined}
      className={cn(
        CONTROL,
        invalid ? "border-red-700" : "border-neutral-300",
        className,
      )}
      {...props}
    />
  );
}

export function Select({
  className,
  invalid,
  ref,
  ...props
}: SelectHTMLAttributes<HTMLSelectElement> & {
  invalid?: boolean;
  ref?: Ref<HTMLSelectElement>;
}) {
  return (
    <select
      ref={ref}
      aria-invalid={invalid || undefined}
      className={cn(
        CONTROL,
        invalid ? "border-red-700" : "border-neutral-300",
        className,
      )}
      {...props}
    />
  );
}
