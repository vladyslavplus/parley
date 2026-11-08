import { Skeleton } from "@/components/ui/skeleton";

export default function SettingsLoading() {
  return (
    <div className="space-y-6">
      <div className="border-b border-border pb-4">
        <Skeleton className="h-7 w-40" />
        <Skeleton className="mt-1 h-4 w-64" />
      </div>

      <div className="space-y-4 max-w-lg">
        <div className="space-y-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-9 w-full" />
        </div>
        <div className="space-y-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-9 w-full" />
        </div>
        <div className="space-y-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-9 w-full" />
        </div>
        <Skeleton className="h-9 w-28" />
      </div>
    </div>
  );
}
