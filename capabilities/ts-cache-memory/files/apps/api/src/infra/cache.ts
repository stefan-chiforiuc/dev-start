import type { FastifyPluginAsync } from "fastify";
import fp from "fastify-plugin";

declare module "fastify" {
  interface FastifyInstance {
    cache: {
      get: <T>(key: string) => Promise<T | null>;
      set: <T>(key: string, value: T, ttlSeconds?: number) => Promise<void>;
      del: (key: string) => Promise<void>;
    };
  }
}

interface Entry {
  value: unknown;
  expiresAt: number | null;
}

const plugin: FastifyPluginAsync = async (app) => {
  const store = new Map<string, Entry>();

  function read<T>(key: string): T | null {
    const entry = store.get(key);
    if (!entry) return null;
    if (entry.expiresAt !== null && entry.expiresAt < Date.now()) {
      store.delete(key);
      return null;
    }
    return entry.value as T;
  }

  app.decorate("cache", {
    async get<T>(key: string) {
      return read<T>(key);
    },
    async set<T>(key: string, value: T, ttlSeconds?: number) {
      const expiresAt = ttlSeconds ? Date.now() + ttlSeconds * 1000 : null;
      store.set(key, { value, expiresAt });
    },
    async del(key: string) {
      store.delete(key);
    },
  });

  app.addHook("onClose", async () => {
    store.clear();
  });
};

export const cachePlugin = fp(plugin, { name: "cache" });
