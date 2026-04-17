# @{{name}}/sdk

Typed client for `{{Name}}`, generated from `src/{{Name}}.Api/openapi.json`.

## Regenerate

```sh
npm --prefix sdk run generate
```

CI runs `npm run check` which regenerates and fails if the spec and the
checked-in SDK drift.

## Use

```ts
import { create{{Name}}Client } from '@{{name}}/sdk';

const client = create{{Name}}Client({ baseUrl: 'http://localhost:5000' });

const { data, error } = await client.GET('/orders');
```
