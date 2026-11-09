import { Search, X } from "lucide-react";
import type { InputHTMLAttributes, Ref } from "react";

import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

interface Props extends Omit<
  InputHTMLAttributes<HTMLInputElement>,
  "value" | "onChange"
> {
  value: string;
  onValueChange: (value: string) => void;
  clearLabel: string;
  inputRef?: Ref<HTMLInputElement>;
}

// Search field at the top of every popover list. The clear button gets its own
// gutter so it never touches the typed text.
export function PickerSearch({
  value,
  onValueChange,
  clearLabel,
  inputRef,
  className,
  ...inputProps
}: Props) {
  return (
    <div className="relative border-b border-border px-2 py-1.5">
      <Search
        aria-hidden="true"
        className="pointer-events-none absolute left-4 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground"
      />
      <Input
        ref={inputRef}
        value={value}
        onChange={(event) => onValueChange(event.target.value)}
        className={cn(
          "h-8 border-0 bg-transparent pl-7 pr-9 text-xs shadow-none focus-visible:outline-none",
          className,
        )}
        {...inputProps}
      />
      {value ? (
        <button
          type="button"
          onClick={() => onValueChange("")}
          aria-label={clearLabel}
          className="absolute right-3 top-1/2 flex h-6 w-6 -translate-y-1/2 cursor-pointer items-center justify-center rounded-[var(--radius-control)] text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
        >
          <X className="h-3.5 w-3.5" />
        </button>
      ) : null}
    </div>
  );
}
