# Agent operating rules (2026-08-08)

Applies to all agent/sub-agent work across Flagship Demo (Security Demo) and Spartan IT, not just this repo.

1. **Token economy.** Brief agents with what's already known — don't make them rediscover context. No speculative exploration beyond what the task needs.
2. **Max 2 retries, then stop and escalate.** Two failed attempts at the same fix/feature → do not attempt a third blind fix. Revert to the simplest correct thing that's actually verified, and report the item as needing a bigger decision (re-architecture, descoping, or explicit owner input) instead of looping or silently dropping it.
3. **A twice-failed item is a backlog item, not a dead end.** Log it below under "Escalated" so it feeds Step 2 planning instead of evaporating.

## Escalated (needs a decision, not another patch)

_None yet._

## Step 2 — Docker/Kubernetes (once Security Demo UI + Spartan IT recruit-me flow are done)

- Containerize APIs only (`Basic.API`, `BasicSTS.API`, `SpartanIT.API`) — UI stays on its current static-hosting deploy, not containerized.
- Local cluster (`kind`/Docker Desktop) first, to prove the pipeline before any AKS spend.
- BasicSTS.API is where Redis/BFF session work lands eventually — containerize it as its own Deployment/Service from day one even before that lands, so adding Redis later is a config change, not a re-migration.
- CI/CD: adapt the existing GitHub Actions workflows (Flagship Demo already has working App Service deploys) to build+push+deploy instead of publish-direct.
