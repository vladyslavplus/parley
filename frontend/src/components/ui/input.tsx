import type { InputHTMLAttributes, Ref, SelectHTMLAttributes } from "react";

import { cn } from "@/lib/utils";

const CONTROL =
  "w-full rounded-[var(--radius-control)] border bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring disabled:cursor-not-allowed disabled:opacity-50 transition-colors";

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
        invalid ? "border-destructive text-destructive" : "border-input",
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
        invalid ? "border-destructive text-destructive" : "border-input",
        className,
      )}
      {...props}
    />
  );
}
