"use client";

import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { useCan } from "@/features/session/components/can";
import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { LoadingBlock } from "@/components/layout/loading-block";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input, Select } from "@/components/ui/input";
import {
  MEMBERSHIP_STATUSES,
  PERMISSIONS,
  SYSTEM_ROLES,
} from "@/constants/permissions";
import { queryKeys } from "@/constants/query-keys";
import {
  useAction,
  useInvalidate,
  useInvitations,
  useMe,
  useMembers,
  useTeams,
} from "@/lib/api/hooks/use-api";
import { useApiForm } from "@/components/forms/use-api-form";
import { backendJson, backendVoid } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import {
  createInviteSchema,
  type InviteValues,
} from "@/features/members/schemas/invite-schema";

function RoleOptions() {
  const t = useTranslations("settings.roles");
  return SYSTEM_ROLES.map((code) => (
    <option key={code} value={code}>
      {t(code)}
    </option>
  ));
}

function TeamOptions({ teams }: { teams: Schemas["TeamView"][] }) {
  const t = useTranslations("settings.members");
  return (
    <>
      <option value="">{t("noTeam")}</option>
      {teams.map((team) => (
        <option key={team.id} value={team.id}>
          {team.name}
        </option>
      ))}
    </>
  );
}

function InviteForm({
  teams,
  pendingIds,
}: {
  teams: Schemas["TeamView"][];
  pendingIds: ReadonlySet<string>;
}) {
  const t = useTranslations("settings.members.invite");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const invalidate = useInvalidate();
  const [outcome, setOutcome] = useState<"sent" | "refreshed" | null>(null);
  const schema = createInviteSchema(tValidation);

  const { form, onSubmit, formError, alertRef } = useApiForm<InviteValues>({
    schema,
    defaultValues: { email: "", roleCode: SYSTEM_ROLES[2], teamId: "" },
    fields: ["email", "roleCode", "teamId"],
    submit: async (values) => {
      setOutcome(null);
      const invitation = await backendJson<Schemas["InvitationView"]>(
        "/invitations",
        {
          method: "POST",
          body: { ...values, teamId: values.teamId || null },
          locale,
        },
      );
      await invalidate(queryKeys.invitations());
      form.reset({ ...values, email: "" });
      setOutcome(pendingIds.has(invitation.id) ? "refreshed" : "sent");
    },
  });

  const { errors, isSubmitting } = form.formState;
  return (
    <section
      aria-labelledby="invite-title"
      className="flex max-w-md flex-col gap-3"
    >
      <h2 id="invite-title" className="text-lg font-semibold">
        {t("title")}
      </h2>
      {outcome ? <Alert tone="info">{t(outcome)}</Alert> : null}
      <ApiForm
        onSubmit={onSubmit}
        formError={formError}
        alertRef={alertRef}
        pending={isSubmitting}
        submitLabel={t("submit")}
      >
        <FormField label={t("email")} error={errors.email?.message}>
          <Input type="email" autoComplete="off" {...form.register("email")} />
        </FormField>
        <FormField label={t("role")} error={errors.roleCode?.message}>
          <Select {...form.register("roleCode")}>
            <RoleOptions />
          </Select>
        </FormField>
        <FormField label={t("team")} error={errors.teamId?.message}>
          <Select {...form.register("teamId")}>
            <TeamOptions teams={teams} />
          </Select>
        </FormField>
      </ApiForm>
    </section>
  );
}

export function MembersPanel() {
  const t = useTranslations("settings.members");
  const locale = useLocale();
  const { data: me } = useMe();
  const canTeams = useCan(PERMISSIONS.teamsManage);
  const members = useMembers();
  const invitations = useInvitations();
  const teamsQuery = useTeams(canTeams);
  const invalidate = useInvalidate();
  const { error, pending, run } = useAction();

  const teams = teamsQuery.data?.pages.flatMap((page) => page.items) ?? [];
  const memberRows = members.data?.pages.flatMap((page) => page.items) ?? [];
  const invitationRows =
    invitations.data?.pages.flatMap((page) => page.items) ?? [];

  const updateMember = (id: string, part: "role" | "team", body: object) =>
    run(async () => {
      await backendVoid(
        part === "role" ? "/members/{id}/role" : "/members/{id}/team",
        { method: "PUT", body, locale },
        `/members/${id}/${part}`,
      );
      await invalidate(queryKeys.members());
    });

  const deactivate = (id: string) => {
    if (!window.confirm(t("deactivateConfirm"))) return;
    void run(async () => {
      await backendVoid(
        "/members/{id}/deactivate",
        { method: "POST", locale },
        `/members/${id}/deactivate`,
      );
      await invalidate(queryKeys.members());
    });
  };

  const revoke = (id: string) =>
    run(async () => {
      await backendVoid(
        "/invitations/{id}/revoke",
        { method: "POST", locale },
        `/invitations/${id}/revoke`,
      );
      await invalidate(queryKeys.invitations());
    });

  if (members.isPending) return <LoadingBlock />;
  if (members.isError) {
    return <Alert>{t("loadFailed")}</Alert>;
  }

  return (
    <div className="flex flex-col gap-8">
      {error ? <Alert>{error}</Alert> : null}
      <section aria-labelledby="members-title" className="flex flex-col gap-3">
        <h2 id="members-title" className="text-lg font-semibold">
          {t("title")}
        </h2>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-border">
                <th scope="col" className="py-2 pr-3">
                  {t("columns.member")}
                </th>
                <th scope="col" className="py-2 pr-3">
                  {t("columns.role")}
                </th>
                <th scope="col" className="py-2 pr-3">
                  {t("columns.team")}
                </th>
                <th scope="col" className="py-2 pr-3">
                  {t("columns.status")}
                </th>
                <th scope="col" className="py-2">
                  <span className="sr-only">{t("columns.actions")}</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {memberRows.map((member) => {
                const active = member.status === MEMBERSHIP_STATUSES.active;
                const label =
                  member.displayName ?? member.email ?? member.userId;
                const self = member.userId === me?.user.id;
                return (
                  <tr key={member.id} className="border-b border-border/40">
                    <td className="py-2 pr-3">
                      <div>{label}</div>
                      {member.email && member.email !== label ? (
                        <div className="text-muted-foreground">
                          {member.email}
                        </div>
                      ) : null}
                    </td>
                    <td className="py-2 pr-3">
                      <Select
                        aria-label={t("columns.role")}
                        value={member.roleCode}
                        disabled={pending || !active}
                        onChange={(event) =>
                          void updateMember(member.id, "role", {
                            roleCode: event.target.value,
                          })
                        }
                      >
                        <RoleOptions />
                      </Select>
                    </td>
                    <td className="py-2 pr-3">
                      {canTeams ? (
                        <Select
                          aria-label={t("columns.team")}
                          value={member.teamId ?? ""}
                          disabled={pending || !active}
                          onChange={(event) =>
                            void updateMember(member.id, "team", {
                              teamId: event.target.value || null,
                            })
                          }
                        >
                          <TeamOptions teams={teams} />
                        </Select>
                      ) : (
                        // Team names load only with the team permission.
                        <span>{member.teamId ? t("inTeam") : t("noTeam")}</span>
                      )}
                    </td>
                    <td className="py-2 pr-3">
                      {t(
                        `status.${member.status === MEMBERSHIP_STATUSES.active ? "active" : "deactivated"}`,
                      )}
                    </td>
                    <td className="py-2">
                      {active && !self ? (
                        <Button
                          variant="danger"
                          disabled={pending}
                          onClick={() => deactivate(member.id)}
                        >
                          {t("deactivate")}
                        </Button>
                      ) : null}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        {members.hasNextPage ? (
          <Button
            variant="secondary"
            onClick={() => void members.fetchNextPage()}
            disabled={members.isFetchingNextPage}
          >
            {t("loadMore")}
          </Button>
        ) : null}
      </section>

      <InviteForm
        teams={teams}
        pendingIds={new Set(invitationRows.map((row) => row.id))}
      />

      <section aria-labelledby="pending-title" className="flex flex-col gap-3">
        <h2 id="pending-title" className="text-lg font-semibold">
          {t("pending.title")}
        </h2>
        {invitationRows.length === 0 ? (
          <p className="text-sm text-muted-foreground">{t("pending.empty")}</p>
        ) : (
          <ul className="flex flex-col gap-2">
            {invitationRows.map((invitation) => (
              <li
                key={invitation.id}
                className="flex flex-wrap items-center justify-between gap-2 text-sm"
              >
                <span>{invitation.email}</span>
                <Button
                  variant="secondary"
                  disabled={pending}
                  onClick={() => void revoke(invitation.id)}
                >
                  {t("pending.revoke")}
                </Button>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
