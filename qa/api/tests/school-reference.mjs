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
  assert.equal(response.status, 200, `sign-in for ${emailOrUserName} failed with ${response.status}`);
  return response.json();
}

async function provision(token, marker, kind, roles) {
  const response = await request('/api/v1/admin/users', {
    method: 'POST',
    token,
    body: {
      displayName: `QA School ${kind} ${marker}`,
      userName: `qa.school.${kind}.${marker}`,
      email: `qa.school.${kind}.${marker}@example.test`,
      roles,
    },
  });
  assert.equal(response.status, 201);
  return response.json();
}

async function createProfile(token, kind, userId, marker) {
  const code = `SCHOOL-${kind.toUpperCase()}-${marker}`;
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

// Helper: perform a request with a sign-in backoff that respects the rate limiter
async function signInWithBackoff(emailOrUserName, password, retries = 10) {
  for (let attempt = 0; attempt < retries; attempt += 1) {
    const response = await request('/api/v1/auth/sign-in', {
      method: 'POST',
      body: { emailOrUserName, password, clientType: 'mobile' },
    });
    if (response.status === 200) return response.json();
    if (response.status === 429) {
      await new Promise((resolve) => setTimeout(resolve, 7_000));
      continue;
    }
    throw new Error(`sign-in failed with ${response.status} on attempt ${attempt + 1}`);
  }
  throw new Error(`sign-in rate-limited after ${retries} retries`);
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

// Create school year
const yearStart = new Date(`${new Date().getFullYear() - 1}-09-01`);
const yearEnd = new Date(`${new Date().getFullYear()}-05-31`);
const schoolYearBody = {
  code: `SY-${marker}`,
  displayName: `Năm học QA ${marker}`,
  startDate: yearStart.toISOString().slice(0, 10),
  endDate: yearEnd.toISOString().slice(0, 10),
};
const createYearResponse = await request('/api/v1/admin/school-years', {
  method: 'POST',
  token: administrator.accessToken,
  body: schoolYearBody,
});
assert.equal(createYearResponse.status, 201, 'create school year should return 201');
const schoolYear = await createYearResponse.json();
assert.ok(schoolYear.id, 'school year must carry id');
assert.equal(schoolYear.code, schoolYearBody.code);

// Duplicate school year rejected
const duplicateYear = await request('/api/v1/admin/school-years', {
  method: 'POST',
  token: administrator.accessToken,
  body: schoolYearBody,
});
await problem(duplicateYear, 409, 'schoolYearAlreadyExists');

// Validation: end date before start date
const invalidRange = await request('/api/v1/admin/school-years', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    code: `INVALID-${marker}`,
    displayName: 'Năm học không hợp lệ',
    startDate: schoolYearBody.endDate,
    endDate: schoolYearBody.startDate,
  },
});
assert.equal(invalidRange.status, 400);

// Create subject
const createSubjectResponse = await request('/api/v1/admin/subjects', {
  method: 'POST',
  token: administrator.accessToken,
  body: { code: `MATH-${marker}`, displayName: 'Toán' },
});
assert.equal(createSubjectResponse.status, 201);
const subject = await createSubjectResponse.json();

// Create class
const createClassResponse = await request('/api/v1/admin/classes', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    code: `CL-${marker}`,
    displayName: `Lớp QA ${marker}`,
    gradeLevel: 10,
    schoolYearId: schoolYear.id,
    homeroomTeacherProfileId: teacherProfile.id,
  },
});
assert.equal(createClassResponse.status, 201, 'create class should return 201');
const classroom = await createClassResponse.json();
assert.equal(classroom.schoolYearCode, schoolYearBody.code);

// Duplicate class code rejected
const duplicateClass = await request('/api/v1/admin/classes', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    code: `CL-${marker}`,
    displayName: 'Lớp khác',
    gradeLevel: 10,
    schoolYearId: schoolYear.id,
  },
});
await problem(duplicateClass, 409, 'classAlreadyExists');

// Update class with valid rowVersion
const updateClassResponse = await request(`/api/v1/admin/classes/${classroom.id}`, {
  method: 'PATCH',
  token: administrator.accessToken,
  body: {
    displayName: `Lớp QA Cập nhật ${marker}`,
    gradeLevel: 11,
    homeroomTeacherProfileId: teacherProfile.id,
    isActive: true,
    rowVersion: classroom.rowVersion,
  },
});
assert.equal(updateClassResponse.status, 200);
const updatedClass = await updateClassResponse.json();
assert.equal(updatedClass.gradeLevel, 11);

// Update with stale rowVersion returns 409
const staleClassUpdate = await request(`/api/v1/admin/classes/${classroom.id}`, {
  method: 'PATCH',
  token: administrator.accessToken,
  body: {
    displayName: 'Stale update',
    gradeLevel: 12,
    homeroomTeacherProfileId: teacherProfile.id,
    isActive: true,
    rowVersion: classroom.rowVersion,
  },
});
await problem(staleClassUpdate, 409, 'concurrencyConflict');

// Create teacher assignment
const assignmentResponse = await request('/api/v1/admin/teacher-assignments', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    teacherProfileId: teacherProfile.id,
    classId: classroom.id,
    subjectId: subject.id,
    schoolYearId: schoolYear.id,
  },
});
assert.equal(assignmentResponse.status, 201);

// Duplicate assignment rejected
const duplicateAssignment = await request('/api/v1/admin/teacher-assignments', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    teacherProfileId: teacherProfile.id,
    classId: classroom.id,
    subjectId: subject.id,
    schoolYearId: schoolYear.id,
  },
});
await problem(duplicateAssignment, 409, 'teacherAssignmentAlreadyExists');

// Create student enrollment
const enrollmentResponse = await request('/api/v1/admin/student-enrollments', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    studentProfileId: studentProfile.id,
    classId: classroom.id,
    schoolYearId: schoolYear.id,
    enrolledOn: schoolYearBody.startDate,
  },
});
assert.equal(enrollmentResponse.status, 201);

// Duplicate enrollment rejected
const duplicateEnrollment = await request('/api/v1/admin/student-enrollments', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    studentProfileId: studentProfile.id,
    classId: classroom.id,
    schoolYearId: schoolYear.id,
    enrolledOn: schoolYearBody.startDate,
  },
});
await problem(duplicateEnrollment, 409, 'studentEnrollmentAlreadyExists');

// /me/classes for teacher returns the assigned class
const teacherSession = await signInWithBackoff(teacherUser.userName, teacherUser.temporaryPassword);
const teacherClasses = await request('/api/v1/me/classes', { token: teacherSession.accessToken });
assert.equal(teacherClasses.status, 200);
const teacherClassList = await teacherClasses.json();
assert.equal(teacherClassList.length, 1);
assert.equal(teacherClassList[0].classId, classroom.id);
assert.equal(teacherClassList[0].subjectCode, `MATH-${marker}`);
assert.equal(teacherClassList[0].isHomeroom, true);

// /me/classes for student returns the enrolled class
const studentSession = await signInWithBackoff(studentUser.userName, studentUser.temporaryPassword);
const studentClasses = await request('/api/v1/me/classes', { token: studentSession.accessToken });
assert.equal(studentClasses.status, 200);
const studentClassList = await studentClasses.json();
assert.equal(studentClassList.length, 1);
assert.equal(studentClassList[0].classId, classroom.id);

// /me/classes for parent returns the child enrolled class
const parentSession = await signInWithBackoff(parentUser.userName, parentUser.temporaryPassword);
const parentClasses = await request('/api/v1/me/classes', { token: parentSession.accessToken });
assert.equal(parentClasses.status, 200);
const parentClassList = await parentClasses.json();
assert.equal(parentClassList.length, 1);
assert.equal(parentClassList[0].classId, classroom.id);
assert.equal(parentClassList[0].studentProfileId, studentProfile.id);

// Non-admin tries to create a school year -> 403
const unauthorizedYear = await request('/api/v1/admin/school-years', {
  method: 'POST',
  token: teacherSession.accessToken,
  body: schoolYearBody,
});
assert.equal(unauthorizedYear.status, 403);

console.log('school-reference contract tests passed');
