"use client";

import { useFormatter, useTranslations } from "next-intl";

import { LoadingBlock } from "@/components/layout/loading-block";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { useAuditEvents, useMe } from "@/lib/api/hooks/use-api";
import { displayTimeZone, parseUtc } from "@/lib/time-zones";

export function AuditPanel() {
  const t = useTranslations("settings.audit");
  const format = useFormatter();
  const events = useAuditEvents();
  const { data: me } = useMe();
  const timeZone = displayTimeZone(me);

  if (events.isPending) return <LoadingBlock />;
  if (events.isError) return <Alert>{t("loadFailed")}</Alert>;
  const rows = events.data.pages.flatMap((page) => page.items);
  if (rows.length === 0)
    return <p className="text-sm text-neutral-600">{t("empty")}</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-neutral-200">
              <th scope="col" className="py-2 pr-3">
                {t("columns.time")}
              </th>
              <th scope="col" className="py-2 pr-3">
                {t("columns.action")}
              </th>
              <th scope="col" className="py-2 pr-3">
                {t("columns.target")}
              </th>
              <th scope="col" className="py-2">
                {t("columns.actor")}
              </th>
            </tr>
          </thead>
          <tbody>
            {rows.map((event) => (
              <tr key={event.id} className="border-b border-neutral-100">
                <td className="py-2 pr-3">
                  {format.dateTime(parseUtc(event.occurredAt), {
                    dateStyle: "medium",
                    timeStyle: "medium",
                    timeZone,
                  })}
                </td>
                <td className="py-2 pr-3">{event.action}</td>
                <td className="py-2 pr-3">{event.targetType ?? ""}</td>
                <td className="py-2">{event.actorUserId ?? ""}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {events.hasNextPage ? (
        <Button
          variant="secondary"
          onClick={() => void events.fetchNextPage()}
          disabled={events.isFetchingNextPage}
        >
          {t("loadMore")}
        </Button>
      ) : null}
    </div>
  );
}
