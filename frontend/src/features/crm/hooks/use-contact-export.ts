"use client";

import { useLocale, useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";

import { LIMITS } from "@/constants/limits";
import { contactExportDownloadRoute } from "@/constants/routes";
import { HEADERS } from "@/constants/session";
import { backendJson } from "@/lib/api/bff-client";
import { describeError, toApiError } from "@/lib/errors";
import type { Schemas } from "@/lib/api/schemas";
import { showErrorToast } from "@/lib/toasts";

export type ExportStatus =
  | "idle"
  | "starting"
  | "queued"
  | "running"
  | "completed"
  | "failed"
  | "expired";

export interface ContactExportState {
  status: ExportStatus;
  exportId: string | null;
  rowCount: number | null;
  downloadUrl: string | null;
  errorMessage: string | null;
}

export function useContactExport() {
  const locale = useLocale();
  const tErrors = useTranslations("errors.kinds");
  const pollTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const generationRef = useRef(0);

  const [state, setState] = useState<ContactExportState>({
    status: "idle",
    exportId: null,
    rowCount: null,
    downloadUrl: null,
    errorMessage: null,
  });

  function cleanupTimer() {
    if (pollTimerRef.current) {
      clearTimeout(pollTimerRef.current);
      pollTimerRef.current = null;
    }
  }

  useEffect(() => {
    return () => {
      generationRef.current += 1;
      cleanupTimer();
    };
  }, []);

  async function pollStatus(id: string, gen: number) {
    if (gen !== generationRef.current) return;

    try {
      const view = await backendJson<Schemas["ContactExportView"]>(
        "/contacts/exports/{id}",
        {
          method: "GET",
          locale,
        },
        `/contacts/exports/${encodeURIComponent(id)}`,
      );

      if (gen !== generationRef.current) return;

      const isExpired =
        view.expiresAt && new Date(view.expiresAt).getTime() < Date.now();

      if (isExpired) {
        cleanupTimer();
        setState({
          status: "expired",
          exportId: id,
          rowCount: view.rowCount ? Number(view.rowCount) : null,
          downloadUrl: null,
          errorMessage: null,
        });
        return;
      }

      if (view.status === "completed") {
        cleanupTimer();
        setState({
          status: "completed",
          exportId: id,
          rowCount: view.rowCount ? Number(view.rowCount) : null,
          downloadUrl: contactExportDownloadRoute(id),
          errorMessage: null,
        });
        return;
      }

      if (view.status === "failed") {
        cleanupTimer();
        setState({
          status: "failed",
          exportId: id,
          rowCount: null,
          downloadUrl: null,
          errorMessage: null,
        });
        return;
      }

      // Still queued or running
      setState((prev) => ({
        ...prev,
        status: view.status,
        rowCount: view.rowCount ? Number(view.rowCount) : null,
      }));

      cleanupTimer();
      pollTimerRef.current = setTimeout(() => {
        void pollStatus(id, gen);
      }, LIMITS.exportPollIntervalMs);
    } catch (err) {
      if (gen !== generationRef.current) return;
      cleanupTimer();
      const apiErr = toApiError(err);
      setState({
        status: "failed",
        exportId: id,
        rowCount: null,
        downloadUrl: null,
        errorMessage: describeError(apiErr, (k) => tErrors(k)),
      });
    }
  }

  async function startExport(filters: {
    q?: string;
    tagId?: string;
    ownerUserId?: string;
  }) {
    cleanupTimer();
    const gen = ++generationRef.current;

    setState({
      status: "starting",
      exportId: null,
      rowCount: null,
      downloadUrl: null,
      errorMessage: null,
    });

    try {
      const idempotencyKey = crypto.randomUUID();
      const body: Schemas["RequestContactExportRequest"] = {
        search: filters.q ?? null,
        tagId: filters.tagId ?? null,
        companyId: null,
        ownerUserId: filters.ownerUserId ?? null,
        createdFrom: null,
        createdTo: null,
      };

      const view = await backendJson<Schemas["ContactExportView"]>(
        "/contacts/exports",
        {
          method: "POST",
          body,
          headers: {
            [HEADERS.idempotencyKey]: idempotencyKey,
          },
          locale,
        },
      );

      if (gen !== generationRef.current) return;

      setState({
        status: view.status,
        exportId: view.id,
        rowCount: view.rowCount ? Number(view.rowCount) : null,
        downloadUrl: null,
        errorMessage: null,
      });

      if (view.status === "completed") {
        setState({
          status: "completed",
          exportId: view.id,
          rowCount: view.rowCount ? Number(view.rowCount) : null,
          downloadUrl: contactExportDownloadRoute(view.id),
          errorMessage: null,
        });
      } else {
        pollTimerRef.current = setTimeout(() => {
          void pollStatus(view.id, gen);
        }, LIMITS.exportPollIntervalMs);
      }
    } catch (err) {
      if (gen !== generationRef.current) return;
      const apiErr = toApiError(err);
      const msg = describeError(apiErr, (k) => tErrors(k));
      showErrorToast(msg);
      setState({
        status: "failed",
        exportId: null,
        rowCount: null,
        downloadUrl: null,
        errorMessage: msg,
      });
    }
  }

  function reset() {
    generationRef.current += 1;
    cleanupTimer();
    setState({
      status: "idle",
      exportId: null,
      rowCount: null,
      downloadUrl: null,
      errorMessage: null,
    });
  }

  return {
    state,
    startExport,
    reset,
  };
}
