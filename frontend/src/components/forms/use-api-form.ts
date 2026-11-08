"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";
import {
  useForm,
  type DefaultValues,
  type FieldValues,
  type Path,
} from "react-hook-form";
import type { ZodType } from "zod";

import { describeError, mapFieldErrors, toApiError } from "@/lib/errors";

interface Options<TValues extends FieldValues> {
  schema: ZodType<TValues, TValues>;
  defaultValues: DefaultValues<TValues>;
  fields: readonly Path<TValues>[];
  submit: (values: TValues) => Promise<void>;
}

// Form wiring shared by every screen: client validation for UX, server field
// errors mapped onto inputs, one form-level message, input kept on failure.
export function useApiForm<TValues extends FieldValues>({
  schema,
  defaultValues,
  fields,
  submit,
}: Options<TValues>) {
  const tKinds = useTranslations("errors.kinds");
  const form = useForm<TValues, unknown, TValues>({
    resolver: zodResolver(schema),
    defaultValues,
    mode: "onBlur",
    reValidateMode: "onChange",
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [errorCode, setErrorCode] = useState<string | null>(null);
  const alertRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (formError) alertRef.current?.focus();
  }, [formError]);

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null);
    setErrorCode(null);
    try {
      await submit(values);
    } catch (caught) {
      const error = toApiError(caught);
      setErrorCode(error.code);
      const mapped = mapFieldErrors(error, fields);
      let focused = false;
      for (const [name, message] of Object.entries(mapped.fields)) {
        form.setError(
          name as Path<TValues>,
          { type: "server", message: message as string },
          { shouldFocus: !focused },
        );
        focused = true;
      }
      if (mapped.formMessage || !focused) {
        setFormError(
          mapped.formMessage ?? describeError(error, (kind) => tKinds(kind)),
        );
      }
    }
  });

  return { form, onSubmit, formError, errorCode, alertRef };
}
