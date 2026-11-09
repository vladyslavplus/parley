import type { ButtonHTMLAttributes, Ref } from "react";

import { cn } from "@/lib/utils";

export type ButtonVariant = "primary" | "secondary" | "danger" | "link";

const VARIANTS: Record<ButtonVariant, string> = {
  primary:
    "bg-primary text-primary-foreground hover:opacity-90 transition-opacity",
  secondary:
    "border border-input bg-card text-foreground hover:bg-muted transition-colors",
  danger:
    "bg-destructive text-destructive-foreground hover:opacity-90 transition-opacity",
  link: "text-foreground underline underline-offset-4 hover:text-muted-foreground",
};

export function buttonVariants({
  variant = "primary",
  className,
}: {
  variant?: ButtonVariant;
  className?: string;
} = {}): string {
  return cn(
    "inline-flex h-[var(--control-height)] items-center justify-center gap-1.5 whitespace-nowrap rounded-[var(--radius-control)] px-3 text-sm font-medium focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring disabled:cursor-not-allowed disabled:opacity-50 transition-colors cursor-pointer select-none",
    VARIANTS[variant],
    className,
  );
}

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  ref?: Ref<HTMLButtonElement>;
}

export function Button({
  variant = "primary",
  className,
  type = "button",
  ref,
  ...props
}: ButtonProps) {
  return (
    <button
      ref={ref}
      type={type}
      className={buttonVariants({ variant, className })}
      {...props}
    />
  );
}
