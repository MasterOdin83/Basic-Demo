# Handoff — 2026-07-19 (cierre 5)

## Qué se hizo
Sesión del 2026-07-19 (un commit, ver hash en `git log`):
- **Refresh tokens (fix del reporte "expirado no desloguea ni refresca"):** el login del STS emite ahora DOS tokens: access de **3 minutos** (`Jwt:AccessTokenMinutes`, valor corto a propósito para la demo) y refresh JWT stateless de **1 día** (`Jwt:RefreshTokenDays`, expiración absoluta — sin rotación ni revocación, decisión deliberada comentada `ponytail:` en `AuthController`). Nuevo `POST /api/auth/refresh`. El refresh token lleva audience `BasicApp.refresh`: las APIs de recursos lo rechazan como bearer y un access token no sirve para refrescar (test cubre ambas direcciones). `ClockSkew = TimeSpan.Zero` en `Program.cs` de AMBAS APIs — el skew default de 5 min mantenía vivo el token de 3 min durante 8. En Angular, `auth.interceptor.ts` ante un 401 refresca una vez y reintenta el request original; si el refresh falla, limpia la sesión (`auth.logout()`) y navega a `/login`. Sesiones logueadas ANTES de este cambio no tienen refresh token guardado: su primer 401 desloguea limpio.
- **Header móvil (≤700px):** el brand muestra solo "Ballastlane" (el resto vive en `<span class="brand-tail">`, oculto por CSS); nav + usuario + Log out van dentro de `.nav-group`, que en desktop es `display: contents` (layout intacto) y en móvil es un dropdown bajo el topbar toggled por el botón hamburguesa (signal `menuOpen` en `App`).
- **Tasks en móvil (≤900px):** `.tasks` pasa a `flex-direction: column` — el two-column del fix desktop recortaba la columna New task en pantallas angostas. New task queda arriba (coincide con el copy "add your first one above"); los botones status/Edit/Delete de cada tarea quedan juntos en una fila.
- Tests: **37 backend + 6 UI** verdes; nuevos: `Refresh_issues_working_token_and_rejects_wrong_token_types` (STS) y `Expired_token_is_unauthorized` (tasks API, pinea el ClockSkew Zero). `TestApp.TokenFor` acepta ahora `expires` opcional.
- REQ actualizado a **v0.5** con estos cambios.

## Estado actual
- Cambios de la sesión commiteados. **NO commiteado (preexistente, no es de esta sesión):** los tres `.csproj` (Basic.API, BasicSTS.API, Basic.Test) tienen agregado `<PackageReference Include="Microsoft.OpenApi" Version="2.7.5" />` — apareció hecho en el working tree (¿NuGet/VS del usuario?). Los tests pasan con eso puesto. Decidir: commitearlo aparte o revertirlo.
- Push a `master` dispara TRES workflows: SWA (UI), `master_qa-demo-api.yml` (tasks API) y `master_qa-demo-sts.yml` (STS).
- UI en SWA `https://thankful-sea-0308a2310.azurestaticapps.net`; App Services `qa-demo-sts-h3dxfshgapatdsdf` / `qa-demo-api-a3dhdwf0aqdbcnck` (centralus-01).
- Los 2 workflows de App Service **siguen bloqueados por el subject OIDC** (las federated credentials aún tienen el subject clásico; comandos `az` del fix en next step 2 — el classifier de permisos impide que Claude los corra).
- Con refresh tokens, el `Jwt__Key` compartido entre App Services (next step 3) pasa de importante a **imprescindible**: expiración ahora exacta (skew 0) ⇒ cualquier mismatch = 401 inmediato.
- Limitación QA conocida: cada App Service tiene su propio SQLite → usuarios registrados en el STS no existen en la DB del tasks API; la seed `demo`/`Password123!` funciona en ambos. Supabase lo resuelve.

## Next steps (en orden de prioridad)
1. **Probar a mano el trabajo de esta sesión** (el stack estaba caído al cerrar; la UI se verifica a mano por regla): header hamburguesa ≤700px, tasks apiladas ≤900px, y el ciclo de token — login, esperar 3+ min, tocar la lista (refresca en silencio); borrar `refreshToken` de localStorage y dejar expirar el access ⇒ logout limpio a `/login`.
2. **Azure — arreglar el subject OIDC de los deploys (bloqueante; los workflows de App Service NUNCA han pasado):** para las identidades `qa-demo-api-id-a045` y `qa-demo-sts-id-a5f7` (RG `HectorResouce`, credential `bihd47z64h46k` en cada una), actualizar el **Subject identifier** a
   `repo:MasterOdin83@55357287/Basic-Demo@1304366521:ref:refs/heads/master`
   (issuer `https://token.actions.githubusercontent.com`, audience `api://AzureADTokenExchange`; en el portal usar tipo "Other" si "GitHub" no acepta el formato). Después re-run de los workflows fallidos. az CLI local: `az login --tenant 7e500456-8895-4221-a368-9525be76ad60`.
3. **Azure — App Settings:** en AMBOS App Services `ASPNETCORE_ENVIRONMENT=QA`, `Jwt__Key` (mismo valor) y, si QA quiere otros TTLs que los de demo, `Jwt__AccessTokenMinutes` / `Jwt__RefreshTokenDays`. Luego smoke manual en la SWA (login `demo` → CRUD → logout) y verificar `/scalar` en ambas APIs.
4. **Copiar el avatar:** poner el archivo del usuario en `Basic.UI/public/avatars/leonidas.png` (la UI ya lo referencia; sigue sin existir). NUNCA generarlo con IA (vetado).
5. **Resolver los `.csproj` sin commitear** (ver Estado actual).
6. **Refactor mayor de la sección de tareas** (planeado por el usuario; el two-column + stack móvil de hoy es el estado de partida).
7. **Pulir UI (hallazgos restantes):** `autocapitalize="none"`+`spellcheck="false"` en username, apóstrofe tipográfico en "Don't", label "In Progress" para `InProgress`, `.task-main` con `min-width:0`+`overflow-wrap:anywhere`, `touch-action: manipulation`.
8. **Supabase como segundo storage:** `Npgsql.EntityFrameworkCore.PostgreSQL` en `Basic.Data`, provider por config (`"Database": "sqlite" | "supabase"`), `Basic.Core` intacto.
9. **Entregable GenAI:** documentar prompts, validación del código generado (tests, review, caso CVE OpenAPI y su reversión, anti-enumeración, bugfix zoneless) y manejo de edge cases/auth.
10. **Presentación:** guion con user story, capas, demo y decisiones.

## Pendientes/backlog acordado
- **Refresh tokens sin rotación/revocación** (stateless a propósito): pasar a tokens almacenados solo si algún día hace falta matar sesiones server-side.
- **Refreshes paralelos:** varios 401 simultáneos disparan varios `/refresh` (inofensivo, stateless); single-flight solo si molesta.
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
dotnet test                         # 37 tests backend
cd Basic.UI; npm test               # 6 tests UI
```
- Smoke rápido de APIs (con el stack arriba): `bash .claude/skills/run-basic-demo/smoke.sh` — curl-only, termina en `SMOKE PASSED`. La UI se prueba a mano.
- Credenciales seed: `demo` / `Password123!`. Sin secretos externos; la única key es la JWT dev de `appsettings.json` (misma en ambas APIs; los TTLs `Jwt:AccessTokenMinutes`/`Jwt:RefreshTokenDays` viven en el del STS).
- Reglas de trabajo permanentes: ninguna respuesta de auth revela el motivo del rechazo (anti user-enumeration); la UI es zoneless — todo estado escrito en callbacks async debe ser signal; nunca generar imágenes con IA para este repo; el drawer de login entra por la derecha; NO usar agent-browser; NUNCA matar procesos corriendo del usuario — si bloquean build/test, reportar "listo para probar" y seguir.
