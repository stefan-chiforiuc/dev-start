import Fastify from "fastify";
import proxy from "@fastify/http-proxy";

const app = Fastify({ logger: true });

// Add a route per downstream service. Wire URLs from env or a config file.
const upstreams: Record<string, string> = {
  "/orders": process.env.ORDERS_UPSTREAM ?? "http://localhost:5001",
  "/users": process.env.USERS_UPSTREAM ?? "http://localhost:5002",
};

for (const [prefix, upstream] of Object.entries(upstreams)) {
  await app.register(proxy, { upstream, prefix, rewritePrefix: prefix });
}

const port = Number(process.env.GATEWAY_PORT ?? "8080");
await app.listen({ host: "0.0.0.0", port });
