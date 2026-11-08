"use client";

import * as Dialog from "@radix-ui/react-dialog";
import type { ColumnDef } from "@tanstack/react-table";
import { Edit2, Plus, RefreshCw, Trash2, UserX } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { parseAsString, parseAsStringLiteral, useQueryState } from "nuqs";
import { useState } from "react";

import {
  DataTable,
  DataTableSearchFilterBar,
  useServerTableState,
} from "@/components/data-table";
import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input, Select } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import {
  MEMBERSHIP_STATUSES,
  PERMISSIONS,
  SYSTEM_ROLES,
} from "@/constants/permissions";
import { queryKeys } from "@/constants/query-keys";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import {
  createInviteSchema,
  type InviteValues,
} from "@/features/members/schemas/invite-schema";
import { useCan } from "@/features/session/components/can";
import { backendJson, backendVoid } from "@/lib/api/bff-client";
import {
  useAction,
  useInvalidate,
  useInvitationsPage,
  useMe,
  useMembersPage,
  useTeamsPage,
} from "@/lib/api/hooks/use-api";
import type { Schemas } from "@/lib/api/schemas";
import { describeError, toApiError } from "@/lib/errors";
import { formatDate } from "@/lib/formatters";
import { showSuccessToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

type MembersTab = "active" | "pending";

const MEMBER_SORTS = ["name", "email", "role", "joinedAt"] as const;
const INVITATION_SORTS = ["createdAt", "email", "expiresAt"] as const;

function resolveMemberSort(
  sort: string,
): (typeof MEMBER_SORTS)[number] | undefined {
  return (MEMBER_SORTS as readonly string[]).includes(sort)
    ? (sort as (typeof MEMBER_SORTS)[number])
    : "name";
}

function resolveInvitationSort(
  sort: string,
): (typeof INVITATION_SORTS)[number] | undefined {
  return (INVITATION_SORTS as readonly string[]).includes(sort)
    ? (sort as (typeof INVITATION_SORTS)[number])
    : "createdAt";
}

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

function InviteMemberDialog({
  open,
  onOpenChange,
  teams,
  canTeams,
  pendingIds,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  teams: Schemas["TeamView"][];
  canTeams: boolean;
  pendingIds: ReadonlySet<string>;
}) {
  const t = useTranslations("settings.members.invite");
  const tCommon = useTranslations("common");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const invalidate = useInvalidate();
  const schema = createInviteSchema(tValidation);

  const { form, onSubmit, formError, alertRef } = useApiForm<InviteValues>({
    schema,
    defaultValues: { email: "", roleCode: SYSTEM_ROLES[2], teamId: "" },
    fields: ["email", "roleCode", "teamId"],
    submit: async (values) => {
      const invitation = await backendJson<Schemas["InvitationView"]>(
        "/invitations",
        {
          method: "POST",
          body: { ...values, teamId: values.teamId || null },
          locale,
        },
      );
      await invalidate(queryKeys.invitations());
      form.reset({ email: "", roleCode: SYSTEM_ROLES[2], teamId: "" });
      showSuccessToast(t(pendingIds.has(invitation.id) ? "refreshed" : "sent"));
      onOpenChange(false);
    },
  });

  const { errors, isSubmitting } = form.formState;

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      form.reset({ email: "", roleCode: SYSTEM_ROLES[2], teamId: "" });
    }
    onOpenChange(nextOpen);
  }

  return (
    <Dialog.Root open={open} onOpenChange={handleOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-sm animate-in fade-in-0 duration-150" />
        <Dialog.Content className="fixed left-[50%] top-[50%] z-50 w-full max-w-md translate-x-[-50%] translate-y-[-50%] rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-floating space-y-4 focus:outline-none">
          <div>
            <Dialog.Title className="text-base font-semibold text-foreground">
              {t("title")}
            </Dialog.Title>
            <Dialog.Description className="mt-1 text-xs text-muted-foreground">
              {t("description")}
            </Dialog.Description>
          </div>

          <ApiForm
            onSubmit={onSubmit}
            formError={formError}
            alertRef={alertRef}
            pending={isSubmitting}
            submitLabel={t("submit")}
          >
            <FormField label={t("email")} error={errors.email?.message}>
              <Input
                type="email"
                autoComplete="off"
                className="h-8 text-xs bg-background"
                {...form.register("email")}
              />
            </FormField>
            <FormField label={t("role")} error={errors.roleCode?.message}>
              <Select
                className="h-8 text-xs bg-background"
                {...form.register("roleCode")}
              >
                <RoleOptions />
              </Select>
            </FormField>
            {canTeams ? (
              <FormField label={t("team")} error={errors.teamId?.message}>
                <Select
                  className="h-8 text-xs bg-background"
                  {...form.register("teamId")}
                >
                  <TeamOptions teams={teams} />
                </Select>
              </FormField>
            ) : null}
            <div className="flex justify-end pt-1">
              <Button
                type="button"
                variant="secondary"
                onClick={() => handleOpenChange(false)}
                disabled={isSubmitting}
                className="h-8 px-3 text-xs"
              >
                {tCommon("cancel")}
              </Button>
            </div>
          </ApiForm>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

function EditMemberDialog({
  member,
  onClose,
  teams,
  canTeams,
}: {
  member: Schemas["MemberView"] | null;
  onClose: () => void;
  teams: Schemas["TeamView"][];
  canTeams: boolean;
}) {
  const t = useTranslations("settings.members");
  const tCommon = useTranslations("common");
  const tKinds = useTranslations("errors.kinds");
  const locale = useLocale();
  const invalidate = useInvalidate();

  const [roleCode, setRoleCode] = useState(member?.roleCode ?? SYSTEM_ROLES[2]);
  const [teamId, setTeamId] = useState(member?.teamId ?? "");
  const [prevMemberId, setPrevMemberId] = useState(member?.id ?? null);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if ((member?.id ?? null) !== prevMemberId) {
    setPrevMemberId(member?.id ?? null);
    setRoleCode(member?.roleCode ?? SYSTEM_ROLES[2]);
    setTeamId(member?.teamId ?? "");
    setError(null);
  }

  const isDirty = Boolean(
    member &&
    (roleCode !== member.roleCode ||
      (canTeams && teamId !== (member.teamId ?? ""))),
  );

  async function handleSave() {
    if (!member || !isDirty) return;
    let mutated = false;
    setPending(true);
    setError(null);
    try {
      if (roleCode !== member.roleCode) {
        await backendVoid(
          "/members/{id}/role",
          { method: "PUT", body: { roleCode }, locale },
          `/members/${member.id}/role`,
        );
        mutated = true;
      }
      if (canTeams && teamId !== (member.teamId ?? "")) {
        await backendVoid(
          "/members/{id}/team",
          { method: "PUT", body: { teamId: teamId || null }, locale },
          `/members/${member.id}/team`,
        );
        mutated = true;
      }
      showSuccessToast(t("editModal.saved"));
      onClose();
    } catch (caught) {
      setError(describeError(toApiError(caught), (kind) => tKinds(kind)));
    } finally {
      if (mutated) {
        await Promise.all([
          invalidate(queryKeys.members()),
          invalidate(queryKeys.teams()),
          invalidate(queryKeys.owners()),
          invalidate(queryKeys.me()),
        ]);
      }
      setPending(false);
    }
  }

  return (
    <Dialog.Root
      open={Boolean(member)}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) {
          setError(null);
          onClose();
        }
      }}
    >
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-sm animate-in fade-in-0 duration-150" />
        <Dialog.Content className="fixed left-[50%] top-[50%] z-50 w-full max-w-md translate-x-[-50%] translate-y-[-50%] rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-floating space-y-4 focus:outline-none">
          <div>
            <Dialog.Title className="text-base font-semibold text-foreground">
              {t("editModal.title")}
            </Dialog.Title>
            <Dialog.Description className="mt-1 text-xs text-muted-foreground">
              {member?.displayName ?? member?.email ?? ""}
            </Dialog.Description>
          </div>

          {error ? <Alert tone="error">{error}</Alert> : null}

          <div className="space-y-4">
            <FormField label={t("columns.role")}>
              <Select
                aria-label={t("columns.role")}
                value={roleCode}
                disabled={pending}
                onChange={(event) => setRoleCode(event.target.value)}
                className="h-8 text-xs bg-background"
              >
                <RoleOptions />
              </Select>
            </FormField>

            {canTeams ? (
              <FormField label={t("columns.team")}>
                <Select
                  aria-label={t("columns.team")}
                  value={teamId}
                  disabled={pending}
                  onChange={(event) => setTeamId(event.target.value)}
                  className="h-8 text-xs bg-background"
                >
                  <TeamOptions teams={teams} />
                </Select>
              </FormField>
            ) : null}
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={onClose}
              disabled={pending}
              className="h-8 px-3 text-xs"
            >
              {tCommon("cancel")}
            </Button>
            <Button
              type="button"
              variant="primary"
              onClick={() => void handleSave()}
              disabled={!isDirty || pending}
              className="h-8 px-3 text-xs"
            >
              {tCommon("save")}
            </Button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

export function MembersPanel() {
  const t = useTranslations("settings.members");
  const tRoles = useTranslations("settings.roles");
  const locale = useLocale();
  const { data: me } = useMe();
  const canTeams = useCan(PERMISSIONS.teamsManage);
  const invalidate = useInvalidate();
  const { error, pending, run } = useAction();

  const [tab, setTab] = useQueryState(
    "tab",
    parseAsStringLiteral(["active", "pending"] as const).withDefault("active"),
  );
  const [roleCode, setRoleCode] = useQueryState(
    "roleCode",
    parseAsString.withDefault(""),
  );
  const [teamId, setTeamId] = useQueryState(
    "teamId",
    parseAsString.withDefault(""),
  );
  const [status, setStatus] = useQueryState(
    "status",
    parseAsStringLiteral(["active", "deactivated", "all"] as const).withDefault(
      "active",
    ),
  );

  const tableState = useServerTableState({
    defaultSort: tab === "pending" ? "createdAt" : "name",
    defaultOrder: tab === "pending" ? "desc" : "asc",
    allowedSorts: tab === "pending" ? INVITATION_SORTS : MEMBER_SORTS,
  });

  const [inviteOpen, setInviteOpen] = useState(false);
  const [editingMember, setEditingMember] = useState<
    Schemas["MemberView"] | null
  >(null);
  const [deactivatingMember, setDeactivatingMember] = useState<
    Schemas["MemberView"] | null
  >(null);
  const [revokingInvitation, setRevokingInvitation] = useState<
    Schemas["InvitationView"] | null
  >(null);

  const membersQuery = useMembersPage(
    {
      after: tab === "active" ? tableState.after : undefined,
      limit: tableState.pageSize,
      q: tab === "active" ? tableState.q || undefined : undefined,
      roleCode: tab === "active" ? roleCode || undefined : undefined,
      teamId: tab === "active" ? teamId || undefined : undefined,
      status: tab === "active" ? status : undefined,
      sort: tab === "active" ? resolveMemberSort(tableState.sort) : undefined,
      order: tab === "active" ? tableState.order : undefined,
    },
    tab === "active",
  );

  const invitationsQuery = useInvitationsPage({
    after: tab === "pending" ? tableState.after : undefined,
    limit: tableState.pageSize,
    q: tab === "pending" ? tableState.q || undefined : undefined,
    roleCode: tab === "pending" ? roleCode || undefined : undefined,
    sort:
      tab === "pending" ? resolveInvitationSort(tableState.sort) : undefined,
    order: tab === "pending" ? tableState.order : undefined,
  });

  const teamsQuery = useTeamsPage(
    {
      limit: LIMITS.crmLookupLimit,
      sort: "name",
      order: "asc",
    },
    canTeams,
  );
  const teams = teamsQuery.data?.items ?? [];
  const teamMap = new Map(teams.map((tm) => [tm.id, tm]));

  const memberRows = membersQuery.data?.items ?? [];
  const invitationRows = invitationsQuery.data?.items ?? [];

  function handleTabSwitch(nextTab: MembersTab) {
    if (nextTab === tab) return;
    void setTab(nextTab);
    tableState.resetAll(
      () => {
        void setRoleCode("");
        void setTeamId("");
        void setStatus("active");
      },
      nextTab === "pending"
        ? { sort: "createdAt", order: "desc" }
        : { sort: "name", order: "asc" },
    );
  }

  async function handleConfirmDeactivate() {
    if (!deactivatingMember) return;
    await backendVoid(
      "/members/{id}/deactivate",
      { method: "POST", locale },
      `/members/${deactivatingMember.id}/deactivate`,
    );
    await Promise.all([
      invalidate(queryKeys.members()),
      invalidate(queryKeys.teams()),
      invalidate(queryKeys.owners()),
    ]);
    showSuccessToast(t("deactivateModal.deactivated"));
    setDeactivatingMember(null);
  }

  async function handleConfirmRevoke() {
    if (!revokingInvitation) return;
    await backendVoid(
      "/invitations/{id}/revoke",
      { method: "POST", locale },
      `/invitations/${revokingInvitation.id}/revoke`,
    );
    await invalidate(queryKeys.invitations());
    showSuccessToast(t("pending.revoked"));
    setRevokingInvitation(null);
  }

  function handleResendInvitation(invitation: Schemas["InvitationView"]) {
    void run(async () => {
      await backendJson<Schemas["InvitationView"]>("/invitations", {
        method: "POST",
        body: {
          email: invitation.email,
          roleCode: invitation.roleCode,
          teamId: invitation.teamId ?? null,
        },
        locale,
      });
      await invalidate(queryKeys.invitations());
      showSuccessToast(t("invite.refreshed"));
    });
  }

  const memberColumns: ColumnDef<Schemas["MemberView"], unknown>[] = [
    {
      id: "name",
      accessorFn: (row) => row.displayName ?? row.email ?? row.userId,
      header: t("columns.member"),
      enableSorting: true,
      cell: ({ row }) => {
        const member = row.original;
        const label = member.displayName ?? member.email ?? member.userId;
        return (
          <div className="flex flex-col">
            <span className="font-medium text-foreground text-xs">{label}</span>
            {member.email && member.email !== label ? (
              <span className="font-mono text-[11px] text-muted-foreground">
                {member.email}
              </span>
            ) : null}
          </div>
        );
      },
    },
    {
      id: "role",
      accessorKey: "roleCode",
      header: t("columns.role"),
      enableSorting: true,
      cell: ({ row }) => {
        const code = row.original.roleCode;
        let roleLabel = code;
        try {
          roleLabel = tRoles(code as "owner");
        } catch {
          roleLabel = code;
        }
        return (
          <span className="inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-medium bg-muted text-muted-foreground uppercase">
            {roleLabel}
          </span>
        );
      },
    },
    {
      id: "team",
      header: t("columns.team"),
      enableSorting: false,
      cell: ({ row }) => {
        const mTeamId = row.original.teamId;
        if (!mTeamId) {
          return (
            <span className="text-xs text-muted-foreground">{t("noTeam")}</span>
          );
        }
        const resolvedTeam = teamMap.get(mTeamId);
        return (
          <span className="text-xs text-foreground">
            {resolvedTeam ? resolvedTeam.name : t("inTeam")}
          </span>
        );
      },
    },
    {
      id: "status",
      accessorKey: "status",
      header: t("columns.status"),
      enableSorting: false,
      cell: ({ row }) => {
        const active = row.original.status === MEMBERSHIP_STATUSES.active;
        return (
          <span
            className={cn(
              "inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-medium uppercase",
              active
                ? "bg-success/10 text-success"
                : "bg-muted text-muted-foreground",
            )}
          >
            {t(`status.${active ? "active" : "deactivated"}`)}
          </span>
        );
      },
    },
    {
      id: "joinedAt",
      accessorKey: "joinedAt",
      header: t("columns.joinedAt"),
      enableSorting: true,
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground whitespace-nowrap">
          {formatDate(row.original.joinedAt, locale)}
        </span>
      ),
    },
    {
      id: "actions",
      header: () => <span className="sr-only">{t("columns.actions")}</span>,
      enableSorting: false,
      cell: ({ row }) => {
        const member = row.original;
        const active = member.status === MEMBERSHIP_STATUSES.active;
        const self = member.userId === me?.user.id;
        const label = member.displayName ?? member.email ?? member.userId;

        if (!active) return null;

        return (
          <div className="flex items-center justify-end gap-1">
            <Button
              variant="secondary"
              onClick={() => setEditingMember(member)}
              className="h-6 w-6 p-0"
              title={t("editMember", { name: label })}
              aria-label={t("editMember", { name: label })}
            >
              <Edit2 className="h-3 w-3 text-muted-foreground" />
            </Button>
            {!self ? (
              <Button
                variant="secondary"
                onClick={() => setDeactivatingMember(member)}
                disabled={pending}
                className="h-6 w-6 p-0 text-destructive hover:bg-destructive/10"
                title={t("deactivateMember", { name: label })}
                aria-label={t("deactivateMember", { name: label })}
              >
                <UserX className="h-3 w-3" />
              </Button>
            ) : null}
          </div>
        );
      },
    },
  ];

  const invitationColumns: ColumnDef<Schemas["InvitationView"], unknown>[] = [
    {
      id: "email",
      accessorKey: "email",
      header: t("pending.columns.email"),
      enableSorting: true,
      cell: ({ row }) => (
        <span className="font-mono text-xs text-foreground">
          {row.original.email}
        </span>
      ),
    },
    {
      id: "role",
      accessorKey: "roleCode",
      header: t("pending.columns.role"),
      enableSorting: false,
      cell: ({ row }) => {
        const code = row.original.roleCode;
        let roleLabel = code;
        try {
          roleLabel = tRoles(code as "owner");
        } catch {
          roleLabel = code;
        }
        return (
          <span className="inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-medium bg-muted text-muted-foreground uppercase">
            {roleLabel}
          </span>
        );
      },
    },
    {
      id: "team",
      header: t("pending.columns.team"),
      enableSorting: false,
      cell: ({ row }) => {
        const invTeamId = row.original.teamId;
        if (!invTeamId) {
          return <span className="text-muted-foreground/40">—</span>;
        }
        const resolvedTeam = teamMap.get(invTeamId);
        return (
          <span className="text-xs text-foreground">
            {resolvedTeam ? resolvedTeam.name : t("inTeam")}
          </span>
        );
      },
    },
    {
      id: "expiresAt",
      accessorKey: "expiresAt",
      header: t("pending.columns.expiresAt"),
      enableSorting: true,
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground whitespace-nowrap">
          {formatDate(row.original.expiresAt, locale)}
        </span>
      ),
    },
    {
      id: "actions",
      header: () => <span className="sr-only">{t("columns.actions")}</span>,
      enableSorting: false,
      cell: ({ row }) => {
        const inv = row.original;
        return (
          <div className="flex items-center justify-end gap-1">
            <Button
              variant="secondary"
              onClick={() => handleResendInvitation(inv)}
              disabled={pending}
              className="h-6 w-6 p-0"
              title={t("pending.resendFor", { email: inv.email })}
              aria-label={t("pending.resendFor", { email: inv.email })}
            >
              <RefreshCw className="h-3 w-3 text-muted-foreground" />
            </Button>
            <Button
              variant="secondary"
              onClick={() => setRevokingInvitation(inv)}
              disabled={pending}
              className="h-6 w-6 p-0 text-destructive hover:bg-destructive/10"
              title={t("pending.revokeFor", { email: inv.email })}
              aria-label={t("pending.revokeFor", { email: inv.email })}
            >
              <Trash2 className="h-3 w-3" />
            </Button>
          </div>
        );
      },
    },
  ];

  const hasActiveFilters = Boolean(
    tableState.q ||
    tableState.after ||
    roleCode ||
    (tab === "active" && (teamId || status !== "active")),
  );

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <p className="text-xs text-muted-foreground">{t("subtitle")}</p>
        <Button
          variant="primary"
          onClick={() => setInviteOpen(true)}
          className="h-8 gap-1.5 text-xs px-3 self-start sm:self-auto whitespace-nowrap"
        >
          <Plus className="h-3.5 w-3.5" />
          <span>{t("invite.title")}</span>
        </Button>
      </div>

      {error ? <Alert>{error}</Alert> : null}

      {/* Tabs: Active members | Pending invitations */}
      <div
        role="tablist"
        aria-label={t("title")}
        className="inline-flex rounded-[var(--radius-control)] border border-border p-1 bg-card"
      >
        <button
          type="button"
          role="tab"
          aria-selected={tab === "active"}
          onClick={() => handleTabSwitch("active")}
          className={cn(
            "px-3 py-1.5 text-xs font-medium rounded-sm transition-colors cursor-pointer",
            tab === "active"
              ? "bg-primary text-primary-foreground shadow-sm"
              : "text-muted-foreground hover:text-foreground",
          )}
        >
          {t("tabs.active")}
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={tab === "pending"}
          onClick={() => handleTabSwitch("pending")}
          className={cn(
            "inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-sm transition-colors cursor-pointer",
            tab === "pending"
              ? "bg-primary text-primary-foreground shadow-sm"
              : "text-muted-foreground hover:text-foreground",
          )}
        >
          <span>{t("tabs.pending")}</span>
          {invitationRows.length > 0 ? (
            <span
              className={cn(
                "inline-flex items-center justify-center rounded-full px-1.5 py-0.2 text-[10px] font-semibold",
                tab === "pending"
                  ? "bg-primary-foreground/20 text-primary-foreground"
                  : "bg-muted text-muted-foreground",
              )}
            >
              {invitationRows.length}
            </span>
          ) : null}
        </button>
      </div>

      {tab === "active" ? (
        membersQuery.isError && !membersQuery.data ? (
          <Alert>{t("loadFailed")}</Alert>
        ) : (
          <DataTable
            data={memberRows}
            columns={memberColumns}
            getRowId={(row) => row.id}
            loading={membersQuery.isPending && !membersQuery.data}
            isFetching={membersQuery.isFetching}
            manualPagination
            manualSorting
            sorting={tableState.sorting}
            onSortingChange={tableState.onSortingChange}
            emptyTitle={t("empty")}
            toolbar={
              <DataTableSearchFilterBar
                searchInput={tableState.searchInput}
                onSearchInputChange={tableState.setSearchInput}
                onClearSearch={tableState.clearSearch}
                searchPlaceholder={t("searchPlaceholder")}
                hasActiveFilters={hasActiveFilters}
                onResetFilters={() =>
                  tableState.resetAll(() => {
                    void setRoleCode("");
                    void setTeamId("");
                    void setStatus("active");
                  })
                }
              >
                <Select
                  aria-label={t("filters.role")}
                  value={roleCode}
                  onChange={(e) => {
                    void setRoleCode(e.target.value);
                    tableState.resetPaging();
                  }}
                  className="h-8 w-auto min-w-[130px] text-xs bg-background py-1"
                >
                  <option value="">{t("filters.allRoles")}</option>
                  <RoleOptions />
                </Select>

                {canTeams ? (
                  <Select
                    aria-label={t("filters.team")}
                    value={teamId}
                    onChange={(e) => {
                      void setTeamId(e.target.value);
                      tableState.resetPaging();
                    }}
                    className="h-8 w-auto min-w-[140px] text-xs bg-background py-1"
                  >
                    <option value="">{t("filters.allTeams")}</option>
                    {teams.map((tm) => (
                      <option key={tm.id} value={tm.id}>
                        {tm.name}
                      </option>
                    ))}
                  </Select>
                ) : null}

                <Select
                  aria-label={t("filters.status")}
                  value={status}
                  onChange={(e) => {
                    void setStatus(
                      e.target.value as "active" | "deactivated" | "all",
                    );
                    tableState.resetPaging();
                  }}
                  className="h-8 w-auto min-w-[130px] text-xs bg-background py-1"
                >
                  <option value="active">{t("status.active")}</option>
                  <option value="deactivated">{t("status.deactivated")}</option>
                  <option value="all">{t("filters.allStatuses")}</option>
                </Select>
              </DataTableSearchFilterBar>
            }
            paginationSlot={
              <KeysetPagination
                hasPreviousPage={tableState.hasPreviousPage}
                hasNextPage={Boolean(membersQuery.data?.nextCursor)}
                onPreviousPage={tableState.handlePreviousPage}
                onNextPage={() =>
                  tableState.handleNextPage(
                    membersQuery.data?.nextCursor ?? null,
                  )
                }
                pageSize={tableState.pageSize}
                onPageSizeChange={tableState.setPageSize}
                itemCount={memberRows.length}
              />
            }
          />
        )
      ) : invitationsQuery.isError && !invitationsQuery.data ? (
        <Alert>{t("loadFailed")}</Alert>
      ) : (
        <DataTable
          data={invitationRows}
          columns={invitationColumns}
          getRowId={(row) => row.id}
          loading={invitationsQuery.isPending && !invitationsQuery.data}
          isFetching={invitationsQuery.isFetching}
          manualPagination
          manualSorting
          sorting={tableState.sorting}
          onSortingChange={tableState.onSortingChange}
          emptyTitle={t("pending.empty")}
          toolbar={
            <DataTableSearchFilterBar
              searchInput={tableState.searchInput}
              onSearchInputChange={tableState.setSearchInput}
              onClearSearch={tableState.clearSearch}
              searchPlaceholder={t("pending.searchPlaceholder")}
              hasActiveFilters={hasActiveFilters}
              onResetFilters={() =>
                tableState.resetAll(() => {
                  void setRoleCode("");
                })
              }
            >
              <Select
                aria-label={t("filters.role")}
                value={roleCode}
                onChange={(e) => {
                  void setRoleCode(e.target.value);
                  tableState.resetPaging();
                }}
                className="h-8 w-auto min-w-[130px] text-xs bg-background py-1"
              >
                <option value="">{t("filters.allRoles")}</option>
                <RoleOptions />
              </Select>
            </DataTableSearchFilterBar>
          }
          paginationSlot={
            <KeysetPagination
              hasPreviousPage={tableState.hasPreviousPage}
              hasNextPage={Boolean(invitationsQuery.data?.nextCursor)}
              onPreviousPage={tableState.handlePreviousPage}
              onNextPage={() =>
                tableState.handleNextPage(
                  invitationsQuery.data?.nextCursor ?? null,
                )
              }
              pageSize={tableState.pageSize}
              onPageSizeChange={tableState.setPageSize}
              itemCount={invitationRows.length}
            />
          }
        />
      )}

      {/* Invite Member Modal */}
      <InviteMemberDialog
        open={inviteOpen}
        onOpenChange={setInviteOpen}
        teams={teams}
        canTeams={canTeams}
        pendingIds={new Set(invitationRows.map((row) => row.id))}
      />

      {/* Edit Member Modal */}
      <EditMemberDialog
        member={editingMember}
        onClose={() => setEditingMember(null)}
        teams={teams}
        canTeams={canTeams}
      />

      {/* Deactivate Member Confirmation Dialog */}
      <ConfirmDialog
        open={Boolean(deactivatingMember)}
        onOpenChange={(open) => !open && setDeactivatingMember(null)}
        title={t("deactivateModal.title")}
        description={t("deactivateModal.description", {
          name:
            deactivatingMember?.displayName ?? deactivatingMember?.email ?? "",
        })}
        confirmLabel={t("deactivate")}
        onConfirm={handleConfirmDeactivate}
        destructive
      />

      {/* Revoke Invitation Confirmation Dialog */}
      <ConfirmDialog
        open={Boolean(revokingInvitation)}
        onOpenChange={(open) => !open && setRevokingInvitation(null)}
        title={t("pending.revokeModal.title")}
        description={t("pending.revokeModal.description", {
          email: revokingInvitation?.email ?? "",
        })}
        confirmLabel={t("pending.revoke")}
        onConfirm={handleConfirmRevoke}
        destructive
      />
    </div>
  );
}
