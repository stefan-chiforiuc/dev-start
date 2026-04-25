import type { FastifyPluginAsync, FastifyRequest } from "fastify";
import fp from "fastify-plugin";
import fastifyJwt from "@fastify/jwt";
import buildGetJwks from "get-jwks";

declare module "@fastify/jwt" {
  interface FastifyJWT {
    user: { sub: string; email?: string; roles: string[] };
  }
}

declare module "fastify" {
  interface FastifyInstance {
    authenticate: (req: FastifyRequest) => Promise<void>;
  }
}

const plugin: FastifyPluginAsync = async (app) => {
  const issuer = process.env.OIDC_ISSUER;
  const audience = process.env.OIDC_AUDIENCE ?? "devstart-api";
  if (!issuer) throw new Error("OIDC_ISSUER is required by ts-auth");

  const getJwks = buildGetJwks({ max: 10, providerDiscovery: true });

  await app.register(fastifyJwt, {
    decode: { complete: true },
    secret: async (_req, token) => {
      const header = (token as unknown as { header: { kid: string; alg: string } }).header;
      return await getJwks.getPublicKey({ kid: header.kid, alg: header.alg, domain: issuer });
    },
    verify: { audience, algorithms: ["RS256"] },
    formatUser: (payload) => ({
      sub: payload.sub as string,
      email: payload.email as string | undefined,
      roles: (payload.realm_access as { roles?: string[] } | undefined)?.roles ?? [],
    }),
  });

  app.decorate("authenticate", async (req: FastifyRequest) => {
    await req.jwtVerify();
  });
};

export const authPlugin = fp(plugin, { name: "auth" });
