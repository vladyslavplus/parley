import { ContactRecordView } from "@/features/crm/components/contact-record-view";

export default async function ContactDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return <ContactRecordView id={id} />;
}
