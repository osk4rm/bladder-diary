# Task 002 — a diary a parent can actually use

Task 001 built the model and the endpoints. Nothing in the browser uses them: after signing in a
parent still lands on the template's placeholder dashboard. This task turns the web application into
the product — add a child, record what happened, see today.

The API from task 001 is the contract. **Do not change it**, with one exception named below.

## Wire format — the one API change

`EntryKind` is currently serialised as its ordinal (`0`, `1`, …). Register
`JsonStringEnumConverter` in `apps/api/Program.cs` so a response carries `"kind": "Water"`.

A client written against ordinals breaks silently the day the enum's order changes, and this client
is about to be written against it. Requests already accept both forms, so this is additive.

## Screens

Two states of one authenticated home page, chosen by whether the household has any children.

### No children — onboarding

- A heading with the product's name, and one sentence saying what to do.
- A form: **child name** (required, max 100) and **date of birth** (optional, a date input). One
  primary button.
- Submitting creates the child, and the page becomes the diary for that child. No full page reload.
- A failed submit shows the API's message next to the form; the form keeps what was typed.

### With children — the diary

- A child selector listing the household's children, the first one selected by default. With one
  child it is still visible, so adding a second is discoverable.
- **Quick record buttons**, one per `EntryKind`: Water, Urination, Stool, Nappy check, Food. One
  click records an entry at the current time for the selected child. This is the main action of the
  product and it must be one click, not a form.
- An optional note field that applies to the next quick record (it may be empty).
- The selected child's entries, **newest first**, each showing the kind, the time, and the note when
  there is one. Times are shown in the browser's local time zone, not UTC.
- After recording, the new entry appears without a reload, and there is a visible confirmation that
  the record was saved. A parent will tap these one-handed and needs to know it worked.
- An **Add another child** control that returns to the onboarding form without losing the diary.

### No household

`GET /api/diary/children` answering `404 {"error":"no_household"}` means the account predates
task 001's onboarding. Show a sentence saying the account has no household yet and that signing out
and back in will not fix it — do not show an empty diary and do not show a generic error.

## Constraints

- **No new npm packages.** React Query, React Router, Tailwind and the existing `apiFetch` are
  enough, and they are already wired up.
- Follow the existing layout: `apps/web/src/features/<area>/`, one component per file, `@/` imports,
  the existing `Button` component for buttons.
- Types live in the feature, named as the API names them (`Child`, `Entry`, `EntryKind`).
- The kind list must come from one place — a `const ENTRY_KINDS` in the feature — so a new kind is
  one line, and the labels are the human names above.
- The existing `h1` in `DashboardPage.tsx` is a placeholder. Replace it; the authenticated home page
  is the diary, titled **Bladder Diary**.
- Keep `Sign out` reachable from the diary.

## Tests

- **Unit (vitest + Testing Library, jsdom):** the onboarding form appears when the household has no
  children; submitting it calls the API and switches to the diary; a quick record posts the right
  body and renders the entry; a `404 no_household` renders the message, not a generic error. Stub
  `apiFetch` — do not reach the network from a unit test.
- **End to end (Playwright, `apps/web/e2e/`):** one test that registers a new account, adds a child,
  taps Water, and sees the entry appear. Follow `auth.spec.ts`.
- **Update the existing e2e assertions** that expect a `Dashboard` heading — three of them in
  `auth.spec.ts` currently look for it. They must expect the new home page; do not delete the
  assertions, change what they look for.

## Definition of done

`npm --prefix apps/web run build`, `npm --prefix apps/web run lint` and
`npm --prefix apps/web test` all pass, and the API's own tests still pass.
