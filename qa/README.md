# MyFSchool disposable QA harness

This directory contains black-box verification only. Product projects must not reference it.

- `api/`: HTTP contract and authorization scenarios.
- `web/`: browser scenarios against the built React application.
- `mobile/`: Maestro flows against a built Android APK.
- `scripts/`: bounded local environment lifecycle commands.
- `fixtures/`: synthetic deterministic inputs.
- `artifacts/`: ignored run evidence and checkpoints.

The repository-root `.env` is the only manually maintained local environment file. Generated run-specific values belong in the ignored `qa/.env.e2e.generated` file and must be removed during teardown.
