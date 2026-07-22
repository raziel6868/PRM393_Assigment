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
  return payload;
}

async function signIn(emailOrUserName, password) {
  const response = await request('/api/v1/auth/sign-in', {
    method: 'POST',
    body: { emailOrUserName, password, clientType: 'mobile' },
  });
  if (response.status === 429) {
    await new Promise((resolve) => setTimeout(resolve, 7_000));
    return signIn(emailOrUserName, password);
  }
  assert.equal(response.status, 200);
  return response.json();
}

async function provision(token, marker, kind, roles) {
  const response = await request('/api/v1/admin/users', {
    method: 'POST',
    token,
    body: {
      displayName: `QA Attendance ${kind} ${marker}`,
      userName: `qa.att.${kind}.${marker}`,
      email: `qa.att.${kind}.${marker}@example.test`,
      roles,
    },
  });
  assert.equal(response.status, 201);
  return response.json();
}

async function createProfile(token, kind, userId, marker) {
  const code = `ATT-${kind.toUpperCase()}-${marker}`;
  const segments = {
    teacher: ['identity-profiles/teachers', 'employeeCode'],
    student: ['identity-profiles/students', 'studentCode'],
    parent: ['identity-profiles/parents', 'parentCode'],
  }[kind];
  const [path, bodyField] = segments;
  const response = await request(`/api/v1/admin/${path}`, {
    method: 'POST',
    token,
    body: { userId, [bodyField]: code },
  });
  assert.equal(response.status, 201);
  return response.json();
}

async function linkParent(token, parentProfileId, studentProfileId) {
  const response = await request('/api/v1/admin/parent-student-links', {
    method: 'POST',
    token,
    body: {
      parentProfileId,
      studentProfileId,
      relationship: 'mother',
      isPrimaryContact: true,
    },
  });
  assert.equal(response.status, 201);
  return response.json();
}

async function finalizePassword(userName, temporaryPassword, marker, tag) {
  const restricted = await signIn(userName, temporaryPassword);
  const newPassword = `Attend-${marker}-${tag}-Aa7!`;
  const changeResponse = await request('/api/v1/auth/change-temporary-password', {
    method: 'POST',
    token: restricted.accessToken,
    body: {
      currentPassword: temporaryPassword,
      newPassword,
      confirmation: newPassword,
    },
  });
  assert.equal(changeResponse.status, 204);
  return newPassword;
}

async function setupClassContext(token, marker) {
  const yearStart = new Date(`${new Date().getFullYear() - 1}-09-01`);
  const yearEnd = new Date(`${new Date().getFullYear()}-05-31`);
  const year = await (await request('/api/v1/admin/school-years', {
    method: 'POST',
    token,
    body: {
      code: `ATT-SY-${marker}`,
      displayName: `Năm học ATT ${marker}`,
      startDate: yearStart.toISOString().slice(0, 10),
      endDate: yearEnd.toISOString().slice(0, 10),
    },
  })).json();
  const subject = await (await request('/api/v1/admin/subjects', {
    method: 'POST',
    token,
    body: { code: `ATT-MATH-${marker}`, displayName: 'Toán' },
  })).json();
  const teacherProfile = await (await request('/api/v1/admin/identity-profiles/teachers', {
    method: 'POST',
    token,
    body: { userId: '00000000-0000-0000-0000-000000000000', employeeCode: `ATT-T-${marker}` },
  })).json();
  return { year, subject, teacherProfile };
}

const marker = Date.now();
const administrator = await signIn(administratorUserName, administratorPassword);

const teacherUser = await provision(administrator.accessToken, marker, 'teacher', ['teacher']);
const studentUser = await provision(administrator.accessToken, marker, 'student', ['student']);
const parentUser = await provision(administrator.accessToken, marker, 'parent', ['parent']);

const teacherProfile = await createProfile(administrator.accessToken, 'teacher', teacherUser.userId, marker);
const studentProfile = await createProfile(administrator.accessToken, 'student', studentUser.userId, marker);
const parentProfile = await createProfile(administrator.accessToken, 'parent', parentUser.userId, marker);
await linkParent(administrator.accessToken, parentProfile.id, studentProfile.id);

const yearStart = new Date(`${new Date().getFullYear() - 1}-09-01`);
const yearEnd = new Date(`${new Date().getFullYear()}-05-31`);
const year = await (await request('/api/v1/admin/school-years', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    code: `ATT-SY-${marker}`,
    displayName: `Năm học ATT ${marker}`,
    startDate: yearStart.toISOString().slice(0, 10),
    endDate: yearEnd.toISOString().slice(0, 10),
  },
})).json();
const subject = await (await request('/api/v1/admin/subjects', {
  method: 'POST',
  token: administrator.accessToken,
  body: { code: `ATT-MATH-${marker}`, displayName: 'Toán' },
})).json();
const classroom = await (await request('/api/v1/admin/classes', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    code: `ATT-CL-${marker}`,
    displayName: `Lớp ATT ${marker}`,
    gradeLevel: 10,
    schoolYearId: year.id,
    homeroomTeacherProfileId: teacherProfile.id,
  },
})).json();
await (await request('/api/v1/admin/teacher-assignments', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    teacherProfileId: teacherProfile.id,
    classId: classroom.id,
    subjectId: subject.id,
    schoolYearId: year.id,
  },
}));
await (await request('/api/v1/admin/student-enrollments', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    studentProfileId: studentProfile.id,
    classId: classroom.id,
    schoolYearId: year.id,
    enrolledOn: yearStart.toISOString().slice(0, 10),
  },
}));

const teacherPassword = await finalizePassword(teacherUser.userName, teacherUser.temporaryPassword, marker, 'T');
const studentPassword = await finalizePassword(studentUser.userName, studentUser.temporaryPassword, marker, 'S');
const parentPassword = await finalizePassword(parentUser.userName, parentUser.temporaryPassword, marker, 'P');

const teacherSession = await signIn(teacherUser.userName, teacherPassword);
const attendanceDate = new Date(yearStart.getTime() + 24 * 60 * 60 * 1000);
const isoDate = attendanceDate.toISOString().slice(0, 10);

// Teacher fetches roster
const rosterResponse = await request(
  `/api/v1/teacher/classes/${classroom.id}/attendance?date=${isoDate}&session=morning`,
  { token: teacherSession.accessToken });
assert.equal(rosterResponse.status, 200);
const roster = await rosterResponse.json();
assert.equal(roster.classId, classroom.id);
assert.equal(roster.entries.length, 1);
assert.equal(roster.entries[0].studentProfileId, studentProfile.id);
assert.equal(roster.entries[0].status, 'unmarked');

// Teacher saves attendance
const saveResponse = await request(
  `/api/v1/teacher/classes/${classroom.id}/attendance`,
  {
    method: 'POST',
    token: teacherSession.accessToken,
    body: {
      attendanceDate: isoDate,
      session: 'morning',
      entries: [{
        studentProfileId: studentProfile.id,
        status: 'present',
        note: 'Đi học đúng giờ',
        rowVersion: roster.entries[0].rowVersion,
      }],
    },
  });
assert.equal(saveResponse.status, 200);
const saved = await saveResponse.json();
assert.equal(saved.savedCount, 1);

// Fetch roster again to verify persistence
const rosterAgainResponse = await request(
  `/api/v1/teacher/classes/${classroom.id}/attendance?date=${isoDate}&session=morning`,
  { token: teacherSession.accessToken });
const rosterAgain = await rosterAgainResponse.json();
assert.equal(rosterAgain.entries[0].status, 'present');
assert.equal(rosterAgain.entries[0].note, 'Đi học đúng giờ');

// Stale rowVersion → 409
const staleSave = await request(
  `/api/v1/teacher/classes/${classroom.id}/attendance`,
  {
    method: 'POST',
    token: teacherSession.accessToken,
    body: {
      attendanceDate: isoDate,
      session: 'morning',
      entries: [{
        studentProfileId: studentProfile.id,
        status: 'late',
        note: null,
        rowVersion: roster.entries[0].rowVersion,
      }],
    },
  });
await problem(staleSave, 409, 'concurrencyConflict');

// Unassigned teacher cannot access class roster
const otherTeacher = await provision(administrator.accessToken, `${marker}-other`, 'teacher', ['teacher']);
const otherTeacherProfile = await createProfile(administrator.accessToken, 'teacher', otherTeacher.userId, `${marker}-other`);
const otherTeacherPassword = await finalizePassword(otherTeacher.userName, otherTeacher.temporaryPassword, marker, 'OT');
const otherTeacherSession = await signIn(otherTeacher.userName, otherTeacherPassword);
const forbidden = await request(
  `/api/v1/teacher/classes/${classroom.id}/attendance?date=${isoDate}&session=morning`,
  { token: otherTeacherSession.accessToken });
await problem(forbidden, 403, 'classAccessDenied');

// Student sees own attendance history
const studentSession = await signIn(studentUser.userName, studentPassword);
const myHistory = await request('/api/v1/students/me/attendance-history?page=1&pageSize=20', { token: studentSession.accessToken });
assert.equal(myHistory.status, 200);
const myHistoryBody = await myHistory.json();
assert.ok(myHistoryBody.items.length >= 1);
assert.equal(myHistoryBody.items[0].status, 'present');

// Parent sees linked child's history
const parentSession = await signIn(parentUser.userName, parentPassword);
const childHistory = await request(
  `/api/v1/students/me/attendance-history?page=1&pageSize=20&studentProfileId=${studentProfile.id}`,
  { token: parentSession.accessToken });
assert.equal(childHistory.status, 200);
const childHistoryBody = await childHistory.json();
assert.ok(childHistoryBody.items.length >= 1);

// Parent requesting another child → 403
const otherStudentUser = await provision(administrator.accessToken, `${marker}-other`, 'student', ['student']);
const otherStudentProfile = await createProfile(administrator.accessToken, 'student', otherStudentUser.userId, `${marker}-other`);
const parentForbidden = await request(
  `/api/v1/students/me/attendance-history?page=1&pageSize=20&studentProfileId=${otherStudentProfile.id}`,
  { token: parentSession.accessToken });
await problem(parentForbidden, 403, 'forbidden');

// Admin does not have an attendance-history route → 403
const adminHistory = await request('/api/v1/students/me/attendance-history?page=1&pageSize=20', { token: administrator.accessToken });
assert.equal(adminHistory.status, 403);

console.log('attendance contract tests passed');
