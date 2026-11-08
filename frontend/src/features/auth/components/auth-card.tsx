import type { ReactNode } from "react";

export function AuthCard({
  title,
  description,
  children,
  footer,
}: {
  title: string;
  description?: string;
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <section className="flex flex-col gap-6">
      <header className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold text-neutral-900">{title}</h1>
        {description ? (
          <p className="text-sm text-neutral-600">{description}</p>
        ) : null}
      </header>
      {children}
      {footer ? (
        <footer className="text-sm text-neutral-600">{footer}</footer>
      ) : null}
    </section>
  );
}
