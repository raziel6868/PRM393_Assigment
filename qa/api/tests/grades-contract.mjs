import assert from 'node:assert/strict';

// External QA contract for the GradeAdministrationService bug fixes
// committed in Unit 2. The test asserts three behaviors that were broken in
// the pre-fix code:
//   1. GetSemestersAsync returns deterministic, opaque semester keys of the
//      form "{schoolYearId:N}-{semesterNumber}" — not fresh Guid.NewGuid()
//      values that change between calls.
//   2. GetGradeSummaryAsync accepts that composite semesterKey and parses it
//      back into (schoolYearId, semesterNumber); an invalid value yields the
//      stable "invalidSemesterKey" problem.
//   3. GetGradeDetailAsync enforces the Parent-Student link: a parent whose
//      child owns the grade entry sees the grade (200); a parent whose child
//      does NOT own the entry sees "studentNotLinked" (403), not the
//      generic "gradeNotFound" (404) the buggy code returned.

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
      displayName: `QA Grades ${kind} ${marker}`,
      userName: `qa.grades.${kind}.${marker}`,
      email: `qa.grades.${kind}.${marker}@example.test`,
      roles,
    },
  });
  assert.equal(response.status, 201);
  return response.json();
}

async function createProfile(token, kind, userId, marker) {
  const code = `GRADES-${kind.toUpperCase()}-${marker}`;
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
  const newPassword = `Grades-${marker}-${tag}-Aa7!`;
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

async function problem(response, expectedStatus, expectedCode) {
  assert.equal(response.status, expectedStatus);
  assert.match(response.headers.get('content-type') ?? '', /^application\/problem\+json/i);
  const payload = await response.json();
  assert.equal(payload.code, expectedCode);
  return payload;
}

function isStableSemesterKey(value, schoolYearId, semesterNumber) {
  // Deterministic key format documented at
  // GradeAdministrationService.BuildSemesterKey.
  return value === `${schoolYearId.toString().replace(/-/g, '').toLowerCase()}-${semesterNumber}`;
}

const marker = Date.now();
const administrator = await signIn(administratorUserName, administratorPassword);

// Three accounts: teacher (to record grades), two parents (one linked, one
// not) of the same student. We keep two parents so the link-vs-no-link
// assertion in the parent detail fallback is meaningful.
const teacherUser = await provision(administrator.accessToken, marker, 'teacher', ['teacher']);
const studentUser = await provision(administrator.accessToken, marker, 'student', ['student']);
const linkedParentUser = await provision(administrator.accessToken, marker, 'parent', ['parent']);
const unrelatedParentUser = await provision(administrator.accessToken, `${marker}-u`, 'parent', ['parent']);

const teacherProfile = await createProfile(administrator.accessToken, 'teacher', teacherUser.userId, marker);
const studentProfile = await createProfile(administrator.accessToken, 'student', studentUser.userId, marker);
const linkedParentProfile = await createProfile(administrator.accessToken, 'parent', linkedParentUser.userId, marker);
const unrelatedParentProfile = await createProfile(administrator.accessToken, 'parent', unrelatedParentUser.userId, `${marker}-u`);
await linkParent(administrator.accessToken, linkedParentProfile.id, studentProfile.id);

const yearStart = new Date(`${new Date().getFullYear() - 1}-09-01`);
const yearEnd = new Date(`${new Date().getFullYear()}-05-31`);
// School-year / class / subject codes are capped at 20 characters
// (CreateSchoolYearRequest / CreateClassRequest / CreateSubjectRequest).
// Use short prefixes so the marker never blows the limit.
const yearCode = `G-SY-${marker}`;
const subjectCode = `G-MATH-${marker}`;
const classCode = `G-CL-${marker}`;
const teacherAssignmentCode = `G-TC-${marker}`;
const enrollmentCode = `G-SE-${marker}`;
const year = await (await request('/api/v1/admin/school-years', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    code: yearCode,
    displayName: `Năm học ${marker}`,
    startDate: yearStart.toISOString().slice(0, 10),
    endDate: yearEnd.toISOString().slice(0, 10),
  },
})).json();
const subject = await (await request('/api/v1/admin/subjects', {
  method: 'POST',
  token: administrator.accessToken,
  body: { code: subjectCode, displayName: 'Toán' },
})).json();
const classroom = await (await request('/api/v1/admin/classes', {
  method: 'POST',
  token: administrator.accessToken,
  body: {
    code: classCode,
    displayName: `Lớp ${marker}`,
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
// Touch the codes so linters don't flag them as unused; the prefix scheme is
// the part that matters for human-readable failure messages.
void teacherAssignmentCode;
void enrollmentCode;

const teacherPassword = await finalizePassword(teacherUser.userName, teacherUser.temporaryPassword, marker, 'T');
const studentPassword = await finalizePassword(studentUser.userName, studentUser.temporaryPassword, marker, 'S');
const linkedParentPassword = await finalizePassword(linkedParentUser.userName, linkedParentUser.temporaryPassword, marker, 'LP');
const unrelatedParentPassword = await finalizePassword(unrelatedParentUser.userName, unrelatedParentUser.temporaryPassword, marker, 'UP');

const teacherSession = await signIn(teacherUser.userName, teacherPassword);
const studentSession = await signIn(studentUser.userName, studentPassword);
const linkedParentSession = await signIn(linkedParentUser.userName, linkedParentPassword);
const unrelatedParentSession = await signIn(unrelatedParentUser.userName, unrelatedParentPassword);

// Teacher creates one assessment for semester 1 of the year.
const createAssessmentResponse = await request('/api/v1/teacher/assessments', {
  method: 'POST',
  token: teacherSession.accessToken,
  body: {
    code: `G-A1-${marker}`,
    displayName: 'Kiểm tra 15 phút',
    assessmentType: 'quiz',
    schoolYearId: year.id,
    semester: 1,
    classId: classroom.id,
    subjectId: subject.id,
    minScore: 0,
    maxScore: 10,
    weight: 1,
    dueDate: yearStart.toISOString().slice(0, 10),
    isPublished: true,
  },
});
assert.equal(createAssessmentResponse.status, 201);
const assessment = await createAssessmentResponse.json();

// Teacher saves a single grade entry for the student.
const saveEntriesResponse = await request(
  `/api/v1/teacher/assessments/${assessment.id}/grade-entries`,
  {
    method: 'POST',
    token: teacherSession.accessToken,
    body: {
      entries: [
        {
          studentProfileId: studentProfile.id,
          score: 8.5,
          teacherComment: 'Bài làm tốt, cần chú ý phép tính chia.',
          rowVersion: null,
        },
      ],
    },
  },
);
assert.equal(saveEntriesResponse.status, 200);
const savedRoster = await saveEntriesResponse.json();
assert.equal(savedRoster.students.length, 1);

// === Assertion 1: stable semester keys ===
const semestersResponse = await request('/api/v1/grades/semesters', {
  token: studentSession.accessToken,
});
assert.equal(semestersResponse.status, 200);
const semestersFirst = await semestersResponse.json();
assert.ok(Array.isArray(semestersFirst), 'semesters must be an array');
const semesterOne = semestersFirst.find((s) => s.semesterNumber === 1);
assert.ok(semesterOne, 'student must see semester 1 in their list');
assert.equal(
  isStableSemesterKey(semesterOne.id, year.id, 1),
  true,
  `semester id must be deterministic "{schoolYearId:N}-{semesterNumber}", got ${semesterOne.id}`,
);

// Call again and confirm the same key comes back (regression for Guid.NewGuid()).
const semestersSecond = await (await request('/api/v1/grades/semesters', {
  token: studentSession.accessToken,
})).json();
const semesterOneSecond = semestersSecond.find((s) => s.semesterNumber === 1);
assert.equal(semesterOneSecond.id, semesterOne.id, 'semester id must be stable between calls');

// === Assertion 2: summary accepts the semester key, rejects garbage ===
const summaryResponse = await request(
  `/api/v1/grades/summary/${semesterOne.id}`,
  { token: studentSession.accessToken },
);
assert.equal(summaryResponse.status, 200);
const summary = await summaryResponse.json();
assert.equal(summary.semesterId, semesterOne.id);
assert.equal(summary.semester, 1);
assert.equal(summary.schoolYearId, year.id);
assert.ok(Array.isArray(summary.subjects));
const subjectSummary = summary.subjects.find((s) => s.subjectId === subject.id);
assert.ok(subjectSummary, 'subject summary must include the recorded subject');
assert.equal(subjectSummary.gradeCount, 1);
assert.equal(subjectSummary.averageScore, 8.5);

const garbageResponse = await request('/api/v1/grades/summary/not-a-real-key', {
  token: studentSession.accessToken,
});
await problem(garbageResponse, 400, 'invalidSemesterKey');

// Parent of the linked student also gets the same deterministic key.
const parentSemesters = await (await request('/api/v1/grades/semesters', {
  token: linkedParentSession.accessToken,
})).json();
const parentSemesterOne = parentSemesters.find((s) => s.semesterNumber === 1);
assert.ok(parentSemesterOne, 'linked parent must see semester 1 for their child');
assert.equal(parentSemesterOne.id, semesterOne.id, 'parent and student must agree on the same semester key');

// === Assertion 3: parent detail enforces the link ===
// The teacher's saved roster exposes the GradeEntryId through the new
// GradeEntryItem shape, so we can drive the detail endpoint without relying
// on the legacy /api/v1/students/me/grades list.
const teacherRosterResponse = await request(
  `/api/v1/teacher/classes/${classroom.id}/assessments?subjectId=${subject.id}&schoolYearId=${year.id}&semester=1`,
  { token: teacherSession.accessToken },
);
assert.equal(teacherRosterResponse.status, 200);
const teacherRosters = await teacherRosterResponse.json();
const teacherRoster = teacherRosters.find((r) => r.id === assessment.id);
assert.ok(teacherRoster, 'teacher must see the assessment in their roster');
const teacherRosterStudent = teacherRoster.students.find((s) => s.studentProfileId === studentProfile.id);
assert.ok(teacherRosterStudent, 'teacher roster must list the student we graded');
const gradeEntryIdReal = teacherRosterStudent.gradeEntryId;
assert.ok(gradeEntryIdReal, 'teacher roster must expose gradeEntryId');

// Linked parent: 200 with the recorded score.
const linkedDetail = await request(`/api/v1/grades/${gradeEntryIdReal}`, {
  token: linkedParentSession.accessToken,
});
assert.equal(linkedDetail.status, 200, 'linked parent must see the grade detail');
const linkedBody = await linkedDetail.json();
assert.equal(linkedBody.gradeId, gradeEntryIdReal);
assert.equal(linkedBody.score, 8.5);

// Unrelated parent: 403 studentNotLinked (NOT 404 gradeNotFound).
const unrelatedDetail = await request(`/api/v1/grades/${gradeEntryIdReal}`, {
  token: unrelatedParentSession.accessToken,
});
await problem(unrelatedDetail, 403, 'studentNotLinked');

// Student: 200.
const studentDetail = await request(`/api/v1/grades/${gradeEntryIdReal}`, {
  token: studentSession.accessToken,
});
assert.equal(studentDetail.status, 200, 'own student must see the grade detail');

// Teacher: 200 (authorized via class assignment).
const teacherDetail = await request(`/api/v1/grades/${gradeEntryIdReal}`, {
  token: teacherSession.accessToken,
});
assert.equal(teacherDetail.status, 200, 'assigned teacher must see the grade detail');

// Unauthenticated: 401.
const anonDetail = await request(`/api/v1/grades/${gradeEntryIdReal}`);
assert.equal(anonDetail.status, 401, 'unauthenticated request must be rejected');
