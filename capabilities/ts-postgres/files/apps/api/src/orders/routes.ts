import type { FastifyPluginAsync } from "fastify";
import { placeOrder, getOrderById } from "./repo.js";
import { placeOrder as placeOrderSchema } from "./schemas.js";

export const ordersRoutes: FastifyPluginAsync = async (app) => {
  app.post("/", async (req, reply) => {
    const parsed = placeOrderSchema.safeParse(req.body);
    if (!parsed.success) {
      return reply.code(422).send({
        type: "https://httpstatuses.io/422",
        title: "Validation failed",
        status: 422,
        errors: parsed.error.flatten(),
      });
    }
    const order = await placeOrder(app.db, parsed.data);
    return reply.code(201).send(order);
  });

  app.get<{ Params: { id: string } }>("/:id", async (req, reply) => {
    const order = await getOrderById(app.db, req.params.id);
    if (!order) return reply.code(404).send({ status: 404, title: "Order not found" });
    return order;
  });
};
