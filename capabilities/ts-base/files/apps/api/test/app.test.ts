import { describe, it, expect, afterAll } from "vitest";
import { getApp } from "./helpers.js";

const app = await getApp();
afterAll(() => app.close());

describe("health", () => {
  it("responds 200 with status ok", async () => {
    const res = await app.inject({ method: "GET", url: "/health" });
    expect(res.statusCode).toBe(200);
    expect(res.json()).toEqual({ status: "ok" });
  });
});
