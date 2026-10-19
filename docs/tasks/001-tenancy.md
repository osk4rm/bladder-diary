# Task 001 — multi-tenant households

Bladder Diary must serve many independent households. Today the template knows only users and
subscriptions; there is no notion of a tenant, so nothing a household records can be isolated.

## Model

Add three entities, following the patterns already in `apps/api/Data`.

| Entity | Fields | Notes |
|---|---|---|
| `Household` | `Id`, `OwnerUserId` → `AppUser`, `Name` (max 100), `CreatedAt` | One household per owner for now. `OwnerUserId` unique. |
| `Child` | `Id`, `HouseholdId` → `Household`, `Name` (max 100, required), `BirthDate` (nullable `DateOnly`), `CreatedAt` | Children belong to a household. |
| `Entry` | `Id`, `HouseholdId`, `ChildId` → `Child`, `Kind`, `OccurredAt`, `Notes` (max 2000, nullable), `CreatedAt` | `HouseholdId` is denormalised on purpose: every read is "entries for my household", and carrying the tenant on the row makes that one indexed predicate instead of a join a later query can forget. |
| `EntryKind` | enum: `Water`, `Urination`, `Stool`, `NappyCheck`, `Food` | |

Indexes: `Entry (HouseholdId, OccurredAt)`, `Entry (ChildId, OccurredAt)`, `Child (HouseholdId)`.

## Migration

One migration, **additive only** — it must pass `scripts/ci/migrations.ps1`. No column drops, no
rewrites of existing tables.

## Onboarding

When a user registers, create their household automatically, in the same transaction as the user.
An existing user without a household must not crash the app: the diary endpoints answer `404`
`{"error":"no_household"}`.

## Endpoints

All under `/api/diary`, all `[Authorize]`, antiforgery on every POST.

- `GET /children` — the household's children, oldest first.
- `POST /children` — `{ name, birthDate? }`, returns 201 with the child.
- `GET /entries?childId=&take=` — newest first, `take` defaulting to 50 with a **hard maximum of 200**.
- `POST /entries` — `{ childId, kind, occurredAt?, notes? }`, returns 201.

## The property that matters

**A household must not be able to read or write another household's data**, and it must be true by
construction rather than by a check someone remembers:

- every query filters by the caller's household;
- no entry is ever looked up by an id from the request;
- the one id accepted from the client — `childId` on create — is verified to belong to the
  caller's household before use, and a foreign id answers `404`, not `403`, so it does not reveal
  that the id exists.

## Tests

At least one integration test that registers **two** accounts, has the first create a child and an
entry, and asserts the second sees neither — using the existing `ApiFactory`.
