# AGENTS.md — how agents work in this repo

This repository is built by the **Swarm HQ** agent swarm. Read this before any
non-trivial change.

## The pipeline

`ai:implement → ai:review → ai:verify → [ai:e2e] → ai:ready`, with failures going
to `ai:changes` and escalations to `ai:blocked`. Agents **never** move `ai:*`
labels — the orchestrator owns every transition.

## Commands (single source of truth)

| Task | Command |
|---|---|
| Build | `pwsh -File scripts/ci/build.ps1` |
| Lint | `pwsh -File scripts/ci/lint.ps1` |
| Test | `pwsh -File scripts/ci/test.ps1` |
| API dev | `dotnet run --project apps/api` |
| Web dev | `npm --prefix apps/web run dev` |
| Stack | `docker compose up` |

CI and the swarm both call these scripts, so keep them honest.

## Conventions

- **Backend**: TypeScript-like strictness — nullable enabled, warnings are errors.
  Controllers stay thin; domain logic lives in services. Cancellation tokens flow
  through async calls. No secrets in code or logs.
- **Auth**: HttpOnly cookie sessions. Never store tokens in `localStorage`. Any
  state-changing endpoint must validate the antiforgery token (`X-CSRF-TOKEN`).
- **Frontend**: function components + hooks, TanStack Query for server state,
  Tailwind utilities, `@/` path alias. Access the API only through `src/lib/api.ts`.
- **Data**: every schema change ships an EF Core migration.
- **Tests**: new behaviour gets a test. Integration tests use Testcontainers; the
  web uses Vitest + Testing Library. Never weaken or delete an existing test to
  make a change pass.
- Conventional commits (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`).

## Definition of done

All five must pass. A change that only satisfies the first two is not done.

1. `scripts/ci/build.ps1`, `lint.ps1`, `test.ps1` all pass.
2. `npm --prefix apps/web test` — component tests.
3. `npm --prefix apps/web run test:e2e` — when the change touches auth, routing, or anything
   a user clicks. If it does not, say so in the PR rather than leaving it implied.
4. The four static gates pass: `migrations.ps1`, `check-compose.ps1`, `check-dockerfile.ps1`,
   and the build itself treats warnings as errors.
5. Acceptance criteria of the linked issue are demonstrably met, and no unrelated changes are
   included.

### Things that will get a change rejected

- **Disabling antiforgery**, widening CORS, or setting `Cookie.SecurePolicy` to `None`. If a
  test needs it, the test is wrong.
- **A migration that breaks the previous version.** Expand/contract: add, backfill, then
  remove in a later release. The gate will catch it; do not bypass it with
  `// swarm:destructive-ok` without saying why in the commit message.
- **Storing a secret in configuration with a default.** If it has a fallback value, it is
  public.
- **Adding a vendor SDK to domain code.** Analytics, email and billing all have seams;
  implement the interface instead.
- **A test that cannot fail.** If removing the line you just wrote does not turn it red, it is
  documentation, not a test.
- **A list endpoint that returns an unbounded collection.** Every GET that returns rows must take a page size with
  a hard maximum. This is a template rule because the template had no paginated endpoint to copy: an agent building a
  Notes feature correctly reproduced the omission, and no gate, test or linter noticed.
- **An unbounded text field.** If a column has no MaxLength, a client can send a megabyte per request. Cap the
  contract and the column together, or say in the PR why the field is genuinely unbounded.

## Host traps

Symptom-to-cause, for failures that read like code defects and are not. Written after a cold agent lost several
turns to the first of them — the traps were documented in reports, which is not where a newcomer looks.

| Symptom | Cause | Fix |
|---|---|---|
| `dotnet restore` or `dotnet run` fails with `cannot access the file ... NuGetScratch\lock` | `%TEMP%\NuGetScratch` is owned by another principal | redirect `TMP`, `TEMP` and `NUGET_PACKAGES` to a directory you own, e.g. `.tmp\` |
| `dotnet ef` says "run `dotnet tool restore`" right after a successful restore | the tool shim and a redirected NuGet store disagree about where the store lives | invoke the shim directly: `~\.dotnet\tools\dotnet-ef.exe migrations add <name> --project apps/api` |
| git reports **dubious ownership** | the checkout is owned by another principal | `git config --global --add safe.directory <path>` |
| `docker` installed but the image will not run | the daemon is stopped, or Docker Desktop is on Windows containers | the agent image is Linux; start the daemon or use a Linux host |
| a command runs in the wrong repository | `cd` to a missing path is a **non-terminating** error in PowerShell, so later commands inherit the previous directory | prefer `git -C <path>` over `cd` |
| `[System.IO.File]::ReadAllText('relative')` cannot find a file `Test-Path` finds | .NET file APIs use the **process** directory, which `cd` does not change | absolute paths with .NET APIs — this one has bitten every session |
| `spawn EINVAL` / `ENOENT` for a tool that works in the shell | `npx`, `opencode` etc. are `.cmd` shims on Windows and Node refuses to spawn those directly | call the real `.exe`, or run the JS entry through `node` |
| Playwright's web server "failed to compile" with no compiler error | `dotnet run` under the harness hit the NuGet scratch lock | same fix as the first row |

If a failure is not in this table and not obviously your change, check `git status` and the exit code of the command
you believe succeeded: several of these produce an error naming a different problem.