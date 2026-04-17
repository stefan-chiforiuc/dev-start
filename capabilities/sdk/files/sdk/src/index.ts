import createClient, { type ClientOptions } from 'openapi-fetch';
import type { paths } from './schema';

export type {{Name}}Client = ReturnType<typeof createClient<paths>>;

export function create{{Name}}Client(options: ClientOptions = {}): {{Name}}Client {
  return createClient<paths>(options);
}

export type * from './schema';
