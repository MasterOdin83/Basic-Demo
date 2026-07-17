# Handoff — 2026-07-17 (cierre 2)

## Qué se hizo
- Renombre visible del proyecto a **"Ballastlane - .NET - Technical Interview Exercise"** (`index.html` title, topbar `app.html`, `README.md`, assert de `app.spec.ts`).
- **Landing para revisores** en la página de login (`Basic.UI/src/app/login/login.html`): tema Ballast Lane (paleta tomada de ballastlane.com/ai-solutions — fondo #0a0a0a, acento #FE5A0B, headline serif con gradiente), tarjetas "how it was built" (stack, arquitectura, TDD, SDLC, GenAI), sección **Requirements coverage** que mapea cada requerimiento del brief V6 a su implementación (el write-up GenAI marcado como pendiente), y hint con credenciales demo.
- **Animaciones** (filosofía Emil Kowalski, CSS puro en `styles.css`): entrada escalonada solo en la landing (ease-out fuerte `cubic-bezier(0.23,1,0.32,1)`, ≤350ms), `scale(0.97)` en `:active` de botones, hovers dentro de `@media (hover: hover) and (pointer: fine)`, `prefers-reduced-motion` conserva fades y elimina movimiento. La página de tasks NO tiene animación de entrada (se ve con frecuencia, a propósito).
- **Anti user-enumeration** (pedido explícito de Héctor, aplica a todo diseño futuro): el registro responde `201 Created` o `400` SIN cuerpo ni motivo — `AuthController` ya no expone `ex.Message` y `Program.cs` de BasicSTS usa `SuppressMapClientErrors = true`. La regla de contraseña (≥8) se valida en la UI (`login.ts`). Test de regresión `Register_rejection_reveals_no_reason` en `EndpointTests.cs`. Decisión documentada en REQ v0.3.
- **Deploy QA**: pull del workflow `master_qa-demo-sts.yml` que Azure generó al crear el App Service del STS y fix para apuntarlo a `BasicSTS.API/BasicSTS.API.csproj` (el genérico `dotnet build` falla en repo multi-proyecto). `environment.prod.ts` con los orígenes reales y esquema `https://` (sin esquema, HttpClient los trata como rutas relativas → 404).
- REQ actualizado a v0.3; tests: **32 backend (xUnit) + 4 UI (vitest), todos verdes**; build de producción de la UI verificado.

## Estado actual
- Push a `master` dispara TRES workflows: SWA (UI), `master_qa-demo-api.yml` (tasks API → qa-demo-api) y `master_qa-demo-sts.yml` (STS → qa-demo-sts).
- App Services creados: `qa-demo-sts-h3dxfshgapatdsdf.centralus-01.azurewebsites.net` y `qa-demo-api-a3dhdwf0aqdbcnck.centralus-01.azurewebsites.net`. UI en SWA `https://thankful-sea-0308a2310.azurestaticapps.net`.
- `appsettings.QA.json` de ambas APIs ya apunta CORS a la SWA real.
- Limitación QA conocida: cada App Service tiene su propio SQLite → un usuario registrado en el STS no existe en la DB del tasks API (FK de Tasks.UserId); la cuenta seed `demo`/`Password123!` funciona en ambos porque el seed es idéntico. Supabase lo resuelve.

## Next steps (en orden de prioridad)
1. **Azure portal (bloqueante para que QA funcione):** en AMBOS App Services agregar App Settings `ASPNETCORE_ENVIRONMENT=QA` y `Jwt__Key` (mismo valor en los dos; hoy usan la key dev de `appsettings.json`). Luego smoke test en la SWA: login `demo` → CRUD → logout.
2. **Verificar los 3 workflows verdes** tras el push (pestaña Actions del repo `MasterOdin83/Basic-Demo`).
3. **Pulir UI (hallazgos restantes de la review 2026-07-17):** submits deshabilitados hasta llenar campos (deben validar al enviar), `autocapitalize="none"`+`spellcheck="false"` en username, apóstrofe tipográfico en "Don't", labels "In Progress" para el enum `InProgress`, headings en Title Case, `.task-main` con `min-width:0`+`overflow-wrap:anywhere`, `touch-action: manipulation`. (Hover states y `<title>` ya quedaron hechos.)
4. **Supabase como segundo storage:** `Npgsql.EntityFrameworkCore.PostgreSQL` en `Basic.Data`, provider por config (`"Database": "sqlite" | "supabase"`), `Basic.Core` intacto.
5. **Entregable GenAI:** documentar prompts, validación del código generado (tests, review, caso CVE OpenAPI, caso anti-enumeración) y manejo de edge cases/auth.
6. **Presentación:** guion con user story, capas, demo y decisiones.

## Pendientes/backlog acordado
- Migraciones EF cuando el esquema evolucione (hoy `EnsureCreated` con guard anti-carrera).
- JWT key a secret store para despliegue real (hoy dev-only en `appsettings.json`; en QA se sobreescribe como App Setting — ver next step 1).

## Cómo retomar
```powershell
dotnet run --project BasicSTS.API   # terminal 1 → http://localhost:5143
dotnet run --project Basic.API      # terminal 2 → http://localhost:5216
cd Basic.UI; npm install; npm start # terminal 3 → http://localhost:58906
dotnet test                         # 32 tests backend
cd Basic.UI; npm test               # 4 tests UI
```
- Credenciales seed: `demo` / `Password123!`. Sin secretos externos; la única key es la JWT dev de `appsettings.json` (misma en ambas APIs).
- Regla de diseño permanente: ninguna respuesta de auth debe revelar el motivo del rechazo (anti user-enumeration).
