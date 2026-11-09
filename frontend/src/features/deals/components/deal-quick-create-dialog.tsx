"use client";

import { useTranslations } from "next-intl";
import { useState } from "react";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Input } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { CompanyPicker } from "@/features/crm/components/company-picker";
import { OwnerPicker } from "@/features/crm/components/owner-picker";
import { QuickCreateDialogShell } from "@/features/crm/components/quick-create-dialog-shell";
import { ContactPicker } from "@/features/deals/components/contact-picker";
import { useDealMutations } from "@/features/deals/hooks/use-deal-mutations";
import {
  createDealQuickSchema,
  type DealQuickValues,
} from "@/features/deals/schemas/deal-schema";
import { useMe } from "@/lib/api/hooks/use-api";
import { showSuccessToast } from "@/lib/toasts";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated?: (dealId: string) => void;
  defaultStageId?: string;
}

export function DealQuickCreateDialog({
  open,
  onOpenChange,
  onCreated,
  defaultStageId,
}: Props) {
  const t = useTranslations("crm.deals.quickCreate");
  const tValidation = useTranslations("validation");
  const { data: me } = useMe();
  const { createDeal } = useDealMutations();

  const organizationCurrency =
    me?.organization?.currency ?? LIMITS.defaultCurrency;

  const [selectedCompanyId, setSelectedCompanyId] = useState<string>("");
  const [selectedContactId, setSelectedContactId] = useState<string>("");
  const [selectedOwnerId, setSelectedOwnerId] = useState<string>("");

  const schema = createDealQuickSchema((k, v) => tValidation(k, v));

  const { form, onSubmit, formError, alertRef } = useApiForm<DealQuickValues>({
    schema,
    defaultValues: {
      title: "",
      amount: "0",
      currency: organizationCurrency,
      companyId: "",
      contactId: "",
      ownerUserId: me?.user.id ?? "",
      stageId: defaultStageId ?? "",
    },
    fields: [
      "title",
      "amount",
      "currency",
      "companyId",
      "contactId",
      "ownerUserId",
    ],
    submit: async (values) => {
      const created = await createDeal({
        title: values.title,
        amount: parseFloat(String(values.amount || "0")),
        currency: organizationCurrency,
        companyId: selectedCompanyId || null,
        contactId: selectedContactId || null,
        ownerUserId: selectedOwnerId || me?.user.id || null,
        stageId: defaultStageId || null,
      });

      showSuccessToast(t("created"));
      form.reset();
      setSelectedCompanyId("");
      setSelectedContactId("");
      setSelectedOwnerId("");
      onOpenChange(false);
      onCreated?.(created.id);
    },
  });

  const {
    register,
    formState: { errors, isSubmitting },
  } = form;

  return (
    <QuickCreateDialogShell
      open={open}
      onOpenChange={onOpenChange}
      title={t("title")}
      description={t("description")}
    >
      <ApiForm
        onSubmit={onSubmit}
        formError={formError}
        alertRef={alertRef}
        pending={isSubmitting}
        submitLabel={t("submit")}
      >
        <FormField label={t("name")} error={errors.title?.message} required>
          <Input
            {...register("title")}
            placeholder={t("namePlaceholder")}
            maxLength={LIMITS.dealTitleMaxLength}
            autoFocus
          />
        </FormField>

        <div className="grid grid-cols-2 gap-3">
          <FormField
            label={t("amount")}
            error={errors.amount?.message}
            required
          >
            <Input
              type="number"
              step="any"
              min="0"
              {...register("amount")}
              placeholder="0.00"
            />
          </FormField>

          <FormField label={t("currency")}>
            <Input
              value={organizationCurrency}
              disabled
              className="bg-muted text-muted-foreground cursor-not-allowed"
            />
          </FormField>
        </div>

        <FormField label={t("company")}>
          <CompanyPicker
            value={selectedCompanyId || null}
            onChange={(id) => {
              setSelectedCompanyId(id ?? "");
              form.setValue("companyId", id ?? "", { shouldDirty: true });
            }}
            placeholder={t("selectCompany")}
          />
        </FormField>

        <FormField label={t("contact")}>
          <ContactPicker
            value={selectedContactId || null}
            companyId={selectedCompanyId || null}
            onChange={(id) => {
              setSelectedContactId(id ?? "");
              form.setValue("contactId", id ?? "", { shouldDirty: true });
            }}
            placeholder={t("selectContact")}
          />
        </FormField>

        <FormField label={t("owner")}>
          <OwnerPicker
            value={selectedOwnerId || me?.user.id || null}
            onChange={(id) => {
              setSelectedOwnerId(id ?? "");
              form.setValue("ownerUserId", id ?? "", { shouldDirty: true });
            }}
            placeholder={t("selectOwner")}
          />
        </FormField>
      </ApiForm>
    </QuickCreateDialogShell>
  );
}
