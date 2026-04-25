import type { FastifyPluginAsync } from "fastify";
import fp from "fastify-plugin";
import amqp, { type Channel, type Connection } from "amqplib";

declare module "fastify" {
  interface FastifyInstance {
    queue: {
      publish: (topic: string, payload: unknown) => Promise<void>;
      consume: (topic: string, handler: (payload: unknown) => Promise<void>) => Promise<void>;
    };
  }
}

const plugin: FastifyPluginAsync = async (app) => {
  const url = process.env.RABBITMQ_URL;
  if (!url) throw new Error("RABBITMQ_URL is required by ts-queue");

  const connection: Connection = await amqp.connect(url);
  const channel: Channel = await connection.createChannel();

  app.decorate("queue", {
    async publish(topic, payload) {
      await channel.assertExchange(topic, "topic", { durable: true });
      channel.publish(topic, "", Buffer.from(JSON.stringify(payload)), { contentType: "application/json" });
    },
    async consume(topic, handler) {
      await channel.assertExchange(topic, "topic", { durable: true });
      const q = await channel.assertQueue(`${topic}.{{name}}`, { durable: true });
      await channel.bindQueue(q.queue, topic, "#");
      channel.consume(q.queue, async (msg) => {
        if (!msg) return;
        try {
          await handler(JSON.parse(msg.content.toString()));
          channel.ack(msg);
        } catch (err) {
          app.log.error({ err }, "queue handler failed");
          channel.nack(msg, false, false);
        }
      });
    },
  });

  app.addHook("onClose", async () => {
    await channel.close();
    await connection.close();
  });
};

export const queuePlugin = fp(plugin, { name: "queue" });
