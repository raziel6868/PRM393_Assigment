# AI Agent Instructions for Project: myfschoolse1910

## 0. Quick Operating Contract — Read First

This section is the execution index for every task. It summarizes the detailed sections; it does not replace them. Do not begin implementation after reading only a mockup or one feature section.

### 0.1. Requirement Precedence

When instructions appear to conflict, apply this order:

1. The user's latest explicit request for the current task.
2. Security, authorization, privacy, data-integrity, and destructive-action invariants in this file.
3. Required scope and priority rules in Section 3.
4. Functional/domain/API requirements in Sections 4-12.
5. Approved UX rules and repository mockups in Section 7 and `templates/`.
6. Roadmap order, examples, sample values, and implementation preferences.

Never let a screenshot, sample name/count/date, inferred behavior, or lower-priority convenience override authorization or a business rule. If two higher-priority requirements remain materially incompatible, stop and ask one precise question instead of choosing silently.

### 0.2. Fixed Decisions

- Product code lives only in `backend/` (.NET 10), `frontend-web/` (React + TypeScript + Vite), and `myfschoolse1910/` (Flutter/Dart). Templates live in `templates/`; disposable verification lives in `qa/`.
- The initial product is a single-school deployment, not a multi-tenant SaaS. `school-wide` means the configured school. Do not add tenant switching, tenant IDs on every API, or cross-school administration unless the user explicitly expands scope.
- The education actors are Teacher, Parent/Guardian, and Student. Administrator is a back-office authorization role.
- Accounts are school-provisioned; there is no public registration or trusted pre-login role selection.
- All visible product content is Vietnamese. Code identifiers/comments and technical documentation are English.
- P0 is the assignment-critical, demo-complete baseline and is Required. P1 contains valuable product extensions to implement only after P0 is green; it is not a prerequisite for the assignment baseline. P2/Deferred work is outside the planned initial delivery unless explicitly promoted.
- API authorization is authoritative. UI hiding, route guards, or possession of an ID never grants resource access.
- Web and Flutter never connect to each other or to SQL Server directly. Both consume the ASP.NET Core API; only Backend infrastructure accesses SQL Server, SMTP, and attachment storage.
- Local development has one manually maintained repository-root `.env`. It is an input to launch/build tooling, not a file to copy wholesale into client bundles. Only explicitly allowlisted public values reach React or Flutter; Backend-only secrets remain server-side.
- The target is a local Windows development/demo environment using locally installed SQL Server and ordinary host processes. Remote deployment and infrastructure-orchestration work are outside scope unless the user explicitly changes this decision.
- No model can promise zero defects. A task may be declared complete only from reproducible evidence satisfying Section 16; never claim correctness from inspection alone.

### 0.3. Mandatory Slice Contract

Before editing, select the verification tier in Section 14.4.3 and write a slice contract in the working update/plan. A `Small` slice records only Goal, In/Out of scope, affected products, acceptance/consistency checks, baseline ownership, and commands. A `Standard` or `Critical` slice completes the full template below; use `Not applicable` with a reason instead of inventing work for an unaffected product:

```text
Goal:
In scope:
Out of scope:
Roles and resource ownership:
Products: Backend / Web / Flutter = Touched | Affected consumer | Not affected (reason)
Data/API/migration impact:
Required UI states: loading | loaded | empty | validation | forbidden | offline/retry | session expired
Acceptance checks: numbered Given/When/Then outcomes including persistence and at least one forbidden/invalid path
Test-first evidence: smallest RED scenario, expected behavioral failure, or a recorded allowed exception
QA scenarios and phase gate:
Verification tier: Small | Standard | Critical, with reason from Section 14.4.3
Review plan: specification-compliance pass, then code-quality/security pass; independent reviewer or recorded self-review fallback
Baseline: branch/HEAD when available, pre-existing modified/untracked files, active QA run/checkpoint
Commit plan: feature branch plus intended atomic commits in dependency/review order
```

Then execute one vertical slice in this order:

1. Inspect existing code, repository status, relevant sections, and the exact mockup file.
2. Reuse compatible work and record assumptions; do not overwrite unrelated user changes.
3. Define or update domain rules and the public API contract before client wiring when server behavior changes.
4. For a non-trivial slice, write and challenge the exact-file implementation plan in Section 14.7.1.
5. Establish the smallest valid RED acceptance scenario in Section 14.7.2 before new production behavior, or record an allowed exception, then implement Backend persistence/authorization/API, actual typed consumers, and requested UI states one acceptance unit at a time.
6. Run the tier-appropriate autonomous loop and review in Section 14.7 plus the change-impact gate in Section 14.4.2. Create atomic commits only when Section 2.3 grants commit authorization.
7. Compare implemented UI with the approved mockup when one exists, document justified deviations, and complete Section 16.

Do not create broad scaffolding, unrelated abstractions, or later-phase features merely because they might be useful. A bounded UI-only task does not authorize an API redesign; an API-contract task does require affected client updates.

### 0.4. Prohibited Shortcuts

- No dead navigation, fake success, hardcoded metrics, permanently loading skeletons, placeholder business actions, or enabled controls without working behavior.
- No secrets, real personal data, machine-specific URLs, reusable QA credentials, plaintext passwords, or tokens in source/logs/artifacts.
- No client-side-only authorization, test bypass, hidden reset endpoint, or direct trust in role/audience/ownership IDs supplied by a client.
- No direct EF entity serialization, silent validation fallback, fabricated zero grade, implicit attendance status, swallowed exception, or catch-all success response.
- No weakening assertions, adding arbitrary sleeps, skipping a failing journey, or repeatedly retrying until green.
- No test files/dependencies inside product directories and no deletion of `qa/` except during explicitly requested release cleanup.
- No destructive cleanup against unresolved paths, broad globs, a workspace root, unrelated local processes/services, databases, or temporary directories.
- No switching the required frameworks, state management, component system, database, or architecture boundary without an explicit decision.
- No editing this requirements file, changing a mockup, weakening acceptance criteria, relabeling Required work as Deferred, or changing expected QA results merely to make an implementation pass. Requirement changes require the user's explicit instruction.
- No `git reset --hard`, `git clean`, destructive checkout, amend, rebase, squash, history rewrite, force-push, merge, or remote push. Local feature-branch creation, explicit-path staging, and local atomic commits are authorized only by Section 2.3 after the user starts a coding task; all other Git mutations require explicit instruction.
- No rerunning an unchanged failing command more than once after reproduction. Before another run, record a new hypothesis and make a relevant code/config/environment change, or classify and report the blocker.

### 0.5. Requirement Navigation Map

- Scope, mandatory flows, and optional boundaries: Section 3.
- Roles, ownership, authentication, and school-assisted account access: Sections 4-5.
- Teacher/Parent/Student/Web behavior: Section 6.
- Brand, mockups, screens, and interaction states: Section 7.
- Excel import and Email/Portal App delivery: Sections 8-9.
- Entities, relationships, lifecycle rules, attachment persistence, and concurrency: Section 10.
- Flutter/.NET/React architecture, root configuration, repository hygiene, and atomic commits: Sections 2.1-2.3 and 11.
- Wire formats, errors, pagination, IDs, enums, and time: Section 12.
- Security/accessibility baseline: Section 13.
- Disposable QA, cross-client E2E, autonomous loop, and release cleanup: Section 14.
- Phase order/gates and completion evidence: Sections 15-16.

Section 3.1.2 is canonical for priority; Sections 1.1 and 4.7 are canonical for actor-client ownership; Sections 3.1.1 and 6.5 own Mobile and Web route scope. Detailed functional, UX, domain, QA, or roadmap text specifies the depth of an already active capability and cannot silently promote it. On the first task in a fresh context and on any requirements/roadmap edit, read this file completely. On a later bounded implementation task, always reload Sections 0, 1.1, 3, 4.7, and 16 plus every task-specific section identified by this map; do not spend the working context re-reading unrelated feature detail unless a cross-reference or conflict requires it.

## 1. Product Definition

`myfschoolse1910` is a school communication and learning-management system inspired by eNetViet. It is not intended to copy eNetViet feature-for-feature. The product serves three primary education actors:

1. **Teacher**
2. **Student**
3. **Parent/Guardian**

`Administrator` is a back-office authorization role used to operate the system. It is not a fourth education actor. Administrator access exists because account provisioning, bulk data import, permissions, and school-wide email cannot safely belong to an ordinary Teacher, Student, or Parent account.

The target solution and fixed product directories are:

- `myfschoolse1910/`: a Flutter application (Dart 3+) for Teachers, Students, and Parents.
- `frontend-web/`: a React + TypeScript + Vite portal for back-office administration and selected Teacher workflows.
- `backend/`: an ASP.NET Core Web API targeting .NET 10 (`net10.0`) and shared by both clients.
- A SQL Server database.
- A shared Orange-first visual identity using the official FPT logo asset.

This is a solo PRM393 university assignment. Android is the primary mobile demo target, while the architecture should remain portable to iOS/Web where Flutter supports it. The initial goal is a coherent, demonstrable school system with a real end-to-end path, not a production-scale clone containing every possible school feature.

### 1.1. Product and Platform Strategy

MyFSchool uses a client-by-actor strategy:

- **Flutter Android is the primary daily-use client** for Teacher, Parent/Guardian, and Student. Android is the required Mobile build/demo target in P0/P1.
- **React Web is a desktop-first, responsive portal** for Administrator back-office work and a deliberately limited set of authorized Teacher desktop workflows.
- Administrator has no required Flutter experience in P0/P1. Parent and Student have no React Web access in P0/P1.
- Teacher Mobile owns attendance, leave review, timetable, announcement inbox/view, and promoted P1 daily-comment/homework/grade-entry/class-feed workflows.
- Teacher Web owns P0 announcement/email composition and a minimal scoped landing; its read-focused dashboard, teaching-schedule detail, and delivery history are promoted P1 capabilities. It does not duplicate attendance, leave decisions, daily comments, homework, grading, or grade entry unless the user explicitly promotes a named Web workflow.
- Responsive React layout preserves the authorized Administrator/Teacher workflows at narrower widths; it does not grant Parent or Student Web access.
- Flutter should remain portable where practical, but Flutter Web is not a P0/P1 product artifact. Do not build, test, deploy, route users to, or maintain a second Web frontend with Flutter unless the user explicitly promotes Flutter Web later.
- Playwright `web-first assertions` are a synchronization/testing technique. They do not mean the product is Web-first and do not alter actor-client ownership.

When a valid Parent-only or Student-only account attempts React portal access, do not establish portal access or show a partial dashboard. Clear/revoke the attempted Web session through the normal logout/session path and show a generic Vietnamese message directing the user to the Mobile application without revealing additional account information.

Apply the symmetric rule to an Administrator-only account in Flutter: do not render back-office capability or a generic education dashboard. A multi-role account may enter a client only through one of that client's supported roles, and its selected context never inherits permissions from an unsupported role.

## 2. Source of Truth and Working Rules

- This file defines project scope and implementation constraints. If a later user request conflicts with this file, the later explicit user request takes precedence.
- Work one requested phase or bounded feature at a time. Do not generate the entire system without an explicit request.
- Before changing code, inspect the existing repository and preserve compatible work already present.
- Do not implement a later phase merely because it appears in the roadmap.
- When a requirement is ambiguous, state the smallest reasonable assumption. Ask the user only when different choices would materially change the product or data model.
- Keep domain and business rules independent from UI frameworks and infrastructure.
- Apply SOLID and clean-code principles pragmatically; avoid unnecessary abstractions and placeholder layers.
- All source-code identifiers, comments, commit messages, and technical documentation must be in English.
- All end-user UI labels, validation messages, email content, and seeded demo content must be in Vietnamese.
- Never commit secrets, SMTP credentials, signing keys, connection strings, or real personal data. Use configuration, environment variables, user secrets, and sanitized sample data.
- Add or update business-critical verification in the disposable root-level QA harness defined in Section 14. Keep test code and test-only dependencies out of the three product directories. Run relevant formatting, static analysis, production builds, and QA gates before declaring a coding task complete.

### 2.1. Repository-Root Configuration Contract

Local development uses exactly one manually maintained configuration file at repository root: `.env`. Commit a sanitized root `.env.example`, and ignore `.env`, `.env.local`, `.env.*.local`, generated QA environment files, and any other real-secret variant. Do not create competing `backend/.env`, `frontend-web/.env`, or `myfschoolse1910/.env` files.

The root `.env` is a source for process/build configuration, not a universal runtime file embedded into every artifact:

- **Backend:** repository PowerShell launch scripts load `.env` into the API process. ASP.NET Core reads normal environment variables through `IConfiguration`; hierarchical keys use portable double underscores, for example `ConnectionStrings__Default`. Do not add a second dotenv parser merely to read the file from application code.
- **React/Vite:** configure Vite's `envDir` to resolve the repository root. Client code may read only explicitly declared `VITE_*` values through a typed configuration module. Every `VITE_*` value is public and build-time embedded; never place a credential, connection string, SMTP setting, signing key, or storage path behind that prefix. Do not spread all `loadEnv` values into `define` or change `envPrefix` to expose everything.
- **Flutter:** repository launch/build scripts parse the root `.env` and pass only an allowlist such as `APP_ENV` and `FLUTTER_API_BASE_URL` through `--dart-define`. Never pass the entire root file through `--dart-define-from-file`, bundle `.env` as an asset, or add a Flutter dotenv package, because Backend secrets must not enter the APK. Read allowlisted values from one typed app-configuration class using `String.fromEnvironment` and fail the build/startup clearly when required values are absent.
- **QA:** root `.env` remains the only manually maintained local environment file. QA scripts may combine its safe inputs with generated run IDs, ports, and one-run secrets into ignored `qa/.env.e2e.generated`; they must delete that file during teardown. A process-level or CI secret overrides the root value without modifying tracked files.
- **Release/demo:** the ignored root `.env` supplies the trusted local machine only. Never bundle or copy it into Web assets, the APK, source control, screenshots, logs, or release artifacts. Remote deployment configuration is outside current scope.

The sanitized `.env.example` must document at least these names with non-secret placeholders and comments identifying public versus Backend-only values:

```dotenv
# Shared/public build inputs
APP_ENV=development
VITE_API_BASE_URL=http://localhost:5080
FLUTTER_API_BASE_URL=http://10.0.2.2:5080/api/v1

# Backend-only runtime configuration
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://0.0.0.0:5080
ConnectionStrings__Default=Server=localhost;Database=MyFSchool;User Id=CHANGE_ME;Password=CHANGE_ME;TrustServerCertificate=True
Auth__JwtSigningKey=CHANGE_ME_TO_A_LONG_RANDOM_SECRET
# Enable only for the P0E communication slice/demo gate
Smtp__Enabled=false
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__UserName=CHANGE_ME_TEST_SENDER@gmail.com
Smtp__Password=CHANGE_ME_GOOGLE_APP_PASSWORD
Smtp__FromEmail=CHANGE_ME_TEST_SENDER@gmail.com
Smtp__FromName=MyFSchool
Smtp__Security=startTls
# Enable only when the active slice accepts stored attachments
Storage__Enabled=false
Storage__Provider=Local
Storage__LocalRoot=CHANGE_ME_TO_AN_ABSOLUTE_WRITABLE_PATH_OUTSIDE_THE_REPOSITORY
QA_SQLSERVER_ADMIN_CONNECTION=CHANGE_ME_TO_A_LOCAL_SQL_SERVER_ADMIN_CONNECTION
QA_GMAIL_RECIPIENT=CHANGE_ME_TO_A_DEDICATED_TEST_GMAIL_ADDRESS
```

`VITE_API_BASE_URL` and `FLUTTER_API_BASE_URL` intentionally may differ because a browser on the host and an Android emulator reach the same API through different hostnames. They must still point to the same Backend deployment and `/api/v1` contract. Validate Backend options with typed options plus startup validation, validate Vite values before rendering, and validate Flutter defines before constructing the API client. Missing/invalid required configuration must fail fast with the setting name but never its secret value.

### 2.2. Repository Hygiene and Change Ownership

Before every coding slice, capture a read-only baseline with `git status --short`, `git diff --name-only`, and `git diff --stat` when Git is available. Treat every pre-existing modified or untracked file as user-owned unless the current task explicitly places it in scope.

- Do not discard, overwrite, reformat, rename, or “clean up” unrelated user changes. If an in-scope file already has changes, inspect the exact diff and make the smallest compatible patch; if the overlap makes intent unsafe to infer, stop with one precise blocker.
- Use patch-based edits for hand-written files. Formatting tools may perform bounded mechanical rewrites only inside the current slice; inspect their diff immediately and revert no user content.
- Never use a successful command from an older commit/build/run as current evidence. After any relevant source, dependency, configuration, migration, or fixture change, invalidate the affected artifact result and rebuild/re-run the required gate.
- Generated application outputs, packages, logs, screenshots, and QA artifacts must stay ignored and outside source ownership. Do not hand-edit generated build output as a fix.
- Before declaring completion, compare final `git status`/diff to the baseline and classify every changed path as requested production work, disposable QA work, generated/ignored output, or pre-existing user work. Unexpected paths make the task incomplete until explained or safely corrected.
- Stage and commit only under Section 2.3. A globally clean working tree is not required because unrelated user work may remain; no in-scope completed change may be left as an unexplained uncommitted blob.

### 2.3. Feature Branch and Atomic Commit Contract

Once the user explicitly starts a coding phase/feature and requests autonomous looping, that request authorizes local commits for that bounded slice under this section. It does not authorize push, pull, merge, rebase, squash, amend, tag, PR creation, or modification of remote branches.

#### 2.3.1. Branch Boundary

- Never commit implementation directly to `main` or `master`. If the current branch is one of them, create one short local branch before the first production edit. Use a lowercase kebab-case work name with no agent/user prefix and no `/`, normally one to three meaningful words: `auth`, `backend-core`, `leave-request`, `password-help`, `excel-import`, or `grades`. Prefer the capability name over generic names such as `feature-1`, `phase-2`, `work`, or an agent identity. If the user already placed the work on another non-protected feature branch, stay on it rather than creating/switching again.
- Before branch creation, record branch/HEAD/status and ensure the operation will not overwrite or hide user work. Existing modified/untracked files remain user-owned and must not be swept into the first commit.
- Use one branch for one user-requested bounded feature or phase. Do not mix roadmap phases or opportunistic refactors into it. A later explicit feature receives another branch after the current handoff unless the user asks to continue on the same branch.

#### 2.3.2. Commit Boundaries

One commit must answer one clear review question and leave its changed product buildable. Commit after a coherent acceptance unit reaches the required narrow green gate, before beginning the next unrelated unit. Do not commit every edit/attempt, and do not wait until several features form one giant commit.

Use this dependency order only for layers actually affected:

Foundational requirements/design assets such as `AGENTS.md` and `templates/` belong in one reviewed baseline commit such as `docs: add implementation contract and design references` before feature branches begin. Never hide previously untracked user-owned baseline files inside the first feature commit; stage them only when the user explicitly puts them in scope.

1. `chore(backend)`, `chore(web)`, or `chore(mobile)` — one-time minimal scaffold for one product, only when the requested phase requires it and that scaffold restores/builds; do not combine all product scaffolds or add placeholder feature screens/fake business behavior.
2. `feat(backend)` — one domain/API acceptance unit. Keep its entity configuration, migration, DTO/OpenAPI, authorization, validation, and Application/Infrastructure implementation in the same commit when they are inseparable. Never put a migration in a later unrelated commit.
3. `feat(web)` — the React typed client and complete Web behavior/UI states for that same accepted contract.
4. `feat(mobile)` — the Flutter model/repository/state/navigation/UI states for that same accepted contract.
5. `test(qa)` — the detachable external API/Web/Maestro scenario and synthetic fixture for that acceptance unit, committed only after it passes against the current artifacts. QA dependencies stay under `qa/`.
6. `docs` or `chore(config)` — documentation or safe example/launcher configuration only when it is independently reviewable; required config tightly coupled to a product change may remain with its owning product commit.

This is a template, not permission to create empty/no-op commits. A backend-only feature uses no Web/Mobile commit. A small cross-client change may use fewer commits when splitting would create broken or meaningless intermediate states; record the reason in the checkpoint. Conversely, two unrelated endpoints/screens never share a commit merely because they were implemented in the same loop.

If later verification finds a defect in an already committed unit, add a focused `fix(backend)`, `fix(web)`, `fix(mobile)`, or `fix(qa)` commit after the fix passes its affected gate. Do not amend/rewrite earlier commits to hide the diagnostic history. Cleanup/refactoring belongs in a separate commit only when required by the feature and behavior-preserving; unrelated cleanup is out of scope.

#### 2.3.3. Commit Readiness and Staging Safety

Before each commit, all conditions below are mandatory:

1. The commit maps to named acceptance check(s), contains no known placeholder/fake success, and the changed product's relevant format/analyzer/lint/build plus narrow positive and invalid/forbidden check pass. Cross-client completion may remain pending until the layer series is complete.
2. Inspect `git status --short`, the unstaged diff, and baseline ownership. Stage only explicit intended paths with `git add -- <path1> <path2>...`; never use `git add .`, `git add -A`, a repository-wide glob, or stage generated artifacts.
3. If an intended file mixes pre-existing user edits with agent edits, do not stage the whole file blindly. Stop and report the overlap unless the user explicitly authorizes a reviewed partial-stage method. Never use checkout/reset to manufacture a clean file.
4. Inspect `git diff --cached --check`, `git diff --cached --stat`, and the complete `git diff --cached`. Confirm no `.env`, credential, real personal data, temporary password, QA artifact, build output, editor file, unrelated formatting, or out-of-scope path is staged. If wrong, unstage only the exact path with `git restore --staged -- <path>` and preserve its working-tree content.
5. Commit hooks must pass. Never use `--no-verify`. After commit, record hash/subject/gates in the checkpoint, inspect `git show --stat --oneline HEAD`, and confirm remaining status contains only planned next-unit work or preserved user work.

Never commit a red build, failing required check, partially wired navigation, disabled assertion, WIP marker, merge conflict, or an implementation known to violate this file. A checkpoint can preserve incomplete work without turning it into a bad commit.

#### 2.3.4. Commit Message and Review Handoff

Use English Conventional Commit subjects in imperative form, normally at most 72 characters:

```text
feat(backend): add parent leave request submission
feat(web): add password help administration
feat(mobile): submit leave request evidence
test(qa): cover leave request cross-client flow
fix(backend): enforce assigned-class leave review
docs: clarify local Gmail configuration
```

Use scopes from `backend`, `web`, `mobile`, `qa`, `config`, `release`, or a concise feature name consistently. The optional body explains `Why`, observable `Behavior`, and `Verified` commands/gates; do not narrate file-by-file implementation or include secrets. Breaking API changes use `!` plus a `BREAKING CHANGE:` footer and require all affected clients in the same bounded branch before handoff.

At handoff, show commits in review order using the baseline-to-HEAD range (for example `git log --reverse --format="%h %s" <baseline>..HEAD`): short hash, subject, acceptance unit, products changed, and fresh gates. Also list any intentional in-scope uncommitted work; a completed slice has none. Local commits remain unpushed so the user can review them. During explicit release preparation, removal of the disposable QA harness is one dedicated final commit such as `chore(release): remove disposable QA harness` after the full suite and cleanup checks pass.

## 3. Scope and Priorities

### 3.1. Assignment Demo Baseline Scope

The Required P0 assignment baseline must demonstrate these end-to-end workflows:

1. An Administrator provisions users and school data through the web portal, including an Excel import with row-level validation.
2. A user signs in with a school-provisioned account. If access is lost, the user submits a generic assistance request; an Administrator verifies identity, issues a unique temporary password, and the user must change it before accessing application data.
3. A Teacher manages assigned-class attendance, reviews Parent leave requests, reads the teaching schedule, and sends scoped announcements.
4. A Parent views linked children, attendance, grades, timetable/events, announcements, and submits/tracks a leave request.
5. A Student views their own grades by semester, grade detail, timetable, school events, leave-request status, clubs, attendance summary, and class announcements.
6. An authorized Administrator or Teacher sends an email/announcement to a permitted audience, with delivery status recorded.
7. An authenticated user can reach the assignment-required mobile destinations from Home without dead links: Grades, Timetable, Events, Leave Requests, and Clubs.

#### 3.1.1. Assignment-Required Mobile Screen Flow

The Teacher-provided assignment flow is mandatory within P0, not a post-baseline enhancement:

```text
Login -> Home
Login -> Quên mật khẩu -> Gửi yêu cầu hỗ trợ tới nhà trường
Mật khẩu tạm do nhà trường cấp -> Đăng nhập hạn chế -> Bắt buộc đổi mật khẩu -> Home
Home -> Điểm theo học kỳ -> Chi tiết điểm
Home -> Lịch học
Home -> Sự kiện -> Chi tiết sự kiện
Home -> Đơn từ -> Tạo đơn / Chi tiết đơn
Home -> Câu lạc bộ -> Chi tiết CLB / Gửi yêu cầu tham gia
```

Public `Đăng ký` is explicitly excluded. Accounts are issued/imported by the school. All protected routes require an authenticated, authorized session.

Canonical P0 Mobile route names are `/login`, `/password-help`, `/change-temporary-password`, `/change-password`, `/home`, `/attendance`, `/grades`, `/grades/:gradeId`, `/schedule`, `/events`, `/events/:eventId`, `/leave-requests`, `/leave-requests/new`, `/leave-requests/:requestId`, `/clubs`, `/clubs/:clubId`, `/announcements`, `/announcements/:announcementId`, and `/profile`. Required P0 Teacher routes are `/teacher/attendance`, `/teacher/leave-requests`, and `/teacher/leave-requests/:requestId`. P1 routes are added only when their slice is promoted: `/daily-comments`, `/homework`, `/homework/:homeworkId`, `/homework/:homeworkId/submit`, `/feed`, `/teacher/daily-comments`, `/teacher/homework`, `/teacher/homework/new`, `/teacher/homework/:homeworkId`, `/teacher/homework/:homeworkId/submissions`, `/teacher/homework/:homeworkId/submissions/:submissionId`, `/teacher/grade-entry`, and `/teacher/feed/new`. Define routes once in `go_router`; nested review/filter/form states do not need a separate route unless they must support restoration/deep links. Do not rename paths, duplicate literals across widgets, or create parallel routes without an explicit requirement.

Required implementation depth:

- **Grades:** read-only semester list and grade detail for Student/Parent; Teacher entry remains a separate workflow.
- **Timetable:** read-only day/week schedule for Student/Parent/Teacher context; timetable administration/editor is not required in this slice.
- **Events:** read-only list and detail are required. Registration, capacity, ticketing, or approval workflows are enhancements unless explicitly requested.
- **Leave Requests:** full Parent submission/history/detail and Pending cancellation, plus Teacher review/decision and Student read-only status.
- **Clubs:** browse/search/filter, detail, membership state, and idempotent join/request-to-join are required. Club creation and moderation are enhancements.

P0 read-only grades, timetable, events, and club catalogs may be supplied by the environment-gated synthetic Development/QA bootstrap or another explicitly authorized data-loading path. P0 does not require separate Administrator editors for those catalogs merely to feed the Mobile demo. The bootstrap remains non-HTTP, idempotent, synthetic-only, and unavailable outside Development/QA; a real authoring/import workflow is added only when explicitly scoped.

#### 3.1.2. Delivery Priority and Completion Milestones

- **P0A — Authentication and account access:** shared sign-in, role/client access, session restoration, school-assisted password help, Administrator temporary-password issuance in React Web, forced password change, and logout.
- **P0B — School directory and required Web operations:** Administrator user/reference-data management sufficient for the demo, one fixed versioned Excel workbook with validation preview/transactional commit, and imported account relationships.
- **P0C — Assignment-required Mobile information flow:** role-aware Home, Student/Parent grades, timetable, events, clubs, announcements, and working navigation between them.
- **P0D — Core cross-role school workflow:** resource scoping, leave-request list/create/detail/status, Parent leave submission/cancellation, Teacher leave review, Teacher attendance, Parent/Student attendance visibility, and documented reconciliation.
- **P0E — Required communication:** authorized Web announcement/email composition, server audience preview, explicit confirmation, immediate Portal App/Gmail delivery, basic per-recipient result, and Mobile announcement inbox.
- **P1 — Product extensions after a green P0:** Teacher daily comments, basic homework assignment/submission/grading, Teacher grade-entry editor, text-first class feed, announcement read state, import/delivery history UI, and a scoped read-focused Teacher Web dashboard. A P1 slice becomes required only when the user explicitly starts/promotes it.
- **P2 — Deferred polish and breadth:** dynamic Excel column mapping, scheduled email, draft autosave, email attachments, rich feed media/interactions, advanced analytics/filters/reports, editable timetable administration, event registration, club administration, advanced conflict-resolution UX, Flutter Web, and non-essential animation.

`Assignment Demo Complete` means every P0 acceptance journey and P0 gate passes. `Extended Product Complete` additionally means every explicitly promoted P1 slice passes its own gate. Do not call unimplemented P1 behavior part of a completed P0 baseline, expose it as active navigation, or scaffold it speculatively. P1/P2 work never blocks an otherwise green P0 release/demo unless the user explicitly reprioritizes it into P0.

#### 3.1.3. Scope Labels

Agents must use these labels consistently:

- **Required/P0:** must be implemented for the assignment demo baseline.
- **Promoted/P1:** a valuable extension implemented only after P0 or when the user explicitly starts it; once promoted into the current slice, its acceptance checks are mandatory.
- **Conditional:** required only when a stated role, school policy, record state, or backend capability applies. For example, leave evidence can be required by policy even though it is not required for every request.
- **Optional UI content:** a decorative or contextual element that may be omitted without removing a business capability, such as a promotional banner or dashboard illustration.
- **Deferred/P2:** intentionally outside the initial required delivery. Do not expose it as a working navigation item until it is implemented end-to-end.
- A nullable field or optional attachment is a data rule, not an optional feature. Do not confuse the word `optional` in a field description with permission to omit the surrounding workflow.

### 3.2. P0 Core Demo Journeys

At minimum, these journeys must be easy to demonstrate:

1. **Parent checks attendance:** sign in, select a child if necessary, see today's status immediately, then open recent history.
2. **Parent requests leave:** choose the child, date range/session, reason, and note; submit; see the new request as Pending.
3. **Teacher records attendance:** select an assigned class/date/session, mark the roster, review unmarked students, and save.
4. **Teacher reviews leave:** open the oldest pending request, view student/context details, approve or reject it; rejection requires a reason.
5. **Administrator imports roster data:** download template, upload, preview row errors, confirm a valid batch, and inspect the result.
6. **Authorized user sends an announcement/email:** select an allowed audience, preview, confirm, and inspect delivery status.
7. **Student checks school information:** move from Home to semester grades/detail, today's timetable, and an upcoming event, then return without losing main-tab state.
8. **Student explores clubs:** search/filter clubs, open a detail, submit a join request once, and see the updated membership state.
9. **School assists account access:** a locked-out user submits a generic help request; an Administrator finds the pending request, verifies identity outside the application, issues one unique temporary password, communicates it through the school's trusted process, and the user cannot reach Home until replacing it with a new password.

When a P1 slice is promoted, add its end-to-end journey to the active milestone gate. Daily-comment, homework/submission/grading, grade-entry, class-feed, and Teacher Web dashboard journeys are not P0 completion conditions.

### 3.3. Deferred / P2 Scope

The following capabilities are not required for the P0 assignment baseline and must not delay P0 or an already promoted P1 slice unless the user explicitly promotes them:

- Real-time chat.
- SMS and mobile push delivery.
- Tuition billing and payment-gateway integration.
- Rich Office-document annotation, advanced submission viewers, and plagiarism checking. Basic homework submission and grading belong only to the promoted P1 homework slice.
- Advanced analytics and report generation.
- Video upload/transcoding.
- Conduct, competency, and complex academic-report templates.
- Timetable creation/administration, complex recurring schedules, and substitution management.
- Event registration, capacity/waitlist/ticketing, and event check-in.
- Club creation, approval, leadership, moderation, and activity management.
- Reactions/comments with full moderation, edit history, and real-time feed updates. P0 announcements remain required; basic text-first Teacher class-feed composition belongs only to the promoted P1 feed slice.
- Direct self-service editing of sensitive Student identity fields. A controlled change-request or Administrator update may be added later.
- Suggestion-library CRUD and positive-recognition sticker management for daily comments.
- Shortcut personalization, promotional content, and other non-essential dashboard customization.

Do not expose Deferred/P2 features as active navigation or fake working actions. A clearly labeled non-interactive roadmap preview is acceptable only when the user explicitly asks for it.

### 3.4. Explicit Non-Goals for the Initial Version

- No public user registration. Accounts are provisioned by the school or created through an approved import.
- No self-service reset link/code by email, social login, Google sign-in, SSO, Microsoft 365 sign-in, or external identity provider.
- No plaintext password storage, shared/derivable school-wide default password, disclosure of an existing password, or emailing any password. A generated temporary password may be shown once to an authorized Administrator solely for assisted recovery and must be changed by the user at the next sign-in.
- No client-side-only authorization. The API is authoritative for every permission check.
- No claim of feature parity with eNetViet.
- No production integration with a payment, SMS, or push provider unless explicitly requested.

## 4. Roles and Authorization

Use ASP.NET Core Identity with policy-based authorization. A user may have more than one role, and authorization must be based on both role and resource ownership/school relationships. Do not replace Identity with a custom password/token store without explicit user approval and a documented migration/security design.

### 4.1. Administrator (Back Office)

- Manage school years, classes, subjects, and account lifecycle.
- Import users, class membership, parent-student links, and teacher assignments.
- View the current import validation/result in P0; searchable multi-batch import history is available only with the promoted P1 history slice.
- Send school-wide or role-targeted email/announcements.
- Review password-help requests, verify identity through the school's offline process, issue a unique temporary password, and deactivate/reactivate accounts.
- Produce security-relevant audit events for active capabilities; an Administrator audit browser/UI is available only with the promoted P1 history/audit slice.

### 4.2. Teacher

- Access only assigned classes and subjects.
- Record and revise attendance within the allowed period.
- When the corresponding P1 slice is promoted, create homework, enter grades/evaluations, and create text-first class posts for assigned classes.
- Review leave requests for assigned classes.
- Send class-scoped announcements/email only to permitted recipients.
- A Teacher must not gain school-wide administration rights merely by using the web portal.

### 4.3. Parent/Guardian

- Access only children connected through an active parent-student relationship.
- View each linked child's attendance, grades/evaluations, and announcements; homework is available only after the P1 homework slice is promoted.
- Submit and track leave requests.
- Update only explicitly allowed profile/contact fields.

### 4.4. Student

- Access only their own academic and class data.
- View grades/evaluations, attendance summary, and announcements; homework is available only after the P1 homework slice is promoted.
- Student academic records are read-only. The P0 Student write operation is club join/request; homework submission is added only with its promoted P1 slice. Comments, posts, and other writes remain unavailable unless promoted later.

### 4.5. Authorization Invariants

- Hiding a button is not authorization; every protected API operation must enforce a server-side policy.
- Parent-child, teacher-class, and teacher-subject relationships must be validated on every scoped read or write.
- Bulk operations must apply the same authorization rules as single-record operations.
- Security-sensitive changes and bulk communication must be auditable.

### 4.6. Domain Permission Matrix

`Own` means the Student's own record; `Linked` means an active Parent-Student relationship; `Assigned` means an active Teacher-Class/Subject assignment. `Audit/read if exposed` is not a required screen; it means that if a later requested audit view exists, it remains read-only. Deny any capability not granted here or in a later explicit requirement; Administrator is not an automatic bypass for ordinary academic writes.

| Capability | Administrator | Teacher | Parent | Student |
| --- | --- | --- | --- | --- |
| Accounts, relationships, reference data | Manage | No | No | No |
| Excel import/history | Manage | No | No | No |
| Attendance | Audit/read if exposed | Read/write Assigned | Read Linked | Read Own |
| Leave request | Audit/read if exposed | Decide Assigned | Create/read/cancel Pending for Linked | Read Own |
| Daily comment | Audit/read if exposed | Create/read Assigned | Read Linked | Read Own only when school policy enables it |
| Homework assignment | Audit/read if exposed | Create/publish/grade Assigned | Read Linked | Read Own and submit Own |
| Grades/evaluations | Configure/audit; no ordinary score overwrite | Enter/read Assigned | Read Linked | Read Own |
| Announcement/feed | School-wide compose/read | Assigned audience compose/read | Read eligible | Read eligible |
| Email delivery | School/role/class audiences | Assigned audiences only | No compose | No compose |
| Timetable/events | Manage reference/publication when implemented | Read Assigned/eligible | Read Linked/eligible | Read Own/eligible |
| Clubs | Deferred administration only | Read eligible | Read Linked/eligible | Read eligible and join/request Own |
| Password assistance | Review request/issue temporary password | Submit generic pre-login request for Own account | Submit generic pre-login request for Own account | Submit generic pre-login request for Own account |

Every list and detail endpoint applies the same matrix. Bulk, export, deep-link, retry, and background-job paths do not receive broader permissions than the corresponding interactive command.

### 4.7. Actor–Client Responsibility Matrix

Domain permission and client availability are separate decisions. A capability allowed by Section 4.6 is not automatically exposed on every client.

| Actor | Flutter Android P0/P1 | React Web P0/P1 | Backend/offline responsibility |
| --- | --- | --- | --- |
| Administrator | Not provided | P0 account/reference data, fixed import, password-help resolution, announcement/email, and current command results; P1 history/audit screens when promoted | Identity verification for password assistance occurs through the school's trusted offline process |
| Teacher | P0 daily-work client for Home/class context, schedule, attendance, leave review, and announcement inbox/view; promoted P1 feature workflows | P0 minimal landing plus announcement/email composition; promoted P1 read-focused dashboard, schedule detail, and delivery history; no duplicated roster editing unless explicitly promoted | API enforces assigned class/subject/audience scope |
| Parent/Guardian | P0 exclusive client for child context, attendance, leave, grades, timetable/events, clubs, and announcements; promoted P1 homework/daily-comment/class-feed reads | Not supported | API enforces active Parent-Student relationships |
| Student | P0 exclusive client for own grades, timetable/events, leave status, clubs, attendance, and announcements; promoted P1 homework/daily-comment/class-feed behavior | Not supported | API enforces own-record/class eligibility |

React route guards derive access from authenticated server capabilities. They do not render a reduced Parent/Student portal. Flutter does not render Administrator back-office navigation. A later explicit request may promote one named capability to another client, but it must update this matrix, route ownership, UX, affected API/client contract, and QA journey in the same slice.

## 5. Authentication and School-Assisted Account Access

### 5.1. Sign-In

- Use short-lived JWT access tokens and securely managed refresh tokens.
- The canonical sign-in identifier is `emailOrUserName`. Teacher/Parent/Administrator accounts normally use a verified email; a Student may use a unique school-issued username derived from the stable Student code when no email exists. Normalize lookup safely but never treat a display name as a login identifier.
- Store refresh tokens as revocable records; rotate them on use and revoke them after temporary-password issuance, successful password change, account deactivation, or suspected compromise.
- The server determines roles from the authenticated account. A pre-login role picker must never be trusted for authorization.
- If a user has multiple valid roles, allow role/context switching only after authentication.
- Rate-limit repeated sign-in and password-help requests and record relevant security events.
- Default access-token lifetime is 15 minutes and rotating refresh-token lifetime is 7 days, both configurable on the server. Mobile stores only the refresh token in platform secure storage and keeps the access token in memory; it restores a session by refreshing. Web stores the refresh token only in an `HttpOnly`, `Secure` cookie and keeps the access token only in memory. Neither client stores tokens in `localStorage`, plain `SharedPreferences`, URLs, or logs.
- `POST /api/v1/auth/refresh` supports the Web cookie transport and the native Mobile secure-token transport through one documented request contract; it must not accept a role override. Logout revokes the current refresh-token family and clears the Web cookie where applicable.

### 5.2. School-Assisted Password Help

There is no email reset link/code and no self-service password replacement for a user who cannot authenticate. Recovery is an internal school workflow:

1. From Login, the user opens `Quên mật khẩu?`, enters `Email hoặc mã tài khoản`, and submits `Gửi yêu cầu hỗ trợ`. `POST /api/v1/auth/password-help-requests` always returns the same accepted response whether the identifier exists, is active, or already has a pending request. Rate-limit by safe request signals and do not reveal account existence.
2. If an active account matches, Backend creates or reuses one Pending `PasswordHelpRequest`; repeated requests must not create an unbounded queue. The public response never includes request/account status. The unauthenticated user cannot choose a new password.
3. An Administrator opens `/password-help-requests`, verifies the person's identity through the school's trusted offline process, then explicitly confirms `Cấp mật khẩu tạm`. Reject/close requests that cannot be verified. A Teacher may be notified operationally but cannot reset another account unless separately granted the Administrator role.
4. `POST /api/v1/admin/users/{userId}/issue-temporary-password` generates a cryptographically random password unique to that reset, stores only its ASP.NET Identity hash, sets `MustChangePassword=true` and `TemporaryPasswordExpiresAt`, revokes all refresh-token families for the user, resolves the linked help request, and writes a security audit event. It returns the plaintext temporary password exactly once to the authorized Administrator and never logs or stores a recoverable copy.
5. The school communicates that temporary password to the verified user through its trusted offline/internal procedure, not email. The password is not a shared school default, is not derived from name/code/date of birth/phone, expires after the configured period, and becomes unusable after successful change or a newer reset.
6. Login with the temporary password yields only a short-lived restricted session carrying `passwordChangeRequired=true`. Every Backend endpoint except `POST /api/v1/auth/change-temporary-password`, logout, and minimum session context rejects that restricted session. Both clients force `/change-temporary-password` and prevent navigation to Home or cached protected content.
7. The forced-change screen requires temporary password/current credential confirmation, new password, and confirmation. Backend enforces Identity password policy, replaces the password, clears the flag/expiry, invalidates the restricted session, and requires a fresh normal sign-in. Failure preserves no plaintext password and exposes no sensitive detail.

Users who are already authenticated may use a separate `Đổi mật khẩu` action requiring their current password; that normal change also revokes other refresh-token families. It must not bypass the assisted workflow for a user who cannot authenticate.

## 6. Functional Requirements

### 6.1. Shared Application Shell

- Vietnamese sign-in, password-help request, forced temporary-password change, and authenticated change-password screens.
- Role-aware navigation derived from authenticated permissions.
- Do not add a pre-login role-selection screen. The server resolves roles after authentication; show a context switcher only for an account that genuinely has multiple roles.
- Notification/announcement inbox.
- Profile, password change, sign-out, and session-expiry handling.
- Consistent loading, empty, offline, validation, and error states.
- Never render `Trò chuyện`, `SMS`, payment, or another deferred destination as an enabled tab or shortcut before its end-to-end workflow exists.

### 6.2. Teacher Module

- **Dashboard and Class Context:** assigned class, student count, today's marked/unmarked attendance summary, pending leave-request count, three recent announcements, and quick actions. A Teacher assigned to multiple classes can switch the active class after authentication; every scoped query, draft, count, and action must refresh consistently.
- **Teaching Schedule:** read-only daily/weekly assigned sessions with subject, class, time, room/online location, and current/next-session state.
- **Attendance:** mark Present, Late, Excused Absence, or Unexcused Absence; support date/session selection, live summary counts, explicit review, confirmation, and idempotent save. A new roster starts as Unmarked rather than silently assuming Present or Absence. Provide an explicit `Đánh dấu học sinh còn lại là Có mặt` action when useful. Approved leave may prefill Excused Absence with a visible source, but the Teacher must review it. Returning to an existing sheet shows saved values and only permits edits within school policy.
- **Leave Requests:** show pending requests oldest first. Detail includes parent, student, date/session, reason, submitted time, and decision controls. Approval may have a note; rejection requires a reason.
- **Daily Comments (P1):** when promoted, select assigned class/date and one or more Students; enter comments manually or apply configured suggestion phrases; review Student recipients and content; confirm once; and display the persisted delivery/result state. Categories must be school-configurable and general enough for the deployed grade level. Positive-recognition stickers are P2 visual enrichment.
- **Homework (P1):** when promoted, list, create, edit drafts, publish, and inspect assignments with subject, class, instructions, due date/time, and approved attachments. Show submission counts and Student states (`Chưa nộp`, `Đã nộp`, `Đã chấm`). Basic grading records a validated score and Teacher comment; rich Office-document annotation remains deferred.
- **Grade Entry (P1):** when promoted, filter by school year, semester, class, subject, and assessment type; validate configured numeric ranges; distinguish draft changes from persisted values; support keyboard Next/Previous between Student rows; review changed/invalid rows before one idempotent batch save. P0 still requires Student/Parent read-only grades populated through synthetic/demo data or authorized administration, not a Teacher grade-entry UI.
- **Class Feed (P1):** when promoted, list and create text-first posts with an audience limited by permissions. Title is required and at most 100 characters; body is required and at most 2,000 characters unless a later explicit requirement changes these limits. Image/media, reactions, comments, and moderation are P2 unless separately promoted.
- **Announcement boundary:** P0 Teacher composition/send is Web-owned under Sections 6.5 and 9; Flutter provides announcement inbox list/detail viewing without requiring tracked read state. A promoted P1 class-feed slice may add Flutter text-post composition, but it must not silently become an email/bulk-send interface. SMS, push delivery, group creation, and real-time chat are deferred.

### 6.3. Parent Module

- Child switcher when a Parent is linked to multiple Students.
- Attendance calendar/history for the selected child, newest first, defaulting to the current month and supporting a date-range filter. The dashboard should expose today's status and a compact seven-day preview when data exists.
- **Daily Comments (P1):** when promoted, show the selected child's permitted comment history with Teacher, date, category/content, and persisted send/result state. The first P1 daily-comment slice does not require recipient read/unread tracking; add it only through a later explicit extension.
- Read-only timetable and eligible school-event list/detail for the selected child.
- Leave-request form with date range, session, reason, note, and status history. Start date must not be after end date; reason/note content is required and capped at 500 characters in P0.
- Leave-request detail shows the full submission, timestamps, status, and Teacher response. A Parent may cancel only a Pending request; an Approved or Rejected request remains immutable to the Parent.
- Read-only grades/evaluations. Homework visibility is added with the promoted P1 homework slice.
- P0 read-only class/school announcement inbox with list and detail. A social/class feed is added only with the promoted P1 class-feed slice.
- Edit only approved profile and contact fields. Student name, date of birth, national ID/personal identifier, and relationship data are sensitive school records: P0 keeps them read-only for Parents and routes corrections through an Administrator or a later controlled change-request workflow.

### 6.4. Student Module

- Dashboard with direct, working access to grades, timetable, events, leave-request status, clubs, attendance, and recent announcements. Homework appears only after the P1 homework slice is promoted and complete.
- **Homework (P1):** when promoted, provide list/detail plus basic submission using text and approved attachment types. A Student can see only their own submission, deadline, upload state, score, and Teacher feedback; submission/edit rules follow the assignment deadline and school policy.
- Read-only grades/evaluations grouped by semester and subject.
- Read-only attendance summary/history, newest first, with the same semantic statuses used by Teachers and Parents.
- Read-only class/school announcement inbox with list/detail in P0. A social/class feed is added only with the promoted P1 class-feed slice.
- Read-only access to the Student's own daily comments is added with the promoted P1 daily-comment slice and only when school policy exposes them directly; otherwise comments remain guardian-facing.
- Read-only timetable and eligible school-event list/detail.
- Club discovery, detail, membership state, and join/request-to-join action.

### 6.5. Web Portal

- Desktop-first responsive portal for authorized Administrator and Teacher users only. Parent/Student React access is not supported in P0/P1.
- Canonical P0 shared Web routes are `/login`, `/password-help`, `/change-temporary-password`, `/change-password`, `/dashboard`, `/announcements`, `/announcements/new`, and `/profile`. P0 Administrator-only routes are `/users`, `/school-years`, `/classes`, `/subjects`, `/relationships`, `/teacher-assignments`, `/password-help-requests`, and `/imports`. Promoted P1 routes are `/delivery-history`, `/import-history`, `/audit`, and the Teacher-capable read route `/schedule`. Use one React Router route registry with capability guards; do not create role-named duplicate pages when the same route can render authorized content.
- Administrator management screens for school years, classes, subjects, users, relationships, and assignments.
- P0 Import center with template download, file upload, fixed-header verification, validation preview, transactional commit, and current result download. Searchable import history is P1.
- P0 Email/announcement composer with permitted recipient selection, preview, confirmation, immediate send, and current delivery result. Searchable delivery history/retry UI is P1.
- In P0, an authorized Teacher lands on a minimal scoped `/dashboard` containing only identity/class context and links to the P0 announcement/email composer; it must not show fake metrics or Administrator navigation. The promoted P1 Teacher Web dashboard expands this into the approved read-focused mockup with schedule, scoped class/attendance summaries, pending work, and links only to Web-owned operations that actually exist. Attendance editing, leave decisions, daily comments, homework/grading, and grade entry remain Flutter-owned.

## 7. UX and Design System

Use one Orange-first, light-mode brand across both clients. Flutter uses Material 3 and a mobile-first interaction model. React Web uses Ant Design and a desktop-first responsive workflow model. Shared tokens, status meanings, language, and accessibility expectations apply to both, but component-library and layout-priority rules do not cross client boundaries. The UI should feel warm, clear, and professional rather than childish. Prefer fast at-a-glance summaries with drill-down detail; a Parent should understand today's attendance in under 30 seconds, and routine Teacher attendance should be completable in a few minutes for a normal class.

### 7.1. Brand and FPT Logo

- Orange is the dominant product color. Use it for brand emphasis, primary actions, active navigation, selected states, and small highlights; keep content surfaces predominantly White/Light Gray so screens remain readable.
- Use an official FPT logo file supplied by the user or school, preferably SVG or a transparent high-resolution PNG. Do not redraw the logo with text, generate an imitation, or substitute an unrelated FPT-style mark.
- Preserve the logo's original colors, proportions, and clear space. Never recolor, stretch, crop, rotate, outline, place inside a busy image, or add decorative effects to it.
- Place the logo on a White or sufficiently light neutral surface. The product name `MyFSchool` may appear beside or below it but must remain visually separate from the logo artwork.
- On web, place the logo in the sign-in panel and at the top of the navigation sidebar/header. On mobile, use it on sign-in and splash/launch branding; avoid repeating a large logo on every content screen.
- Provide `FPT` as the accessible alt text/semantic label. Decorative duplicate logos should be hidden from assistive technology.
- If the official asset is not yet available, reserve a correctly sized logo container with an explicit asset TODO; do not create a fake final logo.

### 7.2. Design Tokens

- Primary Orange: `#E65100` for branding, primary actions, FABs, and selected states.
- Orange Dark: `#BF360C` may be used for filled controls or backgrounds carrying normal-size White text when needed to meet at least WCAG AA 4.5:1 contrast; verify contrast in the implemented theme.
- Orange Tint: `#FFF3E0` for selected backgrounds and low-emphasis orange surfaces.
- Surface Base: `#FAFAFA`; Surface Raised: `#FFFFFF`.
- Primary Text: `#1A1A1A`; Secondary Text: `#666666`; Disabled: `#B0B0B0`; Border: `#E0E0E0`.
- Success/Present/Approved: `#2E7D32`; Warning/Late/Pending: `#F57C00`; Error/Unexcused/Rejected: `#C62828`.
- Excused Absence uses a neutral semantic treatment distinct from Unexcused Absence.
- Spacing scale: 4, 8, 12, 16, 24, and 32 logical pixels; normal mobile page margin is 16.
- Corner radii: 8 for controls/chips, 12 for cards/list tiles, and 16 for sheets/dialogs.

Do not use color as the only status signal. Every status includes Vietnamese text and, where useful, an icon or semantic accessibility label.

### 7.3. Reusable Interaction Patterns

- Dashboard cards contain an icon, title, short summary, and optional status/count; tapping opens the corresponding detail.
- Lists use consistent list tiles and show skeleton/loading, meaningful empty, error-with-retry, and loaded states.
- Use pull-to-refresh where users expect current server data.
- Use full-screen forms for multi-field tasks such as leave requests and announcements; use bottom sheets for compact filters or choices.
- Use confirmation dialogs for consequential actions such as approval/rejection, bulk import commit, and bulk email send.
- Forms show visible labels, inline Vietnamese validation, character counts near defined limits, and disable duplicate submission while saving.
- Success/error feedback uses concise snackbar or inline messaging and preserves user input after recoverable errors.
- Interactive targets must be at least 48x48 logical pixels, support screen-reader labels, and be usable without relying on gestures alone.

### 7.4. Voice and Content

- Vietnamese copy is short, neutral, and specific, for example `Đã lưu điểm danh`, `Chưa có thông báo`, or `Đang chờ duyệt`.
- Avoid excessive exclamation marks, slang, guilt-inducing language, and vague messages such as `Có lỗi xảy ra` when a safe actionable explanation is available.
- Dates, sessions, school years, semesters, scores, and attendance labels must use one shared formatter/source of truth.

### 7.5. Mobile Information Architecture

- Keep three to five top-level destinations per role. Do not turn every feature into a bottom-navigation item; expose less frequent tasks through dashboard cards or contextual actions.
- **Teacher:** `Tổng quan`, `Lớp học`, `Điểm danh`, `Thông báo`, `Cá nhân`. Leave review is a class-scoped P0 action; homework and grade entry appear only after their P1 slices are promoted.
- **Parent:** `Tổng quan`, `Học tập`, `Đơn nghỉ`, `Thông báo`, `Cá nhân`. Attendance and grades for the selected child are summarized on `Tổng quan` and grouped under `Học tập`; homework appears only with the promoted P1 slice.
- **Student:** `Tổng quan`, `Học tập`, `Thông báo`, `Cá nhân`. Student navigation remains simpler; the P0 write action is club join/request, and homework submission is added only with the promoted P1 homework slice.
- Preserve the navigation stack and scroll state of each main tab. Use `go_router` shell routing rather than scattered `Navigator.push` calls.
- Deep links and notification taps must pass through authentication and authorization guards before opening a protected detail screen.
- The Parent dashboard begins with a child identity/context card. The Teacher dashboard begins with the active assigned class/context. Changing context refreshes every scoped summary consistently.

### 7.6. Web Workflow Layouts

The following approved Visily images define the visual direction for the web portal. They are stored inside this repository under `templates/web/`:

- `visily-login.png`
- `visily-admin-dashboard.png`
- `visily-teacher-dashboard.png`
- `visily-user-management.png`
- `visily-excel-import.png`
- `visily-email-and-announcement-composer.png`

Agents implementing the web portal must resolve `templates/web/` from the repository root and inspect the relevant image before coding that screen. Match its information hierarchy, density, Orange/White visual language, component placement, and overall proportions. The images do not override security, authorization, accessibility, responsive behavior, or the functional requirements in this file. Names, dates, counts, charts, photos, versions, storage numbers, and other visible sample values are synthetic content and must come from APIs or fixtures rather than being hardcoded into reusable components.

The Orange square symbol shown beside `MyFSchool` in these mockups is a placeholder. Replace it with the official FPT logo asset according to Section 7.1; do not reproduce the placeholder as the production logo.

#### 7.6.1. Shared Authenticated Shell

- Desktop composition uses a persistent light sidebar, a top utility bar, a scrollable content area, and a small footer. At the reference desktop width, the sidebar is approximately 250-270px and the top bar approximately 64-72px; implement with responsive CSS rather than fixed screenshot coordinates.
- Sidebar header contains the official FPT logo, `MyFSchool`, and a small portal/role label. Primary navigation uses line icons plus Vietnamese labels. The active item has a pale Orange rounded background, darker Orange text/icon, and a right chevron only when it has a meaningful destination.
- Pin `Đăng xuất` to the bottom of the desktop sidebar. On narrower widths, collapse the sidebar to icons or an accessible drawer while keeping sign-out reachable.
- The top bar shows breadcrumbs on the left and, on the right, search, notification bell with unread indicator, separator, signed-in name, role/title, avatar, and presence indicator when presence data is real.
- Search must have a defined scope. If global search is not implemented for the current phase, omit it rather than rendering a non-working control.
- Main pages use a clear H1, one-line description, and a right-aligned action group. Primary actions are solid Orange; secondary actions are White with a neutral border.
- Cards are White with subtle borders/shadows and generous internal spacing. Use responsive grid layout: wide primary content with a narrower contextual column where shown; stack the contextual column below the main content on smaller screens.
- Footer content may include product name, help, terms, privacy, and version only when those destinations/values are configured. Do not invent legal ownership, support links, or version numbers.
- The Teacher shell must not expose `Quản lý người dùng`, school-wide `Nhập liệu Excel`, or `Cài đặt hệ thống` merely because those items appear in the Teacher mockup. Render navigation from server-authorized capabilities.

#### 7.6.2. Web Sign-In

- Use a full-viewport school/campus background with blur and dark overlay, plus a centered two-panel card. On desktop the Orange brand panel is left and the light form panel is right; on small screens hide or condense the decorative panel and prioritize the form.
- Brand panel contains the official FPT logo with `MyFSchool`, a short Vietnamese welcome statement, and no more than three concise product benefits with simple icons.
- Form panel contains `Đăng nhập hệ thống`, a short instruction, `Email hoặc mã tài khoản`, password field with show/hide control, `Quên mật khẩu?`, `Ghi nhớ đăng nhập`, and a full-width Orange submit button.
- Show inline validation, submitting/disabled state, generic invalid-credential feedback, keyboard submit, and correct focus order. Never claim `Bảo mật tuyệt đối` or another unverifiable security guarantee.
- Do not implement or render Google sign-in, Microsoft 365, SSO, social-login, or organization-account buttons even if they appear in the mockup. Authentication is the school-issued username/email and password flow only.
- `Quên mật khẩu?` opens the generic `/password-help` request described in Section 5.2. A login marked `passwordChangeRequired=true` may navigate only to `/change-temporary-password`; apply the same forced-change and fresh-sign-in behavior as Flutter and do not allow dashboard/API access first.
- This portal sign-in is intended for Administrator and authorized Teacher access. Parent/Student web access must not be implied unless explicitly added later.

#### 7.6.3. Administrator Dashboard

- P0 is a lean operational landing page, not a separate analytics product. Start with an Orange welcome/summary hero containing a short status sentence, up to two relevant actions, and an optional school-context image; expose direct navigation to the active P0 operations.
- Summary cards are optional UI content in P0 and must reuse data already required by active slices, such as active users/classes or pending password-help requests. Do not add aggregation endpoints solely to reproduce four mockup cards.
- `Nhiệm vụ cần xử lý`, current import progress, system notifications, and event/deadline panels appear only when their underlying P0 endpoint/state already exists. A new consolidated operational task-center workflow is P2; every displayed action must navigate to a real authorized destination.
- Storage usage, online-user analytics, backup health, release notes, and similar infrastructure panels are P2. Do not build fake metrics solely to reproduce the screenshot.
- Skeleton cards/rows shown in the mockup represent the loading state only. Replace them with actual metrics/content after loading; they must not remain in the settled dashboard.

#### 7.6.4. Teacher Dashboard

- The complete approved dashboard is a promoted P1 read-focused Web extension, not a P0 dependency and not a duplicate of the Teacher Mobile workspace. P0 provides only the minimal scoped Teacher landing defined in Section 6.5 so the Teacher can reach announcement/email composition safely.
- Header greets the Teacher by display name and summarizes today's schedule. Primary CTA is `Soạn thông báo`; secondary CTA `Xem lịch giảng dạy` opens `/schedule`, including its empty state when no sessions exist. Do not retain the mockup's `Điểm danh ngay` Web CTA because attendance editing is Flutter-owned in P0/P1; record this as an intentional scope-driven mockup deviation.
- The first row uses compact summary cards backed by implemented APIs: assigned/active student count, attendance rate, and unread/new announcements. Grading workload appears only after homework/grade-entry slices are promoted. Each card must define its time range and scoped class set.
- `Quản lý lớp học hôm nay` lists teaching sessions with subject, class, start/end time, room, semantic status, and `Quản lý` action. Actions and rows are restricted to assigned classes/subjects.
- Attendance trend uses a simple accessible bar/line chart with text summary or data-table alternative. Never show a chart without a defined period, unit, and empty state.
- `Điểm danh gần đây` lists Student, class, recorded time, and semantic status. The screenshot's column ordering is visual guidance; use clear table headers and do not place a Student name under a `Trạng thái` header.
- Context column may show deadlines/events and safe pending-work summaries. Report download and grading/grade-entry progress appear only after those specific capabilities are promoted and backed by authorized APIs. Promotional cards are optional and must not displace pending school work.
- Schedule access is a deliberate Teacher Web capability defined in Section 1.1, independent of the Mobile assignment flow. Event links appear only when a real authorized Web event destination has also been promoted; do not infer Web scope from a Mobile requirement.

#### 7.6.5. User Management

- P0 includes paginated role tabs/list, search/basic status filtering, user detail/create/edit, relationship/assignment management, account activate/deactivate, and authorized temporary-password issuance. Export/report, arbitrary bulk edit, and summary analytics are P2 unless explicitly promoted.
- Page header contains `Quản lý người dùng`, a short description, and primary `Thêm thành viên`. Show `Xuất báo cáo` only after the report capability is implemented end-to-end.
- Main card starts with segmented role tabs for `Giáo viên`, `Học sinh`, and `Phụ huynh`, followed by scoped search and a filter button with active-filter count. Add `Thao tác hàng loạt` only with an implemented promoted bulk capability.
- Table shows avatar/fallback, full name, stable user code, email, role-specific columns, account status, and explicit row actions. Selection checkboxes appear only with an implemented authorized bulk action. Teacher rows may show subject group and join date; Student/Parent tabs use columns appropriate to their relationships.
- Search covers name, email, or stable code. Filters include account status and applicable class/subject relationships. Preserve query/filter/page state in the URL where practical.
- If a later promoted bulk action exists, keep it disabled until rows are selected and require confirmation for deactivate/reactivate or other high-impact changes. Never render a non-working bulk-action control or offer an action the current operator lacks permission to execute.
- Pagination shows visible range and total count. Empty search/filter results distinguish `Không tìm thấy kết quả` from a genuinely empty user type.
- Summary cards below the table are optional UI content and, when used, show API-derived totals already available to the page; they do not justify a new P0 analytics slice.
- Each account row/detail has an authorized `Cấp mật khẩu tạm` action. It requires identity-verification confirmation, shows the generated temporary password once with copy/acknowledge controls, warns that it cannot be viewed again, and never places it in a URL, table, notification history, export, or log.
- `/password-help-requests` lists Pending requests without exposing them to ordinary Teachers/Parents/Students. Administrator can inspect safe account context, resolve as `Không xác minh được`, or issue a temporary password; filters and counts come from the API.

#### 7.6.6. Excel Import Center

- Use a centered, wide workflow with a four-step progress indicator: `Tải tệp lên` -> `Khớp dữ liệu` -> `Kiểm tra` -> `Hoàn tất`. Completed, active, future, error, and disabled steps must be visually distinct and accessible.
- Step 1 contains a large dashed drag-and-drop zone, supported-format/size guidance, and `Chọn tệp từ máy tính`. Also show cards for `Tải tệp mẫu` and `Hướng dẫn nhập liệu`.
- In P0, Step 2 verifies the fixed versioned workbook sheets/headers against Section 8, shows detected fields and a sample-row preview, and allows returning without losing the uploaded batch. Arbitrary user-defined column mapping is P2; do not build a generic mapping engine for the assignment baseline.
- Step 3 shows total/valid/warning/error counts and a filterable table with sheet, row, column, original value, stable error code, and Vietnamese explanation. Block commit when blocking errors remain and explain why.
- Commit requires explicit confirmation summarizing create/update/skip counts and the affected school year/classes. Never write records during file parsing or column mapping.
- Step 4 displays batch ID, timestamps, created/updated/skipped/failed totals, and actions to download the current result or start another import. A searchable multi-batch import-history UI is P1.
- Uploading, parsing, validating, committing, success, partial failure, fatal failure, and retry/recovery are distinct states. A browser refresh should recover the current server-side batch when possible.

#### 7.6.7. Email and Announcement Composer

- Use the mockup's three-part composition: shared sidebar, wide editor workspace, and narrower recipient/settings rail. At smaller widths, move the right rail into drawers/stacked sections without changing field order.
- Header contains `Soạn thảo Thông báo & Email`, description, and primary `Tiếp tục xem trước`. P0 does not require saved drafts. Never send directly without preview and final confirmation.
- P0 editor includes required title/subject, delivery-channel chips (`Email`, `Portal App`), a small sanitized rich-text toolbar/body, and no attachment or schedule-send control. `Portal App` means a persisted in-app announcement. Saved templates, draft autosave, attachments, and scheduled send are P2 unless explicitly promoted.
- Recipient rail shows selected audiences as removable chips, group checkboxes, member counts, and an exact deduplicated total. Administrator can choose school/role/class/explicit audiences; Teacher choices are limited to authorized classes and recipients.
- Show a clear scope notice such as `Thông báo sẽ được gửi tới 342 người nhận qua Email và ứng dụng`, calculated from the server preview rather than client estimates.
- Advanced header images, response/acknowledgement requirements, and attachments are P2 and appear only when corresponding backend tracking and file authorization have been explicitly promoted. The mockup's 25MB is a visual example, not a P0 constant.
- `Xem trước hiển thị` renders representative mobile/in-app and email previews using the same sanitized content contract as actual delivery. Preview must not execute unsafe HTML or external scripts.
- P0 `Gửi ngay` confirmation summarizes subject, channels, audience, and deduplicated count. Scheduled delivery is P2.
- Recent drafts/sends and help links are optional contextual content. P0 must show the current command's per-recipient result; searchable delivery history and operator retry UI are P1.

#### 7.6.8. Responsive and UI-State Contract

- Desktop is the primary web target; support common laptop widths without horizontal page scrolling. Tables may use controlled horizontal scrolling only when columns cannot be responsibly collapsed.
- Below the desktop breakpoint, collapse the sidebar, stack multi-column dashboard sections, and turn wide composer/settings layouts into sequential panels. Preserve actions and data rather than simply hiding them.
- Every page implements loading, loaded, empty, recoverable error, forbidden, and session-expired behavior where applicable. Destructive/high-impact actions show affected scope and require confirmation.
- Icon-only controls require visible tooltip and accessible name. All interactive controls need keyboard focus, logical tab order, and minimum 44x44 CSS-pixel target where feasible.
- Use skeletons shaped like final content only while loading. Avoid layout shifts by reserving approximate card/table dimensions.
- Implement reusable shell, page header, metric card, status tag, data table, empty/error state, upload drop zone, stepper, recipient selector, rich editor wrapper, confirmation modal, and preview components rather than duplicating screen-specific markup.

#### 7.6.9. Web Profile and Password Change

- `/profile` is available only to authenticated Administrator/Teacher portal users and shows school-issued identity, active supported role/context, and permitted contact fields. Omit unsupported sensitive edits and never expose Parent/Student portal UI through this route.
- `/change-password` requires current password, new password, confirmation, server policy feedback, duplicate-submit prevention, success logout, and fresh sign-in. It is distinct from `/change-temporary-password` and cannot be used as pre-login recovery.

### 7.7. Flutter Screen Specifications

The following approved Visily images define the visual direction for the Flutter application. They are stored inside this repository under `templates/flutter-app/`:

- `visily-login.png`
- `visily-home.png`
- `visily-grades.png`
- `visily-grade-detail.png`
- `visily-leave-requests.png`
- `visily-create-leave-request.png`
- `visily-leave-request-detail.png`
- `visily-schedule.png`
- `visily-clubs.png`

Agents implementing a listed screen must resolve `templates/flutter-app/` from the repository root and inspect its image before coding. Reproduce the information hierarchy, rounded-card language, Orange emphasis, spacing rhythm, and interaction placement, but do not treat the screenshot as a pixel-coordinate specification. All visible English copy must become natural Vietnamese. Names, dates, scores, courses, rooms, counts, photos, status-bar time, and other sample values are fixtures, not constants.

The Orange lightning/square mark in the Flutter mockups is a placeholder. Replace it with the official FPT logo according to Section 7.1. Terms such as `University`, `Credits`, `GPA`, and higher-education course examples must be mapped to the actual school domain or omitted; do not accidentally turn MyFSchool into a university portal.

These mockups cover the assignment-required mobile flow in Section 3.1.1. Grades, timetable, events, leave requests, and clubs must have working destinations in the MVP. Where a feature is intentionally shallow, implement the required depth stated in Section 3 instead of replacing it with a placeholder.

#### 7.7.1. Shared Mobile Scaffold

- Design for common phone widths around 360-430 logical pixels and adapt to smaller phones, large phones, text scaling, and tablets. Use `SafeArea`/system insets; never draw a fake status bar or hardcode `9:41`.
- Use a light page background, White cards, subtle borders/shadows, 12-16px radii, 16-24px page padding, and Orange for selected/navigation/primary-action emphasis.
- Top app bars are simple: back button when needed, Vietnamese title, and at most one or two contextual actions. Home may show logo/product name and avatar. Show an unread badge/bell only after the promoted P1 announcement read-state capability exists; otherwise use a neutral inbox link or omit it.
- Role-specific bottom navigation persists through `go_router` shell routing, uses icons plus Vietnamese labels, and highlights the active destination in Orange. The exact `Home/Events/Requests/Profile` labels in the images are not universal; follow Section 7.5.
- Keep authenticated user/session/role context in an application-scoped Riverpod provider. Keep temporary filter/form/UI state scoped to its feature/route. Do not use widget-private variables, static globals, or constructor chains as a substitute for cross-screen session state.
- A floating action button is reserved for the screen's primary create action. Do not show both a FAB and another equally dominant button for the same action.
- Long pages scroll vertically; horizontal scrolling is reserved for compact carousels/date strips/category chips. Preserve scroll/tab state when switching main destinations.
- Use reusable `AppScaffold`, app bar, bottom navigation, section header, information card, status chip, service tile, empty/error view, and primary-button components rather than restyling each feature independently.

#### 7.7.2. Mobile Sign-In

- Follow `visily-login.png`: soft Orange-tint upper background, centered brand area, and a rounded White form card that visually overlaps the upper section. Use the official FPT logo with `MyFSchool`, not the lightning placeholder or `University Student Portal` wording.
- All-role copy is Vietnamese: `Đăng nhập`, a short instruction, `Email hoặc mã tài khoản`, `Mật khẩu`, `Quên mật khẩu?`, password visibility control, and full-width Orange submit button.
- There is no public registration. Replace `Don't have an account?` with concise support text such as `Chưa có hoặc không truy cập được tài khoản?` and action `Liên hệ nhà trường`.
- On valid submit show an in-button progress state and prevent duplicates. Show inline format/required errors and generic invalid-credential feedback without account enumeration.
- A single-role account goes directly to its dashboard; a multi-role account chooses context only after authentication. Back, keyboard, autofill, session restoration, offline, and expired-session behavior must be handled.
- `Quên mật khẩu?` opens `/password-help`, which accepts `Email hoặc mã tài khoản`, submits the generic assistance request, and explains that the user must contact/wait for the school to verify identity and issue a temporary password. It must not offer an email reset link or let the unauthenticated user choose a password.
- A successful login response with `passwordChangeRequired=true` redirects only to `/change-temporary-password`. That screen accepts the current temporary password, new password, and confirmation; after success it clears local session state and returns to Login for a fresh sign-in. Back/deep links cannot bypass this gate.
- Privacy/terms links appear only when real destinations exist.

#### 7.7.3. Role-Aware Home Dashboard

- Follow `visily-home.png`: compact branded app bar, personalized greeting, horizontally scrollable `Có gì mới` carousel, two at-a-glance summary cards, and a two-column grid of service shortcuts.
- News cards use a licensed/supplied image, category chip, concise title, relative/published time, readable image overlay, and `Xem tất cả`. Provide loading, empty, and image-fallback states.
- Summary cards are role-aware: Student may see latest average/next class; Parent sees selected child's attendance/latest grade; Teacher sees next teaching session/pending attendance or leave work. Do not show `GPA` unless that metric is defined by the school.
- Service tiles use a colored icon block, Vietnamese title, one-line description, and full-card tap target. Render only authorized and implemented destinations.
- The optional `Thêm lối tắt` tile and Orange add FAB represent personalization. Implement only if shortcut persistence exists; otherwise omit both.
- Parent home begins with or provides an obvious child-context selector. Teacher home uses active assigned-class context. All summaries refresh together when context changes.

#### 7.7.4. Grade List

- Follow `visily-grades.png`: top app bar, semester selector, optional filter action, `Kết quả học tập` section header with subject count, vertically stacked subject cards, and persistent role navigation.
- Each card shows subject code when available, Vietnamese subject name, assessment/grade-type summary rather than university credits, relevant date/semester, prominent score, and chevron to detail.
- Use the school's configured score scale (normally 0-10 for this project) and rounding rules. Score color/status thresholds come from domain configuration; do not infer semantics from the mockup's inconsistent Orange side rails.
- Parent sees grades for the selected linked child; Student sees only their own; Teacher grade entry uses its dedicated scoped workflow.
- Sort and filter consistently by semester, subject, and assessment type. Show unavailable/unpublished grades without fabricating zero values.
- Implement loading skeleton, no-grades-for-semester, filtered-empty, forbidden, and retry states.

#### 7.7.5. Grade Detail

- Follow `visily-grade-detail.png`: subject title, large summary card with score ring and optional `Đạt`/`Chưa đạt` chip, subject/assessment summary, Teacher card, component breakdown, and textual evaluation.
- Replace university fields with school fields such as `Ngày đánh giá`, `Học kỳ`, `Loại điểm`, and `Môn học`. Show the Teacher only when that relationship is available.
- Grade components show component name/type, weight when the grading scheme uses weights, and score. Weighted total is calculated server-side or by a shared domain rule and clearly labeled; never invent missing weights/components.
- If the grade is not final or the school does not define pass/fail, omit the pass chip rather than guessing.
- Teacher notes/evaluation use a visually distinct, readable card. Preserve line breaks, limit unsafe rich text, and do not label ordinary feedback as an official transcript unless the domain says it is.
- Provide an accessible text equivalent for the score ring; the numeric score must remain readable without animation or color.

#### 7.7.6. Leave-Request List

- Follow `visily-leave-requests.png`: page title, count/context header, period/status filter, vertically stacked cards, semantic status chip, colored side accent, detail link, role navigation, and contextual FAB.
- Each card shows request ID, Student identity where applicable, leave date range/session, reason preview, submitted date, and status `Đang chờ`, `Đã duyệt`, `Từ chối`, or `Đã hủy`.
- Parent view lists only requests the Parent submitted for the selected linked child and shows the create FAB. Student view is read-only for their own records. Teacher view is a scoped review queue for assigned Students and must not show a create FAB.
- The mockup combines multiple Student names with a create action; do not reproduce that authorization ambiguity. A Parent cannot browse unrelated Students, and a Teacher cannot submit as a Parent.
- Default ordering is newest first for Parent/Student history and oldest pending first for the Teacher review queue. Filters include status and date/period without hardcoding `Fall 2023`.
- Provide pull-to-refresh, pagination/infinite loading as needed, empty state with permitted action, and retry state.

#### 7.7.7. Create Leave Request

- Follow `visily-create-leave-request.png`: top app bar, policy notice, structured sections, date fields, reason-category chips, detailed-reason field with counter, validation summary, review action, and a bottom-safe-area primary CTA. The attachment drop zone/list appears only when leave evidence is enabled in the active slice/policy; otherwise omit it rather than showing a dead control.
- Parent must select or confirm the linked child before dates. Required fields are child, start/end date, applicable session, reason category, and detailed reason. Validate `start <= end`, minimum 20 and maximum 500 characters, and any overlap/past-date school policy.
- The advance-notice message such as `gửi trước ít nhất 24 giờ` is school policy supplied by configuration/API. Do not hardcode 24 hours if the backend does not enforce it.
- Category chips use configured Vietnamese values such as `Sức khỏe`, `Gia đình`, `Cá nhân`, or `Học tập`; horizontal scrolling must still expose all values accessibly.
- Evidence is optional unless policy makes it conditionally required. Support PDF/JPG/PNG, show effective server limit (5MB default only if configured), upload progress, success/error, file size, retry, and remove. Validate content/type on the server as well as client.
- `Xem lại đơn` opens a review/confirmation state. `Gửi đơn xin nghỉ` stays disabled while invalid/submitting and uses an idempotent submit to prevent duplicate requests.
- Preserve entered text/dates and, when attachment support is active, successfully uploaded attachment references after recoverable errors.

#### 7.7.8. Leave-Request Detail and Review

- Follow `visily-leave-request-detail.png`: top status summary with request ID, Student information card, leave reason/submitted timestamp, and Teacher response card with responder and response time.
- Translate every label and date into the shared Vietnamese locale/format. Include session and attachment links when available.
- Pending Parent view shows `Hủy đơn` when cancellation is still allowed by the lifecycle policy; the API revalidates Pending state at submission time. Approved/Rejected/Cancelled are read-only. Rejected detail prominently shows the required rejection reason.
- Teacher pending view adds sticky or clearly visible `Duyệt` and `Từ chối` actions. Both require confirmation; rejection requires a reason; submitted state prevents repeated decisions.
- Student view is read-only. All roles see only authorized requests, and the API rechecks scope when opening a deep link.
- Status chips/text use the same tokens and vocabulary as the list; do not show conflicting English `Approved` labels.

#### 7.7.9. Timetable / Schedule

- Follow `visily-schedule.png`: app bar with calendar action, horizontally scrollable week/date strip, day summary/next-session chip, chronological session cards, optional preparation note, and role navigation.
- Session cards show start/end time, subject/code, class when relevant, room or online location, Teacher, and semantic state such as `Sắp diễn ra`, `Đang diễn ra`, or `Đã kết thúc`.
- Current/in-progress styling may use Orange tint/border but must keep readable contrast. Time/current-state calculations use the configured school timezone and refresh when the app resumes.
- Student sees their enrolled timetable; Parent sees the selected child's timetable; Teacher sees assigned teaching sessions. Deep links must preserve the selected date and context.
- Show `Không có tiết học` for an empty day and separate loading/error states. Preparation notes appear only when supplied by a class/session source.

#### 7.7.10. Events

- A school-event list and detail are required even though there is no standalone event mockup. Use the same visual language as the Home news carousel, Schedule cards, and other approved Flutter screens.
- Event list supports upcoming/past context, date/category filters, and cards with supplied/licensed image or fallback, title, start/end time, location/online indicator, organizer, audience, and semantic state.
- Event detail shows full title, description, schedule, location, organizer/contact, eligible audience, attachments/links when safe, and related announcement when available.
- Student sees events eligible for their class/school; Parent sees events relevant to the selected child; Teacher sees applicable school/class events. API authorization remains authoritative.
- Registration controls must not appear unless the registration workflow is implemented end-to-end. The required P0 depth is reliable list/detail navigation, not fake registration.
- Home `Sự kiện` tile and news/event carousel open this module through guarded routes and preserve the selected event on deep link or refresh.

#### 7.7.11. Clubs

- Follow `visily-clubs.png`: title/filter app bar, search, horizontally scrollable category chips, `Đề xuất cho bạn`, two-column club-card grid, optional recruitment banner, contextual FAB, and role navigation.
- All content is Vietnamese. Each card includes image/fallback, category, club name, short description, member count, membership state, and action such as `Tham gia`, `Đã tham gia`, or `Chờ duyệt`.
- Search and category filters work together and expose active state. Recommendations require an actual rule or may fall back to `Câu lạc bộ nổi bật`; never claim personalization without supporting data.
- Joining/requesting membership is an authenticated server operation with loading, idempotency, capacity/eligibility checks, and configured approval behavior. A Student cannot create/manage a club unless explicitly authorized.
- P0 may configure a club for immediate `Active` membership or create a `Pending` request. It does not require a club-approval UI; when approval is required, the demo must honestly remain `Chờ duyệt` until a later promoted administration workflow or authorized bootstrap changes it.
- Use appropriately licensed images with fixed aspect ratios and graceful failure. Clamp descriptions to keep the two-column grid aligned, but expose full content in Club detail.
- On very narrow screens or large text scaling, switch to one column rather than shrinking cards below usable size.

#### 7.7.12. Announcement Inbox and Profile

- `/announcements` is a P0 authorized inbox, not the P1 social feed. It shows newest-first title, sender/scope, published time, channel/status where relevant, and list/empty/error states; selecting an item opens `/announcements/:announcementId` with sanitized body and safe supplied links. Do not display an unread badge until the P1 read-state slice is active.
- Parent/Student visibility follows selected child/enrollment/audience scope; Teacher visibility follows assigned/eligible audience scope. Deep links re-authorize the exact recipient/resource rather than trusting a list item ID.
- `/profile` shows school-issued identity, stable code, active role/context, and permitted contact fields. Sensitive Student identity and relationship fields remain read-only as defined in Section 6; unsupported edits are omitted, not disabled with fake save behavior.
- `/change-password` is reached from Profile and requires current password, new password, confirmation, validation, duplicate-submit prevention, success sign-out, and fresh sign-in. It is distinct from the forced temporary-password route and cannot reset an unknown account.

#### 7.7.13. Flutter State, Localization, and Verification Contract

- Vietnamese is mandatory in the implemented UI even where mockups contain English. Use centralized localization/string resources; do not scatter translated literals across widgets.
- Every async screen has explicit initial/loading, loaded, empty, recoverable error, offline/stale, forbidden, and session-expired handling where relevant.
- All tappable controls meet the 48dp target where practical, support semantics labels on required Android targets, and do not rely only on color or swipe. Flutter Web-specific keyboard/focus verification is Deferred with Flutter Web; physical keyboard behavior on Android is checked only where the promoted feature requires it.
- Test at common small/large Android viewports, with text scale increased, long Vietnamese labels, keyboard open, and system navigation insets. No clipped text, hidden sticky CTA, or unintended horizontal page overflow is acceptable.
- P0 disposable QA covers reusable status behavior and critical conditional actions: sign-in/password assistance/forced change, role/client routing, every required P0 Home destination, grade drill-down, timetable/event detail, club join idempotency, Parent leave submission/detail, Teacher leave decision, attendance save/reopen, and announcement delivery. Daily-comment retry, homework publish/submit/grade, grade-entry batch validation/save, and roster keyboard/focus scenarios are added only with their promoted P1 slice.
- Visual implementation is complete only after comparing emulator screenshots with the relevant approved mockups and documenting intentional deviations caused by role scope, Vietnamese localization, accessibility, or explicitly deferred depth beyond the required screen flow.

### 7.8. Teacher Mobile Workflow Specifications

The following specifications define P0 Teacher attendance/leave/announcement behavior and the depth of promoted P1 Teacher extensions even though they do not yet have standalone approved Visily screens. Do not implement a P1 subsection until its slice is explicitly started. Build active slices from the shared mobile design system in Sections 7.1-7.5 and 7.7 rather than copying another product's branding or hardcoded sample data.

#### 7.8.1. Teacher Home and Class Context

- The Teacher dashboard provides an assigned-class context selector, news/announcement carousel, scoped summaries, and service tiles for `Điểm danh`, `Nhận xét hằng ngày`, `Giao bài tập`, `Nhập điểm`, `Thông báo`, and `Lịch giảng dạy` when authorized.
- P0 renders only implemented P0 tiles: `Điểm danh`, `Thông báo`, and `Lịch giảng dạy`, plus leave review through class context. `Nhận xét hằng ngày`, `Giao bài tập`, and `Nhập điểm` appear only after their P1 slices pass their gates.
- Changing the active class clears or reloads class-scoped drafts only after warning about unsaved changes. Counts, roster data, and quick actions must never mix Students from different classes.
- Use the Teacher navigation defined in Section 7.5. Do not add Chat or Contacts tabs merely to resemble the reference flow; deferred tabs must not be dead destinations.

#### 7.8.2. Daily Comments

This is a P1 extension, not a P0 completion condition.

```text
Teacher Home -> Nhận xét hằng ngày -> Chọn lớp/ngày -> Nhập hoặc áp dụng câu gợi ý
             -> Chọn học sinh nhận -> Xem lại -> Xác nhận gửi -> Kết quả
```

- Render the roster with stable Student identity and per-Student comment state keyed by Student ID. Updating one row must not recreate controllers or lose edits in other rows.
- Support a multiline manual comment and a searchable/grouped suggestion selector. Suggestions can be single- or multi-select according to configuration; applying them must preview the resulting text and never silently overwrite existing manual content.
- Required categories are configuration-driven, for example `Học tập`, `Tham gia hoạt động`, and `Nhận xét khác`. Do not hardcode preschool-only eating/sleeping categories for every school level.
- The review step shows class, date, selected Students, missing/empty comments, and the exact content to be sent. Submission is idempotent and reports per-Student success/failure without duplicating successful comments on retry.
- Configured suggestion phrases are read-only in the first P1 slice. Suggestion-library CRUD and positive-recognition sticker management are Deferred/P2.

#### 7.8.3. Attendance and Leave Review

```text
Teacher Home -> Điểm danh -> Chọn lớp/ngày/buổi -> Đánh dấu danh sách
             -> Rà soát học sinh chưa đánh dấu -> Xác nhận -> Lưu kết quả
Attendance -> Đơn nghỉ học -> Chi tiết -> Duyệt/Từ chối
```

- Use two clearly labeled tabs or equivalent segmented navigation: `Điểm danh` and `Đơn nghỉ học`. Preserve each tab's filter and scroll state.
- Each roster row has exactly one of `Chưa đánh dấu`, `Có mặt`, `Đi muộn`, `Nghỉ có phép`, or `Nghỉ không phép`. State changes update visible summary counts immediately without rebuilding unrelated rows.
- Do not default every Student to Present. The explicit bulk action for remaining Students requires a visible affected count and can be undone before save.
- Before save, highlight unmarked Students, summarize all status counts, and require confirmation. Saving uses an attendance-sheet version/idempotency token and handles stale edits explicitly.
- Approved leave overlapping the selected date/session is shown beside the Student and may propose `Nghỉ có phép`; it must not create contradictory duplicate attendance records. A leave decision after attendance is saved triggers a documented reconciliation warning rather than silently rewriting history.

#### 7.8.4. Class Feed and Announcements

```text
Teacher Home/Thông báo -> Feed -> Tạo bài viết -> Chọn đối tượng -> Soạn nội dung
                       -> Xem trước -> Xác nhận đăng -> Bài viết đã lưu
```

- P0 requires the announcement inbox and authorized composition/delivery path, not a social feed. The remaining feed behavior in this subsection is a P1 extension.
- P1 feed cards contain author, published time, audience summary, content, and permitted overflow actions. Media preview, video upload/transcoding, reactions, and comments are Deferred/P2.
- Create Post requires an authorized audience and body; a title is required when the content is also sent as an announcement/email. Audience choices come from the server and are limited to assigned classes/roles.
- The first P1 feed slice is text-only. Image/media upload and multi-image layout are P2 unless separately promoted with the complete upload, preview, retry, remove, authorization, and server-validation path.
- Teacher edit/delete applies only to authorized posts and requires server-side lifecycle rules. Do not display reaction, comment, comment-lock, or group-chat controls until their corresponding API, authorization, moderation, and state handling exist.

#### 7.8.5. Homework Assignment, Submission, and Grading

This is a P1 extension, not a P0 completion condition.

```text
Teacher Home -> Bài tập -> Tạo mới -> Chọn lớp/môn/hạn nộp -> Xem lại -> Đăng
Bài tập -> Chi tiết -> Danh sách nộp -> Bài làm học sinh -> Chấm điểm/nhận xét -> Hoàn thành
Student Home -> Bài tập -> Chi tiết -> Nộp bài -> Xác nhận -> Trạng thái nộp
```

- Homework list distinguishes Draft, Published, Closed, and Archived state plus due/submission counts. Only Published assignments are visible to Students in the intended class.
- Create/Edit uses reusable labeled controls for class, subject, title, instructions, due date/time, and approved attachments. Validate that due time is in the permitted window and show a review step before first publication.
- Student submission supports text and configured PDF/JPG/PNG attachments with upload progress, retry/remove, deadline rules, and idempotent submit. Do not claim Word/Excel inline preview support unless a safe viewer actually exists.
- Submission list separates `Chưa nộp`, `Đã nộp`, and `Đã chấm`. `Nộp muộn` is an additional badge/filter derived from the submitted timestamp and deadline, not an exclusive lifecycle state; a late submission can later be graded. The Teacher can open only submissions for an assigned class/subject.
- Grading shows safe preview/download, submitted timestamp, attempt/late status, configured score range, and Teacher comment. Completion requires confirmation and must not overwrite a newer grade silently.

#### 7.8.6. Grade Entry

This is a P1 extension, not a P0 completion condition. P0 grade list/detail uses authorized seeded/imported/demo academic records without requiring this editor.

```text
Teacher Home -> Nhập điểm -> Chọn năm học/học kỳ/lớp/môn/cột điểm
             -> Nhập điểm theo danh sách -> Rà soát thay đổi/lỗi -> Ghi lại
```

- Keep filters above the roster and require a complete valid filter context before loading editable rows. Preserve an unsaved-draft warning when any filter or route changes.
- Each Student row shows stable identity and a numeric field configured for the assessment's scale/decimal precision. Empty means not entered, never an implicit zero.
- Own a stable controller/focus node per visible Student ID in feature state. Keyboard `Next` moves to the next editable row, and scrolling must not discard uncommitted values.
- Validate format/range immediately, summarize invalid and changed rows before save, and block the batch while invalid values remain. Save changed rows only through one idempotent/concurrency-aware command and show row-level conflicts or failures.

#### 7.8.7. Messaging Boundary

- Required P0 messaging consists of the persisted announcement inbox/list-detail, Teacher class-scoped broadcast composition, and Web Email/Portal App delivery described in Section 9. Per-recipient read/unread state and the social/class feed are separate promoted P1 capabilities.
- Real-time one-to-one/group chat, media chat, typing/presence indicators, SMS, and mobile push delivery remain Deferred/P2. Do not add their tabs, fake conversations, or non-working buttons in P0/P1.

## 8. Excel/CSV Import Requirements

Use `.xlsx` as the primary format. CSV may be supported for simple, single-table imports but must not replace the required Excel workflow.

### 8.1. Supported Data

At minimum, support a versioned template for:

- Students and their class membership.
- Parents/guardians and parent-student relationships.
- Teachers and teacher-class/subject assignments.

Use one versioned `.xlsx` workbook contract for the initial implementation with documented sheets `Students`, `Parents`, `ParentStudentLinks`, `Teachers`, and `TeacherAssignments`. Classes/subjects/school year referenced by stable codes must already exist or be rejected with row-level reference errors. Do not also create unrelated per-role workbook formats in the same initial slice.

Canonical initial columns are:

| Sheet | Required columns | Optional columns/rules |
| --- | --- | --- |
| `Students` | `studentCode`, `fullName`, `dateOfBirth`, `classCode` | `userName` defaults deterministically to normalized `studentCode`; `email` may be empty |
| `Parents` | `parentCode`, `fullName`, `email` | `phone`; email must be unique/valid because it is the normal sign-in and school-communication channel |
| `ParentStudentLinks` | `parentCode`, `studentCode`, `relationship` | `isPrimaryContact`, default `false`; referenced codes must exist in the workbook or database |
| `Teachers` | `employeeCode`, `fullName`, `email` | `phone`; email must be unique/valid |
| `TeacherAssignments` | `employeeCode`, `classCode`, `subjectCode`, `schoolYearCode` | no implicit assignment inferred from names |

The downloadable template owns exact machine headers and a Vietnamese instruction sheet. Accept genuine Excel date cells and documented `YYYY-MM-DD` text for `dateOfBirth`; normalize them to a date-only value. Do not import passwords, password hashes, roles outside the sheet contract, formulas as executable content, or unknown hidden-sheet data.

### 8.2. Import Workflow

1. **Tải tệp lên:** offer the correct versioned template and import guide, then accept the upload with size, extension, MIME-type, and workbook-structure validation. Create a recoverable import batch but do not modify school records.
2. **Khớp dữ liệu:** parse into staging models, verify the fixed versioned sheets and machine headers, show recognized/missing columns, and preview sample rows. Never write domain records during parsing/header verification. Arbitrary column mapping is P2.
3. **Kiểm tra:** validate every staged row, show aggregate counts and row-level errors by sheet/row/column/code/message, allow downloading a correction report, and block confirmation while blocking errors remain.
4. **Hoàn tất:** after explicit confirmation, commit the logical batch in a transaction, record the operator/template/timestamps/checksum/counts/status, then show and allow downloading the current final result plus a new-import action. Searchable access to prior batches is P1 through `/import-history`.

### 8.3. Import Rules

- Define stable natural identifiers such as student code, employee code, class code, and school year; do not match people by display name alone.
- Detect duplicates both inside the uploaded file and against existing records.
- Validate references, dates, email addresses, enum values, and required relationships.
- Normalize harmless formatting differences but never silently guess ambiguous identity data.
- Use a transaction for the commit step. The default behavior is all-or-nothing for a logical import batch.
- Re-uploading the same valid data must not create duplicate accounts or relationships; document whether records are created, updated, or rejected.
- Do not include plaintext passwords in an exported error/result file. Account activation or reset must use the secure account workflow.
- Start with synchronous processing for small assignment-sized files. Introduce background jobs only when file size or measured execution time justifies them.

ClosedXML is the preferred Excel library because of its straightforward licensing for this project. If another library is chosen, verify its license and document the reason.

## 9. Email and Announcement Requirements

Email is a delivery channel, while an in-app announcement is a persisted domain record. Important school messages should remain visible in the application even if email delivery fails.

### 9.1. Authorization and Audience

- Administrator may target the whole school, roles, classes, or explicitly selected recipients.
- Teacher may target only assigned classes and permitted recipients.
- Resolve and snapshot recipients on the server at send time.
- Never expose recipient addresses to other recipients; send Gmail messages individually with bounded pacing for the small demo audience.

### 9.2. Composition and Delivery

- P0 supports subject, a small sanitized rich-text body, audience summary, preview, confirmation, immediate `portalApp` persistence, and immediate Gmail delivery. Saved drafts, autosave, scheduling, templates, and attachments are P2 unless explicitly promoted.
- Use Vietnamese templates with safe placeholder substitution such as recipient name or class name.
- Send through one `IEmailSender` port implemented by `GmailSmtpEmailSender` with MailKit; this boundary exists for architecture and does not imply multiple providers in scope.
- Keep SMTP credentials in secure configuration.
- P0 is intentionally bounded to small synthetic/demo audiences and uses direct controlled individual sending with bounded pacing. A background job/queue, scheduled delivery, and large-audience operational controls are P2.
- Prevent accidental duplicate sends with an idempotency key or equivalent send-command identifier.

### 9.3. Tracking and Privacy

- Persist the message, creator, audience definition, recipient snapshot, timestamps, and status per recipient (`Pending`, `Sent`, or `Failed`).
- Record failure categories without exposing secrets or unnecessary personal data.
- If the P1 operator-retry workflow is promoted, retry only failed recipients and never resend successful delivery records.
- Sanitize HTML in every active composition path. Validate attachment type/size only when the P2 attachment capability is separately promoted; P0 accepts no email/announcement attachments.
- Audit school-wide sends and do not log message bodies or email addresses unnecessarily.

The password-help workflow never sends a reset link, code, temporary password, or existing password by email. Gmail SMTP is used only for authorized school announcements/notifications required by this project.

### 9.4. Fixed Gmail SMTP and App-Password Contract

- The only email transport in scope is Gmail SMTP through MailKit: `smtp.gmail.com`, port `587`, STARTTLS, the full Gmail sender address as username, and a Google App Password as password. Do not add Mailpit, SMTP relay, OAuth2, Microsoft 365, SendGrid, or a provider-selection UI/configuration layer.
- Enable Google 2-Step Verification on a dedicated project sender account and create its App Password outside the repository. Never use the account's ordinary Google password.
- Only Backend owns `Smtp__UserName` and `Smtp__Password`. React and Flutter call Backend APIs and must never receive either value. The ignored root `.env` is the sole local source; `.env.example` contains placeholders only.
- `Smtp__FromEmail` must match the authenticated sender or an explicitly valid alias; use a stable Vietnamese sender name. Startup validation rejects an enabled configuration that is missing credentials, uses a non-Gmail host, does not use port `587`/STARTTLS, or has mismatched sender settings. Never print credentials or loosen TLS certificate validation.
- Development/demo sends must use synthetic or explicitly approved addresses. Autonomous QA uses one dedicated Gmail test mailbox and plus-address aliases with a unique run marker; it must never send to imported real users, arbitrary addresses, or a school-wide audience.

## 10. Domain Model Baseline

Introduce domain concepts only in the vertical slice that first needs them; do not model the entire roadmap before a client consumes it. Preserve these concepts and relationships when their P0 or promoted P1 slice is implemented:

- `User`, `Role`, `RefreshToken`, `PasswordHelpRequest`, temporary-password state/expiry, and account status/audit data. Use GUID primary keys consistently for ASP.NET Core Identity users/roles and domain entities; keep human codes separate.
- One `SchoolProfile`/configuration for the single-school deployment, containing safe school identity/contact/timezone settings without introducing multi-tenant scoping.
- `TeacherProfile`, `StudentProfile`, and `ParentProfile` linked to identity users.
- `SchoolYear`, `Semester`, `Class`, `Subject`, and teacher assignments.
- Student class enrollment with effective dates or school-year scope.
- Parent-student relationship supporting multiple guardians and multiple children.
- `TimetableEntry`/`ClassSession` with day/date, start/end time, class, subject, Teacher, room/online location, and optional preparation note.
- `SchoolEvent` with schedule, location, organizer, audience/publication state, and optional safe links. Stored event attachments exist only after an attachment-bearing event slice activates Section 10.1.
- `Club` and `ClubMembership`/join request with category, description, capacity/eligibility where used, and membership status.
- `AttendanceRecord` with date, session, status, recorder, and timestamps.
- `LeaveRequest` with requester, student, period, reason, decision, and reviewer.
- P1 `DailyComment`/delivery result with class/date, Student, Teacher, category/content, and timestamps; suggestion phrases are separately configurable reference data.
- P1 `HomeworkAssignment` with class/subject, due date, publication status, and attachments, plus `HomeworkSubmission`/attachment and grading result scoped to one Student.
- `Assessment`, `Grade`, and textual `Evaluation` with explicit score constraints.
- P0 `Announcement` plus audience and publication state; P1 may add text-first `FeedPost`. Announcement/feed attachments are P2.
- `EmailMessage`, recipient delivery records, and import batch/row-result records.
- When an attachment-bearing slice is active, `StoredFile` metadata plus explicit attachment links for its approved owners. P0 leave evidence is conditional, P1 homework activates assignment/submission storage, and feed/announcement storage is P2. Binary file contents are never stored in SQL Server.

Important rules:

- Enforce at most one attendance record per `(StudentId, AttendanceDate, Session)` and preserve who created/last changed it.
- Attendance sheets begin with an explicit Unmarked draft state; a bulk Present action is a user command, not an implicit database default. Define how an approved leave and an already-saved attendance record are reconciled without silently losing audit history.
- Leave-request transitions are explicit: `Pending -> Approved`, `Pending -> Rejected`, or `Pending -> Cancelled`; terminal decisions are not overwritten through ordinary edit endpoints.
- Homework transitions are `Draft -> Published -> Closed -> Archived`. Drafts are Teacher-only and editable; Students see only Published/Closed assignments intended for their class. Publishing is explicit and idempotent. Do not silently return a Published assignment to Draft; material post-publication changes require a documented revision and audience notification.
- Homework submission transitions are `NotSubmitted -> Submitted -> Graded`. While the assignment is Published, a Student may submit/resubmit before the deadline; each resubmission creates an auditable attempt version and exactly one current attempt. By default, a first submission after the deadline is accepted and marked `isLate` until the Teacher closes the assignment, but a late attempt cannot be replaced. Closed/Archived assignments reject submission. `isLate` is derived metadata, not a lifecycle transition.
- Daily-comment and homework/grade submission commands are idempotent. Define unique business keys for one Student assignment submission/attempt and one published daily comment per intended Student/date/category so retries cannot duplicate records.
- Initial grade/homework scores use the inclusive `0.0-10.0` scale with at most one decimal place. Keep the rule server-owned/configurable for a later school policy; empty, zero, absent, and not-yet-published are distinct states.
- When the P1 announcement read-state slice is promoted, read/unread state belongs to each recipient (or recipient membership), not one global `isRead` flag on the announcement.
- The relationship model supports multiple guardians per Student and multiple Students per Parent even if demo seed data starts with one child.
- Timetable, event, and club queries apply the same Student enrollment, Parent-child, and Teacher assignment scoping as other academic data.
- Club join/request commands are idempotent and enforce valid state transitions; repeated taps must not create duplicate memberships.
- Club membership transitions are `NotJoined -> Pending -> Active` or `Pending -> Rejected`; `Active -> Left` is allowed when policy permits. A new request after Rejected/Left requires an explicit policy and must not reactivate membership accidentally.
- Password-help request transitions are `Pending -> Resolved` or `Pending -> Rejected`. At most one Pending request exists per account; issuing a newer temporary password invalidates the older one. The plaintext temporary password is never persisted.
- Use UTC for persisted instants and convert to the configured school timezone for display. Store date-only school dates as date values, not UTC timestamps.
- Prefer status changes/soft deletion for records that must remain auditable.
- Add unique constraints and indexes for business identifiers and common relationship queries.
- Define optimistic concurrency behavior for frequently edited records such as attendance and grades.

### 10.1. Attachment Storage Contract

All user files cross the Backend API. Neither Flutter nor React may write to SQL Server, a server directory, or object storage directly.

This contract activates only when the current slice accepts user attachments: conditionally for P0 leave evidence, necessarily for the promoted P1 homework file path, or later for a separately promoted P2 media/announcement attachment path. Excel import uses its own bounded staging/batch path and does not by itself require the general `StoredFile` attachment API. Do not scaffold `StoredFile`, upload endpoints, cleanup jobs, or client pickers before an active consumer needs them.

- SQL Server stores only file metadata and ownership/association data: server-generated file ID, opaque storage key, sanitized original display name, server-detected content type, byte length, SHA-256 hash, uploader, timestamps, lifecycle/scan state, and the owning domain record/attachment order. Never store a client path, public URL, base64 payload, or raw file bytes in a domain table.
- The first active attachment slice uses `LocalFileStorage` behind an Application-level `IFileStorage` port. It writes under the absolute `Storage__LocalRoot` supplied by Backend configuration on the local Windows machine. The path must be outside the repository, `wwwroot`, build output, and source folders. The API must fail readiness when storage is enabled but the configured root is absent, non-absolute, inside the repository/public web root, or not writable; disabled storage must not block unrelated slices.
- Store bytes under server-generated opaque keys, never the original filename. Prevent traversal and symlink/reparse-point escape by resolving and verifying every final path remains below the configured root. Do not expose the storage root as static files.
- `POST /api/v1/files` performs an authorized pending upload for a declared purpose, streams to a temporary file with bounded size, validates extension plus detected content/MIME, computes the hash, then atomically moves the accepted bytes into storage and returns an opaque file ID. The subsequent leave/homework/announcement command supplies owned pending file IDs; Backend rechecks uploader, purpose, type, size, count, scope, and expiry before attaching them transactionally.
- A failed domain submit leaves valid uploads pending so the client can retry without re-uploading. Pending files expire after a configurable default of 24 hours and are removed by a safe bounded cleanup job. Cleanup must never delete attached/auditable files or follow untrusted paths.
- `GET /api/v1/files/{fileId}` re-authorizes access from the associated domain record on every request and streams with safe `Content-Type`, `Content-Length`, and sanitized `Content-Disposition`. Do not return permanent public filesystem/object URLs. Unsupported inline content is downloaded; only explicitly supported safe PDF/JPG/PNG previews may render in clients.
- Leave evidence is linked to the `LeaveRequest`; Teacher assignment material is linked to `HomeworkAssignment`; every Student upload is linked to the exact auditable `HomeworkSubmission` attempt. Resubmission creates a new attempt and file links without overwriting prior bytes or metadata.
- Physical deletion follows retention/audit policy. Removing an attachment from an unsubmitted draft may delete it after it becomes orphaned; submitting, grading, approving, rejecting, closing, or archiving must not silently erase evidence.
- External object storage is Deferred/P2 for this single-instance assignment. If later adopted, implement the same `IFileStorage` contract and authorization semantics; do not change public DTOs to provider URLs or let clients bypass Backend.

## 11. Technology and Architecture

Use this initial source layout unless compatible existing code already establishes an equivalent structure. Do not create multiple competing architectures:

```text
backend/
  MyFSchool.sln
  src/
    MyFSchool.Domain/
    MyFSchool.Application/
    MyFSchool.Infrastructure/
    MyFSchool.Api/

frontend-web/
  src/
    app/          # bootstrap, router, providers, shell
    features/     # feature-owned pages/components/hooks
    api/          # shared HTTP client and generated/manual DTO boundary
    shared/       # reusable UI, utilities, constants

myfschoolse1910/
  lib/
    app/          # bootstrap, router, theme, app-scoped providers
    core/         # networking, auth/session, errors, storage, shared utilities
    features/     # feature-first presentation/application/data code
    shared/       # reusable design-system widgets only
```

- Domain has no dependency on Application, Infrastructure, or API. Application depends on Domain abstractions. Infrastructure implements Application/Domain ports. API is the composition/transport boundary.
- React and Flutter features may use `shared`/`core` and the typed API boundary but must not import another feature's private internals. Promote genuinely shared code deliberately; do not create a generic dumping folder.
- Keep one application entry point, one router, one authenticated-session owner, one theme/token source, and one configured API client per frontend.

### 11.1. Flutter Client

- Flutter with Dart 3 or later.
- Feature-first structure, for example `lib/features/attendance/` and `lib/features/homework/`.
- `flutter_riverpod` for state and dependency management.
- `go_router` for guarded, role-aware navigation and deep links.
- `dio` for API access through a centralized client with token refresh and consistent error mapping.
- Follow Section 5.1 exactly: keep the access token in memory and the rotating refresh token in platform secure storage; never persist either token in plain `SharedPreferences`, URLs, or logs.
- Read API base URLs and environment-specific values through the allowlisted root configuration flow in Section 2.1. Do not hardcode a developer machine URL in feature code.
- Flutter never connects directly to SQL Server and must not include a SQL Server driver. All domain reads/writes and file transfers go through the authenticated Backend API.
- SQLite/Drift and other persistent local domain databases are not part of P0/P1. Use Riverpod-managed in-memory server-state caching, platform secure storage only for the refresh token, and `SharedPreferences` only for non-sensitive presentation preferences. On cold start, fetch authoritative domain data from Backend and show explicit loading/offline/retry states; do not claim offline-first behavior.
- An offline-first local database/sync queue is Deferred/P2 and requires a separate explicit design for per-user encryption, schema migration, TTL/freshness, logout/account-switch wipe, queued-command idempotency, conflict resolution, and attachment cleanup before adding SQLite/Drift.
- Use `image_picker`, `file_picker`, `table_calendar`, or a viewer package only when the current feature requires it.
- Keep widgets focused on presentation; place use cases, validation, repositories, and DTO mapping outside widgets.
- For roster editors such as attendance, daily comments, and grade entry, key row state by stable Student ID and update granular provider state so editing one row does not reset or unnecessarily rebuild the whole list. Dispose text controllers and focus nodes with the feature lifecycle.
- Follow the supplied mockups and the design tokens in Section 7.
- Use `snake_case.dart` file names and PascalCase Dart types consistently.
- If a phase uses mock data, inject a fake repository/data source behind the same typed interface used by the API implementation. Do not put global mutable mock lists directly in widgets.

### 11.2. ASP.NET Core API

- ASP.NET Core on .NET 10 LTS with `<TargetFramework>net10.0</TargetFramework>`, Entity Framework Core 10, and SQL Server.
- Use a supported .NET 10 SDK and current compatible patch releases. Keep Microsoft ASP.NET Core, EF Core, and `Microsoft.Extensions.*` package major versions aligned with .NET 10 unless official compatibility documentation requires otherwise; do not mix earlier major-version packages into the initial solution.
- If a `global.json` is added, pin an available .NET 10 SDK feature band deliberately and choose an appropriate roll-forward policy; do not pin a machine-specific preview SDK.
- Use the four-project layered structure defined above with clear Domain, Application, Infrastructure, and API boundaries; do not scaffold a second N-Tier/vertical-slice solution alongside it.
- Use ASP.NET Core Identity for account management.
- EF Core already provides repository/unit-of-work behavior; introduce custom repositories only for meaningful domain queries or test boundaries.
- Do not add MediatR/CQRS in the initial implementation. Use explicit Application services/use cases and add another dispatch abstraction only after an explicit user decision based on demonstrated complexity.
- Expose versioned REST endpoints and OpenAPI documentation.
- Use request DTOs, server-side validation, centralized exception handling, structured logs, and consistent problem details.
- Support pagination/filtering for lists and cancellation tokens for I/O operations.
- Database migrations belong in source control; never auto-reset or silently seed a non-development database.

### 11.3. React Web Portal

- `frontend-web/` uses React, TypeScript, and Vite. Use stable mutually compatible releases selected at scaffold time and commit the package-manager lockfile; do not silently replace React with Next.js, Angular, Vue, Blazor, or another web framework.
- Use Ant Design for the data-heavy administration UI; do not mix multiple component systems without an explicit user-approved reason.
- Use React Router for route guards/layouts, TanStack Query for server state/cache invalidation, and React Hook Form with Zod for forms/client validation. Do not introduce Redux or a second server-state/form system unless a documented requirement cannot be met by this stack.
- Use a typed API layer, protected routes, server-derived permissions, reusable form validation, and accessible responsive layouts.
- Keep authenticated session/role/capabilities in one application auth provider/store; keep page-private form/view state within the page feature and durable filters in the URL. Do not use unrelated component-private variables or mutable module globals to simulate a cross-page session.
- Follow Section 5.1 exactly: keep the Web access token in memory and use an `HttpOnly`, `Secure`, appropriately `SameSite` refresh-token cookie. Use HTTPS and an explicit allowed origin/credential policy; never place access or refresh tokens in `localStorage`/`sessionStorage`.
- Bulk import and email-send screens must show preview/confirmation and clear partial/failure states.

## 12. API and Data Conventions

### 12.1. Canonical Wire Contract

- Base route is `/api/v1`. Use plural kebab-case resources such as `/api/v1/leave-requests`; do not invent a second `/api`, GraphQL, or feature-specific versioning convention.
- Use controller-based REST endpoints with explicit request/response DTOs; do not expose EF entities directly. Use normal resource verbs for CRUD and explicit action endpoints only for lifecycle commands such as `POST /leave-requests/{id}/approve`.
- Entity IDs exposed by the API are server-generated GUID strings. Human-facing codes such as StudentCode/ClassCode remain separate stable unique fields; never use a display name as an identifier.
- The Backend OpenAPI document is the public contract. The initial clients use manually maintained typed DTOs/models behind one centralized API layer; do not call `fetch`, `axios`, or Dio directly from pages/widgets. Do not introduce client generation midway unless the user explicitly approves one generator for both client workflows.
- JSON property names use `camelCase`. Wire enums use the exact lower-camel English values in Section 12.2; never serialize enum ordinals or Vietnamese display labels.
- Date-only values use `YYYY-MM-DD`; time-only school values use `HH:mm:ss`; persisted instants cross the wire as ISO 8601/RFC 3339 UTC with `Z`. The configurable default school timezone is IANA `Asia/Ho_Chi_Minh`; clients format for display but do not reinterpret date-only values as UTC instants.
- A paginated response has exactly `{ items, page, pageSize, totalCount, totalPages }`. `page` is one-based, default `1`; `pageSize` defaults to `20` and has maximum `100`. Invalid paging/filter/sort input returns validation details rather than silent clamping, except an omitted value uses its documented default.
- Lists return `[]`, not `null`. Omitted/nullable fields must have documented semantics; never use empty string, zero, `0001-01-01`, or a fabricated object as a missing-value sentinel.
- Use direct typed success DTOs. Errors use RFC 7807 `ProblemDetails`/`ValidationProblemDetails` with stable English `code`, correlation `traceId`, Vietnamese safe `title/detail`, and field-keyed `errors` for validation. Never return stack traces, SQL errors, or exception messages to clients.
- Use standard status semantics consistently: `200` read/update/action result, `201` create with location where appropriate, `204` successful no-body delete/action, `400` malformed/validation, `401` unauthenticated, `403` authenticated but forbidden, `404` absent or intentionally concealed scoped resource, `409` lifecycle/duplicate/concurrency conflict, `413` upload too large, and `429` rate limited.
- Frequently edited DTOs expose a base64 `rowVersion` concurrency token. The client returns it on mutation; stale writes return `409` with code `concurrencyConflict` and current-safe context instead of last-write-wins unless a feature explicitly documents another policy.
- Retryable commands that can duplicate effects—import commit, announcement/email send, leave/homework submission, attendance batch save, grade batch save, club join—accept an `Idempotency-Key` header or equivalent explicit command ID and persist/detect the result server-side.
- Validate files on both client and server; server validation is authoritative. Validate content signature/MIME, extension, size, authorization, and ownership before persistence.
- Avoid logging access tokens, credentials, plaintext temporary passwords, national IDs, raw import rows, uploaded contents, or unnecessary personal data. Return/correlate a safe `traceId` instead.

### 12.2. Canonical Status Vocabulary

Use these internal/wire values and map them to the listed Vietnamese UI. Do not create synonyms in another feature:

| Domain | Wire values | Vietnamese UI |
| --- | --- | --- |
| Role | `administrator`, `teacher`, `parent`, `student` | `Quản trị viên`, `Giáo viên`, `Phụ huynh`, `Học sinh` |
| School session | `morning`, `afternoon` | `Buổi sáng`, `Buổi chiều`; a full-day leave selects both rather than creating a third attendance session |
| Guardian relationship | `father`, `mother`, `guardian`, `other` | `Cha`, `Mẹ`, `Người giám hộ`, `Khác` |
| Attendance | `unmarked`, `present`, `late`, `excusedAbsence`, `unexcusedAbsence` | `Chưa đánh dấu`, `Có mặt`, `Đi muộn`, `Nghỉ có phép`, `Nghỉ không phép` |
| Leave request | `pending`, `approved`, `rejected`, `cancelled` | `Đang chờ`, `Đã duyệt`, `Từ chối`, `Đã hủy` |
| Homework | `draft`, `published`, `closed`, `archived` | `Bản nháp`, `Đã giao`, `Đã đóng`, `Lưu trữ` |
| Homework submission | `notSubmitted`, `submitted`, `graded`; separate `isLate` boolean | `Chưa nộp`, `Đã nộp`, `Đã chấm`; additional badge `Nộp muộn` |
| Club membership | `notJoined`, `pending`, `active`, `rejected`, `left` | `Chưa tham gia`, `Chờ duyệt`, `Đã tham gia`, `Bị từ chối`, `Đã rời` |
| Delivery | `pending`, `sent`, `failed` | `Đang chờ`, `Đã gửi`, `Gửi thất bại` |
| Delivery channel | `email`, `portalApp` | `Email`, `Ứng dụng` |
| Password-help request | `pending`, `resolved`, `rejected` | `Đang chờ`, `Đã xử lý`, `Không xác minh được` |

If a new state is genuinely required, update Domain rules, OpenAPI/DTOs, every actual consuming client mapping, filters/chips, transition validation, and QA fixtures/scenarios in the same slice. Never repurpose an existing state to mean something else.

### 12.3. Contract Change Discipline

- Change Backend DTO/OpenAPI and server validation first, then update every actual consuming client and UI mapping in the same bounded slice. A client that does not call, model, route to, or depend on the changed contract is `Not affected`; record the concrete reason instead of modifying it speculatively.
- Treat property rename/removal, type/nullability change, new required field, enum change, validation/status-code change, and authorization/scope change as breaking for every actual consumer until affected clients and QA prove compatibility. Shared authentication/session, global error, public configuration, and cross-role authorization contracts normally affect both clients.
- Do not keep duplicate old/new fields indefinitely. If a temporary compatibility window is explicitly required, document its removal condition and test both forms externally.
- Contract examples and seed fixtures use synthetic Vietnamese data and must conform to the same validators as real requests.
- Use optimistic concurrency or an explicit documented alternative for attendance and grade editing; never silently overwrite a newer value.

### 12.4. Initial Configurable Defaults

These are implementation defaults, not scattered client constants. A default is enforced/exposed only when its capability is active. The Backend is authoritative and exposes effective safe client settings through a typed public configuration/metadata response or the relevant form-init response:

- School timezone: `Asia/Ho_Chi_Minh`.
- Score scale: `0.0-10.0`, maximum one decimal place.
- Pagination: page `1`, page size `20`, maximum `100`.
- Excel import: `.xlsx`, maximum `10 MB` per workbook.
- Leave evidence and homework submission: PDF/JPG/PNG, maximum `5 MB` per file and `3` files per submission.
- Pending unattached upload lifetime: `24 hours`; attached/auditable files follow their owning record's retention policy.
- P2 feed image when promoted: at most `1` JPG/PNG image in the first media slice, maximum `5 MB`.
- P2 announcement/email attachments when promoted: approved safe types, maximum `5` files and `10 MB` total before encoding/provider overhead.
- Access token: `15 minutes`; rotating refresh token: `7 days`; generated temporary password: `24 hours` and forced change on first use.

Changing a default requires Backend validation/configuration, every actual consuming client display/validation, OpenAPI/metadata, and QA boundary cases in the same slice. Client validation improves feedback but never replaces server enforcement.

## 13. Quality, Security, and Accessibility

- Follow OWASP guidance for authentication, authorization, file upload, HTML sanitization, and secret handling.
- CORS uses an explicit allowlist of actual Web origins. Never combine wildcard origins with credentials. Validate `Origin`/same-site protections on cookie-bearing refresh/logout requests and add anti-forgery protection before any general state-changing cookie-authenticated endpoint is introduced.
- Apply appropriate Web security headers, including a deployable Content Security Policy, clickjacking protection, MIME sniffing protection, and a conservative referrer policy; do not weaken them solely to make embedded preview content work.
- Validate uploads by content as well as extension; enforce configurable size limits and store them outside the public web root.
- Apply least privilege to database, SMTP, and file-storage credentials.
- Add audit records for login, password-help request resolution, temporary-password issuance, forced password change, role or relationship changes, imports, bulk sends, attendance edits, and grade edits. Never include plaintext temporary passwords in audit data.
- Verify domain rules, authorization decisions, authentication, scoped data access, import commit behavior, P0 email send idempotency/current results, and the core demo journeys through the disposable QA harness in Section 14. Verify failed-recipient operator retry only after that P1 workflow is promoted.
- Prefer black-box contract and end-to-end checks against built/running product artifacts. Temporary source-level tests are allowed only under `qa/`; the three product directories must not acquire test projects, test directories, test dependencies, or test-only endpoints.
- Coverage may be collected temporarily as a diagnostic, but a percentage is not a completion target. Required journeys and critical authorization/validation branches must be explicitly mapped to QA scenarios.
- Provide meaningful empty/error/loading states and follow the accessibility rules in Section 7.
- Support common mobile screen sizes and keyboard navigation for the web import and data-entry workflows.
- Use only synthetic data in tests, screenshots, demonstrations, and repository fixtures.

## 14. Autonomous Implementation and Disposable QA Harness

### 14.1. Goals and Non-Negotiable Boundaries

Agents may autonomously implement, run, diagnose, and repeat a requested bounded feature until its quality gate passes, subject to the loop limits in Section 14.7. Verification infrastructure must be detachable from the release source tree.

- Product directories are `backend/`, `myfschoolse1910/`, and `frontend-web/`. They contain production code, production configuration abstractions, and build dependencies only.
- All automated test code, runners, fixtures, fake infrastructure configuration, reports, screenshots, traces, videos, and test-tool dependencies live under one root-level `qa/` directory. Do not create `test/`, `tests/`, `__tests__/`, `integration_test/`, `*.spec.*`, `*.test.*`, or `*Tests.csproj` inside a product directory.
- Product projects must never reference `qa/`; do not add a QA project to a production solution/project reference graph. QA tools may consume public HTTP/OpenAPI/UI contracts and built APK/web/API artifacts.
- Do not add test-only controllers, bypass authentication, magic headers, fixed test users, fake production services, `if (test)` branches, or hidden data-reset endpoints to product code.
- Production-worthy capabilities such as health/readiness endpoints, environment-based configuration, structured logs, accessibility semantics, stable user-visible labels, migrations, and safe bootstrap operations may remain because they are operational product behavior, not test hooks.
- A normal development task does not delete QA assets. Destructive release cleanup happens only when the user explicitly requests release preparation.

### 14.2. Disposable QA Directory Contract

When the first coding slice requiring verification begins and this harness does not yet exist, creating the following independently removable shape is an authorized implementation step. Do not wait for a second request merely to create required QA; do not create it for a documentation-only task.

```text
qa/
  .gitignore
  README.md
  .env.e2e.generated  # ignored and deleted during teardown
  scripts/
    preflight.ps1
    up.ps1
    reset.ps1
    run-smoke.ps1
    run-full.ps1
    down.ps1
    verify-release-clean.ps1
  api/
    package.json
    playwright.config.ts
    tests/
  web/
    package.json
    playwright.config.ts
    tests/
  mobile/
    config.yaml
    flows/
    common/
  fixtures/
    excel/
    uploads/
    expected/
  artifacts/
    <runId>/
      implementation-plan.md
      checkpoint.md
      result-manifest.json
```

- `qa/api/` uses Playwright `APIRequestContext` or an equivalent external HTTP client for API contract, authentication, authorization, validation, idempotency, and cross-role setup/assertions.
- `qa/web/` uses Playwright against the built React web portal. Prefer role/name/label locators and web-first assertions; do not depend on CSS implementation details or arbitrary sleeps.
- `qa/mobile/` uses Maestro YAML flows against the built Android APK on an emulator. Reuse common login/navigation subflows and pass credentials/URLs through runtime environment variables. Use visible Vietnamese text and accessibility semantics as selectors rather than adding test-only widget keys.
- `qa/fixtures/` contains synthetic, deterministic, versioned input files only. Each dataset uses a unique run identifier so parallel/repeated runs cannot collide.
- `qa/artifacts/` is generated and ignored: redacted checkpoints, logs, API responses, screenshots, videos, Playwright traces, Maestro output, process manifests, and test reports. Never store credentials or plaintext temporary passwords in retained artifacts.
- `qa/.gitignore` excludes `artifacts/`, tool caches, installed packages, `.env.e2e.generated`, generated credentials, and temporary builds. Root `.env.example` is the sole tracked environment template; do not maintain a second QA template that can drift.
- Do not add the QA Node packages to `frontend-web/package.json`, Maestro/Flutter integration dependencies to `myfschoolse1910/pubspec.yaml`, or QA packages to backend production projects.
- `preflight.ps1` is read-only apart from its ignored diagnostic output. `reset.ps1` operates only on the exact active run manifest: stop owned child processes, drop/recreate only the exact run database, clear only the exact run storage directory, reapply migrations/fixtures, and restart readiness. It must refuse to run without a valid manifest and safe run ID.
- `run-smoke.ps1` and `run-full.ps1` call the same lower-level gates used during iteration, propagate the first non-zero exit code, and still invoke exact teardown through `finally` when they own the environment. Never use `try/catch`, PowerShell preferences, shell chaining, or wrapper scripts to turn a failing child command into exit `0`.
- Every script prints concise phase/command names, start/end timestamps, duration, and final status while redacting secrets. All external waits have explicit deadlines and periodic progress; no script waits forever or prompts for interactive input during an autonomous run.

The repository currently contains Flutter's generated `myfschoolse1910/test/widget_test.dart` and `flutter_test` development dependency. Treat them as removable scaffold residue: do not expand them. Remove them during explicit release cleanup, or earlier when the external QA harness is established and their removal is part of the requested task.

### 14.3. Ephemeral Local Integration Environment

The QA harness runs directly on the local Windows machine. It requires a reachable local SQL Server Developer/Express/LocalDB-compatible instance, .NET 10 SDK, the locked Node toolchain, Flutter/Android tooling, and a dedicated Gmail test account with Google App Password when the email gate is in scope. Use these host tools directly without introducing another infrastructure runtime.

Before starting processes, `qa/scripts/up.ps1` performs only the steps required by the selected tier and affected-product map:

1. Checks for an existing run manifest. Resume it only when its recorded PIDs/start times, database name, storage path, ports, and artifact/worktree identity still match and readiness passes; otherwise run exact safe teardown before creating a new run. Never start a second concurrent harness accidentally.
2. Loads the repository-root `.env`, validates only settings required by the affected gate, creates a unique safe run ID, reserves explicit API/Web ports, and writes resolved non-committed inputs to `qa/.env.e2e.generated`.
3. Connects through `QA_SQLSERVER_ADMIN_CONNECTION`, creates only a uniquely named database such as `MyFSchool_QA_<safeRunId>`, derives the run-specific application connection string, and applies normal EF Core migrations. It must refuse to continue if the target name lacks the exact QA prefix/run ID or collides with an existing non-owned database.
4. When an attachment/file gate is affected, creates a unique attachment directory under the operating-system temporary directory, not inside the repository, and supplies it as the run's absolute `Storage__LocalRoot`; otherwise leaves storage disabled.
5. Starts the published ASP.NET Core API whenever an API-backed consumer/integration gate is affected. Starts the built React static server only for an affected Web or named cross-client gate. Record exact PIDs/start times/commands and redirected log paths in a run manifest, and wait on bounded HTTP readiness probes; a process existing is not sufficient readiness.
6. Builds/installs Flutter only for an affected Mobile or named cross-client gate, using the same API exposed through an emulator-safe host such as `10.0.2.2`. Do not require Node/Web, an emulator, Gmail, or attachment storage for a gate that does not consume it.

Provision the initial synthetic Administrator through the documented production-safe Development/QA bootstrap command established in the Executable Skeleton; it must be environment-gated, idempotent, non-HTTP, and must not log/retain plaintext credentials. Before P0B exists, use that same safe path/API setup to create the minimum accounts needed for P0A tests without depending on unfinished import. After the fixed Excel import slice passes, prefer import plus normal APIs for Teacher/Parent/Student/class relationships because that verifies the required workflow. Generate runtime passwords, temporary passwords, database names, and run markers per run; never hardcode or retain them in source/artifacts.

Email verification uses the fixed Gmail transport in Section 9.4:

- `QA_GMAIL_RECIPIENT` must be a dedicated test mailbox controlled by the developer. Test fixtures derive unique plus-address aliases and subjects from the run ID; they never use real school addresses.
- The external QA mail checker connects to Gmail IMAP over TLS with the same dedicated account and Google App Password, waits with a bounded timeout for exact run-marked messages, and verifies subject/recipient/count without recording bodies or credentials. It may delete only messages containing the exact current run marker during `finally` cleanup.
- If Gmail credentials/connectivity are missing while the email feature is an affected gate, report the gate blocked; do not silently replace Gmail with a fake/capture server, skip the scenario, or claim delivery from a database status alone.

Whenever the harness owns resources, run `qa/scripts/down.ps1` in a `finally` path. It verifies the run manifest, stops only child PIDs whose executable/start time/command still match, drops only the exact run-owned QA database after revalidating its name, deletes an attachment directory only when the manifest records one for this run, deletes the generated environment file, and leaves unrelated SQL databases, processes, files, and emulator data untouched. Never use broad process kills, SQL wildcard drops, or recursive cleanup against an unresolved path.

### 14.4. Test Layers and Order

Run the cheapest deterministic checks first and stop the gate on failure:

1. **Static/build gate:** format, analyze/lint, restore, and production-build each touched product. Before a cross-client or release gate, build all three products from the same worktree revision.
2. **Infrastructure gate:** create only the isolated database/storage resources required by the affected scenario, start only required API/Web processes, and wait for bounded readiness checks; validate Gmail connectivity only when email is affected.
3. **API integration gate:** for an affected Backend/API path, verify health plus the applicable authentication, authorization, validation, concurrency, idempotency, import, email, and feature scenarios. Do not rerun unrelated API suites during a narrow slice.
4. **Web E2E gate:** when Web is affected, run the relevant Administrator/Teacher browser journey against the real API/database, initially on Chromium; add a smaller cross-browser smoke matrix only after Chromium is stable.
5. **Mobile E2E gate:** when Mobile is affected, install the newly built APK, clear app state for each independent scenario, and run the relevant Maestro flow against the same real API/database.
6. **Cross-client journey gate:** only when required by Section 14.4.1/14.4.2 or the active phase gate, verify a mutation made by one client becomes visible with correct authorization and state in another client.
7. **Evidence gate:** retain redacted failure artifacts and a concise result manifest until diagnosis or release cleanup.

Do not replace the API with route mocks in end-to-end gates. Network mocking is acceptable only for an explicitly isolated UI-state check and must not be counted as backend/frontend/mobile integration coverage.

#### 14.4.1. Three-Product Coverage Matrix

The complete project QA strategy covers all three products, but a bounded slice verifies only touched products and actual affected consumers. Do not build or test an unrelated client merely because it exists. A full three-product gate is mandatory only for shared authentication/session/public-configuration/error behavior, a journey intentionally crossing React and Flutter, an explicitly named milestone gate, or final release verification.

- **`backend/` (.NET 10):** restore with a .NET 10 SDK, verify every project targets `net10.0`, build in `Release`, apply EF Core migrations to a fresh SQL Server QA database, start the real API, then run external health/OpenAPI/authentication/authorization/validation/idempotency/integration scenarios. A .NET build alone is not an API gate.
- **`frontend-web/` (React):** perform a clean locked dependency install, TypeScript type-check, lint, production Vite build, serve the built output, and run Playwright against the real QA API/database. A mocked API or Vite development server alone does not satisfy the cross-client gate.
- **`myfschoolse1910/` (Flutter):** restore packages, format-check, analyze, build a production-representative Android APK with the QA API URL supplied externally, install it on a clean emulator state, and run Maestro flows against the same QA API/database. Widget rendering alone does not satisfy the mobile E2E gate.
- **Shared contract trigger:** a change to endpoint path, method, authentication, authorization, request/response DTO, enum, validation code, pagination, file contract, or OpenAPI schema marks Backend and every actual consumer as affected. Update only consuming typed clients/mappers and run their relevant scenarios. Authentication/session/global-error/public-config changes normally affect both clients; a feature-specific Web-only or Mobile-only endpoint does not.
- **Artifact identity:** the result manifest records the Git/worktree identity when available, .NET SDK/runtime and API artifact, Node/package-manager and Web build, Flutter/Dart SDK and APK checksum, migration version, QA run ID, and scenario results. Do not combine results from unrelated builds and call them one passing full-stack run.

#### 14.4.2. Change-Impact Decision Table

Use this table mechanically. `Required verification` is the minimum, not permission to ignore an observed failure elsewhere.

| Change category | Product classification | Required verification |
| --- | --- | --- |
| Documentation or mockup-reference edit only | All `Not affected` | Markdown/path/consistency checks; no product build required |
| Backend internal refactor with unchanged observable contract | Backend `Touched`; clients `Not affected` only with evidence | .NET Release build plus targeted API integration; run a consumer smoke if behavior could change |
| Database entity/migration/query change | Backend `Touched`; clients `Affected` when DTO/behavior changes | Clean migration on fresh SQL Server, API integration, affected Web/Mobile journeys |
| Shared authentication/session, relationship/resource authorization, or global error-contract change | Backend plus every supported actual client `Affected` (normally all three) | Backend + affected Web/Flutter builds, API security scenarios, and relevant role journeys in each actual client |
| Web refresh-cookie, CORS, origin, or anti-forgery behavior with unchanged native token contract | Backend and Web `Affected`; Flutter `Not affected` with evidence | Backend/Web builds, cookie/origin/security API checks, and targeted Web E2E; no Flutter gate unless the native contract also changed |
| Endpoint/DTO/enum/nullability/validation/pagination/file/OpenAPI change | Backend `Touched`; actual consuming clients `Affected consumers`; non-consumers `Not affected` with reason | Backend build/integration plus every consuming client build/journey; full three-product gate only for a genuinely shared contract |
| React/Flutter copy or isolated styling with no navigation/state/contract change | Owning client `Touched`; Backend/other client `Not affected` | Owning client format/type/analyze and build plus narrow semantic/visual comparison; no SQL/API/Gmail, though an affected Mobile visual check may use an emulator |
| React-only navigation or local form behavior with unchanged API | Web `Touched`; Backend API dependency; Flutter `Not affected` with reason | Web type-check/lint/build, targeted Playwright positive/invalid state, API readiness/contract smoke only when the page consumes it |
| Flutter-only navigation or local form behavior with unchanged API | Flutter `Touched`; Backend API dependency; Web `Not affected` with reason | Flutter format/analyze/APK, targeted Maestro positive/invalid state, API readiness/contract smoke only when the screen consumes it |
| Shared business journey or role permission change | All participating products `Affected` | Three-product integration gate and every affected role path |
| Dependency/runtime/build/configuration change | Product owning the manifest `Touched`; consumers affected if runtime contract changes | Clean restore/install and production build; targeted smoke, then cross-client gate when connectivity/contract/security changes |
| QA-harness-only change | QA `Touched`; product code `Not affected` | QA preflight plus a known passing smoke and a deliberate safe failure proving diagnostics/exit code; no product manifest mutation |

When uncertain between two rows, choose the broader verification row. The agent may narrow it only after recording concrete evidence in the slice contract.

#### 14.4.3. Slice Verification Tiers

Choose the smallest tier that satisfies Section 14.4.2; an observed failure outside the chosen minimum still must be investigated.

| Tier | Applies to | Required minimum |
| --- | --- | --- |
| `Small` | Documentation, requirement/mockup reference, copy, isolated styling, or behavior-preserving local refactor with no shared contract/security/data change | Structural/format/static checks and the narrow affected UI or consistency check; no SQL/API/emulator/Gmail environment unless directly needed |
| `Standard` | One product behavior with unchanged shared authentication/authorization/data contract, or one feature-specific Backend contract with known consumers | Touched product build, targeted external positive plus invalid/forbidden scenario, and builds/journeys for actual consumers only |
| `Critical` | Authentication/session, authorization/resource scope, database migration, shared API/error/config contract, import commit, file access, email delivery, or intentional cross-client state | Full affected-product integration, security/invalid paths, migration/environment checks, and named cross-client scenario |

Milestone/release gates aggregate their completed slices and may require all three products. A `Small` or `Standard` slice must not be escalated to `Critical` solely because optional tooling or an unrelated product is unavailable.

### 14.5. Required Cross-Client Scenarios

Build these scenarios incrementally. Scenarios 1-3 and 6-8 are P0 only to the depth defined below; scenarios 4, 5, and 9 are added when their P1 slices are promoted:

1. Administrator imports synthetic Teacher/Parent/Student/class relationships in Web; those accounts authenticate through the API and see only scoped Flutter data.
2. Teacher records attendance in Flutter; Parent and Student Flutter views show the correct status while unrelated users receive `403`/not-found behavior according to the API contract. React Web attendance editing is not a P0/P1 workflow.
3. Parent submits a leave request in Flutter; the assigned Teacher reviews/decides it, an unrelated account cannot access it, and the Parent sees the decision; attendance reconciliation follows Section 7.8.3. When leave evidence is enabled in the active P0D slice, extend this same scenario with authorized upload/download and unrelated-user file denial; otherwise evidence is not a P0 gate.
4. **Promoted P1 homework:** Teacher publishes homework with an attachment; an intended Student can download it and submits text plus an attachment in Flutter; the Teacher can open the exact attempt and grade it; Student and linked Parent see only the authorized result, while an unrelated Student cannot access either file.
5. **Promoted P1 grade entry:** Teacher saves grades; Student/Parent grade screens show the persisted values, while a concurrent stale save is rejected or handled by the documented policy. P0 validates read-only grade scoping using authorized synthetic/demo records without requiring this mutation path.
6. Administrator/Teacher sends a Portal App announcement and Gmail Email from Web to unique aliases of the dedicated QA mailbox; authorized Flutter inboxes receive the persisted announcement, Gmail IMAP confirms only the deduplicated run-marked messages, and no real school address is contacted.
7. Password assistance returns a generic pre-login response, creates at most one Pending request for a matching account, lets only an Administrator issue a one-time-visible temporary password, revokes old refresh tokens, blocks all protected routes during the restricted session, forces password change, rejects reuse/expiry, and allows a fresh normal sign-in afterward. No recovery email is sent.
8. Student submits a club join request twice; only one membership/request exists and the UI shows the resulting state.
9. **Promoted P1 daily comments:** Teacher sends one scoped comment to selected Students; retry does not duplicate successful recipients; the linked Parent and policy-eligible Student can read only the intended record while unrelated accounts cannot.

Each scenario owns its data or uses a fresh database snapshot. Tests must not depend on execution order unless the file represents one named cross-client journey; setup must make that dependency explicit.

### 14.6. Determinism, Diagnostics, and Anti-Flakiness Rules

- Use explicit readiness polling, Playwright web-first assertions, and Maestro visibility waits with bounded timeouts. Do not use fixed sleeps as synchronization.
- Freeze or inject the school clock only through a production-safe time abstraction when business rules genuinely depend on time; otherwise create data relative to the current configured school timezone.
- Keep browser contexts and user sessions isolated. Never reuse an Administrator storage state for Teacher/Parent/Student tests.
- Default automatic retries to zero. One diagnostic retry after environment reset is allowed; a pass only on retry is reported as flaky and does not satisfy the gate.
- On failure capture the failing step, client logs, server correlation ID, sanitized request/response metadata, screenshot/trace, relevant child-process log tail, and database/run ID. Do not dump secrets, temporary passwords, or unrelated personal data.
- Classify failures as Product, QA harness, Environment, or Flaky before editing. Never weaken an assertion, add a delay, skip a scenario, or change expected output merely to make a failure disappear.

A gate passes only when all of the following are true:

- Every required command exits successfully and the executed command list is recorded.
- No required scenario is skipped, filtered out, marked expected-to-fail, or passed only on retry.
- Assertions prove persisted business state and authorization, not merely that a page rendered or returned HTTP 200.
- Browser/app output contains no unexpected uncaught exception, console error, blank screen, or API `5xx`; known harmless warnings must be named and justified.
- The result belongs to the current artifact identities and QA run ID, and teardown completes without affecting unrelated resources.

### 14.7. Autonomous Coding and Verification Loop

For each user-requested bounded slice, the agent follows the smallest applicable path without waiting for routine confirmation. `Small` uses `INSPECT -> CONTRACT -> IMPLEMENT or allowed no-code action -> narrow STATIC_BUILD/check -> combined REVIEW_UNIT -> HANDOFF`; it skips environment, RED_QA, checkpoint, cross-client, and branch review states that do not apply. `Standard` and `Critical` use the full sequence below, but `CROSS_CLIENT` runs only when Section 14.4 requires it. `COMMIT_UNIT` runs only when Section 2.3 grants local-commit authorization; otherwise record a reviewed commit-ready boundary and leave Git history unchanged.

Use these states in order. A state is complete only when its exit evidence is written to the current checkpoint:

| State | Required action | Exit evidence |
| --- | --- | --- |
| `INSPECT` | Read this file, relevant code/mockup/API, Git baseline, existing checkpoint/run manifest, and QA coverage | Known baseline and no unexplained overlapping change |
| `CONTRACT` | Write the Section 0.3 slice contract and map change impact mechanically through Section 14.4.2 | Numbered acceptance checks and required gates |
| `PLAN` | For a non-trivial slice, write the exact-file, dependency-ordered implementation plan defined below and challenge it for scope/YAGNI | Every task maps to acceptance, RED scenario, commands, and atomic commit |
| `RED_QA` | For a new behavior/bug, add the smallest disposable black-box scenario and run it before production code; documentation/configuration and other allowed Section 14.7.2 exceptions record the reason instead | It fails for the expected missing/incorrect behavior, not a typo/environment error, or a valid exception is recorded |
| `IMPLEMENT` | Make the smallest production change for one acceptance unit; update contract/migration and affected typed clients together | Intentional diff reviewed; no test-only production branch |
| `STATIC_BUILD` | Format/analyze/lint and build touched/affected artifacts | Commands exit `0`; artifact identity recorded |
| `TARGETED_QA` | Start/resume the exact local harness and run the narrowest positive plus invalid/forbidden scenario | Persisted state and authorization assertions pass |
| `DIAGNOSE` | On failure, preserve evidence, fingerprint, reproduce once, classify, choose one root-cause hypothesis, and apply one bounded fix | A relevant state change exists before re-run |
| `REVIEW_UNIT` | For Standard/Critical units, run separate specification-compliance and code-quality/security reviews; a Small slice may combine them into one recorded self-review | No unresolved Blocker/Major finding; fixes reverified |
| `COMMIT_UNIT` | When Section 2.3 authorizes commits, apply it to the green acceptance unit/layer; stage explicit paths and review the complete index diff | Atomic local commit hash/subject/gates recorded; otherwise a commit-ready boundary is recorded without staging |
| `CROSS_CLIENT` | When required by the change-impact/phase gate, rebuild stale artifacts and run affected consumer journey against the same API/database | Required cross-client gate passes without retry/skip, or state is documented as not applicable |
| `REVIEW_BRANCH` | For Standard/Critical branch work, review the complete baseline-to-current diff/authorized commit range for integration gaps, scope drift, architecture, security, and missing verification | No unresolved Blocker/Major finding; any correction is reverified and committed only when authorized |
| `HANDOFF` | Compare final diff to baseline, teardown when no immediate follow-up run is needed, and complete Section 16 | Self-contained result/checkpoint with no unexplained resource or file |

Operational loop:

1. Execute `INSPECT`, `CONTRACT`, and when required `PLAN` once per bounded slice; do not code from the roadmap title alone. Run `qa/scripts/preflight.ps1` before broad edits to verify only affected prerequisites: the owning SDK/toolchain, SQL when a database/API integration gate is required, an emulator when Mobile E2E is required, storage when files are active, and Gmail only when Email is affected. A missing required prerequisite is discovered early and reported precisely, not after hours of unrelated implementation.
2. Break acceptance into the smallest dependency-ordered units. For each Standard/Critical new behavior/bug cycle `RED_QA -> IMPLEMENT -> STATIC_BUILD -> TARGETED_QA -> REVIEW_UNIT`, then `COMMIT_UNIT` when authorized; do not implement several unrelated screens before obtaining the first vertical passing path. Without commit authorization, keep each reviewed unit explicitly classified in the handoff rather than staging or mutating history.
3. On failure enter `DIAGNOSE`. Define the failure fingerprint as `{command/scenario, exit code or timeout, first stable error code/message, failing route/test step}`. Cosmetic timestamps, ports, GUIDs, and line numbers do not create a new fingerprint.
4. Reproduce once without editing. If the fingerprint is the same, investigate the earliest incorrect boundary/state, compare an equivalent working path, state one falsifiable root-cause hypothesis, and change only the smallest cause. Invalidate every affected prior result and return through the required RED/green gates. If reproduction passes, classify as Flaky and follow Section 14.6; it is not a pass.
5. After each reviewed green layer/unit, create its atomic local commit before starting unrelated work only when Section 2.3 authorizes commits. Run `CROSS_CLIENT` whenever the completed contract/shared journey crosses products, followed by `REVIEW_BRANCH` for Standard/Critical branch work. A page render, HTTP `200`, successful compilation, existence of commits, or reviewer opinion without commands is insufficient.
6. Enter `HANDOFF` only after every acceptance check maps to fresh evidence and branch review is clear. Otherwise report `incomplete` or `blocked`, never “mostly done” as complete.

Guardrails:

- Never loop indefinitely. Stop and report after three consecutive fix cycles with the same failure fingerprint, or immediately when blocked by missing credentials, unavailable external infrastructure, destructive authority, or a product decision that materially changes scope.
- Count a fix cycle only when a relevant file/config/environment state changed and the affected build/check ran. Repeated unchanged commands do not reset the counter and are prohibited by Section 0.4.
- A new failure caused by the latest fix must be addressed before broadening the slice. Do not accumulate unrelated refactors while chasing a test.
- Do not advance to another roadmap phase merely because the current loop passed.
- Do not delete the QA harness after an ordinary passing task; it remains available for regression checks until explicit release preparation.
- Use non-interactive commands with explicit bounded timeouts. Long-running servers run as recorded child processes; readiness is polled in short bounded intervals. A hanging command is an Environment failure with evidence, not permission to wait forever.

#### 14.7.1. Plan Contract for Non-Trivial Slices

A slice is non-trivial when it touches two or more products, changes database/API/authentication/authorization/file/email behavior, or contains more than one acceptance unit. Before production code, write `qa/artifacts/<runId>/implementation-plan.md`; a documentation-only or truly isolated one-file change may use only the Section 0.3 contract.

The plan is disposable operational guidance and must contain:

```text
Goal and numbered acceptance IDs:
Architecture/data-flow summary:
Exact files to create/modify and one responsibility per file:
Dependency-ordered tasks, each small enough for one coherent review:
For every task: acceptance ID | RED scenario/expected failure | minimal change | commands/gates | commit subject
Migration/API/client compatibility order:
Authorization, validation, idempotency, concurrency, failure-state checks:
Explicit out-of-scope/YAGNI list:
```

Review the plan before execution: every task must serve an acceptance check, file responsibilities must remain focused, and independent subsystems must be separate slices. Do not ask the user to approve routine implementation detail already fixed by this file; stop only when an unresolved choice materially changes behavior, data model, security, or scope. If implementation discovers a material plan error, update the plan/checkpoint and explain it before continuing; never silently drift.

#### 14.7.2. Disposable Acceptance-Test-First Contract

For every new behavior and bug fix, write the smallest observable external scenario under `qa/` before production implementation:

1. Name one behavior and assert real public output/state, not internal method calls or mock call counts.
2. Run it and observe RED. It must fail because the behavior is missing/wrong. A syntax error, unavailable environment, wrong selector, bad fixture, or unrelated failure is not valid RED; fix the scenario/harness until the expected failure is observed.
3. Implement only enough production behavior to make that scenario and existing affected scenarios pass. Do not add speculative options or later acceptance units.
4. Observe GREEN with clean output, then perform behavior-preserving refactoring only if needed and rerun GREEN.

Production code written before a required RED observation must not be treated as verified. Revert only the agent-owned premature change safely, establish RED, then implement from the acceptance contract; never discard user work. Every defect found during manual/reviewer/cross-client testing first receives a minimal failing regression scenario before its fix.

Allowed exceptions are generated scaffolding, documentation, pure configuration wiring, and behavior-preserving refactoring. Record the exception in the plan/checkpoint. Refactoring requires a passing characterization scenario before change and the same behavior afterward. UI layout additionally requires automated semantic/navigation/state assertions plus visual comparison to the approved mockup; a screenshot alone is not a behavior test.

This project intentionally uses disposable external acceptance tests rather than permanent product-internal unit-test projects. It is therefore acceptance-test-driven rather than strict method-level TDD, preserving the release rule that product directories contain no test code.

#### 14.7.3. Two-Stage Review Gate

Review occurs after targeted GREEN and before commit for each acceptance unit, then once across the entire branch after cross-client verification:

1. **Specification compliance:** compare only the current requirements/acceptance IDs, plan, API contract, mockup when relevant, diff, and evidence. Identify missing behavior, unauthorized extra scope, wrong role/resource access, dead states/navigation, or stale/unproven claims.
2. **Code quality and security:** only after specification compliance passes, inspect correctness, data integrity, authorization enforcement, error handling, concurrency/idempotency, secret/file safety, architecture boundaries, maintainability, accessibility, and verification quality.

When the platform supports subagents, use a fresh read-only reviewer context for each stage. Give it the precise requirement/plan, baseline and current diff/commit range, and verification summary—not the implementer's conversational reasoning. The reviewer must not edit files or accept claims without inspecting evidence. When independent review is unavailable, perform two explicitly separated self-review passes using the same checklists and record that the review was not independent.

Classify findings as `Blocker` (security/data loss/fundamental requirement failure), `Major` (correctness, authorization, integration, or maintainability defect that should block the unit), or `Minor` (bounded polish/non-blocking improvement). No unit/branch may commit or complete with an unresolved Blocker/Major. The implementer owns fixes, reruns affected RED/green/build/E2E gates, and requests/repeats the relevant review; reviewers do not patch their own findings. In-scope Minor findings are fixed or explicitly listed at handoff without being silently converted into Deferred product scope.

#### 14.7.4. Checkpoint and Resume Protocol for Long Runs

Checkpoint cadence follows the Section 14.4.3 tier. For a Critical slice, update after every state transition, failed command, and applied fix. For a Standard slice, update at acceptance-unit boundaries, failures, commits, and handoff/interruption. A Small slice needs no `qa/` checkpoint unless it becomes long-running or crosses a context handoff; its working update and final evidence are sufficient. When a checkpoint is required, overwrite `qa/artifacts/<runId>/checkpoint.md` with a concise redacted snapshot:

```text
Slice/acceptance unit:
Current state and next exact action:
Branch/baseline HEAD/status, atomic commit plan, and commits created:
Intentional changed paths plus preserved user-owned paths:
Plan path/current task and any recorded test-first exception:
RED scenario/command and expected observed failure:
Products/gates: Backend | Web | Flutter = pending | pass | stale | blocked
Last commands with exit codes/durations:
Current artifact identities/checksums:
QA run ID, database name, storage path, ports, owned PIDs:
Failure fingerprint/classification/hypothesis/fix-cycle count:
Acceptance checks passed/pending:
Unit/branch reviews: spec status | quality/security status | reviewer/fallback | unresolved findings:
Known limitations/blocker:
```

Never include `.env` values, connection strings, App Passwords, JWTs, temporary user passwords, email bodies, or real personal data. The checkpoint is operational memory, not proof by itself; command results and result manifest remain the evidence.

On resume, reload the instruction sections required by Section 0.5, then re-run Git status and validate the run manifest. Trust a recorded pass only when its artifact identity still matches current sources/config/migration/fixture inputs. Mark affected gates `stale` after any relevant change and continue from the first non-passing state; do not restart the whole project, repeat completed unaffected work, or claim completion from a stale checkpoint.

If new user edits appear during the run, compare them to the baseline. Preserve and continue when paths are unrelated; if they overlap the current slice or invalidate an assumption, stop before overwriting and report the exact overlap.

### 14.8. Release Cleanup: Zero Test Code in Final Source

When and only when the user explicitly requests final release preparation:

1. Run the full QA suite against clean production builds and save the redacted result manifest outside the repository if evidence must be retained.
2. Run the bounded local QA teardown: stop only manifest-owned child processes, drop only the exact run-owned QA database, remove only the exact run-owned temporary attachment directory, and delete the generated QA environment file.
3. Remove QA-only entries such as `QA_SQLSERVER_ADMIN_CONNECTION` and `QA_GMAIL_RECIPIENT` from the tracked root `.env.example` and any tracked root launcher documentation/configuration. Never open, print, delete, or rewrite the ignored real `.env` automatically; tell the user which obsolete local QA keys they may remove manually.
4. Remove the root `qa/` directory, QA-only CI jobs/workflows, QA scripts/configuration, generated reports, traces, screenshots, videos, fixture data, and temporary environment files.
5. Remove test directories/files from all product trees, including Flutter scaffold tests, `integration_test/`, `test_driver/`, `test/`, `tests/`, `__tests__/`, `*.test.*`, `*.spec.*`, snapshots, coverage, and `*Tests.csproj`.
6. Remove test-only manifest dependencies/scripts and lockfile entries: for example `flutter_test`/`integration_test`, Playwright/Vitest/Jest packages in a product package, and test projects or runners in solution files. Do not remove analyzers, lints, secure configuration, health checks, logs, semantics, or migrations merely because QA used them.
7. Run a release-clean verifier that scans the three product directories and root manifests/solutions for forbidden test files, test project references, test-only dependencies, QA environment names, fixed QA credentials, and test-only branches/endpoints.
8. Re-run restore, analyzer/linter, and production builds after cleanup. Inspect `git diff`/`git status` and report exactly what was removed; do not claim a clean release if generated QA files or test dependencies remain.

Before any deletion, enumerate and resolve every exact target under the repository root. Do not use broad recursive globs or delete unrelated user-authored files merely because a name contains `test`; confirm from content/manifest references that each target is QA-only.

The release-clean verifier is a temporary QA tool and is deleted last, after it has written its final result outside the repository. Release artifacts must be built after cleanup so no stale test dependency can be packaged.

### 14.9. Methodology References

When implementing the harness, prefer current official documentation and verify version-specific commands:

- Playwright best practices and isolated browser contexts: `https://playwright.dev/docs/best-practices` and `https://playwright.dev/docs/browser-contexts`.
- Playwright project dependencies/teardown, retries, traces, API testing, and local-server orchestration: `https://playwright.dev/docs/test-projects`, `https://playwright.dev/docs/test-retries`, `https://playwright.dev/docs/trace-viewer-intro`, `https://playwright.dev/docs/api-testing`, and `https://playwright.dev/docs/test-webserver`.
- Maestro YAML flows, nested reusable flows, runtime parameters, and Android execution: `https://docs.maestro.dev/maestro-flows`, `https://docs.maestro.dev/maestro-flows/flow-control-and-logic/nested-flows`, and `https://docs.maestro.dev/getting-started/build-and-install-your-app/android`.
- PowerShell process startup/inspection and guaranteed cleanup: `https://learn.microsoft.com/powershell/module/microsoft.powershell.management/start-process`, `https://learn.microsoft.com/powershell/module/microsoft.powershell.management/get-process`, and `about_Try_Catch_Finally` in Microsoft Learn.
- ASP.NET Core integration-test concepts and application/infrastructure boundaries: `https://learn.microsoft.com/aspnet/core/test/integration-tests`.
- Flutter integration-test guidance is a reference for scenario design, but do not add `integration_test` to the product when detachable Maestro black-box coverage is sufficient: `https://docs.flutter.dev/testing/integration-tests`.
- .NET 10 support lifecycle, `net10.0` target-framework guidance, and ASP.NET Core 10 documentation: `https://dotnet.microsoft.com/platform/support/policy`, `https://learn.microsoft.com/dotnet/standard/frameworks`, and `https://learn.microsoft.com/aspnet/core/?view=aspnetcore-10.0`.
- React with TypeScript guidance: `https://react.dev/learn/typescript`.

## 15. Delivery Roadmap

Start a phase only when the user explicitly requests it. A request may intentionally select a smaller slice than a complete phase.

Every roadmap item is a vertical slice: introduce only the domain/data/API needed by the named client behavior, wire its actual consumers, and pass its gate before broadening scope. Do not implement all entities/endpoints first, postpone all clients, or defer integration to a final phase.

1. **Executable Skeleton:** root configuration, minimal four-project Backend boundary, health/readiness, minimal React/Flutter shells, safe Development/QA bootstrap path, and QA smoke harness. Do not scaffold feature entities/screens.
2. **P0A Authentication and Assisted Access:** Backend Identity/session/resource-role foundation; Flutter login/password-help/forced-change/role Home entry for Teacher/Parent/Student and Administrator-only rejection; React Administrator/Teacher eligibility plus Administrator password-help queue/temporary-password issuance and Parent/Student-only rejection; logout/session restoration.
3. **P0B School Directory and Fixed Import:** school year/class/subject minimum, profiles and relationships, Administrator Web management sufficient for the demo, fixed versioned workbook upload/header verification/row validation/transactional commit, and imported-account sign-in/scoping.
4. **P0C-1 Mobile Information Flow:** role-aware Flutter Home plus read-only grades/detail, timetable, events/detail, announcements, and exact guarded navigation using authorized synthetic/imported records.
5. **P0C-2 Clubs:** Flutter discovery/search/filter/detail and Student idempotent join/request with eligibility/scope enforcement. P0C is complete only after both P0C-1 and P0C-2 pass; they remain separate vertical slices for manageable implementation/review.
6. **P0D Leave and Attendance:** Parent Flutter leave create/history/detail/cancel, Teacher Flutter queue/decision and attendance save/reopen, Parent/Student attendance visibility, attachments only if included in the active slice, and reconciliation behavior.
7. **P0E Announcement and Gmail:** Administrator/Teacher React composition with server audience preview, immediate Portal App/Gmail send, basic current delivery result, and authorized Flutter inbox visibility.
8. **Promoted P1 Daily Comments:** Teacher Flutter create/review/send plus permitted Parent/Student read and retry idempotency.
9. **Promoted P1 Homework:** Teacher publish, Student text/file submission, Teacher basic grading, and authorized Student/Parent result.
10. **Promoted P1 Grade Entry:** Teacher Flutter batch entry/validation/concurrency plus Student/Parent read-back.
11. **Promoted P1 Feed, Histories, and Teacher Web:** text-first class feed, announcement read state, import/delivery history, audit view, and scoped read-focused Teacher Web dashboard/schedule.
12. **Milestone Rehearsal and Release:** run the active milestone journeys against one current worktree/database, fix integration/accessibility/visual issues, prepare synthetic demo data, and perform Section 14.8 cleanup only after explicit user approval.

P1 roadmap items are dormant until the user explicitly starts/promotes them. Finishing P0 does not authorize starting P1 automatically.

### 15.1. Phase Gates

- **Executable-skeleton gate:** .NET 10 API Release build/readiness, React production build, Android APK build, root configuration validation, and a safe known-pass/known-fail QA smoke work without feature placeholders.
- **P0A authentication gate:** sign-in failure/success, React accepts only Administrator/Teacher contexts, Flutter accepts only Teacher/Parent/Student contexts, unsupported-role sessions are cleared, logout, session restoration, token refresh, generic password-help request, Administrator temporary-password issuance, forced password change, single-role redirect, supported multi-role selection, and protected-route denial pass end-to-end.
- **P0B directory/import gate:** migrations apply to a fresh QA database; invalid references/duplicates produce row errors without domain writes; a valid fixed workbook commits once; imported accounts authenticate and receive only relationship-scoped data.
- **P0C Mobile-information gate:** after both P0C-1 and P0C-2, role-aware Home plus Grades, Timetable, Events, Clubs, Announcements, and Profile work without dead links for every role to which each capability applies; grade/event drill-down, club idempotency, Parent-child/Student-own/Teacher-assignment authorization, and loading/empty/error states pass on Android.
- **P0D school-workflow gate:** Leave Requests/status has working list/create/detail routes, and Parent leave plus Teacher decision and Teacher attendance save/reopen work end-to-end, including validation, authorization, status transitions, idempotency, cancellation, and reconciliation behavior.
- **P0E communication gate:** Web audience preview/confirmation and immediate Portal App/Gmail delivery produce deduplicated basic recipient results; intended Flutter inboxes receive the persisted announcement; unrelated users and real/non-test QA recipients are excluded.
- **Promoted P1 slice gate:** only the named P1 journey is added to the active milestone and must pass its feature-specific authorization, failure-state, persistence, and actual-consumer checks before that slice is complete.
- **Assignment-demo gate:** the .NET 10 API Release build, React production build, and Flutter Android APK come from the same current worktree; all nine P0 journeys in Section 3.2 pass against one isolated local SQL Server QA database with synthetic data, and Gmail is exercised only by the communication journey.
- **Extended-product gate:** the Assignment-demo gate plus every explicitly promoted P1 journey passes. Dormant P1/P2 work is not a failure.

If a gate fails, report the exact failure and finish that gate before moving to dependent work unless the user explicitly reprioritizes.

## 16. Definition of Done for Any Coding Task

A requested coding task is complete only when:

- The requested behavior and authorization rules are implemented without silently expanding scope.
- Validation, error, loading, and empty states appropriate to the task are handled.
- Every Standard/Critical non-trivial slice has the Section 14.7.1 exact-file plan, and the final implementation has no unexplained drift from it. A Small slice uses the Section 0.3 contract without a redundant long plan.
- Every new behavior/bug has a valid observed RED acceptance scenario before its production change, or an allowed Section 14.7.2 exception with a recorded reason; GREEN was then observed after the minimal implementation.
- Relevant disposable QA scenarios are added or updated under `qa/` and pass for Standard/Critical behavior. A documentation-only Small slice performs structural/path/consistency checks without creating the QA harness. No test code/dependency is added to a product directory.
- Every formatter, analyzer/linter, production build, API integration, and UI E2E command required by the selected Section 14.4 tier/change-impact row has been run; unrelated product/tool gates are not required. Any required blocker is reported precisely.
- For Standard/Critical Flutter work, run the relevant subset of `flutter pub get`, format check, `flutter analyze`, Android APK build, and external Maestro flow required by Section 14.4.2/14.4.3; Critical Mobile journeys require the production-representative APK.
- For Standard/Critical Backend work, verify .NET 10/`net10.0`, restore/build Release, and run the required external API/integration scenarios; apply migrations to disposable SQL Server only when persistence/migration behavior is affected or a milestone gate requires it.
- For Standard/Critical React work, run the lockfile-matched install when dependencies are absent/changed, configured TypeScript type-check/lint, production Vite build, and targeted external Playwright scenarios required by the selected tier.
- Scope fast commands to touched components during iteration. Run every actual affected consumer before completing a contract task; run the full three-product integration gate only for the triggers in Section 14.4.1 or an active milestone/release gate.
- No secrets or real personal data are introduced.
- API/schema/configuration changes are documented where future phases depend on them.
- The final diff has been compared with the Section 2.2 baseline; every changed path is intentional/classified, no user-owned change was overwritten, and no affected result is stale.
- For Standard/Critical work, the final checkpoint/result manifest maps every numbered acceptance check to a fresh command/scenario result and records exact teardown status. A Small slice records commands/results in the final handoff. An unrun, skipped, retry-only, stale, or blocked required gate makes the outcome `incomplete` or `blocked`, not `completed`.
- The Section 14.7.3 reviews appropriate to the tier passed: separate specification and quality/security passes for Standard/Critical units and final branch, or one combined recorded self-review for a Small slice. No Blocker/Major finding remains unresolved.
- When Section 2.3 grants local-commit authorization, commits are atomic and ordered for review, each hash/gate is recorded, no commit contains unrelated/pre-existing/generated/secret content, and no completed in-scope change remains as an unexplained blob. Without that authorization, no Git mutation is performed and the handoff identifies commit-ready units/changed paths instead. Nothing is pushed or history-rewritten without explicit instruction.
- The final response summarizes changed files, verification performed, remaining limitations, and the next logical phase without starting it automatically.

Compilation alone is not completion. If a required environment or gate cannot run, report the task as incomplete/blocked with the exact command, failure, evidence path, and safe next action; do not describe it as done.

Use outcome words mechanically:

- `completed`: every numbered acceptance check and required fresh gate passed, teardown/resource ownership is known, and the final diff is intentional.
- `incomplete`: implementation or verification remains, a required gate fails/is stale/was not run, or only a narrower partial path passed; state the first unfinished acceptance unit.
- `blocked`: progress requires a missing user decision/credential/tool/service/authority after safe in-scope alternatives and preflight evidence are exhausted, or the same failure fingerprint reached the Section 14.7 stop rule. Do not use `blocked` merely because work is large or a first attempt failed.

Use this final handoff shape so another agent can continue without reconstructing state:

```text
Outcome: completed | incomplete | blocked
Slice and acceptance result:
Production files changed:
API/DB/config changes:
Branch and ordered commits (hash | subject | acceptance unit):
Test-first evidence (RED command/failure or allowed exception -> GREEN):
Products verified: Backend | Web | Flutter
Commands and results:
Cross-client/phase gates:
Reviews (unit and branch; spec then quality/security; unresolved Minor findings):
QA evidence/artifact run ID:
Intentional mockup deviations:
Remaining limitations or blocker:
Next logical phase (not started):
```

## 17. Agent Acknowledgement

When the user asks only whether these instructions were understood, reply:

> AGENTS.md loaded successfully. I am ready to work on myfschoolse1910 one requested phase at a time.
