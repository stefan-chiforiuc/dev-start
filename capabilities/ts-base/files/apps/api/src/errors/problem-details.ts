import type { FastifyError, FastifyReply, FastifyRequest } from "fastify";

export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
}

export function toProblemDetails(
  err: FastifyError,
  request: FastifyRequest,
): ProblemDetails {
  const status = err.statusCode ?? 500;
  return {
    type: `https://httpstatuses.io/${status}`,
    title: err.name,
    status,
    detail: err.message,
    instance: request.url,
  };
}

export async function problemDetailsHook(
  err: FastifyError,
  request: FastifyRequest,
  reply: FastifyReply,
) {
  const pd = toProblemDetails(err, request);
  reply.code(pd.status).type("application/problem+json").send(pd);
}
