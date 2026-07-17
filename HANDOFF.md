# Handoff — 2026-07-17

## Qué se hizo
- MVP completo del ejercicio BLA (ver `REQ-BLA-technical-exercise.md` v0.2, sección "Estado de implementación").
- Nuevas librerías `Basic.Core` (negocio puro: entidades, validaciones, ownership, PBKDF2) y `Basic.Data` (EF Core + SQLite, repos, seed) integradas al `.slnx` y referenciadas por ambas APIs.
- `BasicSTS.API`: register / login con JWT / `me` autorizado. `Basic.API`: CRUD de tasks con `[Authorize]` + `statuses` anónimo. CORS configurado para la UI.
- `Basic.UI` (Angular 21, NgModule): login/registro, authGuard, interceptor JWT, página de tasks con CRUD completo, estilos responsive.
- Tests: 31 backend (xUnit: servicios con fakes, repos con SQLite in-memory, endpoints con `WebApplicationFactory`) + 4 UI (vitest). Todos verdes, sin warnings.
- Fix de carrera al arrancar ambas APIs a la vez (compartían `EnsureCreated` sobre el mismo archivo SQLite): guard en `Basic.Data/DataExtensions.cs`.
- OpenAPI eliminado de ambas APIs (CVE GHSA-v5pm-xwqc-g5wc sin versión parcheada compatible con el source generator de .NET 10).

## Estado actual
- Todo compila y corre local: STS en `http://localhost:5143`, tasks API en `http://localhost:5216`, UI en `http://localhost:58906` (puerto fijado en `Basic.UI/angular.json`; el CORS de ambas `appsettings.json` lo refleja).
- GitHub Actions: workflow de Azure Static Web Apps (generado desde el portal) despliega SOLO la UI en cada push a `master`.
- Environments listos: UI usa `src/environments/environment.ts` (dev, localhost) / `environment.prod.ts` (QA, con `fileReplacements` en build production — hoy con URLs `REPLACE-*` placeholder); APIs tienen `appsettings.QA.json` (activar con `ASPNETCORE_ENVIRONMENT=QA`).
- DB: `basic-demo.db` en la raíz del repo (gitignored), auto-creada y seeded al arrancar; borrarla y reiniciar la regenera.
- Credenciales seed: `demo` / `Password123!` (más 3 tareas de ejemplo).
- Smoke test manual verificado: login → CRUD de tasks → 401 anónimo.

## Next steps (en orden de prioridad)
1. **Pulir la UI (review de Web Interface Guidelines, 2026-07-17)** — hallazgos concretos, todos chicos; el único cambio de comportamiento es el de submits:
   - `login.html:27` y `tasks.html:27`: los submit se deshabilitan hasta llenar campos; deben quedar habilitados (solo `busy` los bloquea) y validar al enviar con error inline ("Title is required" / credenciales).
   - `login.html:7`: username sin `autocapitalize="none"` + `spellcheck="false"`.
   - `login.html:33`: apóstrofe recto en "Don't" → `Don’t`.
   - `tasks.html:21,67`: el select de status muestra el enum crudo `InProgress` → label "In Progress" (mandando el valor enum igual).
   - `tasks.html:2,42` y `login.html:2`: headings a Title Case ("New Task", "My Tasks", "Log In"...).
   - `styles.css:96`: botones sin estado `:hover`.
   - `styles.css:142`: `.task-main` (hijo flex) sin `min-width: 0` + `overflow-wrap: anywhere` — títulos/descripciones largos sin espacios desbordan la fila.
   - `styles.css:80`: inputs/selects/buttons sin `touch-action: manipulation`.
   - `index.html:5`: `<title>` "BasicUI" → "Basic Tasks".
   - OK verificado (no tocar): labels envuelven inputs, `autocomplete`, `role="alert"/"status"`, `:focus-visible`, confirmación de delete, empty state.
2. **Completar deploy de servicios a Azure:** la tasks API ya tiene App Service (`qa-demo-api`, workflow `master_qa-demo-api.yml`, apuntado a `Basic.API` explícitamente; su URL ya está en `environment.prod.ts`) — falta ponerle `ASPNETCORE_ENVIRONMENT=QA` como App Setting. Para STS: crear su App Service (workflow análogo apuntando a `BasicSTS.API/BasicSTS.API.csproj`) con `ASPNETCORE_ENVIRONMENT=QA` (carga `appsettings.QA.json`: SQLite local al servicio + CORS hacia la SWA). Luego (a) reemplazar las URLs `REPLACE-*` en `Basic.UI/src/environments/environment.prod.ts` con los hosts reales `*.azurewebsites.net`, y (b) sobreescribir `Jwt__Key` (misma en ambos) y, si el dominio SWA real difiere, `Cors__UiOrigin` como App Settings en Azure. Nota: cada App Service tendrá su propio archivo SQLite (no compartido); Supabase resuelve eso.
3. **Supabase como segundo storage:** agregar `Npgsql.EntityFrameworkCore.PostgreSQL` a `Basic.Data`, elegir provider por config (`"Database": "sqlite" | "supabase"` + connection string) en `DataExtensions.AddBasicData`. `Basic.Core` no se toca; los repos EF son los mismos.
4. **Entregable GenAI:** documentar los prompts usados, cómo se validó el código generado (tests, smoke test, revisión del CVE de OpenAPI como ejemplo de pensamiento crítico) y manejo de edge cases/auth.
5. **Presentación:** guion con user story, capas y demo funcional.

## Pendientes/backlog acordado
- Migraciones EF (hoy `EnsureCreated`) cuando el esquema evolucione.
- Mover la JWT key a un secret store para cualquier despliegue real (hoy vive en `appsettings.json` de ambas APIs como valor dev-only, documentado en README).

## Cómo retomar
```powershell
dotnet run --project BasicSTS.API   # terminal 1
dotnet run --project Basic.API      # terminal 2
cd Basic.UI; npm install; npm start # terminal 3 → http://localhost:58906
dotnet test                         # 31 tests backend
cd Basic.UI; npm test               # 4 tests UI
```
- No hay secretos externos: la única "key" es la JWT dev de `appsettings.json` (misma en ambas APIs, debe coincidir).
