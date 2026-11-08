import { afterEach, describe, expect, it } from "vitest";

import { createApiClients } from "./client";
import { requireApiBaseUrl } from "./env";

const originalApiBaseUrl = process.env.API_BASE_URL;

afterEach(() => {
  if (originalApiBaseUrl === undefined) {
    delete process.env.API_BASE_URL;
  } else {
    process.env.API_BASE_URL = originalApiBaseUrl;
  }
});

describe("requireApiBaseUrl", () => {
  it("returns the configured base URL with trailing slashes trimmed", () => {
    process.env.API_BASE_URL = "http://localhost:5000/";
    expect(requireApiBaseUrl()).toBe("http://localhost:5000");
  });

  it("throws when API_BASE_URL is missing", () => {
    delete process.env.API_BASE_URL;
    expect(() => requireApiBaseUrl()).toThrow("API_BASE_URL is not configured");
  });
});

describe("createApiClients", () => {
  it("creates fetch and react-query clients", () => {
    const { fetchClient, $api } = createApiClients({
      baseUrl: "http://localhost:5000",
    });
    expect(fetchClient).toBeDefined();
    expect(typeof $api.useQuery).toBe("function");
    expect(typeof $api.useMutation).toBe("function");
  });

  it("throws when no base URL is configured", () => {
    delete process.env.API_BASE_URL;
    expect(() => createApiClients()).toThrow("API_BASE_URL is not configured");
  });
});
