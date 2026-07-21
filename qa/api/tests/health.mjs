import assert from 'node:assert/strict';

const apiOrigin = process.env.QA_API_ORIGIN ?? 'http://127.0.0.1:5080';

let response;
try {
  response = await fetch(`${apiOrigin}/health`, {
    signal: AbortSignal.timeout(5_000),
  });
} catch (error) {
  const errorNames = [error?.name, error?.cause?.name];
  if (errorNames.includes('TimeoutError') || errorNames.includes('AbortError')) {
    throw new Error('GET /health did not complete within the 5-second deadline.');
  }
  throw new Error('GET /health failed before receiving an HTTP response.');
}
assert.equal(response.status, 200, 'GET /health must return HTTP 200');

const payload = await response.json();
assert.equal(payload.status, 'healthy');
assert.equal(typeof payload.traceId, 'string');
assert.ok(payload.traceId.length > 0, 'health response must include a traceId');
