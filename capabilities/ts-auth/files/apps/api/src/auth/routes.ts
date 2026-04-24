import type { FastifyPluginAsync } from "fastify";

export const authRoutes: FastifyPluginAsync = async (app) => {
  app.get("/me", { preHandler: app.authenticate }, async (req) => req.user ?? {});
};
