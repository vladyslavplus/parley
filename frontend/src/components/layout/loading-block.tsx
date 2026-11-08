import { cn } from "@/lib/utils";

export function LoadingBlock({ className }: { className?: string }) {
  return (
    <div
      aria-busy="true"
      className={cn(
        "h-40 animate-pulse rounded-lg bg-muted motion-reduce:animate-none",
        className,
      )}
    />
  );
}
