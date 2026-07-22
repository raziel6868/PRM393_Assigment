import assert from 'node:assert/strict';

const apiOrigin = process.env.QA_API_ORIGIN ?? 'http://127.0.0.1:5082';
const administratorUserName = process.env.QA_ADMIN_USERNAME;
const administratorPassword = process.env.QA_ADMIN_PASSWORD;
assert.ok(administratorUserName, 'QA_ADMIN_USERNAME is required');
assert.ok(administratorPassword, 'QA_ADMIN_PASSWORD is required');

async function request(path, { method = 'GET', token, body } = {}) {
  const headers = {};
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

async function provision(token, marker, kind, roles) {
  const response = await request('/api/v1/admin/users', {
    method: 'POST',
    token,
    body: {
      displayName: `${kind} quan hệ QA`,
      userName: `qa-rel-${kind.toLowerCase()}-${marker}`,
      email: `qa-rel-${kind.toLowerCase()}-${marker}@example.invalid`,
      roles,
    },
  });
  assert.equal(response.status, 201);
  return response.json();
}

const marker = Date.now();
const administrator = await signIn(administratorUserName, administratorPassword);
const parentUser = await provision(administrator.accessToken, marker, 'parent', ['parent']);
const studentUser = await provision(administrator.accessToken, marker, 'student', ['student']);
const secondStudentUser = await provision(administrator.accessToken, marker, 'student-two', ['student']);

const parentProfileResponse = await request('/api/v1/admin/identity-profiles/parents', {
  method: 'POST',
  token: administrator.accessToken,
  body: { userId: parentUser.userId, parentCode: `PH-${marker}` },
});
assert.equal(parentProfileResponse.status, 201);
const parentProfile = await parentProfileResponse.json();
assert.equal(parentProfile.userId, parentUser.userId);
assert.equal(parentProfile.parentCode, `PH-${marker}`);
assert.equal(parentProfile.isActive, true);

const studentProfileResponse = await request('/api/v1/admin/identity-profiles/students', {
  method: 'POST',
  token: administrator.accessToken,
  body: { userId: studentUser.userId, studentCode: `HS-${marker}` },
});
assert.equal(studentProfileResponse.status, 201);
const studentProfile = await studentProfileResponse.json();

await problem(await request('/api/v1/admin/identity-profiles/parents', {
  method: 'POST',
  token: administrator.accessToken,
  body: { userId: studentUser.userId, parentCode: `PH-SAI-${marker}` },
}), 400, 'profileRoleMismatch');

await problem(await request('/api/v1/admin/identity-profiles/students', {
  method: 'POST',
  token: administrator.accessToken,
  body: { userId: secondStudentUser.userId, studentCode: studentProfile.studentCode },
}), 409, 'profileAlreadyExists');

await validationProblem(await request('/api/v1/admin/parent-student-links', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    parentProfileId: parentProfile.id,
    studentProfileId: studentProfile.id,
    relationship: 'Father',
    isPrimaryContact: true,
  },
}), 'relationship');

const linkResponse = await request('/api/v1/admin/parent-student-links', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    parentProfileId: parentProfile.id,
    studentProfileId: studentProfile.id,
    relationship: 'father',
    isPrimaryContact: true,
  },
});
assert.equal(linkResponse.status, 201);
let link = await linkResponse.json();
assert.equal(link.relationship, 'father');
assert.equal(link.isActive, true);
assert.equal(typeof link.rowVersion, 'string');
assert.ok(link.rowVersion.length > 0);

await problem(await request('/api/v1/admin/parent-student-links', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    parentProfileId: parentProfile.id,
    studentProfileId: studentProfile.id,
    relationship: 'guardian',
    isPrimaryContact: false,
  },
}), 409, 'relationshipAlreadyExists');

const restrictedParent = await signIn(parentUser.userName, parentUser.temporaryPassword);
const normalPassword = `Relationship-${marker}!Aa7`;
assert.equal((await request('/api/v1/auth/change-temporary-password', {
  method: 'POST',
  token: restrictedParent.accessToken,
  body: {
    currentPassword: parentUser.temporaryPassword,
    newPassword: normalPassword,
    confirmation: normalPassword,
  },
})).status, 204);
const parent = await signIn(parentUser.userName, normalPassword);
delete parentUser.temporaryPassword;
delete studentUser.temporaryPassword;
delete secondStudentUser.temporaryPassword;

await problem(await request('/api/v1/admin/parent-student-links', {
  method: 'POST',
  token: parent.accessToken,
  body: {
    parentProfileId: parentProfile.id,
    studentProfileId: studentProfile.id,
    relationship: 'other',
    isPrimaryContact: false,
  },
}), 403, 'forbidden');

const childrenResponse = await request('/api/v1/relationships/children', { token: parent.accessToken });
assert.equal(childrenResponse.status, 200);
const children = await childrenResponse.json();
assert.equal(children.length, 1);
assert.deepEqual(children[0], {
  studentProfileId: studentProfile.id,
  userId: studentUser.userId,
  displayName: studentUser.displayName,
  studentCode: studentProfile.studentCode,
  relationship: 'father',
  isPrimaryContact: true,
});

await problem(await request('/api/v1/relationships/children', {
  token: administrator.accessToken,
}), 403, 'forbidden');

await validationProblem(await request(`/api/v1/admin/parent-student-links/${link.id}`, {
  method: 'PATCH',
  token: administrator.accessToken,
  body: {
    relationship: 'mother',
    isPrimaryContact: false,
    isActive: false,
    rowVersion: '',
  },
}), 'rowVersion');

const staleRowVersion = link.rowVersion;
const deactivateResponse = await request(`/api/v1/admin/parent-student-links/${link.id}`, {
  method: 'PATCH',
  token: administrator.accessToken,
  body: {
    relationship: 'mother',
    isPrimaryContact: false,
    isActive: false,
    rowVersion: link.rowVersion,
  },
});
assert.equal(deactivateResponse.status, 200);
link = await deactivateResponse.json();
assert.equal(link.relationship, 'mother');
assert.equal(link.isActive, false);
assert.notEqual(link.rowVersion, staleRowVersion);
assert.deepEqual(await (await request('/api/v1/relationships/children', { token: parent.accessToken })).json(), []);

await problem(await request(`/api/v1/admin/parent-student-links/${link.id}`, {
  method: 'PATCH',
  token: administrator.accessToken,
  body: {
    relationship: 'guardian',
    isPrimaryContact: true,
    isActive: true,
    rowVersion: staleRowVersion,
  },
}), 409, 'concurrencyConflict');

const reactivateResponse = await request(`/api/v1/admin/parent-student-links/${link.id}`, {
  method: 'PATCH',
  token: administrator.accessToken,
  body: {
    relationship: 'guardian',
    isPrimaryContact: true,
    isActive: true,
    rowVersion: link.rowVersion,
  },
});
assert.equal(reactivateResponse.status, 200);
link = await reactivateResponse.json();
assert.equal(link.relationship, 'guardian');
assert.equal(link.isActive, true);
assert.equal((await (await request('/api/v1/relationships/children', { token: parent.accessToken })).json()).length, 1);
