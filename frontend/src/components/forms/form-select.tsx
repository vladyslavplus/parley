"use client";

import {
  useController,
  type Control,
  type FieldValues,
  type Path,
} from "react-hook-form";

import { SimpleSelect } from "@/components/ui/select";
import { cn } from "@/lib/utils";

export interface SelectOption {
  value: string;
  label: string;
}

interface FormSelectProps<TValues extends FieldValues> {
  control: Control<TValues>;
  name: Path<TValues>;
  options: readonly SelectOption[];
  placeholder?: string;
  // Injected by FormField.
  id?: string;
  invalid?: boolean;
  required?: boolean;
  "aria-describedby"?: string;
}

// Design-system select bound to react-hook-form; replaces native <select>.
export function FormSelect<TValues extends FieldValues>({
  control,
  name,
  options,
  placeholder,
  id,
  invalid,
  "aria-describedby": describedBy,
}: FormSelectProps<TValues>) {
  const { field } = useController({ control, name });
  return (
    <SimpleSelect
      id={id}
      value={(field.value as string | null | undefined) ?? ""}
      onValueChange={(next) => field.onChange(next)}
      options={options}
      placeholder={placeholder}
      className={cn(invalid && "border-destructive text-destructive")}
      ariaDescribedBy={describedBy}
      invalid={invalid}
    />
  );
}
