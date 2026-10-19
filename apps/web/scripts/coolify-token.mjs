import { readFileSync } from 'node:fs'
import { chromium } from 'playwright'

/**
 * Creates a Coolify API token through the UI, because that is the only way one exists.
 *
 * Phase 1 (this run): log in, then report what the page actually offers, so the token flow is
 * discovered rather than guessed. Credentials are read from the operator's notes and never
 * printed - a script that echoes a password into a transcript is a leak with extra steps.
 */
const notes = readFileSync(process.env.COOLIFY_NOTES, 'utf8')
const url = /coolify:\s*(http\S+)/i.exec(notes)?.[1]
const email = /([\w.+-]+@[\w.-]+)/.exec(notes)?.[1]
const password = /@[\w.-]+\s*\/\s*(\S+)/.exec(notes)?.[1]

if (!url || !email || !password) {
  console.error('could not parse url, email and password from the notes file')
  process.exit(2)
}
console.log('target:', url, '| account:', email, '| password:', '*'.repeat(password.length))

const browser = await chromium.launch({ headless: true })
const context = await browser.newContext({ ignoreHTTPSErrors: true })
const page = await context.newPage()

await page.goto(`${url.replace(/\/login$/, '')}/login`, { waitUntil: 'domcontentloaded', timeout: 60_000 })
console.log('login page title:', await page.title())

// Coolify's form uses plain email/password inputs; be tolerant of the exact markup.
await page.fill('input[type="email"], input[name="email"], #email', email)
await page.fill('input[type="password"], input[name="password"], #password', password)
await Promise.all([
  page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 60_000 }).catch(() => undefined),
  page.click('button[type="submit"]'),
])

console.log('after login:', page.url())

await context.storageState({ path: process.env.COOLIFY_STATE ?? 'coolify-state.json' })
console.log('session saved')

// What does this Coolify actually offer? Print the navigation rather than assume the route.
const links = await page.$$eval('a[href]', (anchors) =>
  [...new Set(anchors.map((a) => a.getAttribute('href')))].filter((h) => h && h.startsWith('/')),
)
console.log('links:', links.slice(0, 40).join('\n       '))

await browser.close()
