import type { FastifyPluginAsync } from "fastify";
import fp from "fastify-plugin";
import { readFile } from "node:fs/promises";
import { join } from "node:path";
import { OpenFeature, type Provider, type ResolutionDetails } from "@openfeature/server-sdk";

type FlagShape = { default: unknown };
interface FlagsFile { flags: Record<string, FlagShape> }

class FileProvider implements Provider {
  readonly metadata = { name: "file-provider" } as const;
  runsOn = "server" as const;
  hooks = [];
  private flags: FlagsFile = { flags: {} };

  async initialize() {
    const path = join(process.cwd(), "flags.json");
    const raw = await readFile(path, "utf8");
    this.flags = JSON.parse(raw) as FlagsFile;
  }

  resolveBooleanEvaluation(key: string, fallback: boolean): ResolutionDetails<boolean> {
    const flag = this.flags.flags[key];
    return { value: typeof flag?.default === "boolean" ? flag.default : fallback, reason: "STATIC" };
  }
  resolveStringEvaluation(key: string, fallback: string): ResolutionDetails<string> {
    const flag = this.flags.flags[key];
    return { value: typeof flag?.default === "string" ? flag.default : fallback, reason: "STATIC" };
  }
  resolveNumberEvaluation(key: string, fallback: number): ResolutionDetails<number> {
    const flag = this.flags.flags[key];
    return { value: typeof flag?.default === "number" ? flag.default : fallback, reason: "STATIC" };
  }
  resolveObjectEvaluation<T>(key: string, fallback: T): ResolutionDetails<T> {
    const flag = this.flags.flags[key];
    return { value: (flag?.default as T) ?? fallback, reason: "STATIC" };
  }
}

const plugin: FastifyPluginAsync = async (app) => {
  await OpenFeature.setProviderAndWait(new FileProvider());
  const client = OpenFeature.getClient();
  app.decorate("flags", client);
};

declare module "fastify" {
  interface FastifyInstance {
    flags: ReturnType<typeof OpenFeature.getClient>;
  }
}

export const flagsPlugin = fp(plugin, { name: "flags" });
