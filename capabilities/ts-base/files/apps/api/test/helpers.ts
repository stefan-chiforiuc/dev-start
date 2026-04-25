import { buildApp } from "../src/app.js";

export async function getApp() {
  const app = await buildApp();
  await app.ready();
  return app;
}
