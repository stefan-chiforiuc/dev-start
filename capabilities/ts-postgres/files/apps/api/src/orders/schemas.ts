import { z } from "zod";

export const lineItem = z.object({
  sku: z.string().min(1),
  quantity: z.number().int().positive(),
  unitPriceCents: z.number().int().nonnegative(),
});

export const placeOrder = z.object({
  customerId: z.string().uuid(),
  lines: z.array(lineItem).min(1),
});

export const orderDto = z.object({
  id: z.string().uuid(),
  customerId: z.string().uuid(),
  totalCents: z.number().int().nonnegative(),
  status: z.enum(["placed", "fulfilled", "cancelled"]),
  placedAt: z.string(),
  lines: z.array(lineItem),
});

export type LineItem = z.infer<typeof lineItem>;
export type PlaceOrderInput = z.infer<typeof placeOrder>;
export type OrderDto = z.infer<typeof orderDto>;
