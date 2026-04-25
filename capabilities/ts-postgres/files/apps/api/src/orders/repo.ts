import { randomUUID } from "node:crypto";
import type { Kysely } from "kysely";
import type { Database } from "../infra/db.js";
import type { OrderDto, PlaceOrderInput } from "./schemas.js";

export async function placeOrder(db: Kysely<Database>, input: PlaceOrderInput): Promise<OrderDto> {
  const id = randomUUID();
  const total = input.lines.reduce((sum, l) => sum + l.quantity * l.unitPriceCents, 0);
  const placedAt = new Date();

  return await db.transaction().execute(async (tx) => {
    await tx
      .insertInto("orders")
      .values({
        id,
        customer_id: input.customerId,
        total_cents: total,
        status: "placed",
        placed_at: placedAt,
      })
      .execute();

    await tx
      .insertInto("order_lines")
      .values(
        input.lines.map((l) => ({
          order_id: id,
          sku: l.sku,
          quantity: l.quantity,
          unit_price_cents: l.unitPriceCents,
        })),
      )
      .execute();

    return {
      id,
      customerId: input.customerId,
      totalCents: total,
      status: "placed",
      placedAt: placedAt.toISOString(),
      lines: input.lines,
    };
  });
}

export async function getOrderById(db: Kysely<Database>, id: string): Promise<OrderDto | null> {
  const row = await db.selectFrom("orders").where("id", "=", id).selectAll().executeTakeFirst();
  if (!row) return null;
  const lines = await db
    .selectFrom("order_lines")
    .where("order_id", "=", id)
    .select(["sku", "quantity", "unit_price_cents"])
    .execute();
  return {
    id: row.id,
    customerId: row.customer_id,
    totalCents: row.total_cents,
    status: row.status as OrderDto["status"],
    placedAt: row.placed_at.toISOString(),
    lines: lines.map((l) => ({
      sku: l.sku,
      quantity: l.quantity,
      unitPriceCents: l.unit_price_cents,
    })),
  };
}
