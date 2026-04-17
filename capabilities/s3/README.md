# s3 capability

Object storage via the AWS SDK (against MinIO in dev, any S3-compatible
endpoint in prod).

## Wires

- MinIO service in compose, bucket auto-created on first boot.
- `IObjectStorage` abstraction with `PutAsync`, `GetAsync`, `PresignAsync`.
- Signed-URL helper returns a short-lived URL for direct client upload/download.
- Integration test asserts signed URL roundtrip.

## Opinions

- **Client-side upload via signed URL** for anything > 5 MB — don't proxy
  through the API.
- **Server-side encryption** on; bucket policy blocks public read.
- **Never store PII in the object key.** Keys should be opaque UUIDs.

## Escape hatches

- Azure Blob: swap the client; the abstraction is vendor-neutral.
