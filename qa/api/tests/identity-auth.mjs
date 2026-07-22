import assert from 'node:assert/strict';

const apiOrigin = process.env.QA_API_ORIGIN ?? 'http://127.0.0.1:5080';
const administratorUserName = process.env.QA_ADMIN_USERNAME;
const administratorPassword = process.env.QA_ADMIN_PASSWORD;
assert.ok(administratorUserName, 'QA_ADMIN_USERNAME is required');
assert.ok(administratorPassword, 'QA_ADMIN_PASSWORD is required');

async function request(path, { method = 'GET', token, body, headers: extraHeaders = {} } = {}) {
  const headers = { ...extraHeaders };
  if (token) headers.authorization = `Bearer ${token}`;
  if (body !== undefined) headers['content-type'] = 'application/json';
  return fetch(`${apiOrigin}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    signal: AbortSignal.timeout(5_000),
  });
}

function cookiePair(setCookie) {
  return setCookie.split(';', 1)[0];
}

async function problem(response, expectedStatus, expectedCode) {
  assert.equal(response.status, expectedStatus);
  assert.match(response.headers.get('content-type') ?? '', /^application\/problem\+json/i);
  const payload = await response.json();
  assert.equal(payload.code, expectedCode);
  assert.equal(typeof payload.traceId, 'string');
  assert.ok(payload.traceId.length > 0);
  assert.equal('stackTrace' in payload, false);
  assert.equal('exception' in payload, false);
  return payload;
}

async function validationProblem(response, field) {
  const payload = await problem(response, 400, 'validationFailed');
  assert.ok(Array.isArray(payload.errors[field]));
  assert.ok(payload.errors[field].length > 0);
}

await validationProblem(await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: 'validation-probe',
    password: 'Synthetic-Invalid-Password-7!',
    clientType: 'desktop',
  },
}), 'clientType');

await validationProblem(await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: 'validation-probe',
    password: 'Synthetic-Invalid-Password-7!',
    clientType: 'Mobile',
  },
}), 'clientType');

await problem(await request('/api/v1/auth/sign-in', {
  method: 'POST',
  headers: { origin: 'http://localhost:5173' },
  body: {
    emailOrUserName: 'transport-probe',
    password: 'Synthetic-Invalid-Password-7!',
    clientType: 'mobile',
  },
}), 403, 'invalidClientTransport');

await validationProblem(await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: { emailOrUserName: '', password: '', clientType: 'mobile' },
}), 'emailOrUserName');

await problem(await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: 'missing-account',
    password: 'Synthetic-Invalid-Password-7!',
    clientType: 'mobile',
  },
}), 401, 'invalidCredentials');

const mobileLoginResponse = await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: administratorUserName,
    password: administratorPassword,
    clientType: 'mobile',
  },
});
assert.equal(mobileLoginResponse.status, 200);
const mobileLogin = await mobileLoginResponse.json();
assert.deepEqual(mobileLogin.roles, ['administrator']);
assert.equal(mobileLogin.passwordChangeRequired, false);
assert.equal(typeof mobileLogin.accessToken, 'string');
assert.equal(typeof mobileLogin.refreshToken, 'string');
assert.ok(mobileLogin.refreshToken.length >= 64);

const sessionResponse = await request('/api/v1/auth/session', { token: mobileLogin.accessToken });
assert.equal(sessionResponse.status, 200);
const session = await sessionResponse.json();
assert.equal(session.userId, mobileLogin.userId);
assert.deepEqual(session.roles, ['administrator']);

const tokenSegments = mobileLogin.accessToken.split('.');
assert.equal(tokenSegments.length, 3);
const signatureIndex = Math.floor(tokenSegments[2].length / 2);
tokenSegments[2] = `${tokenSegments[2].slice(0, signatureIndex)}${tokenSegments[2][signatureIndex] === 'a' ? 'b' : 'a'}${tokenSegments[2].slice(signatureIndex + 1)}`;
const tamperedAccessToken = tokenSegments.join('.');
await problem(await request('/api/v1/auth/session', { token: tamperedAccessToken }), 401, 'unauthorized');

await problem(await request('/api/v1/admin/users', {
  method: 'POST',
  body: {
    displayName: 'Không được tạo',
    userName: 'unauthorized-user',
    roles: ['Teacher'],
  },
}), 401, 'unauthorized');

const allowedPreflight = await request('/api/v1/auth/refresh', {
  method: 'OPTIONS',
  headers: {
    origin: 'http://localhost:5173',
    'access-control-request-method': 'POST',
    'access-control-request-headers': 'content-type',
  },
});
assert.equal(allowedPreflight.status, 204);
assert.equal(allowedPreflight.headers.get('access-control-allow-origin'), 'http://localhost:5173');
assert.equal(allowedPreflight.headers.get('access-control-allow-credentials'), 'true');
const deniedPreflight = await request('/api/v1/auth/refresh', {
  method: 'OPTIONS',
  headers: {
    origin: 'http://untrusted.example.invalid',
    'access-control-request-method': 'POST',
  },
});
assert.equal(deniedPreflight.headers.get('access-control-allow-origin'), null);

const webLoginResponse = await request('/api/v1/auth/sign-in', {
  method: 'POST',
  headers: { origin: 'http://localhost:5173' },
  body: {
    emailOrUserName: administratorUserName,
    password: administratorPassword,
    clientType: 'web',
  },
});
assert.equal(webLoginResponse.status, 200);
const webLogin = await webLoginResponse.json();
assert.equal(webLogin.refreshToken, null);
const setCookies = webLoginResponse.headers.getSetCookie();
assert.equal(setCookies.length, 1);
assert.match(setCookies[0], /^myfschool\.refresh=/);
assert.match(setCookies[0], /HttpOnly/i);
assert.match(setCookies[0], /Secure/i);
assert.match(setCookies[0], /SameSite=Strict/i);
assert.match(setCookies[0], /Path=\/api\/v1\/auth/i);
const webCookie = cookiePair(setCookies[0]);

await problem(await request('/api/v1/auth/refresh', {
  method: 'POST',
  headers: { cookie: webCookie, origin: 'http://untrusted.example.invalid' },
  body: { clientType: 'web' },
}), 403, 'untrustedOrigin');

const webRotateResponse = await request('/api/v1/auth/refresh', {
  method: 'POST',
  headers: { cookie: webCookie, origin: 'http://localhost:5173' },
  body: { clientType: 'web' },
});
assert.equal(webRotateResponse.status, 200);
const webRotatedPayload = await webRotateResponse.json();
assert.equal(webRotatedPayload.refreshToken, null);
const webRotatedCookie = cookiePair(webRotateResponse.headers.getSetCookie()[0]);
assert.notEqual(webRotatedCookie, webCookie);
await problem(await request('/api/v1/auth/refresh', {
  method: 'POST',
  headers: { cookie: webCookie, origin: 'http://localhost:5173' },
  body: { clientType: 'web' },
}), 401, 'invalidRefreshToken');
await problem(await request('/api/v1/auth/refresh', {
  method: 'POST',
  headers: { cookie: webRotatedCookie, origin: 'http://localhost:5173' },
  body: { clientType: 'web' },
}), 401, 'invalidRefreshToken');

const rotateResponse = await request('/api/v1/auth/refresh', {
  method: 'POST',
  body: { clientType: 'mobile', refreshToken: mobileLogin.refreshToken },
});
assert.equal(rotateResponse.status, 200);
const rotated = await rotateResponse.json();
assert.equal(typeof rotated.refreshToken, 'string');
assert.notEqual(rotated.refreshToken, mobileLogin.refreshToken);

await problem(await request('/api/v1/auth/refresh', {
  method: 'POST',
  body: { clientType: 'mobile', refreshToken: mobileLogin.refreshToken },
}), 401, 'invalidRefreshToken');
await problem(await request('/api/v1/auth/refresh', {
  method: 'POST',
  body: { clientType: 'mobile', refreshToken: rotated.refreshToken },
}), 401, 'invalidRefreshToken');

const freshLoginResponse = await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: administratorUserName,
    password: administratorPassword,
    clientType: 'mobile',
  },
});
assert.equal(freshLoginResponse.status, 200);
const freshLogin = await freshLoginResponse.json();
const logoutResponse = await request('/api/v1/auth/logout', {
  method: 'POST',
  token: freshLogin.accessToken,
  body: { clientType: 'mobile', refreshToken: freshLogin.refreshToken },
});
assert.equal(logoutResponse.status, 204);
await problem(await request('/api/v1/auth/refresh', {
  method: 'POST',
  body: { clientType: 'mobile', refreshToken: freshLogin.refreshToken },
}), 401, 'invalidRefreshToken');

const provisionResponse = await request('/api/v1/admin/users', {
  method: 'POST',
  token: mobileLogin.accessToken,
  body: {
    displayName: 'Giáo viên QA',
    userName: `qa-teacher-${Date.now()}`,
    email: `qa-teacher-${Date.now()}@example.invalid`,
    roles: ['teacher'],
  },
});
assert.equal(provisionResponse.status, 201);
const provisioned = await provisionResponse.json();
assert.deepEqual(provisioned.roles, ['teacher']);
assert.equal(typeof provisioned.temporaryPassword, 'string');
assert.ok(provisioned.temporaryPassword.length >= 12);
const temporaryPassword = provisioned.temporaryPassword;
delete provisioned.temporaryPassword;

const restrictedLoginResponse = await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: provisioned.userName,
    password: temporaryPassword,
    clientType: 'mobile',
  },
});
assert.equal(restrictedLoginResponse.status, 200);
const restrictedLogin = await restrictedLoginResponse.json();
assert.equal(restrictedLogin.passwordChangeRequired, true);
assert.equal(restrictedLogin.refreshToken, null);
assert.deepEqual(restrictedLogin.roles, ['teacher']);

await problem(await request('/api/v1/admin/users', {
  method: 'POST',
  token: restrictedLogin.accessToken,
  body: {
    displayName: 'Không được tạo bởi giáo viên',
    userName: `qa-forbidden-${Date.now()}`,
    roles: ['student'],
  },
}), 403, 'forbidden');

const duplicateResponse = await request('/api/v1/admin/users', {
  method: 'POST',
  token: mobileLogin.accessToken,
  body: {
    displayName: 'Giáo viên trùng',
    userName: provisioned.userName,
    email: provisioned.email,
    roles: ['teacher'],
  },
});
await problem(duplicateResponse, 409, 'accountAlreadyExists');

await validationProblem(await request('/api/v1/admin/users', {
  method: 'POST',
  token: mobileLogin.accessToken,
  body: {
    displayName: 'Vai trò lỗi',
    userName: `qa-invalid-role-${Date.now()}`,
    roles: ['headTeacher'],
  },
}), 'roles');
await validationProblem(await request('/api/v1/admin/users', {
  method: 'POST',
  token: mobileLogin.accessToken,
  body: {
    displayName: 'Vai trò sai kiểu chữ',
    userName: `qa-role-case-${Date.now()}`,
    roles: ['Teacher'],
  },
}), 'roles');

const restrictedAdminProvisionResponse = await request('/api/v1/admin/users', {
  method: 'POST',
  token: mobileLogin.accessToken,
  body: {
    displayName: 'Quản trị viên tạm QA',
    userName: `qa-restricted-admin-${Date.now()}`,
    email: `qa-restricted-admin-${Date.now()}@example.invalid`,
    roles: ['administrator'],
  },
});
assert.equal(restrictedAdminProvisionResponse.status, 201);
const restrictedAdmin = await restrictedAdminProvisionResponse.json();
const restrictedAdminPassword = restrictedAdmin.temporaryPassword;
delete restrictedAdmin.temporaryPassword;
const restrictedAdminLoginResponse = await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: restrictedAdmin.userName,
    password: restrictedAdminPassword,
    clientType: 'mobile',
  },
});
assert.equal(restrictedAdminLoginResponse.status, 200);
const restrictedAdminLogin = await restrictedAdminLoginResponse.json();
assert.equal(restrictedAdminLogin.passwordChangeRequired, true);
await problem(await request('/api/v1/admin/users', {
  method: 'POST',
  token: restrictedAdminLogin.accessToken,
  body: {
    displayName: 'Không được tạo bởi phiên hạn chế',
    userName: `qa-restricted-denied-${Date.now()}`,
    roles: ['student'],
  },
}), 403, 'forbidden');
assert.equal((await request('/api/v1/auth/session', { token: restrictedAdminLogin.accessToken })).status, 200);

const crossLogoutFamilyResponse = await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: administratorUserName,
    password: administratorPassword,
    clientType: 'mobile',
  },
});
assert.equal(crossLogoutFamilyResponse.status, 200);
const crossLogoutFamily = await crossLogoutFamilyResponse.json();
assert.equal((await request('/api/v1/auth/logout', {
  method: 'POST',
  body: { clientType: 'mobile', refreshToken: crossLogoutFamily.refreshToken },
})).status, 204);
await problem(await request('/api/v1/auth/refresh', {
  method: 'POST',
  body: { clientType: 'mobile', refreshToken: crossLogoutFamily.refreshToken },
}), 401, 'invalidRefreshToken');

const concurrentFamilyResponse = await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: administratorUserName,
    password: administratorPassword,
    clientType: 'mobile',
  },
});
assert.equal(concurrentFamilyResponse.status, 200);
const concurrentFamily = await concurrentFamilyResponse.json();
const concurrentResults = await Promise.all([
  request('/api/v1/auth/refresh', {
    method: 'POST',
    body: { clientType: 'mobile', refreshToken: concurrentFamily.refreshToken },
  }),
  request('/api/v1/auth/refresh', {
    method: 'POST',
    body: { clientType: 'mobile', refreshToken: concurrentFamily.refreshToken },
  }),
]);
assert.deepEqual(concurrentResults.map(response => response.status).sort(), [200, 401]);
const concurrentFailure = concurrentResults.find(response => response.status === 401);
await problem(concurrentFailure, 401, 'invalidRefreshToken');

const logoutWebLoginResponse = await request('/api/v1/auth/sign-in', {
  method: 'POST',
  headers: { origin: 'http://localhost:5173' },
  body: {
    emailOrUserName: administratorUserName,
    password: administratorPassword,
    clientType: 'web',
  },
});
assert.equal(logoutWebLoginResponse.status, 200);
const logoutWebLogin = await logoutWebLoginResponse.json();
const logoutWebCookie = cookiePair(logoutWebLoginResponse.headers.getSetCookie()[0]);
await problem(await request('/api/v1/auth/logout', {
  method: 'POST',
  token: logoutWebLogin.accessToken,
  headers: { cookie: logoutWebCookie, origin: 'http://untrusted.example.invalid' },
  body: { clientType: 'web' },
}), 403, 'untrustedOrigin');
const webLogoutResponse = await request('/api/v1/auth/logout', {
  method: 'POST',
  headers: { cookie: logoutWebCookie, origin: 'http://localhost:5173' },
  body: { clientType: 'web' },
});
assert.equal(webLogoutResponse.status, 204);
assert.match(webLogoutResponse.headers.getSetCookie()[0], /expires=Thu, 01 Jan 1970|Max-Age=0/i);
await problem(await request('/api/v1/auth/refresh', {
  method: 'POST',
  headers: { cookie: logoutWebCookie, origin: 'http://localhost:5173' },
  body: { clientType: 'web' },
}), 401, 'invalidRefreshToken');

for (let attempt = 0; attempt < 5; attempt += 1) {
  await problem(await request('/api/v1/auth/sign-in', {
    method: 'POST',
    body: {
      emailOrUserName: provisioned.userName,
      password: `Wrong-Password-${attempt}!`,
      clientType: 'mobile',
    },
  }), 401, 'invalidCredentials');
}
await problem(await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: provisioned.userName,
    password: temporaryPassword,
    clientType: 'mobile',
  },
}), 401, 'invalidCredentials');

let rateLimitedResponse;
for (let attempt = 0; attempt < 20; attempt += 1) {
  const response = await request('/api/v1/auth/sign-in', {
    method: 'POST',
    headers: { origin: 'http://localhost:5173' },
    body: {
      emailOrUserName: 'rate-limit-probe',
      password: 'Synthetic-Invalid-Password-7!',
      clientType: 'web',
    },
  });
  if (response.status === 429) {
    rateLimitedResponse = response;
    break;
  }
}
assert.ok(rateLimitedResponse, 'Expected the sign-in rate limiter to reject a bounded request');
await problem(rateLimitedResponse, 429, 'tooManyRequests');
assert.equal(rateLimitedResponse.headers.get('access-control-allow-origin'), 'http://localhost:5173');
assert.equal(rateLimitedResponse.headers.get('access-control-allow-credentials'), 'true');
