import assert from 'node:assert/strict';

const apiOrigin = process.env.QA_API_ORIGIN ?? 'http://127.0.0.1:5082';
const administratorUserName = process.env.QA_ADMIN_USERNAME;
const administratorPassword = process.env.QA_ADMIN_PASSWORD;
assert.ok(administratorUserName, 'QA_ADMIN_USERNAME is required');
assert.ok(administratorPassword, 'QA_ADMIN_PASSWORD is required');

async function run() {

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
      displayName: `QA Leave ${kind} ${marker}`,
      userName: `qa.leave.${kind}.${marker}`,
      email: `qa.leave.${kind}.${marker}@example.test`,
      roles,
    },
  });
  assert.equal(response.status, 201);
  return response.json();
}

async function createProfile(token, kind, userId, marker) {
  const code = `LEAVE-${kind.toUpperCase()}-${marker}`;
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
  const newPassword = `Leave-${marker}-${tag}-Aa7!`;
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

const marker = Date.now();
const administrator = await signIn(administratorUserName, administratorPassword);

const teacherUser = await provision(administrator.accessToken, marker, 'teacher', ['teacher']);
const studentUser = await provision(administrator.accessToken, marker, 'student', ['student']);
const parentUser = await provision(administrator.accessToken, marker, 'parent', ['parent']);
const otherStudentUser = await provision(administrator.accessToken, `${marker}-os`, 'student', ['student']);

const teacherProfile = await createProfile(administrator.accessToken, 'teacher', teacherUser.userId, marker);
const studentProfile = await createProfile(administrator.accessToken, 'student', studentUser.userId, marker);
const parentProfile = await createProfile(administrator.accessToken, 'parent', parentUser.userId, marker);
const otherStudentProfile = await createProfile(administrator.accessToken, 'student', otherStudentUser.userId, `${marker}-os`);
await linkParent(administrator.accessToken, parentProfile.id, studentProfile.id);

const yearStart = new Date(`${new Date().getFullYear() - 1}-09-01`);
const yearEnd = new Date(`${new Date().getFullYear()}-05-31`);
const year = await (await request('/api/v1/admin/school-years', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    code: `LY${marker}`.slice(-20),
    displayName: `Năm học LEAVE ${marker}`.slice(0, 200),
    startDate: yearStart.toISOString().slice(0, 10),
    endDate: yearEnd.toISOString().slice(0, 10),
  },
})).json();
const subject = await (await request('/api/v1/admin/subjects', {
  method: 'POST',
  token: administrator.accessToken,
  body: { code: `LM${marker}`.slice(-20), displayName: 'Toán'.slice(0, 200) },
})).json();
const classroom = await (await request('/api/v1/admin/classes', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    code: `LC${marker}`.slice(-20),
    displayName: `Lớp LEAVE ${marker}`.slice(0, 200),
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

const parentPassword = await finalizePassword(parentUser.userName, parentUser.temporaryPassword, marker, 'P');
const teacherPassword = await finalizePassword(teacherUser.userName, teacherUser.temporaryPassword, marker, 'T');
const studentPassword = await finalizePassword(studentUser.userName, studentUser.temporaryPassword, marker, 'S');

const parentSession = await signIn(parentUser.userName, parentPassword);
const teacherSession = await signIn(teacherUser.userName, teacherPassword);
const studentSession = await signIn(studentUser.userName, studentPassword);

const leaveStart = new Date(yearStart.getTime() + 7 * 24 * 60 * 60 * 1000);
const leaveEnd = new Date(yearStart.getTime() + 9 * 24 * 60 * 60 * 1000);
const isoStart = leaveStart.toISOString().slice(0, 10);
const isoEnd = leaveEnd.toISOString().slice(0, 10);

// Parent submits leave request
const submitResponse = await request('/api/v1/leave-requests', {
  method: 'POST',
  token: parentSession.accessToken,
  body: {
    studentProfileId: studentProfile.id,
    startDate: isoStart,
    endDate: isoEnd,
    startSession: 'morning',
    endSession: 'afternoon',
    reasonCategory: 'health',
    reason: 'Học sinh bị sốt nhẹ cần nghỉ để theo dõi sức khoẻ tại nhà.',
  },
});
assert.equal(submitResponse.status, 201);
const leaveRequest = await submitResponse.json();
assert.equal(leaveRequest.status, 'pending');

// Duplicate pending request rejected
const duplicate = await request('/api/v1/leave-requests', {
  method: 'POST',
  token: parentSession.accessToken,
  body: {
    studentProfileId: studentProfile.id,
    startDate: isoStart,
    endDate: isoEnd,
    startSession: 'morning',
    endSession: 'afternoon',
    reasonCategory: 'health',
    reason: 'Đơn trùng ngày với đơn đang chờ phía trên để test dedup.',
  },
});
await problem(duplicate, 409, 'leaveRequestAlreadyPending');

// Parent lists own requests
const listResponse = await request('/api/v1/leave-requests?page=1&pageSize=20', { token: parentSession.accessToken });
assert.equal(listResponse.status, 200);
const listBody = await listResponse.json();
assert.ok(listBody.items.length >= 1);
assert.equal(listBody.items[0].id, leaveRequest.id);

// Reason too short rejected
const shortReason = await request('/api/v1/leave-requests', {
  method: 'POST',
  token: parentSession.accessToken,
  body: {
    studentProfileId: studentProfile.id,
    startDate: isoStart,
    endDate: isoEnd,
    startSession: 'morning',
    endSession: 'afternoon',
    reasonCategory: 'health',
    reason: 'short',
  },
});
assert.equal(shortReason.status, 400);

// Invalid date range rejected
const invalidRange = await request('/api/v1/leave-requests', {
  method: 'POST',
  token: parentSession.accessToken,
  body: {
    studentProfileId: studentProfile.id,
    startDate: isoEnd,
    endDate: isoStart,
    startSession: 'morning',
    endSession: 'afternoon',
    reasonCategory: 'health',
    reason: 'Cố tình đảo ngày để test validation.',
  },
});
await problem(invalidRange, 400, 'invalidDateRange');

// Teacher sees pending request in queue
const queueResponse = await request('/api/v1/teacher/leave-requests/queue?page=1&pageSize=20', { token: teacherSession.accessToken });
assert.equal(queueResponse.status, 200);
const queueBody = await queueResponse.json();
const queueItem = queueBody.items.find((item) => item.id === leaveRequest.id);
assert.ok(queueItem, 'teacher queue should include pending leave request for assigned student');

// Teacher approves
const approveResponse = await request(`/api/v1/teacher/leave-requests/${leaveRequest.id}/decide`, {
  method: 'POST',
  token: teacherSession.accessToken,
  body: {
    approve: true,
    decisionNote: 'Đồng ý cho nghỉ, em học sinh hồi phục sức khoẻ.',
    rowVersion: leaveRequest.rowVersion,
  },
});
assert.equal(approveResponse.status, 200);
const approved = await approveResponse.json();
assert.equal(approved.status, 'approved');

// Approving twice rejected as concurrency
const reapprove = await request(`/api/v1/teacher/leave-requests/${leaveRequest.id}/decide`, {
  method: 'POST',
  token: teacherSession.accessToken,
  body: {
    approve: true,
    decisionNote: 'Duyệt lần hai',
    rowVersion: leaveRequest.rowVersion,
  },
});
await problem(reapprove, 409, 'concurrencyConflict');

// Parent tries to cancel an Approved request → 409
const cancelApproved = await request(`/api/v1/leave-requests/${leaveRequest.id}/cancel`, {
  method: 'POST',
  token: parentSession.accessToken,
  body: { rowVersion: approved.rowVersion },
});
await problem(cancelApproved, 409, 'leaveRequestNotPending');

// Parent submits another request and rejects it
const submitTwo = await request('/api/v1/leave-requests', {
  method: 'POST',
  token: parentSession.accessToken,
  body: {
    studentProfileId: studentProfile.id,
    startDate: new Date(leaveStart.getTime() + 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10),
    endDate: new Date(leaveStart.getTime() + 32 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10),
    startSession: 'morning',
    endSession: 'afternoon',
    reasonCategory: 'family',
    reason: 'Gia đình có việc đột xuất nên xin phép nghỉ hai ngày cuối tuần.',
  },
});
assert.equal(submitTwo.status, 201);
const leaveTwo = await submitTwo.json();

// Reject without reason → 400
const rejectNoReason = await request(`/api/v1/teacher/leave-requests/${leaveTwo.id}/decide`, {
  method: 'POST',
  token: teacherSession.accessToken,
  body: {
    approve: false,
    decisionNote: null,
    rowVersion: leaveTwo.rowVersion,
  },
});
await problem(rejectNoReason, 400, 'rejectionReasonRequired');

// Reject with reason
const reject = await request(`/api/v1/teacher/leave-requests/${leaveTwo.id}/decide`, {
  method: 'POST',
  token: teacherSession.accessToken,
  body: {
    approve: false,
    decisionNote: 'Đơn quá gần ngày học, gia đình vui lòng sắp xếp khác.',
    rowVersion: leaveTwo.rowVersion,
  },
});
assert.equal(reject.status, 200);
const rejected = await reject.json();
assert.equal(rejected.status, 'rejected');

// Parent cancels Pending request
const submitThree = await request('/api/v1/leave-requests', {
  method: 'POST',
  token: parentSession.accessToken,
  body: {
    studentProfileId: studentProfile.id,
    startDate: new Date(leaveStart.getTime() + 60 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10),
    endDate: new Date(leaveStart.getTime() + 61 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10),
    startSession: 'morning',
    endSession: 'afternoon',
    reasonCategory: 'personal',
    reason: 'Đơn cần test huỷ để kiểm tra vòng đời của leave request.',
  },
});
assert.equal(submitThree.status, 201);
const leaveThree = await submitThree.json();

const cancelPending = await request(`/api/v1/leave-requests/${leaveThree.id}/cancel`, {
  method: 'POST',
  token: parentSession.accessToken,
  body: { rowVersion: leaveThree.rowVersion },
});
assert.equal(cancelPending.status, 200);
const cancelled = await cancelPending.json();
assert.equal(cancelled.status, 'cancelled');

// Parent cannot access other child's request
const otherRequest = await request('/api/v1/leave-requests', {
  method: 'POST',
  token: parentSession.accessToken,
  body: {
    studentProfileId: otherStudentProfile.id,
    startDate: isoStart,
    endDate: isoEnd,
    startSession: 'morning',
    endSession: 'afternoon',
    reasonCategory: 'personal',
    reason: 'Đơn cho học sinh không phải con của phụ huynh để test 403.',
  },
});
assert.equal(otherRequest.status, 403);

// Student sees own leave requests via /students/me/leave-requests
const studentOwn = await request('/api/v1/students/me/leave-requests?page=1&pageSize=20', { token: studentSession.accessToken });
assert.equal(studentOwn.status, 200);
const studentOwnBody = await studentOwn.json();
assert.ok(studentOwnBody.items.length >= 3);

console.log('leave-request contract tests passed');
}

run().catch((err) => {
  console.error('leave-request scenario failed:', err && err.stack ? err.stack : err);
  process.exit(1);
});