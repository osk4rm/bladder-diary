import { spawn } from 'node:child_process'
import { mkdirSync, rmSync } from 'node:fs'
import path from 'node:path'
import EmbeddedPostgres from 'embedded-postgres'

/**
 * Runs the end-to-end suite against a real PostgreSQL, with no Docker and no setup.
 *
 * The product being tested is a web application whose most interesting behaviour lives in
 * the interaction between a browser, an API and a database. That interaction is exactly what
 * a host without a Docker daemon cannot test — so rather than making end-to-end coverage
 * conditional on infrastructure, this starts a real PostgreSQL as an ordinary user process
 * and hands its connection string to both the API and the Playwright run.
 *
 * If `ConnectionStrings__Default` is already set, that database is used and nothing is
 * started: CI should point at its own service, and a developer debugging a specific database
 * should not have it swapped underneath them.
 */
const provided = process.env.ConnectionStrings__Default
if (provided) {
  console.log('[e2e] using the database from ConnectionStrings__Default')
  await ensureBrowsers()
  process.exit(await run())
}

const port = Number(process.env.SWARM_E2E_PG_PORT ?? 55441)
const dataDir = path.join('e2e-data', 'pg')

// A directory left behind by a killed run makes initdb refuse to start, and the error reads
// like a code defect rather than stale state.
rmSync(dataDir, { recursive: true, force: true })

const database = new EmbeddedPostgres({
  databaseDir: dataDir,
  user: 'e2e',
  password: 'e2e',
  port,
  persistent: false,
})

console.log('[e2e] starting a PostgreSQL for the run…')
await database.initialise()
await database.start()
console.log(`[e2e] PostgreSQL ready on port ${port}`)

await ensureBrowsers()

const code = await run({
  ConnectionStrings__Default: `Host=127.0.0.1;Port=${port};Database=postgres;Username=e2e;Password=e2e`,
})

await database.stop()
rmSync(dataDir, { recursive: true, force: true })
process.exit(code)

/**
 * Installs Playwright's browser if it is not there.
 *
 * `@playwright/test` is a dependency, but **the browser is not**: it downloads on first use, and
 * on a clean CI runner that download had never happened, so the first test died with
 * `browserType.launch: Executable doesn't exist at ~/.cache/ms-playwright/…`. The product's only
 * browser-level test could not pass anywhere it had not already been run by hand.
 *
 * `--with-deps` installs the system libraries the browser needs; it shells out to the platform's
 * package manager, so it is passed on Linux only. The install is a no-op when the browser is
 * already present, which is why it can run unconditionally.
 */
function ensureBrowsers() {
  // No `playwright` element here: this is the Playwright CLI itself, invoked as
  // `node node_modules/@playwright/test/cli.js install …`. Passing its own name as the first
  // argument makes it parse "Unknown command" and exit non-zero - which is how this line was
  // first written, and an agent caught it by actually running the suite.
  const args = ['install', ...(process.platform === 'linux' ? ['--with-deps'] : []), 'chromium']
  const cli = path.join('node_modules', '@playwright', 'test', 'cli.js')
  console.log(`[e2e] installing the browser: ${args.join(' ')}`)

  const child = spawn(process.execPath, [cli, ...args], { stdio: 'inherit' })
  return new Promise((resolve) => {
    child.on('exit', (code) => {
      if (code !== 0) {
        console.error(`[e2e] the browser install exited ${code}`)
        process.exit(code ?? 1)
      }
      resolve()
    })
  })
}

/** Runs Playwright and resolves with its exit code. */
function run(extraEnv = {}) {
  const cli = path.join('node_modules', '@playwright', 'test', 'cli.js')
  // A clean Windows host fails `dotnet run` on a stale, foreign-owned %TEMP%\NuGetScratch
  // lock. Isolating those paths here means the e2e gate works on a machine where nobody has
  // configured anything - which is the only reason a gate gets run.
  //
  // They must be **absolute**. NuGet rejects a relative `NUGET_PACKAGES` outright ("Element
  // NUGET_PACKAGES must contain an absolute path"), and it does so in about two seconds, from
  // inside the build that `dotnet run` performs on behalf of Playwright. CI therefore reported
  // "The build failed. Fix the build errors and run again." for a change that had just been
  // built in the previous step - while the same command worked on a developer machine, because
  // there `NUGET_PACKAGES` was already set and this fallback never ran.
  const tmp = path.resolve('.tmp', 'tmp')
  const isolated = {
    TMP: tmp,
    TEMP: tmp,
    // .NET reads TMPDIR on Linux/macOS, not TMP: setting only TMP isolated nothing off Windows.
    TMPDIR: tmp,
  }
  if (process.platform === 'win32' && !process.env.NUGET_PACKAGES) {
    // Only where the trap exists: forcing a private package folder on CI would re-restore every
    // package on every run to work around a Windows-only lock.
    isolated.NUGET_PACKAGES = path.resolve('.tmp', 'nuget')
  }
  for (const dir of [isolated.TMP, isolated.NUGET_PACKAGES]) {
    if (dir) mkdirSync(dir, { recursive: true })
  }

  const child = spawn(process.execPath, [cli, 'test', ...process.argv.slice(2)], {
    stdio: 'inherit',
    env: { ...process.env, ...isolated, ...extraEnv },
  })

  return new Promise((resolve) => child.on('exit', (code) => resolve(code ?? 1)))
}
