# MyFSchool — Web Portal (Administrator / Teacher)

React + TypeScript + Vite single-page application for the school back-office.
The portal is consumed exclusively by **Administrator** and **Teacher** accounts
in P0 (per AGENTS.md § 1.1 and § 4.7). Parent and Student access must not be
exposed through this client.

## Stack

- React 18 + TypeScript 5
- Vite 5 (production builds + dev server)
- Ant Design 5 (data-dense back-office UI)
- React Router 6 (route guards derived from authenticated capabilities)
- TanStack Query 5 (server-state cache and revalidation)
- React Hook Form + Zod (form state and validation)
- dayjs (school date/time formatting)

## Configuration

This client reads **only** `VITE_API_BASE_URL` from the repository-root `.env`.
No other backend-only value is bundled. The Vite `envDir` is set to the repository
root, and the build will fail fast when `VITE_API_BASE_URL` is missing.

```dotenv
# repository-root .env
VITE_API_BASE_URL=http://localhost:5080/api/v1
```

`http://localhost:5173` (the Vite dev origin) must be present in the backend
`WebOrigins__AllowedOrigins` allowlist — this is already configured in
`backend/src/MyFSchool.Api/appsettings.json` and `.env.example`.

## Scripts

```bash
npm install
npm run dev      # http://localhost:5173
npm run build    # type-check + production bundle into dist/
npm run preview  # serve the built bundle on port 5173
npm run typecheck
```

## Routes (P0)

| Route | Guard | Description |
| --- | --- | --- |
| `/login` | Anonymous | Email/username + password sign-in |
| `/password-help` | Anonymous | Generic password-help request |
| `/change-temporary-password` | Restricted session | Forced password change |
| `/dashboard` | Authenticated | Role-aware landing |
| `/password-help-requests` | Administrator | Pending request queue, reject, issue temporary password |
| `/imports` | Administrator | Excel import center (template → upload → validate → commit → result) |

The portal uses **refresh-cookie transport** for `clientType=web`:
the rotating refresh token is set by the backend as an `HttpOnly`, `Secure`,
`SameSite=Strict` cookie scoped to `/api/v1/auth`. The short-lived access
token is held in memory via the auth provider only.
