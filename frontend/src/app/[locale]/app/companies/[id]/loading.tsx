import { Skeleton } from "@/components/ui/skeleton";

export default function CompanyDetailLoading() {
  return (
    <div className="space-y-6">
      <Skeleton className="h-5 w-28" />
      <div className="rounded-[var(--radius-card)] border border-border bg-card p-6 space-y-4">
        <Skeleton className="h-7 w-64" />
        <Skeleton className="h-4 w-40" />
        <div className="grid grid-cols-2 md:grid-cols-3 gap-4 pt-4 border-t border-border">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      </div>
      <Skeleton className="h-8 w-80" />
      <Skeleton className="h-48 w-full rounded-[var(--radius-card)]" />
    </div>
  );
}
