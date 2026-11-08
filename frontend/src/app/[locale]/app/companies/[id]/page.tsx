import { CompanyRecordView } from "@/features/crm/components/company-record-view";

export default async function CompanyDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return <CompanyRecordView id={id} />;
}
