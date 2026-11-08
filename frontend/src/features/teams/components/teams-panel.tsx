"use client";

import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { LoadingBlock } from "@/components/layout/loading-block";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { queryKeys } from "@/constants/query-keys";
import { useAction, useInvalidate, useTeams } from "@/lib/api/hooks/use-api";
import { useApiForm } from "@/components/forms/use-api-form";
import { backendVoid } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import {
  createTeamSchema,
  type TeamValues,
} from "@/features/teams/schemas/team-schema";

function TeamForm({
  initialName,
  submitLabel,
  onSubmitName,
  onCancel,
}: {
  initialName: string;
  submitLabel: string;
  onSubmitName: (name: string) => Promise<void>;
  onCancel?: () => void;
}) {
  const t = useTranslations("settings.teams");
  const tValidation = useTranslations("validation");
  const schema = createTeamSchema(tValidation);

  const { form, onSubmit, formError, alertRef } = useApiForm<TeamValues>({
    schema,
    defaultValues: { name: initialName },
    fields: ["name"],
    submit: async (values) => {
      await onSubmitName(values.name);
      form.reset({ name: initialName });
    },
  });
  const { errors, isSubmitting } = form.formState;

  return (
    <ApiForm
      onSubmit={onSubmit}
      formError={formError}
      alertRef={alertRef}
      pending={isSubmitting}
      submitLabel={submitLabel}
    >
      <FormField label={t("name")} error={errors.name?.message}>
        <Input {...form.register("name")} />
      </FormField>
      {onCancel ? (
        <Button variant="secondary" onClick={onCancel}>
          {t("cancel")}
        </Button>
      ) : null}
    </ApiForm>
  );
}

function TeamRow({ team }: { team: Schemas["TeamView"] }) {
  const t = useTranslations("settings.teams");
  const locale = useLocale();
  const invalidate = useInvalidate();
  const { error, pending, run } = useAction();
  const [editing, setEditing] = useState(false);

  const rename = async (name: string) => {
    await backendVoid(
      "/teams/{id}",
      { method: "PUT", body: { name }, locale },
      `/teams/${team.id}`,
    );
    await invalidate(queryKeys.teams());
    setEditing(false);
  };

  const remove = () => {
    if (!window.confirm(t("deleteConfirm"))) return;
    void run(async () => {
      await backendVoid(
        "/teams/{id}",
        { method: "DELETE", locale },
        `/teams/${team.id}`,
      );
      await invalidate(queryKeys.teams());
    });
  };

  return (
    <li className="flex flex-col gap-2 border-b border-border/40 py-2">
      {error ? <Alert>{error}</Alert> : null}
      {editing ? (
        <TeamForm
          initialName={team.name}
          submitLabel={t("save")}
          onSubmitName={rename}
          onCancel={() => setEditing(false)}
        />
      ) : (
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-sm">{team.name}</span>
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => setEditing(true)}>
              {t("rename")}
            </Button>
            <Button variant="danger" disabled={pending} onClick={remove}>
              {t("delete")}
            </Button>
          </div>
        </div>
      )}
    </li>
  );
}

export function TeamsPanel() {
  const t = useTranslations("settings.teams");
  const locale = useLocale();
  const teams = useTeams();
  const invalidate = useInvalidate();

  const create = async (name: string) => {
    await backendVoid("/teams", { method: "POST", body: { name }, locale });
    await invalidate(queryKeys.teams());
  };

  if (teams.isPending) return <LoadingBlock />;
  if (teams.isError) return <Alert>{t("loadFailed")}</Alert>;
  const rows = teams.data.pages.flatMap((page) => page.items);

  return (
    <div className="flex max-w-lg flex-col gap-6">
      <section aria-labelledby="teams-title" className="flex flex-col gap-2">
        <h2 id="teams-title" className="text-lg font-semibold">
          {t("title")}
        </h2>
        {rows.length === 0 ? (
          <p className="text-sm text-muted-foreground">{t("empty")}</p>
        ) : (
          <ul>
            {rows.map((team) => (
              <TeamRow key={team.id} team={team} />
            ))}
          </ul>
        )}
        {teams.hasNextPage ? (
          <Button
            variant="secondary"
            onClick={() => void teams.fetchNextPage()}
            disabled={teams.isFetchingNextPage}
          >
            {t("loadMore")}
          </Button>
        ) : null}
      </section>
      <section
        aria-labelledby="create-team-title"
        className="flex flex-col gap-2"
      >
        <h2 id="create-team-title" className="text-lg font-semibold">
          {t("create")}
        </h2>
        <TeamForm
          initialName=""
          submitLabel={t("create")}
          onSubmitName={create}
        />
      </section>
    </div>
  );
}
