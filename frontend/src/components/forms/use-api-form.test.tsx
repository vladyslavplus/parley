import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { z } from "zod";

import { useApiForm } from "@/components/forms/use-api-form";

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string) => key,
}));

const schema = z.object({ name: z.string().min(2, "tooShort") });
type Values = z.infer<typeof schema>;

function Harness() {
  const { form } = useApiForm<Values>({
    schema,
    defaultValues: { name: "" },
    fields: ["name"],
    submit: async () => {},
  });
  return (
    <div>
      <input aria-label="name" {...form.register("name")} />
      <p role="alert">{form.formState.errors.name?.message}</p>
    </div>
  );
}

describe("useApiForm live validation", () => {
  afterEach(cleanup);

  it("shows the error after blur and clears it as soon as the value becomes valid", async () => {
    render(<Harness />);
    const input = screen.getByLabelText("name");

    fireEvent.change(input, { target: { value: "a" } });
    fireEvent.blur(input);
    await waitFor(() =>
      expect(screen.getByRole("alert")).toHaveTextContent("tooShort"),
    );

    fireEvent.change(input, { target: { value: "ab" } });
    await waitFor(() =>
      expect(screen.getByRole("alert")).toBeEmptyDOMElement(),
    );
  });
});
