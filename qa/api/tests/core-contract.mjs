import assert from 'node:assert/strict';
import { request } from 'node:http';

const apiOrigin = process.env.QA_API_ORIGIN ?? 'http://127.0.0.1:5080';

async function get(path, options = {}) {
  try {
    return await fetch(`${apiOrigin}${path}`, {
      ...options,
      signal: AbortSignal.timeout(5_000),
    });
  } catch (error) {
    const errorNames = [error?.name, error?.cause?.name];
    if (errorNames.includes('TimeoutError') || errorNames.includes('AbortError')) {
      throw new Error(`GET ${path} did not complete within the 5-second deadline.`);
    }
    throw new Error(`GET ${path} failed before receiving an HTTP response.`);
  }
}

function getWithHost(path, host) {
  return new Promise((resolve, reject) => {
    let settled = false;
    let response;
    let deadline;
    const rejectOnce = (message) => {
      if (settled) return;
      settled = true;
      clearTimeout(deadline);
      reject(new Error(message));
    };
    const resolveOnce = (value) => {
      if (settled) return;
      settled = true;
      clearTimeout(deadline);
      resolve(value);
    };
    const req = request(new URL(path, apiOrigin), { headers: { host } }, (incomingResponse) => {
      response = incomingResponse;
      response.once('aborted', () => rejectOnce(`GET ${path} response ended unexpectedly.`));
      response.once('error', () => rejectOnce(`GET ${path} response ended unexpectedly.`));
      response.resume();
      response.once('end', () => resolveOnce(response));
    });
    req.once('error', () => rejectOnce(`GET ${path} failed before receiving an HTTP response.`));
    deadline = setTimeout(() => {
      const deadlineMessage = `GET ${path} did not complete within the 5-second deadline.`;
      rejectOnce(deadlineMessage);
      response?.destroy();
      req.destroy();
    }, 5_000);
    req.end();
  });
}

const emulatorHealthResponse = await getWithHost('/health', '10.0.2.2');
assert.equal(
  emulatorHealthResponse.statusCode,
  200,
  'Android emulator Host header must reach the shared Backend',
);

const healthResponse = await get('/health');
for (const [name, expected] of Object.entries({
  'x-content-type-options': 'nosniff',
  'x-frame-options': 'DENY',
  'content-security-policy': "default-src 'none'; frame-ancestors 'none'",
  'referrer-policy': 'no-referrer',
})) {
  assert.equal(healthResponse.headers.get(name), expected, `${name} must use the safe baseline`);
}

const openApiResponse = await get('/openapi/v1.json');
assert.equal(openApiResponse.status, 200, 'Development OpenAPI must return HTTP 200');
const openApi = await openApiResponse.json();
assert.equal(typeof openApi.openapi, 'string', 'OpenAPI document must declare its version');
assert.ok(openApi.paths?.['/health'], 'OpenAPI must describe GET /health');
assert.ok(openApi.paths?.['/ready'], 'OpenAPI must describe GET /ready');

const notFoundResponse = await get('/api/v1/does-not-exist');
assert.equal(notFoundResponse.status, 404, 'Unknown API route must return HTTP 404');
assert.match(notFoundResponse.headers.get('content-type') ?? '', /^application\/problem\+json/i);
const problem = await notFoundResponse.json();
assert.equal(problem.status, 404);
assert.equal(problem.code, 'notFound');
assert.equal(typeof problem.traceId, 'string');
assert.ok(problem.traceId.length > 0, 'ProblemDetails must include a traceId');
assert.equal(problem.title, 'Không tìm thấy tài nguyên');
assert.equal(
  problem.detail,
  'Tài nguyên bạn yêu cầu không tồn tại hoặc bạn không có quyền truy cập.',
);
assert.equal('stackTrace' in problem, false);
assert.equal('exception' in problem, false);
