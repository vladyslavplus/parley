import { cloneElement, useId, type ReactElement } from "react";

interface ControlProps {
  id?: string;
  invalid?: boolean;
  "aria-describedby"?: string;
  required?: boolean;
}

export function FormField({
  label,
  error,
  hint,
  required,
  children,
}: {
  label: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: ReactElement<ControlProps>;
}) {
  const id = useId();
  const errorId = `${id}-error`;
  const hintId = `${id}-hint`;

  const describedBy = [error && errorId, hint && hintId]
    .filter(Boolean)
    .join(" ");

  return (
    <div className="flex flex-col gap-1.5">
      <label
        htmlFor={id}
        className="text-xs font-medium text-foreground flex items-center gap-1"
      >
        <span>{label}</span>
        {required && (
          <span className="text-destructive font-semibold" aria-hidden="true">
            *
          </span>
        )}
      </label>

      {cloneElement(children, {
        id,
        invalid: Boolean(error),
        required,
        "aria-describedby": describedBy || undefined,
      })}

      {hint && !error && (
        <p id={hintId} className="text-xs text-muted-foreground">
          {hint}
        </p>
      )}

      {error ? (
        <p id={errorId} className="text-xs font-medium text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}
