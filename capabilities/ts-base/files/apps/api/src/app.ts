import Fastify from "fastify";
import sensible from "@fastify/sensible";
import cors from "@fastify/cors";

import { config } from "./config.js";
import { problemDetailsHook } from "./errors/problem-details.js";
import { healthRoutes } from "./routes/health.js";

export async function buildApp() {
  const app = Fastify({
    logger: { level: config.LOG_LEVEL },
    disableRequestLogging: false,
  });

  await app.register(sensible);
  await app.register(cors, { origin: true });

  app.setErrorHandler(problemDetailsHook);

  // devstart:app-plugins
  // Capabilities register plugins here via dev-start injectors.

  // devstart:app-routes
  await app.register(healthRoutes);

  return app;
}
