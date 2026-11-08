"use client";

import { Check, Edit2, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useRef, useState, type KeyboardEvent } from "react";

import { Button } from "./button";
import { Input } from "./input";
import { cn } from "@/lib/utils";

export interface InlineEditProps {
  value: string;
  onSave: (newValue: string) => void | Promise<void>;
  placeholder?: string;
  ariaLabel?: string;
  className?: string;
}

export function InlineEdit({
  value,
  onSave,
  placeholder,
  ariaLabel,
  className,
}: InlineEditProps) {
  const t = useTranslations("forms.inlineEdit");
  const [isEditing, setIsEditing] = useState(false);
  const [draft, setDraft] = useState(value);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const isCancellingRef = useRef(false);

  const effectivePlaceholder = placeholder ?? t("clickToEdit");
  const effectiveAriaLabel = ariaLabel ?? t("editField");

  function startEditing() {
    isCancellingRef.current = false;
    setDraft(value);
    setIsEditing(true);
  }

  async function handleSave() {
    if (isCancellingRef.current) return;
    setIsEditing(false);
    triggerRef.current?.focus();
    if (draft.trim() !== value.trim()) {
      await onSave(draft.trim());
    }
  }

  function handleCancel() {
    isCancellingRef.current = true;
    setDraft(value);
    setIsEditing(false);
    setTimeout(() => {
      triggerRef.current?.focus();
      isCancellingRef.current = false;
    }, 0);
  }

  function handleKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Enter") {
      e.preventDefault();
      handleSave();
    } else if (e.key === "Escape") {
      e.preventDefault();
      e.stopPropagation();
      handleCancel();
    }
  }

  if (isEditing) {
    return (
      <div className={cn("inline-flex items-center gap-1", className)}>
        <Input
          autoFocus
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={handleKeyDown}
          onBlur={handleSave}
          aria-label={effectiveAriaLabel}
          className="h-8 py-1 text-xs"
        />
        <Button
          variant="secondary"
          className="h-8 w-8 p-0 text-success hover:bg-success/10"
          onMouseDown={(e) => {
            e.preventDefault();
            handleSave();
          }}
          aria-label={t("save")}
        >
          <Check className="h-3.5 w-3.5" />
        </Button>
        <Button
          variant="secondary"
          className="h-8 w-8 p-0 text-muted-foreground hover:bg-muted"
          onMouseDown={(e) => {
            e.preventDefault();
            handleCancel();
          }}
          aria-label={t("cancel")}
        >
          <X className="h-3.5 w-3.5" />
        </Button>
      </div>
    );
  }

  return (
    <button
      ref={triggerRef}
      type="button"
      onClick={startEditing}
      className={cn(
        "group inline-flex items-center gap-1.5 rounded px-2 py-1 text-xs text-foreground hover:bg-muted transition-colors text-left",
        className,
      )}
      aria-label={`${effectiveAriaLabel}: ${value || effectivePlaceholder}`}
    >
      <span className={!value ? "text-muted-foreground italic" : undefined}>
        {value || effectivePlaceholder}
      </span>
      <Edit2 className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
    </button>
  );
}
