---
name: run-basic-demo
description: Run, start, launch, or smoke-test the Basic Demo app (Angular UI + BasicSTS.API auth + Basic.API tasks). Use when you need the stack up or want to verify a change works against the running APIs. Browser automation (agent-browser) is deliberately NOT part of this skill for now — UI checks are done by the user by hand.
---

# Run Basic Demo

Three processes: BasicSTS.API (auth, :5143), Basic.API (tasks, :5216), Angular
dev server (:58906). SQLite file `basic-demo.db` at repo root, auto-created and
seeded on first API startup. APIs are driven programmatically with `curl`.
**Do not use `agent-browser` (or any browser automation) on this app** — it
missed real UI issues and is shelved until the user says otherwise; UI is
verified by the user by hand at http://127.0.0.1:58906. All paths below are
relative to repo root; scripts are bash (Git Bash on this machine).

## Prerequisites

Already on this machine — verify only if something's broken:
.NET SDK 10.0.302 (`dotnet`), Node 24 (`node`, `npm`), `curl`, `python` (used
by smoke.sh for JSON parsing).
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

Waits for all three ports, then API login → me → create/update/delete a task
via curl. Ends with `SMOKE PASSED` and exit 0. Leaves the DB as it found it.
UI is NOT exercised — the user checks it by hand (login: `demo` /
`Password123!`, shown on the login drawer).

## Run (human path)

Three terminals: the same three commands as above, then open
http://127.0.0.1:58906. Ctrl-C each to stop.

## Tests

```bash
dotnet test                 # 35 xUnit backend tests, ~15s
cd Basic.UI && npm test     # 6 vitest UI tests
```

## Gotchas

- **Launching over an already-running stack fails uglily**: `dotnet run` loops
  10 retries of MSB3026 then fails MSB3027/MSB3021 (file locked by the running
  API process); `ng serve` exits with "Port 58906 is already in use". That's
  why you probe first.
- **DB reset**: stop both APIs, delete `basic-demo.db` (and `-shm`/`-wal`),
  restart — recreated and reseeded (user `demo`, 3 tasks) by `EnsureCreated()`
  at startup. Don't delete while APIs run (file is locked).

## Troubleshooting

- Smoke "stack not up after 60s" → check the three processes actually started
  (see Launch), most likely a port collision with a half-dead instance.
- API returns 401 with a fresh-looking token → both APIs must share the same
  `Jwt:Key` (they do in `appsettings.json`); a restarted STS does not
  invalidate old tokens (stateless JWT, 8h expiry).
