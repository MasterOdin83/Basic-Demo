# Handoff — 2026-07-17 (cierre 3)

## Qué se hizo
- **Bugfix raíz "el mensaje de error de registro nunca aparece":** la app Angular 21 es zoneless (no hay zone.js en `package.json`), así que el estado escrito dentro de callbacks de `subscribe` no re-renderizaba. `Login` y `Tasks` ahora usan **signals** para todo estado async (`error`, `info`, `busy`, `tasks`, `editingId`…). Sin este fix ningún mensaje async del app era visible.
- **Rutas:** `Tasks` movido a `/tasks` (antes ruta vacía); `/` y rutas desconocidas redirigen a la landing `/login`; el brand del topbar es link a `/` (siempre lleva a la página principal) y el chip **"My tasks"** (`routerLinkActive` + `aria-current`) marca dónde estás.
- **Formulario de tareas → Reactive Forms, alta y edición separadas:** el card "New task" es solo para altas; **Edit abre un editor inline en la propia fila** con su propio `FormGroup` — editar nunca pisa un alta en curso (test `editing a task leaves the add form untouched`). El botón Add **ya no se deshabilita por validación**: valida al enviar, muestra "Title is required." bajo el campo con animación de entrada, focus + shake WAAPI en el campo inválido (respeta `prefers-reduced-motion`); título no puede ser solo espacios; descripción es `textarea`.
- **Default Pending pineado:** `TaskItemStatus.Pending = 0` ⇒ POST sin `status` crea Pending; test `Create_without_status_defaults_to_pending`.
- **Landing logueado:** el auth card muestra tarjeta de identidad (avatar + "Logged in as {user}" + CTA "Go to my tasks") en vez del form. La cuenta `demo` usa `avatars/leonidas.png`; **el archivo NO existe aún** — hay que copiarlo a `Basic.UI/public/avatars/leonidas.png` (NUNCA generarlo con IA; el usuario lo vetó). Las demás cuentas (y demo mientras falte el archivo) muestran placeholder CSS con la inicial.
- **OpenAPI + Scalar reintroducidos** en ambas APIs (`Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore`): documento en `/openapi/v1.json` y explorador en **`/scalar`** para probar endpoints sin Postman. La razón del veto anterior (Microsoft.OpenApi parcheado rompía el source generator de .NET 10) ya no se reproduce con las versiones actuales; 2 tests de regresión lo cubren.
- **Security headers en la SWA** (`Basic.UI/public/staticwebapp.config.json`, la nota era C): CSP estricta (`'self'` + `connect-src` solo a los 2 orígenes QA + `frame-ancestors 'self'`; `style-src` mantiene `'unsafe-inline'` porque Angular inyecta estilos inline), `X-Frame-Options: SAMEORIGIN`, `Permissions-Policy` negando camera/mic/geolocation/payment. Si cambian los hostnames de las APIs hay que actualizar la CSP.
- **Polish UI:** bordes izquierdos por status en tarjetas (accent = InProgress, verde = Done), entrada escalonada de la lista (35ms/item, track por id ⇒ solo animan tarjetas nuevas), animación de entrada de mensajes error/info (también en login), focus rings en links del topbar.
- REQ actualizado a **v0.4**; tests: **35 backend (xUnit) + 6 UI (vitest), todos verdes**; build de producción de la UI verificado.

## Estado actual
- Push a `master` dispara TRES workflows: SWA (UI), `master_qa-demo-api.yml` (tasks API → qa-demo-api) y `master_qa-demo-sts.yml` (STS → qa-demo-sts).
- UI en SWA `https://thankful-sea-0308a2310.azurestaticapps.net`; App Services `qa-demo-sts-h3dxfshgapatdsdf` / `qa-demo-api-a3dhdwf0aqdbcnck` (centralus-01).
- Los 2 workflows de App Service **siguen bloqueados** por el subject OIDC (ver next step 1) — Scalar/OpenAPI no llegarán a QA hasta arreglarlo.
- Limitación QA conocida: cada App Service tiene su propio SQLite → usuarios registrados en el STS no existen en la DB del tasks API; la cuenta seed `demo`/`Password123!` funciona en ambos. Supabase lo resuelve.

## Next steps (en orden de prioridad)
1. **Copiar el avatar:** poner el archivo del usuario en `Basic.UI/public/avatars/leonidas.png` (la UI ya lo referencia; con el archivo presente aparece solo para `demo`). No generarlo con IA.
2. **Azure portal — arreglar el login OIDC de los deploys (bloqueante; los 2 workflows de App Service NUNCA han pasado):** fallan en "Login to Azure" con `AADSTS700213`. Para CADA App Service (`qa-demo-api` y `qa-demo-sts`): Deployment Center → identidad federada → poner como **Subject identifier** exactamente
   `repo:MasterOdin83@55357287/Basic-Demo@1304366521:ref:refs/heads/master`
   (si el tipo "GitHub" no lo permite, usar tipo "Other" con issuer `https://token.actions.githubusercontent.com` y audience `api://AzureADTokenExchange`). Después re-run de los workflows fallidos desde Actions.
3. **Azure portal — App Settings:** en AMBOS App Services `ASPNETCORE_ENVIRONMENT=QA` y `Jwt__Key` (mismo valor en los dos). Smoke test en la SWA: login `demo` → CRUD → logout; verificar `/scalar` en ambas APIs y la nota de securityheaders.com tras el deploy.
4. **Pulir UI (hallazgos restantes):** `autocapitalize="none"`+`spellcheck="false"` en username, apóstrofe tipográfico en "Don't", labels "In Progress" para `InProgress`, `.task-main` con `min-width:0`+`overflow-wrap:anywhere`, `touch-action: manipulation`. (El hallazgo "submits deshabilitados hasta llenar campos" ya quedó resuelto con la validación al enviar.)
5. **Supabase como segundo storage:** `Npgsql.EntityFrameworkCore.PostgreSQL` en `Basic.Data`, provider por config (`"Database": "sqlite" | "supabase"`), `Basic.Core` intacto.
6. **Entregable GenAI:** documentar prompts, validación del código generado (tests, review, caso CVE OpenAPI y su reversión, anti-enumeración, bugfix zoneless) y manejo de edge cases/auth.
7. **Presentación:** guion con user story, capas, demo y decisiones.

## Pendientes/backlog acordado
- Si Scalar no ofrece campo Bearer en su pestaña de auth (no verificado en runtime), declarar el security scheme en el documento OpenAPI de `Basic.API`.
- Headers de seguridad en los App Services (el scan calificado era el de la SWA); middleware pequeño si se pide.
- Migraciones EF cuando el esquema evolucione (hoy `EnsureCreated` con guard anti-carrera).
- JWT key a secret store para despliegue real (en QA se sobreescribe como App Setting — next step 3).
- Solo una fila puede estar en edición a la vez (decisión deliberada; mapa de forms por fila si algún día se quiere multi-edición).

## Cómo retomar
```powershell
dotnet run --project BasicSTS.API   # terminal 1 → http://localhost:5143 (/scalar para explorar)
dotnet run --project Basic.API      # terminal 2 → http://localhost:5216 (/scalar para explorar)
cd Basic.UI; npm install; npm start # terminal 3 → http://localhost:58906
dotnet test                         # 35 tests backend
cd Basic.UI; npm test               # 6 tests UI
```
- Credenciales seed: `demo` / `Password123!`. Sin secretos externos; la única key es la JWT dev de `appsettings.json` (misma en ambas APIs).
- Reglas de diseño permanentes: ninguna respuesta de auth revela el motivo del rechazo (anti user-enumeration); la UI es zoneless — todo estado escrito en callbacks async debe ser signal; nunca generar imágenes con IA para este repo.
