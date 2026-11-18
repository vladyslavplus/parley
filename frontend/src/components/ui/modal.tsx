"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { X } from "lucide-react";
import { useTranslations } from "next-intl";
import {
  createContext,
  useContext,
  useRef,
  useState,
  type ComponentProps,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";

import { cn } from "@/lib/utils";

import { Button } from "./button";

const ModalFooterContext = createContext<HTMLElement | null>(null);

/**
 * Puts actions in the modal footer from a component rendered in the body, so a
 * form keeps its state local and the primary button still sits in the footer.
 */
export function ModalFooter({ children }: { children: ReactNode }) {
  const node = useContext(ModalFooterContext);
  return node ? createPortal(children, node) : null;
}

export type ModalSize = "sm" | "md" | "lg" | "xl";

// The first field the user can type in: a form modal starts there, not on the
// close cross, while menus and confirmations keep Radix's own choice.
const FIRST_FIELD =
  'input:not([type="hidden"]):not([disabled]), textarea:not([disabled]), select:not([disabled]), [role="combobox"]:not([disabled])';

const SIZE_CLASS: Record<ModalSize, string> = {
  sm: "max-w-sm",
  md: "max-w-md",
  lg: "max-w-lg",
  xl: "max-w-5xl",
};

export interface ModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: ReactNode;
  /** Leading mark in the header: a status icon or an entity tile. */
  icon?: ReactNode;
  children: ReactNode;
  /** Actions row. Form modals put one full-width primary button here. */
  footer?: ReactNode;
  size?: ModalSize;
  /** `alertdialog` for destructive decisions. */
  role?: "dialog" | "alertdialog";
  /**
   * `false` keeps Escape and outside clicks from closing the modal; the
   * explicit close cross, Cancel or Confirm still close it.
   */
  dismissible?: boolean;
  /** Blocks every close path while a request is in flight. */
  closeDisabled?: boolean;
  onOpenAutoFocus?: ComponentProps<typeof Dialog.Content>["onOpenAutoFocus"];
  contentClassName?: string;
  bodyClassName?: string;
}

/**
 * The one modal frame: title, close cross, scrollable body and a footer for
 * actions. Escape, the overlay and the cross close it unless the caller says
 * otherwise; Radix provides the focus trap and focus restore.
 */
export function Modal({
  open,
  onOpenChange,
  title,
  description,
  icon,
  children,
  footer,
  size = "md",
  role = "dialog",
  dismissible = true,
  closeDisabled = false,
  onOpenAutoFocus,
  contentClassName,
  bodyClassName,
}: ModalProps) {
  const tCommon = useTranslations("common");
  const [footerNode, setFooterNode] = useState<HTMLDivElement | null>(null);
  const bodyRef = useRef<HTMLDivElement>(null);

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen && closeDisabled) return;
    onOpenChange(nextOpen);
  }

  function blockImplicitClose(event: { preventDefault: () => void }) {
    event.preventDefault();
  }

  function handleOpenAutoFocus(
    event: Parameters<
      NonNullable<ComponentProps<typeof Dialog.Content>["onOpenAutoFocus"]>
    >[0],
  ) {
    if (onOpenAutoFocus) {
      onOpenAutoFocus(event);
      return;
    }
    const field = bodyRef.current?.querySelector<HTMLElement>(FIRST_FIELD);
    if (!field) return;
    event.preventDefault();
    field.focus();
  }

  return (
    <Dialog.Root open={open} onOpenChange={handleOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/40 backdrop-blur-xs transition-opacity duration-[var(--duration-fast)] ease-[var(--ease-default)] motion-reduce:transition-none data-[state=closed]:opacity-0 data-[state=open]:opacity-100" />
        <Dialog.Content
          role={role}
          onEscapeKeyDown={dismissible ? undefined : blockImplicitClose}
          onPointerDownOutside={dismissible ? undefined : blockImplicitClose}
          onInteractOutside={dismissible ? undefined : blockImplicitClose}
          onOpenAutoFocus={handleOpenAutoFocus}
          className={cn(
            "fixed left-1/2 top-1/2 z-50 flex max-h-[calc(100dvh-2rem)] w-[calc(100vw-2rem)] -translate-x-1/2 -translate-y-1/2 flex-col overflow-hidden rounded-[var(--radius-card)] border border-border bg-card shadow-[var(--shadow-floating)] focus:outline-none",
            SIZE_CLASS[size],
            contentClassName,
          )}
        >
          <div className="flex shrink-0 items-start justify-between gap-4 border-b border-border p-5">
            <div className="flex min-w-0 items-start gap-3">
              {icon ? <div className="shrink-0">{icon}</div> : null}
              <div className="min-w-0 space-y-1">
                <Dialog.Title className="text-base font-semibold text-foreground">
                  {title}
                </Dialog.Title>
                {description ? (
                  <Dialog.Description className="text-xs leading-relaxed text-muted-foreground">
                    {description}
                  </Dialog.Description>
                ) : null}
              </div>
            </div>
            <Dialog.Close asChild>
              <Button
                variant="secondary"
                className="h-7 w-7 shrink-0 p-0"
                aria-label={tCommon("close")}
                disabled={closeDisabled}
              >
                <X className="h-4 w-4" aria-hidden="true" />
              </Button>
            </Dialog.Close>
          </div>

          <div
            ref={bodyRef}
            className={cn(
              "flex min-h-0 flex-1 flex-col overflow-y-auto p-5 [&>*]:shrink-0",
              bodyClassName,
            )}
          >
            <ModalFooterContext value={footerNode}>
              {children}
            </ModalFooterContext>
          </div>

          <div
            ref={setFooterNode}
            className="shrink-0 border-t border-border bg-muted/30 p-4 empty:hidden"
          >
            {footer}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
