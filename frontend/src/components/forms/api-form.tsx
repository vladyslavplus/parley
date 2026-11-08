import type { ComponentProps, ReactNode, Ref } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";

export function ApiForm({
  onSubmit,
  formError,
  alertRef,
  pending,
  submitLabel,
  children,
}: {
  onSubmit: ComponentProps<"form">["onSubmit"];
  formError: string | null;
  alertRef: Ref<HTMLDivElement>;
  pending: boolean;
  submitLabel: string;
  children: ReactNode;
}) {
  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
      {formError ? <Alert ref={alertRef}>{formError}</Alert> : null}
      {children}
      <Button type="submit" disabled={pending}>
        {submitLabel}
      </Button>
    </form>
  );
}
