import type { InputHTMLAttributes, Ref, TextareaHTMLAttributes } from "react";

import { cn } from "@/lib/utils";

export const CONTROL_HEIGHT = "h-[var(--control-height)]";

export const CONTROL_BASE =
  "w-full rounded-[var(--radius-control)] border bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring disabled:cursor-not-allowed disabled:opacity-50 transition-colors";

const CONTROL = `${CONTROL_HEIGHT} ${CONTROL_BASE}`;

// Trigger of every searchable picker: same box as Input so forms line up.
export const PICKER_TRIGGER = `flex ${CONTROL_HEIGHT} w-full cursor-pointer items-center justify-between gap-2 rounded-[var(--radius-control)] border border-input bg-background px-3 text-left text-sm text-foreground hover:bg-muted/50 focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring disabled:cursor-not-allowed disabled:opacity-50 transition-colors`;

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

export function Textarea({
  className,
  invalid,
  ref,
  ...props
}: TextareaHTMLAttributes<HTMLTextAreaElement> & {
  invalid?: boolean;
  ref?: Ref<HTMLTextAreaElement>;
}) {
  return (
    <textarea
      ref={ref}
      aria-invalid={invalid || undefined}
      className={cn(
        CONTROL_BASE,
        "min-h-20 resize-y py-2",
        invalid ? "border-destructive text-destructive" : "border-input",
        className,
      )}
      {...props}
    />
  );
}
