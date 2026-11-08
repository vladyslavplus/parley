import { notFound } from "next/navigation";

import { TokensClient } from "./tokens-client";

export default function DevTokensPage() {
  if (process.env.NODE_ENV === "production") {
    notFound();
  }

  return <TokensClient />;
}
