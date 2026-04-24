import type { FastifyPluginAsync } from "fastify";
import fp from "fastify-plugin";
import Redis from "ioredis";

declare module "fastify" {
  interface FastifyInstance {
    cache: {
      get: <T>(key: string) => Promise<T | null>;
      set: <T>(key: string, value: T, ttlSeconds?: number) => Promise<void>;
      del: (key: string) => Promise<void>;
    };
  }
}

const plugin: FastifyPluginAsync = async (app) => {
  const url = process.env.REDIS_URL;
  if (!url) throw new Error("REDIS_URL is required by ts-cache");
  const redis = new Redis(url);

  app.decorate("cache", {
    async get<T>(key: string) {
      const raw = await redis.get(key);
      return raw ? (JSON.parse(raw) as T) : null;
    },
    async set<T>(key: string, value: T, ttlSeconds?: number) {
      const raw = JSON.stringify(value);
      if (ttlSeconds) await redis.set(key, raw, "EX", ttlSeconds);
      else await redis.set(key, raw);
    },
    async del(key: string) {
      await redis.del(key);
    },
  });

  app.addHook("onClose", async () => {
    redis.disconnect();
  });
};

export const cachePlugin = fp(plugin, { name: "cache" });
