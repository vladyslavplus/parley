import type { ComponentProps, ReactNode, Ref } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";

export function ApiForm({
  id,
  onSubmit,
  formError,
  alertRef,
  pending,
  submitDisabled,
  submitLabel,
  children,
}: {
  /** Set when the submit button lives outside the form, e.g. in a modal footer. */
  id?: string;
  onSubmit: ComponentProps<"form">["onSubmit"];
  formError: string | null;
  alertRef: Ref<HTMLDivElement>;
  pending: boolean;
  submitDisabled?: boolean;
  /** Omit to render the submit button elsewhere (`<Button type="submit" form={id}>`). */
  submitLabel?: string;
  children: ReactNode;
}) {
  return (
    <form
      id={id}
      onSubmit={onSubmit}
      noValidate
      className="flex flex-col gap-4"
    >
      {formError ? <Alert ref={alertRef}>{formError}</Alert> : null}
      {children}
      {submitLabel ? (
        <Button type="submit" disabled={pending || Boolean(submitDisabled)}>
          {submitLabel}
        </Button>
      ) : null}
    </form>
  );
}
