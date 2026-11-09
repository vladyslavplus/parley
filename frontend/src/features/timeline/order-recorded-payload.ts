export interface OrderRecordedPayload {
  invoiceNo: string | null;
  totalAmount: number | null;
  currency: string | null;
  isCancellation: boolean;
}

// Reads the order activity payload defensively: every field may be missing or
// of an unexpected type, and an empty value is never surfaced.
export function readOrderRecordedPayload(
  payload: unknown,
): OrderRecordedPayload {
  const record =
    payload !== null && typeof payload === "object"
      ? (payload as Record<string, unknown>)
      : {};

  const invoice =
    typeof record.invoiceNo === "string" ? record.invoiceNo.trim() : "";
  const currency =
    typeof record.currency === "string" ? record.currency.trim() : "";
  const amount =
    typeof record.totalAmount === "number" ||
    (typeof record.totalAmount === "string" && record.totalAmount.trim() !== "")
      ? Number(record.totalAmount)
      : Number.NaN;

  return {
    invoiceNo: invoice || null,
    totalAmount: Number.isFinite(amount) ? amount : null,
    currency: currency || null,
    isCancellation: record.isCancellation === true,
  };
}
