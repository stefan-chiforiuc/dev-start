import type { FastifyPluginAsync } from "fastify";
import fp from "fastify-plugin";
import pg from "pg";
import { Kysely, PostgresDialect } from "kysely";

export interface Database {
  orders: {
    id: string;
    customer_id: string;
    total_cents: number;
    status: string;
    placed_at: Date;
  };
  order_lines: {
    order_id: string;
    sku: string;
    quantity: number;
    unit_price_cents: number;
  };
}

declare module "fastify" {
  interface FastifyInstance {
    db: Kysely<Database>;
  }
}

const plugin: FastifyPluginAsync = async (app) => {
  const url = process.env.DATABASE_URL;
  if (!url) throw new Error("DATABASE_URL is required by ts-postgres");

  const pool = new pg.Pool({ connectionString: url, max: 10 });
  const db = new Kysely<Database>({ dialect: new PostgresDialect({ pool }) });

  app.decorate("db", db);
  app.addHook("onClose", async () => {
    await db.destroy();
  });
};

export const dbPlugin = fp(plugin, { name: "db" });
