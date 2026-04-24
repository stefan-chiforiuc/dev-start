import type { paths } from "./schema.js";

export type Paths = paths;

export interface ClientOptions {
  baseUrl: string;
  fetch?: typeof fetch;
  headers?: HeadersInit;
}

export function createClient(options: ClientOptions) {
  const fetcher = options.fetch ?? fetch;
  return {
    async request<T>(path: string, init?: RequestInit): Promise<T> {
      const res = await fetcher(options.baseUrl + path, {
        ...init,
        headers: { ...options.headers, ...init?.headers },
      });
      if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
      return (await res.json()) as T;
    },
  };
}
