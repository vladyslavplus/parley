"use client";

import { X } from "lucide-react";
import { useTranslations } from "next-intl";
import { Drawer as VaulDrawer } from "vaul";
import type { ReactNode } from "react";

import { Button } from "./button";
import { isEditableTarget } from "@/lib/shortcuts/is-editable";
import { cn } from "@/lib/utils";

export interface DrawerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  caption?: string;
  title: string;
  description?: string;
  children: ReactNode;
  footer?: ReactNode;
  direction?: "right" | "bottom";
  className?: string;
}

export function Drawer({
  open,
  onOpenChange,
  caption,
  title,
  description,
  children,
  footer,
  direction = "right",
  className,
}: DrawerProps) {
  const tCommon = useTranslations("common");

  return (
    <VaulDrawer.Root
      open={open}
      onOpenChange={onOpenChange}
      direction={direction}
    >
      <VaulDrawer.Portal>
        <VaulDrawer.Overlay className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-sm" />
        <VaulDrawer.Content
          aria-describedby={description ? "drawer-desc" : undefined}
          onEscapeKeyDown={(e) => {
            if (isEditableTarget(e.target)) {
              e.preventDefault();
            }
          }}
          className={cn(
            "fixed z-50 flex flex-col bg-card border-border shadow-floating focus:outline-none",
            direction === "right"
              ? "right-0 top-0 bottom-0 w-full sm:w-[460px] border-l"
              : "bottom-0 left-0 right-0 max-h-[85vh] rounded-t-lg border-t",
            className,
          )}
        >
          {/* Header */}
          <div className="flex items-start justify-between border-b border-border p-5">
            <div className="space-y-1 min-w-0">
              {caption && (
                <span className="block text-[11px] font-medium text-muted-foreground">
                  {caption}
                </span>
              )}
              <VaulDrawer.Title className="text-base font-semibold text-foreground truncate">
                {title}
              </VaulDrawer.Title>
              {description && (
                <VaulDrawer.Description
                  id="drawer-desc"
                  className="text-xs text-muted-foreground"
                >
                  {description}
                </VaulDrawer.Description>
              )}
            </div>
            <Button
              variant="secondary"
              className="h-7 w-7 p-0 shrink-0"
              onClick={() => onOpenChange(false)}
              aria-label={tCommon("close")}
            >
              <X className="h-4 w-4" />
            </Button>
          </div>

          {/* Body */}
          <div className="flex-1 overflow-y-auto p-5 space-y-4">{children}</div>

          {/* Optional Footer */}
          {footer && (
            <div className="border-t border-border p-4 bg-muted/30">
              {footer}
            </div>
          )}
        </VaulDrawer.Content>
      </VaulDrawer.Portal>
    </VaulDrawer.Root>
  );
}
