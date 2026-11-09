import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { LocaleCacheSync } from "@/components/locale-cache-sync";

let mockLocale = "en";
vi.mock("next-intl", () => ({ useLocale: () => mockLocale }));

describe("LocaleCacheSync", () => {
  afterEach(cleanup);

  it("invalidates cached queries only when the locale changes", () => {
    const client = new QueryClient();
    const spy = vi.spyOn(client, "invalidateQueries");
    const tree = () => (
      <QueryClientProvider client={client}>
        <LocaleCacheSync />
      </QueryClientProvider>
    );

    mockLocale = "en";
    const { rerender } = render(tree());
    expect(spy).not.toHaveBeenCalled();

    mockLocale = "uk";
    rerender(tree());
    expect(spy).toHaveBeenCalledTimes(1);

    rerender(tree());
    expect(spy).toHaveBeenCalledTimes(1);
  });
});
