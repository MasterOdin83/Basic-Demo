# Handoff — 2026-07-18 (cierre 4)

## Qué se hizo
Sesiones del 2026-07-18 (todo pusheado a `master`):
- **Landing dividida de login** (commit `0d7104f`): Home vive en `/` como componente propio; el login es SOLO el auth card, abierto desde el botón "Log in" del topbar en un `<dialog>` drawer que entra desde la DERECHA (decisión deliberada del usuario — no moverlo). `/login` queda solo como target del auth-guard para deep links; logout navega a `/`. Home tiene entrada blur-fade (CSS puro, replicando un BlurFade de framer-motion que pasó el usuario; sin dependencia nueva).
- **Copy de landing refrescado para recruiters** (`9fde43d`), links a scan/fuentes/docs de API + glosas hover de acrónimos + favicon "B" (`b9fda25`).
- **Password visibility toggle + layout móvil** (`53b4a4e`); el campo pasó a flex para que los divs que inyecta LastPass no envuelvan el toggle (`3141bcd`).
- **Skill `run-basic-demo`** (`df841fd`, `f061e1f`): levanta el stack y corre smoke por curl. **En esta sesión se le REMOVIÓ toda la automatización de browser (agent-browser)**: el smoke de UI pasaba en verde mientras se escapaban issues reales de UI, y dejaba servers y el daemon del browser corriendo — más carga que señal. `smoke.sh` ahora es curl-only (login → me → create/update/delete → `SMOKE PASSED`); la UI se verifica A MANO en http://127.0.0.1:58906. Los patrones de selectores borrados viven en git (`df841fd`) para cuando se retome. Regla persistida en la memoria de Claude: no usar agent-browser en este repo hasta que el usuario lo levante; arrancar el stack siempre requiere aprobación explícita en la conversación.
- **Animación leftover** (`806029f`): `.hero .source-line` se une al blur-fade escalonado del home.
- Del 2026-07-17 tarde (post-cierre 3): fix CSP `inlineCritical` (`16f8be8`) — Angular emitía `<link onload=...>` que `script-src 'self'` bloqueaba.

## Estado actual
- Push a `master` dispara TRES workflows: SWA (UI), `master_qa-demo-api.yml` (tasks API) y `master_qa-demo-sts.yml` (STS).
- UI en SWA `https://thankful-sea-0308a2310.azurestaticapps.net` (verde); App Services `qa-demo-sts-h3dxfshgapatdsdf` / `qa-demo-api-a3dhdwf0aqdbcnck` (centralus-01).
- Los 2 workflows de App Service **siguen bloqueados por el subject OIDC** (verificado 2026-07-18: ambas federated credentials aún tienen el subject clásico; los comandos `az` del fix fueron re-entregados al usuario — el classifier de permisos bloquea que Claude los corra).
- Tests: 35 backend (xUnit) + 6 UI (vitest), verdes al último run. REQ en v0.4 (sin cambios de producto en este cierre).
- Limitación QA conocida: cada App Service tiene su propio SQLite → usuarios registrados en el STS no existen en la DB del tasks API; la seed `demo`/`Password123!` funciona en ambos. Supabase lo resuelve.

## Next steps (en orden de prioridad)
1. **Copiar el avatar:** poner el archivo del usuario en `Basic.UI/public/avatars/leonidas.png` (la UI ya lo referencia; sigue sin existir). NUNCA generarlo con IA (vetado).
2. **Azure — arreglar el subject OIDC de los deploys (bloqueante; los workflows de App Service NUNCA han pasado):** para las identidades `qa-demo-api-id-a045` y `qa-demo-sts-id-a5f7` (RG `HectorResouce`, credential `bihd47z64h46k` en cada una), actualizar el **Subject identifier** a
   `repo:MasterOdin83@55357287/Basic-Demo@1304366521:ref:refs/heads/master`
   (issuer `https://token.actions.githubusercontent.com`, audience `api://AzureADTokenExchange`; en el portal usar tipo "Other" si "GitHub" no acepta el formato). Después re-run de los workflows fallidos. az CLI local: `az login --tenant 7e500456-8895-4221-a368-9525be76ad60`.
3. **Azure — App Settings:** en AMBOS App Services `ASPNETCORE_ENVIRONMENT=QA` y `Jwt__Key` (mismo valor). Luego smoke manual en la SWA (login `demo` → CRUD → logout) y verificar `/scalar` en ambas APIs.
4. **Refactor de la sección de tareas:** el two-column (`.tasks`/`.card-task`) quedó iniciado en `0d7104f`; el usuario planea un refactor mayor de esa sección.
5. **Pulir UI (hallazgos restantes):** `autocapitalize="none"`+`spellcheck="false"` en username, apóstrofe tipográfico en "Don't", label "In Progress" para `InProgress`, `.task-main` con `min-width:0`+`overflow-wrap:anywhere`, `touch-action: manipulation`.
6. **Supabase como segundo storage:** `Npgsql.EntityFrameworkCore.PostgreSQL` en `Basic.Data`, provider por config (`"Database": "sqlite" | "supabase"`), `Basic.Core` intacto.
7. **Entregable GenAI:** documentar prompts, validación del código generado (tests, review, caso CVE OpenAPI y su reversión, anti-enumeración, bugfix zoneless) y manejo de edge cases/auth.
8. **Presentación:** guion con user story, capas, demo y decisiones.

## Pendientes/backlog acordado
- **Browser automation en pausa:** reintroducir agent-browser al skill solo cuando el usuario diga que "ya somos buenos en esto"; base en commit `df841fd`.
- Sin max lengths server-side en username/password (vector DoS por PBKDF2) ni en title/description de tasks — reportado, no corregido.
- Security scheme Bearer en el OpenAPI de `Basic.API` si Scalar no ofrece campo de auth en runtime.
- Headers de seguridad en los App Services (el scan calificado fue el de la SWA).
- Migraciones EF cuando el esquema evolucione (hoy `EnsureCreated` con guard anti-carrera).
- JWT key a secret store para despliegue real (en QA se sobreescribe como App Setting — next step 3).
- Solo una fila de task en edición a la vez (decisión deliberada).

## Cómo retomar
```powershell
dotnet run --project BasicSTS.API   # terminal 1 → http://localhost:5143 (/scalar)
dotnet run --project Basic.API      # terminal 2 → http://localhost:5216 (/scalar)
cd Basic.UI; npm install; npm start # terminal 3 → http://127.0.0.1:58906
dotnet test                         # 35 tests backend
cd Basic.UI; npm test               # 6 tests UI
```
- Smoke rápido de APIs (con el stack arriba): `bash .claude/skills/run-basic-demo/smoke.sh` — curl-only, termina en `SMOKE PASSED`. La UI se prueba a mano.
- Credenciales seed: `demo` / `Password123!`. Sin secretos externos; la única key es la JWT dev de `appsettings.json` (misma en ambas APIs).
- Reglas de diseño permanentes: ninguna respuesta de auth revela el motivo del rechazo (anti user-enumeration); la UI es zoneless — todo estado escrito en callbacks async debe ser signal; nunca generar imágenes con IA para este repo; el drawer de login entra por la derecha; NO usar agent-browser.
