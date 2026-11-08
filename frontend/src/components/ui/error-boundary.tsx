"use client";

import { AlertTriangle, RotateCcw } from "lucide-react";
import { useTranslations } from "next-intl";
import { Component, type ReactNode } from "react";

import { Button } from "./button";

interface Props {
  children: ReactNode;
  fallbackTitle?: string;
  fallbackDescription?: string;
  onReset?: () => void;
}

interface State {
  hasError: boolean;
}

function DefaultErrorFallback({
  title,
  description,
  onReset,
}: {
  title?: string;
  description?: string;
  onReset: () => void;
}) {
  const t = useTranslations("errors.boundary");

  return (
    <div
      role="alert"
      className="flex flex-col items-center justify-center rounded-[var(--radius-card)] border border-destructive/30 bg-destructive/5 p-6 text-center"
    >
      <div className="mb-2 flex h-10 w-10 items-center justify-center rounded-full bg-destructive/10 text-destructive">
        <AlertTriangle className="h-5 w-5" aria-hidden="true" />
      </div>
      <h3 className="text-sm font-semibold text-foreground">
        {title ?? t("title")}
      </h3>
      <p className="mt-1 max-w-sm text-xs text-muted-foreground">
        {description ?? t("description")}
      </p>
      <Button
        variant="secondary"
        onClick={onReset}
        className="mt-4 gap-1.5 text-xs"
      >
        <RotateCcw className="h-3.5 w-3.5" />
        {t("retry")}
      </Button>
    </div>
  );
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  handleReset = (): void => {
    this.setState({ hasError: false });
    this.props.onReset?.();
  };

  render(): ReactNode {
    if (this.state.hasError) {
      return (
        <DefaultErrorFallback
          title={this.props.fallbackTitle}
          description={this.props.fallbackDescription}
          onReset={this.handleReset}
        />
      );
    }

    return this.props.children;
  }
}
