import { cn } from "@/lib/utils";

export function LoadingBlock({ className }: { className?: string }) {
  return (
    <div
      aria-busy="true"
      className={cn(
        "h-40 animate-pulse rounded-md bg-neutral-100 motion-reduce:animate-none",
        className,
      )}
    />
  );
}
