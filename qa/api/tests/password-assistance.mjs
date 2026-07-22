import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const apiOrigin = process.env.QA_API_ORIGIN ?? 'http://127.0.0.1:5080';
const administratorUserName = process.env.QA_ADMIN_USERNAME;
const administratorPassword = process.env.QA_ADMIN_PASSWORD;
const expireFixturePath = fileURLToPath(new URL('../../scripts/expire-temporary-password.ps1', import.meta.url));
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

async function signIn(emailOrUserName, password) {
  const response = await request('/api/v1/auth/sign-in', {
    method: 'POST',
    body: { emailOrUserName, password, clientType: 'mobile' },
  });
  assert.equal(response.status, 200);
  return response.json();
}

const administrator = await signIn(administratorUserName, administratorPassword);
const marker = Date.now();
const teacherUserName = `qa-help-teacher-${marker}`;
const teacherEmail = `qa-help-teacher-${marker}@example.invalid`;
const provisionResponse = await request('/api/v1/admin/users', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    displayName: 'Giáo viên hỗ trợ QA',
    userName: teacherUserName,
    email: teacherEmail,
    roles: ['teacher'],
  },
});
assert.equal(provisionResponse.status, 201);
const provisioned = await provisionResponse.json();
const initialTemporaryPassword = provisioned.temporaryPassword;
delete provisioned.temporaryPassword;
const initialRestricted = await signIn(teacherUserName, initialTemporaryPassword);
assert.equal(initialRestricted.passwordChangeRequired, true);

const firstNormalPassword = `First-Normal-${marker}!Aa7`;
await validationProblem(await request('/api/v1/auth/change-temporary-password', {
  method: 'POST',
  token: initialRestricted.accessToken,
  body: {
    currentPassword: initialTemporaryPassword,
    newPassword: firstNormalPassword,
    confirmation: `${firstNormalPassword}-mismatch`,
  },
}), 'confirmation');
const firstChangeResponse = await request('/api/v1/auth/change-temporary-password', {
  method: 'POST',
  token: initialRestricted.accessToken,
  body: {
    currentPassword: initialTemporaryPassword,
    newPassword: firstNormalPassword,
    confirmation: firstNormalPassword,
  },
});
assert.equal(firstChangeResponse.status, 204);
await problem(await request('/api/v1/auth/session', {
  token: initialRestricted.accessToken,
}), 401, 'unauthorized');
const normalTeacher = await signIn(teacherUserName, firstNormalPassword);
assert.equal(normalTeacher.passwordChangeRequired, false);
assert.equal(typeof normalTeacher.refreshToken, 'string');

for (let attempt = 0; attempt < 5; attempt += 1) {
  await problem(await request('/api/v1/auth/sign-in', {
    method: 'POST',
    body: {
      emailOrUserName: teacherUserName,
      password: `Wrong-Recovery-Password-${attempt}!`,
      clientType: 'mobile',
    },
  }), 401, 'invalidCredentials');
}
await problem(await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: { emailOrUserName: teacherUserName, password: firstNormalPassword, clientType: 'mobile' },
}), 401, 'invalidCredentials');
await problem(await request('/api/v1/admin/users', {
  method: 'POST',
  token: normalTeacher.accessToken,
  body: {
    displayName: 'Không được tạo',
    userName: `qa-help-forbidden-${marker}`,
    roles: ['student'],
  },
}), 403, 'forbidden');

const unknownStartedAt = performance.now();
const genericUnknown = await request('/api/v1/auth/password-help-requests', {
  method: 'POST',
  body: { emailOrUserName: `unknown-${marker}` },
});
const unknownDuration = performance.now() - unknownStartedAt;
assert.equal(genericUnknown.status, 202);
const genericUnknownBody = await genericUnknown.json();
assert.deepEqual(Object.keys(genericUnknownBody), ['message']);
const knownStartedAt = performance.now();
const genericKnownResponses = await Promise.all([
  request('/api/v1/auth/password-help-requests', {
    method: 'POST',
    body: { emailOrUserName: teacherEmail },
  }),
  request('/api/v1/auth/password-help-requests', {
    method: 'POST',
    body: { emailOrUserName: teacherEmail },
  }),
]);
const knownDuration = performance.now() - knownStartedAt;
assert.ok(unknownDuration >= 250 && unknownDuration < 1_500);
assert.ok(knownDuration >= 250 && knownDuration < 1_500);
assert.ok(Math.abs(unknownDuration - knownDuration) < 500);
for (const response of genericKnownResponses) {
  assert.equal(response.status, 202);
  assert.deepEqual(await response.json(), genericUnknownBody);
}

await problem(await request('/api/v1/admin/password-help-requests'), 401, 'unauthorized');
await problem(await request('/api/v1/admin/password-help-requests', {
  token: normalTeacher.accessToken,
}), 403, 'forbidden');
const pendingResponse = await request('/api/v1/admin/password-help-requests?status=pending&page=1&pageSize=20', {
  token: administrator.accessToken,
});
assert.equal(pendingResponse.status, 200);
const pendingPage = await pendingResponse.json();
assert.equal(pendingPage.page, 1);
assert.equal(pendingPage.pageSize, 20);
assert.equal(pendingPage.totalCount, 1);
assert.equal(pendingPage.totalPages, 1);
assert.equal(pendingPage.items.length, 1);
const pending = pendingPage.items[0];
assert.equal(pending.userId, provisioned.userId);
assert.equal(pending.status, 'pending');
assert.equal(typeof pending.rowVersion, 'string');
assert.ok(pending.rowVersion.length > 0);

await problem(await request(`/api/v1/admin/users/${provisioned.userId}/issue-temporary-password`, {
  method: 'POST',
  token: normalTeacher.accessToken,
  body: { confirmed: true, rowVersion: pending.rowVersion },
}), 403, 'forbidden');
await validationProblem(await request(`/api/v1/admin/users/${provisioned.userId}/issue-temporary-password`, {
  method: 'POST',
  token: administrator.accessToken,
  body: { confirmed: false, rowVersion: pending.rowVersion },
}), 'confirmed');
await validationProblem(await request(`/api/v1/admin/users/${provisioned.userId}/issue-temporary-password`, {
  method: 'POST',
  token: administrator.accessToken,
  body: { confirmed: true },
}), 'rowVersion');

const concurrentIssueResponses = await Promise.all([
  request(`/api/v1/admin/users/${provisioned.userId}/issue-temporary-password`, {
    method: 'POST',
    token: administrator.accessToken,
    body: { confirmed: true, rowVersion: pending.rowVersion },
  }),
  request(`/api/v1/admin/users/${provisioned.userId}/issue-temporary-password`, {
    method: 'POST',
    token: administrator.accessToken,
    body: { confirmed: true, rowVersion: pending.rowVersion },
  }),
  request('/api/v1/auth/refresh', {
    method: 'POST',
    body: { clientType: 'mobile', refreshToken: normalTeacher.refreshToken },
  }),
]);
assert.deepEqual(concurrentIssueResponses.slice(0, 2).map(response => response.status).sort(), [200, 409]);
const issueResponse = concurrentIssueResponses.find(response => response.status === 200);
const concurrentIssueConflict = concurrentIssueResponses.find(response => response.status === 409);
await problem(concurrentIssueConflict, 409, 'passwordHelpRequestNotPending');
const concurrentRefreshResponse = concurrentIssueResponses[2];
assert.ok([200, 401].includes(concurrentRefreshResponse.status));
assert.equal(issueResponse.status, 200);
assert.equal(issueResponse.headers.get('cache-control'), 'no-store');
const issued = await issueResponse.json();
assert.equal(issued.userId, provisioned.userId);
assert.equal(typeof issued.temporaryPassword, 'string');
assert.ok(issued.temporaryPassword.length >= 12);
const issuedTemporaryPassword = issued.temporaryPassword;
delete issued.temporaryPassword;
if (concurrentRefreshResponse.status === 200) {
  const concurrentlyRotated = await concurrentRefreshResponse.json();
  await problem(await request('/api/v1/auth/refresh', {
    method: 'POST',
    body: { clientType: 'mobile', refreshToken: concurrentlyRotated.refreshToken },
  }), 401, 'invalidRefreshToken');
} else {
  await problem(concurrentRefreshResponse, 401, 'invalidRefreshToken');
}

const resolvedPageResponse = await request('/api/v1/admin/password-help-requests?status=resolved&page=1&pageSize=20', {
  token: administrator.accessToken,
});
assert.equal(resolvedPageResponse.status, 200);
const resolvedPage = await resolvedPageResponse.json();
const resolvedItem = resolvedPage.items.find(item => item.userId === provisioned.userId);
assert.ok(resolvedItem);
assert.equal(resolvedItem.status, 'resolved');
assert.equal('temporaryPassword' in resolvedItem, false);

await problem(await request(`/api/v1/admin/users/${provisioned.userId}/issue-temporary-password`, {
  method: 'POST',
  token: administrator.accessToken,
  body: { confirmed: true, rowVersion: pending.rowVersion },
}), 409, 'passwordHelpRequestNotPending');
await problem(await request('/api/v1/auth/refresh', {
  method: 'POST',
  body: { clientType: 'mobile', refreshToken: normalTeacher.refreshToken },
}), 401, 'invalidRefreshToken');
await problem(await request('/api/v1/auth/session', {
  token: normalTeacher.accessToken,
}), 401, 'unauthorized');
await problem(await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: { emailOrUserName: teacherUserName, password: firstNormalPassword, clientType: 'mobile' },
}), 401, 'invalidCredentials');

const issuedRestricted = await signIn(teacherUserName, issuedTemporaryPassword);
assert.equal(issuedRestricted.passwordChangeRequired, true);
assert.equal(issuedRestricted.refreshToken, null);
const finalPassword = `Final-Normal-${marker}!Aa7`;
await validationProblem(await request('/api/v1/auth/change-temporary-password', {
  method: 'POST',
  token: issuedRestricted.accessToken,
  body: {
    currentPassword: issuedTemporaryPassword,
    newPassword: issuedTemporaryPassword,
    confirmation: issuedTemporaryPassword,
  },
}), 'newPassword');
await problem(await request('/api/v1/auth/change-temporary-password', {
  method: 'POST',
  token: issuedRestricted.accessToken,
  body: {
    currentPassword: 'Wrong-Current-Password-7!',
    newPassword: finalPassword,
    confirmation: finalPassword,
  },
}), 400, 'invalidCurrentPassword');
await validationProblem(await request('/api/v1/auth/change-temporary-password', {
  method: 'POST',
  token: issuedRestricted.accessToken,
  body: {
    currentPassword: issuedTemporaryPassword,
    newPassword: 'weak-password',
    confirmation: 'weak-password',
  },
}), 'newPassword');
assert.equal((await request('/api/v1/auth/change-temporary-password', {
  method: 'POST',
  token: issuedRestricted.accessToken,
  body: {
    currentPassword: issuedTemporaryPassword,
    newPassword: finalPassword,
    confirmation: finalPassword,
  },
})).status, 204);
await problem(await request('/api/v1/auth/change-temporary-password', {
  method: 'POST',
  token: issuedRestricted.accessToken,
  body: {
    currentPassword: issuedTemporaryPassword,
    newPassword: finalPassword,
    confirmation: finalPassword,
  },
}), 401, 'unauthorized');
await problem(await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: { emailOrUserName: teacherUserName, password: issuedTemporaryPassword, clientType: 'mobile' },
}), 401, 'invalidCredentials');
const finalTeacher = await signIn(teacherUserName, finalPassword);
assert.equal(finalTeacher.passwordChangeRequired, false);

const rejectedUserName = `qa-help-rejected-${marker}`;
const rejectedProvisionResponse = await request('/api/v1/admin/users', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    displayName: 'Học sinh bị từ chối QA',
    userName: rejectedUserName,
    roles: ['student'],
  },
});
assert.equal(rejectedProvisionResponse.status, 201);
const rejectedUser = await rejectedProvisionResponse.json();
delete rejectedUser.temporaryPassword;
assert.equal((await request('/api/v1/auth/password-help-requests', {
  method: 'POST',
  body: { emailOrUserName: rejectedUserName },
})).status, 202);
const rejectedPendingResponse = await request('/api/v1/admin/password-help-requests?status=pending&page=1&pageSize=20', {
  token: administrator.accessToken,
});
assert.equal(rejectedPendingResponse.status, 200);
const rejectedPendingPage = await rejectedPendingResponse.json();
const rejectedPending = rejectedPendingPage.items.find(item => item.userId === rejectedUser.userId);
assert.ok(rejectedPending);
assert.equal((await request(`/api/v1/admin/password-help-requests/${rejectedPending.requestId}/reject`, {
  method: 'POST',
  token: administrator.accessToken,
  body: { confirmed: true, rowVersion: rejectedPending.rowVersion },
})).status, 204);
const rejectedPageResponse = await request('/api/v1/admin/password-help-requests?status=rejected&page=1&pageSize=20', {
  token: administrator.accessToken,
});
assert.equal(rejectedPageResponse.status, 200);
const rejectedPage = await rejectedPageResponse.json();
assert.ok(rejectedPage.items.some(item => item.requestId === rejectedPending.requestId && item.status === 'rejected'));

await problem(await request(`/api/v1/admin/users/${rejectedUser.userId}/issue-temporary-password`, {
  method: 'POST',
  token: administrator.accessToken,
  body: { confirmed: true, rowVersion: rejectedPending.rowVersion },
}), 409, 'passwordHelpRequestNotPending');

const expiryUserName = `qa-help-expiry-${marker}`;
const expiryProvisionResponse = await request('/api/v1/admin/users', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    displayName: 'Học sinh hết hạn QA',
    userName: expiryUserName,
    roles: ['student'],
  },
});
assert.equal(expiryProvisionResponse.status, 201);
const expiryUser = await expiryProvisionResponse.json();
const expiryTemporaryPassword = expiryUser.temporaryPassword;
delete expiryUser.temporaryPassword;
const preExpirySession = await signIn(expiryUserName, expiryTemporaryPassword);
assert.equal(preExpirySession.passwordChangeRequired, true);
execFileSync('powershell.exe', [
  '-NoProfile',
  '-NonInteractive',
  '-File',
  path.resolve(expireFixturePath),
  expiryUser.userId,
], { stdio: 'pipe', timeout: 5_000 });
await problem(await request('/api/v1/auth/sign-in', {
  method: 'POST',
  body: {
    emailOrUserName: expiryUserName,
    password: expiryTemporaryPassword,
    clientType: 'mobile',
  },
}), 401, 'temporaryPasswordExpired');
await problem(await request('/api/v1/auth/change-temporary-password', {
  method: 'POST',
  token: preExpirySession.accessToken,
  body: {
    currentPassword: expiryTemporaryPassword,
    newPassword: `Expired-Replacement-${marker}!Aa7`,
    confirmation: `Expired-Replacement-${marker}!Aa7`,
  },
}), 401, 'temporaryPasswordExpired');
await problem(await request('/api/v1/admin/users', {
  method: 'POST',
  token: preExpirySession.accessToken,
  body: {
    displayName: 'Không được tạo bởi phiên hết hạn',
    userName: `qa-expired-forbidden-${marker}`,
    roles: ['student'],
  },
}), 403, 'forbidden');

let rateLimitedResponse;
for (let attempt = 0; attempt < 12; attempt += 1) {
  const response = await request('/api/v1/auth/password-help-requests', {
    method: 'POST',
    body: { emailOrUserName: `rate-help-${marker}-${attempt}` },
  });
  if (response.status === 429) {
    rateLimitedResponse = response;
    break;
  }
}
assert.ok(rateLimitedResponse, 'Expected password-help rate limiting');
await problem(rateLimitedResponse, 429, 'tooManyRequests');
