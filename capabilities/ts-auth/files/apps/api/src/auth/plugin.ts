import type { FastifyPluginAsync, FastifyRequest } from "fastify";
import fp from "fastify-plugin";
import fastifyJwt from "@fastify/jwt";
import { Issuer, type Client } from "openid-client";

declare module "fastify" {
  interface FastifyInstance {
    oidc: Client;
    authenticate: (req: FastifyRequest) => Promise<void>;
  }
  interface FastifyRequest {
    user?: { sub: string; email?: string; roles: string[] };
  }
}

const plugin: FastifyPluginAsync = async (app) => {
  const issuerUrl = process.env.OIDC_ISSUER;
  const audience = process.env.OIDC_AUDIENCE ?? "devstart-api";
  if (!issuerUrl) throw new Error("OIDC_ISSUER is required by ts-auth");

  const issuer = await Issuer.discover(issuerUrl);
  const client = new issuer.Client({ client_id: audience, token_endpoint_auth_method: "none" });
  app.decorate("oidc", client);

  await app.register(fastifyJwt, {
    secret: {
      public: issuer.metadata.jwks_uri ?? "",
      private: "",
    },
    verify: { audience, algorithms: ["RS256"] },
    formatUser: (payload: { sub: string; email?: string; realm_access?: { roles?: string[] } }) => ({
      sub: payload.sub,
      email: payload.email,
      roles: payload.realm_access?.roles ?? [],
    }),
  });

  app.decorate("authenticate", async (req: FastifyRequest) => {
    await req.jwtVerify();
  });
};

export const authPlugin = fp(plugin, { name: "auth" });
