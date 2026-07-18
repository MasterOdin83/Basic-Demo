---
name: run-basic-demo
description: Run, start, launch, smoke-test, or screenshot the Basic Demo app (Angular UI + BasicSTS.API auth + Basic.API tasks). Use when you need the stack up, want to drive the UI in a browser (login, task CRUD), verify a change works in the running app, or take screenshots for UI/UX work.
---

# Run Basic Demo

Three processes: BasicSTS.API (auth, :5143), Basic.API (tasks, :5216), Angular
dev server (:58906). SQLite file `basic-demo.db` at repo root, auto-created and
seeded on first API startup. Driven programmatically with `curl` (APIs) and
`agent-browser` (UI, globally installed). All paths below are relative to repo
root; scripts are bash (Git Bash on this machine).

## Prerequisites

Already on this machine — verify only if something's broken:
.NET SDK 10.0.302 (`dotnet`), Node 24 (`node`, `npm`), `agent-browser` 0.32+
(global npm), `curl`, `python` (used by smoke.sh for JSON parsing).
UI deps: `cd Basic.UI && npm install` (skip if `node_modules` exists).

## Launch (agent path)

**Probe first — the stack is often already running** (the user leaves it up):

```bash
curl -s -o /dev/null -w "UI %{http_code} "  http://127.0.0.1:58906 --max-time 2
curl -s -o /dev/null -w "STS %{http_code} " http://localhost:5143/api/auth/me --max-time 2
curl -s -w "API %{http_code}\n" -o /dev/null http://localhost:5216/api/tasks/statuses --max-time 2
```

`UI 200 / STS 401 / API 200` = already up, skip launch. Anything else: launch
each missing process **in the background** (each blocks its terminal):

```bash
dotnet run --project BasicSTS.API   # from repo root
dotnet run --project Basic.API
cd Basic.UI && npm start            # ng serve on 127.0.0.1:58906
```

APIs are up in ~5s, `ng serve` in ~15s (cold). Re-run the probe until it shows
200/401/200. To free a stuck port (PowerShell):

```powershell
Get-NetTCPConnection -LocalPort 5216 -State Listen | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
```

## Smoke driver

```bash
bash .claude/skills/run-basic-demo/smoke.sh   # from repo root
```

Waits for all three ports, then: API login → me → create/update/delete a task
(curl), then real browser: login as `demo` / `Password123!`, create a task via
the UI form, delete it again, screenshot to `./smoke-tasks.png`. Ends with
`SMOKE PASSED` and exit 0. Leaves the DB as it found it.

## Driving the UI ad hoc

`agent-browser open http://127.0.0.1:58906`, then `snapshot` (accessibility
tree with `@e1` refs), `fill`, `select`, `screenshot <path>`, `eval <js>`,
`close`. Seed login: `demo` / `Password123!` (shown on the login drawer).

**The one rule: dispatch every click/submit via JS in `eval` — never trust
`click`/`press`/`mouse`.** See Gotchas for why. Verified patterns:

```bash
# open login drawer, fill, submit (drawer <dialog> markup is always in the DOM)
agent-browser eval "document.querySelector('header button').click()"
agent-browser wait 'input[name="username"]'
agent-browser fill 'input[name="username"]' demo
agent-browser fill 'input[name="password"]' 'Password123!'
agent-browser eval "(() => { const f=[...document.querySelectorAll('form')].find(f=>f.querySelector('input[name=username]')); f.requestSubmit(); return 'ok'; })()"
agent-browser wait 'form.task-form'

# create a task (task form uses reactive forms -> formcontrolname selectors)
agent-browser fill 'form.task-form input[formcontrolname="title"]' 'My task'
agent-browser eval "document.querySelector('form.task-form').requestSubmit()"

# change a status (select IS safe - it's JS-dispatched)
agent-browser select 'li.task:first-child select' Done

# delete a task (confirm() must be overridden, see Gotchas)
agent-browser eval "window.confirm = () => true"
agent-browser eval "(() => { const li=[...document.querySelectorAll('li.task')].find(l=>l.querySelector('strong')?.textContent.trim()==='My task'); li.querySelector('button.danger').click(); return 'deleted'; })()"
```

Useful selectors: `header button` (Log in / Log out area), `li.task` (task
cards), `button.danger` (Delete), `.task-actions select` (status), edit form =
`form.task-edit`. Login inputs use `name=`, task forms use `formcontrolname=`.

## Run (human path)

Three terminals: the same three commands as above, then open
http://127.0.0.1:58906. Ctrl-C each to stop.

## Tests

```bash
dotnet test                 # 35 xUnit backend tests, ~15s
cd Basic.UI && npm test     # 6 vitest UI tests
```

## Gotchas

- **Trusted input dies silently.** agent-browser's `click`, `press`, and raw
  `mouse` events report `✓ Done` but stop arriving at the page at some point
  after login/navigation — permanently for that browser process (survives
  `reload` and re-`open`; a window-level capture listener sees nothing, even
  for `mouse move/down/up` at exact coordinates). Fresh sessions start working,
  then break the same way. Root cause unknown (agent-browser 0.32.1 on this
  Windows box). `eval` JS dispatch (`el.click()`, `form.requestSubmit()`),
  `fill`, `select`, `snapshot`, `screenshot` work 100% of the time — use only
  those.
- **`(ngSubmit)` forms need `requestSubmit()`**, not `.click()` on the submit
  button… except the login button, which also has a `(click)` handler. Just
  always use `form.requestSubmit()`.
- **Delete asks `confirm()`** — headless automation auto-dismisses it, so the
  click "does nothing". Run `agent-browser eval "window.confirm = () => true"`
  once per session before deleting.
- **`eval` calls share one JS scope** — a second `const li=...` throws
  `Identifier 'li' has already been declared`. Wrap evals in an IIFE.
- **Launching over an already-running stack fails uglily**: `dotnet run` loops
  10 retries of MSB3026 then fails MSB3027/MSB3021 (file locked by the running
  API process); `ng serve` exits with "Port 58906 is already in use". That's
  why you probe first.
- **DB reset**: stop both APIs, delete `basic-demo.db` (and `-shm`/`-wal`),
  restart — recreated and reseeded (user `demo`, 3 tasks) by `EnsureCreated()`
  at startup. Don't delete while APIs run (file is locked).
- The tasks list snapshot refs (`@e...`) reshuffle after every DOM change —
  re-`snapshot` before using refs, or prefer CSS selectors.

## Troubleshooting

- `smoke.sh` dies at a `fill` with "Element not found" → a stale browser
  session from a previous run; `agent-browser close` and re-run.
- Smoke "stack not up after 60s" → check the three processes actually started
  (see Launch), most likely a port collision with a half-dead instance.
- API returns 401 with a fresh-looking token → both APIs must share the same
  `Jwt:Key` (they do in `appsettings.json`); a restarted STS does not
  invalidate old tokens (stateless JWT, 8h expiry).
