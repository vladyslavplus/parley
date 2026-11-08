"use client";

import {
  ArrowLeft,
  Building2,
  Calendar,
  Mail,
  MapPin,
  Phone,
  Plus,
  RefreshCw,
  Trash2,
  User,
} from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { AlertDialog } from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { InlineEdit } from "@/components/ui/inline-edit";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { LIMITS } from "@/constants/limits";
import { PERMISSIONS } from "@/constants/permissions";
import { companyRecordRoute, ROUTES } from "@/constants/routes";
import { CrmCustomFieldsRender } from "@/features/crm/components/crm-custom-fields-render";
import {
  DealsPlaceholder,
  OrdersPlaceholder,
  ScoresPlaceholder,
  TasksPlaceholder,
  TimelinePlaceholder,
} from "@/features/crm/components/crm-placeholders";
import { CrmTagBadge } from "@/features/crm/components/crm-tag-badge";
import { RecordTabs } from "@/features/crm/components/record-tabs";
import { useCompanies } from "@/features/crm/hooks/use-companies";
import { useContact } from "@/features/crm/hooks/use-contact";
import { useCrmMutations } from "@/features/crm/hooks/use-crm-mutations";
import { useCustomFields } from "@/features/crm/hooks/use-custom-fields";
import { useSoftDeleteWithUndo } from "@/features/crm/hooks/use-soft-delete-with-undo";
import { useTags } from "@/features/crm/hooks/use-tags";
import { Can, useCan } from "@/features/session/components/can";
import { Link, useRouter } from "@/i18n/navigation";
import { useMembers } from "@/lib/api/hooks/use-api";
import { toApiError } from "@/lib/errors";
import { formatDate } from "@/lib/formatters";

type TabKey = "overview" | "timeline" | "tasks" | "deals" | "orders" | "scores";

export function ContactRecordView({ id }: { id: string }) {
  const t = useTranslations("crm.contacts");
  const tCrm = useTranslations("crm");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const router = useRouter();

  const canWrite = useCan(PERMISSIONS.contactsWrite);

  const [activeTab, setActiveTab] = useState<TabKey>("overview");
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [addTagOpen, setAddTagOpen] = useState(false);
  const [selectedTagId, setSelectedTagId] = useState("");
  const [newTagName, setNewTagName] = useState("");

  const contactQuery = useContact(id);
  const customFieldsQuery = useCustomFields("contact");
  const tagsQuery = useTags();
  const membersQuery = useMembers();
  const companiesQuery = useCompanies({ limit: LIMITS.crmLookupLimit });

  const {
    updateContact,
    deleteContact,
    restoreContact,
    attachContactTags,
    detachContactTag,
    createTag,
  } = useCrmMutations();

  const { handleDelete } = useSoftDeleteWithUndo({
    deleteEntity: () => deleteContact(id),
    restoreEntity: () => restoreContact(id),
    deletedMessage: t("record.deleted"),
    onDeleted: () => {
      setDeleteConfirmOpen(false);
      router.push(ROUTES.contacts);
    },
    onRestored: () => {
      router.push(`${ROUTES.contacts}/${id}`);
    },
  });

  if (contactQuery.isPending) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-40" />
        <div className="rounded-[var(--radius-card)] border border-border bg-card p-6 space-y-4">
          <Skeleton className="h-7 w-64" />
          <Skeleton className="h-4 w-48" />
          <div className="grid grid-cols-2 gap-4 pt-4">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </div>
        </div>
      </div>
    );
  }

  if (contactQuery.isError || !contactQuery.data) {
    const apiError = contactQuery.error ? toApiError(contactQuery.error) : null;
    const is404 = apiError?.status === 404;

    return (
      <div className="space-y-4">
        <Link
          href={ROUTES.contacts}
          className="inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          <span>{t("record.back")}</span>
        </Link>
        <div className="rounded-[var(--radius-card)] border border-border bg-card p-8 text-center space-y-3">
          <p className="text-sm text-muted-foreground">
            {is404 ? tCrm("record.notFound") : tCrm("record.loadError")}
          </p>
          {!is404 && (
            <Button
              variant="secondary"
              onClick={() => contactQuery.refetch()}
              className="h-8 gap-1.5 text-xs px-3"
            >
              <RefreshCw className="h-3.5 w-3.5" />
              <span>{tCrm("record.retry")}</span>
            </Button>
          )}
        </div>
      </div>
    );
  }

  const contact = contactQuery.data;
  const customFieldDefs = customFieldsQuery.data ?? [];
  const allTags = tagsQuery.data?.items ?? [];
  const tagMap = new Map(allTags.map((tg) => [tg.id, tg]));
  const companies = companiesQuery.data?.items ?? [];
  const companyMap = new Map(companies.map((c) => [c.id, c]));
  const members = membersQuery.data?.pages.flatMap((p) => p.items) ?? [];
  const memberMap = new Map(members.map((m) => [m.userId, m]));

  const contactTags = (contact.tagIds ?? [])
    .map((tagId) => tagMap.get(tagId))
    .filter(Boolean);

  const availableTags = allTags.filter(
    (tg) => !(contact.tagIds ?? []).includes(tg.id),
  );

  async function handleFieldSave(
    field:
      | "firstName"
      | "lastName"
      | "email"
      | "phone"
      | "jobTitle"
      | "country"
      | "city"
      | "companyId",
    value: string | null,
  ) {
    try {
      await updateContact(contact.id, {
        version: contact.version,
        firstName: field === "firstName" ? value : contact.firstName,
        lastName: field === "lastName" ? value : contact.lastName,
        email: field === "email" ? value : contact.email,
        phone: field === "phone" ? value : contact.phone,
        jobTitle: field === "jobTitle" ? value : contact.jobTitle,
        country: field === "country" ? value : contact.country,
        city: field === "city" ? value : contact.city,
        companyId: field === "companyId" ? value : contact.companyId,
      });
      toast.success(tCrm("customFields.updated"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  async function handleCustomFieldSave(key: string, value: unknown) {
    // Archived keys are rejected by the API, so submit active definitions only
    const activeDefs = customFieldDefs.filter((d) => !d.archivedAt);
    const current = (contact.customFields as Record<string, unknown>) ?? {};
    const nextCustomFields: Record<string, unknown> = {};
    for (const d of activeDefs) {
      nextCustomFields[d.key] =
        d.key === key ? value : (current[d.key] ?? null);
    }

    try {
      await updateContact(contact.id, {
        version: contact.version,
        firstName: contact.firstName,
        lastName: contact.lastName,
        email: contact.email,
        phone: contact.phone,
        jobTitle: contact.jobTitle,
        country: contact.country,
        city: contact.city,
        companyId: contact.companyId,
        customFields: nextCustomFields,
      });
      toast.success(tCrm("customFields.updated"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  async function handleAttachTag() {
    if (!selectedTagId) return;
    await attachContactTags(contact.id, [selectedTagId]);
    setSelectedTagId("");
    setAddTagOpen(false);
    toast.success(tCrm("tags.attached"));
  }

  async function handleCreateAndAttachTag() {
    if (!newTagName.trim()) return;
    try {
      const colorIndex = allTags.length % LIMITS.tagColorCount;
      const created = await createTag(newTagName.trim(), colorIndex);
      await attachContactTags(contact.id, [created.id]);
      setNewTagName("");
      setAddTagOpen(false);
      toast.success(tCrm("tags.attached"));
    } catch {
      // error handled in mutation
    }
  }

  async function handleDetachTag(tagId: string) {
    await detachContactTag(contact.id, tagId);
    toast.success(tCrm("tags.detached"));
  }

  const currentCompany = contact.companyId
    ? companyMap.get(contact.companyId)
    : null;
  const currentOwner = contact.ownerUserId
    ? memberMap.get(contact.ownerUserId)
    : null;

  const tabItems = [
    {
      key: "overview" as const,
      label: t("record.sections.overview"),
      content: (
        <div className="space-y-4">
          <h3 className="text-sm font-semibold text-foreground">
            {t("record.sections.customFields")}
          </h3>
          <CrmCustomFieldsRender
            definitions={customFieldDefs}
            values={contact.customFields as Record<string, unknown>}
            onSave={handleCustomFieldSave}
            disabled={!canWrite}
          />
        </div>
      ),
    },
    {
      key: "timeline" as const,
      label: t("record.sections.timeline"),
      content: <TimelinePlaceholder />,
    },
    {
      key: "tasks" as const,
      label: t("record.sections.tasks"),
      content: <TasksPlaceholder />,
    },
    {
      key: "deals" as const,
      label: t("record.sections.deals"),
      content: <DealsPlaceholder />,
    },
    {
      key: "orders" as const,
      label: t("record.sections.orders"),
      content: <OrdersPlaceholder />,
    },
    {
      key: "scores" as const,
      label: t("record.sections.scores"),
      content: <ScoresPlaceholder />,
    },
  ];

  return (
    <div className="space-y-6">
      {/* Back button */}
      <div>
        <Link
          href={ROUTES.contacts}
          className="inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          <span>{t("record.back")}</span>
        </Link>
      </div>

      {/* Main Header Card */}
      <div className="rounded-[var(--radius-card)] border border-border bg-card p-6 space-y-5 shadow-sm">
        <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
          <div className="space-y-1">
            <h1 className="flex items-center flex-wrap gap-1.5 text-xl font-bold tracking-tight text-foreground">
              <InlineEdit
                disabled={!canWrite}
                value={contact.firstName}
                onSave={(v) => handleFieldSave("firstName", v)}
                placeholder={t("record.fields.firstName")}
                ariaLabel={t("record.fields.firstName")}
                className="text-xl font-bold tracking-tight px-1 py-0.5"
              />
              <InlineEdit
                disabled={!canWrite}
                value={contact.lastName ?? ""}
                onSave={(v) => handleFieldSave("lastName", v || null)}
                placeholder={t("record.fields.lastName")}
                ariaLabel={t("record.fields.lastName")}
                className="text-xl font-bold tracking-tight px-1 py-0.5"
              />
            </h1>

            <div className="text-xs text-muted-foreground">
              <InlineEdit
                disabled={!canWrite}
                value={contact.jobTitle ?? ""}
                onSave={(v) => handleFieldSave("jobTitle", v || null)}
                placeholder={t("record.fields.jobTitle")}
                ariaLabel={t("record.fields.jobTitle")}
              />
            </div>
          </div>

          {/* Delete Action */}
          <Can permission={PERMISSIONS.contactsDelete}>
            <Button
              variant="secondary"
              onClick={() => setDeleteConfirmOpen(true)}
              className="h-8 gap-1.5 text-xs px-3 text-destructive hover:bg-destructive/10 self-start sm:self-auto"
            >
              <Trash2 className="h-3.5 w-3.5" />
              <span>{t("record.delete")}</span>
            </Button>
          </Can>
        </div>

        {/* Key Info Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 pt-3 border-t border-border/60">
          {/* Email */}
          <div className="space-y-1">
            <span className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
              <Mail className="h-3.5 w-3.5" />
              <span>{t("record.fields.email")}</span>
            </span>
            <InlineEdit
              disabled={!canWrite}
              value={contact.email ?? ""}
              onSave={(v) => handleFieldSave("email", v || null)}
              placeholder={t("record.fields.email")}
              ariaLabel={t("record.fields.email")}
            />
          </div>

          {/* Phone */}
          <div className="space-y-1">
            <span className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
              <Phone className="h-3.5 w-3.5" />
              <span>{t("record.fields.phone")}</span>
            </span>
            <InlineEdit
              disabled={!canWrite}
              value={contact.phone ?? ""}
              onSave={(v) => handleFieldSave("phone", v || null)}
              placeholder={t("record.fields.phone")}
              ariaLabel={t("record.fields.phone")}
            />
          </div>

          {/* Company */}
          <div className="space-y-1">
            <span className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
              <Building2 className="h-3.5 w-3.5" />
              <span>{t("record.fields.company")}</span>
            </span>
            {currentCompany ? (
              <Link
                href={companyRecordRoute(currentCompany.id)}
                className="text-xs font-medium text-info hover:underline inline-block pt-1"
              >
                {currentCompany.name}
              </Link>
            ) : contact.companyId ? (
              <span className="text-xs text-foreground inline-block pt-1">
                {t("record.fields.linkedCompany")}
              </span>
            ) : (
              <span className="text-xs text-muted-foreground/60 inline-block pt-1">
                {t("record.fields.noCompany")}
              </span>
            )}
          </div>

          {/* Owner */}
          <div className="space-y-1">
            <span className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
              <User className="h-3.5 w-3.5" />
              <span>{t("record.fields.owner")}</span>
            </span>
            <span className="text-xs text-foreground inline-block pt-1">
              {currentOwner
                ? (currentOwner.displayName ?? currentOwner.email)
                : contact.ownerUserId
                  ? t("record.fields.assigned")
                  : t("record.fields.unassigned")}
            </span>
          </div>
        </div>

        {/* Additional Info Row: City, Country, Created */}
        <div className="flex flex-wrap items-center gap-6 pt-2 border-t border-border/40 text-xs text-muted-foreground">
          <div className="flex items-center gap-1.5">
            <MapPin className="h-3.5 w-3.5" />
            <InlineEdit
              disabled={!canWrite}
              value={contact.city ?? ""}
              onSave={(v) => handleFieldSave("city", v || null)}
              placeholder={t("record.fields.city")}
              ariaLabel={t("record.fields.city")}
            />
            <span>, </span>
            <InlineEdit
              disabled={!canWrite}
              value={contact.country ?? ""}
              onSave={(v) =>
                handleFieldSave("country", v ? v.toUpperCase() : null)
              }
              placeholder={t("record.fields.country")}
              ariaLabel={t("record.fields.country")}
            />
          </div>

          <div className="flex items-center gap-1.5">
            <Calendar className="h-3.5 w-3.5" />
            <span>
              {tCrm("record.createdWithDate", {
                date: formatDate(contact.createdAt, locale),
              })}
            </span>
          </div>
        </div>

        {/* Tags Editor */}
        <div className="pt-2 border-t border-border/40 space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              {t("record.sections.tags")}
            </span>
            <Can permission={PERMISSIONS.contactsWrite}>
              {!addTagOpen && (
                <Button
                  variant="secondary"
                  onClick={() => setAddTagOpen(true)}
                  className="h-6 gap-1 text-[11px] px-2"
                >
                  <Plus className="h-3 w-3" />
                  <span>{tCrm("tags.add")}</span>
                </Button>
              )}
            </Can>
          </div>

          <div className="flex flex-wrap items-center gap-1.5">
            {contactTags.map((tg) =>
              tg ? (
                <CrmTagBadge
                  key={tg.id}
                  name={tg.name}
                  colorIndex={tg.colorIndex}
                  onRemove={
                    canWrite ? () => void handleDetachTag(tg.id) : undefined
                  }
                />
              ) : null,
            )}

            {contactTags.length === 0 && !addTagOpen && (
              <span className="text-xs text-muted-foreground/60 italic">
                {tCrm("tags.noTags")}
              </span>
            )}

            {addTagOpen && (
              <div className="flex flex-wrap items-center gap-2 p-2 rounded-[var(--radius-control)] border border-border bg-muted/20">
                {availableTags.length > 0 && (
                  <div className="w-36">
                    <Select
                      value={selectedTagId}
                      onValueChange={setSelectedTagId}
                    >
                      <SelectTrigger className="h-7 text-xs bg-background">
                        <SelectValue placeholder={tCrm("tags.select")} />
                      </SelectTrigger>
                      <SelectContent>
                        {availableTags.map((tg) => (
                          <SelectItem key={tg.id} value={tg.id}>
                            {tg.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                )}

                {availableTags.length > 0 && (
                  <Button
                    variant="primary"
                    onClick={() => void handleAttachTag()}
                    disabled={!selectedTagId}
                    className="h-7 px-2.5 text-xs"
                  >
                    {tCrm("tags.add")}
                  </Button>
                )}

                <div className="flex items-center gap-1">
                  <Input
                    placeholder={tCrm("tags.name")}
                    value={newTagName}
                    onChange={(e) => setNewTagName(e.target.value)}
                    className="h-7 w-28 text-xs bg-background"
                  />
                  <Button
                    variant="secondary"
                    disabled={!newTagName.trim()}
                    onClick={() => void handleCreateAndAttachTag()}
                    className="h-7 px-2 text-xs"
                  >
                    {tCrm("tags.create")}
                  </Button>
                </div>

                <Button
                  variant="secondary"
                  onClick={() => {
                    setAddTagOpen(false);
                    setSelectedTagId("");
                    setNewTagName("");
                  }}
                  className="h-7 px-2 text-xs text-muted-foreground"
                >
                  {tCommon("cancel")}
                </Button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Accessible RecordTabs */}
      <RecordTabs
        tabs={tabItems}
        activeTab={activeTab}
        onTabChange={setActiveTab}
      />

      {/* Delete confirmation dialog */}
      <AlertDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title={t("record.deleteConfirm")}
        description={t("record.deleteConfirmDescription")}
        confirmLabel={t("record.delete")}
        onConfirm={handleDelete}
        destructive
      />
    </div>
  );
}
