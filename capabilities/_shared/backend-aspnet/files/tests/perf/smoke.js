// k6 smoke test. Runs in CI via the reusable dev-start workflow.
// Budget: p95 latency < 200ms, error rate < 1%.
// Tune for your app and explain the change in an ADR.

import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 5,
  duration: '15s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<200'],
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  const health = http.get(`${BASE}/healthz`);
  check(health, { 'health 200': (r) => r.status === 200 });

  const orders = http.get(`${BASE}/v1/orders/00000000-0000-0000-0000-000000000000`);
  check(orders, { 'orders 404 on missing id': (r) => r.status === 404 });

  sleep(0.5);
}
