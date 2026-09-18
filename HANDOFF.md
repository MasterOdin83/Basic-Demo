# Handoff — 2026-09-17 (cierre) · demo contra el STS de Turbo y el API de Spartan; Angular 22 (`master` = QA)

## Estado actual
- `master` `3f18e14` = QA thankful-sea. La UI entra por correo + reCAPTCHA contra `qa-turboempresa-api` (STS) y pide sus tareas a `qa-spartanit-api` (`/api/tasks`). Funciona cuando ambos APIs tengan `Jwt__Key` y el de Spartan esté desplegado. `Basic.API`, `BasicSTS.API`, `qa-demo-sts` y sus workflows ya no se usan.

## Next steps
1. Héctor: `Jwt__Key` en los dos App Services; dominio `thankful-sea-0308a2310.7.azurestaticapps.net` (y `localhost`) en la key de reCAPTCHA en Google Cloud; decidir si borro `Basic.API`/`BasicSTS.API` y si la UI del demo se mueve al sitio de Spartan.
2. Probar: login por correo, `/tasks` con las 3 tareas sembradas, recarga conserva la sesión.

---
# Handoff — 2026-09-17 (noche, 2) · Basic.UI en Angular 22

## Resumen para Héctor
- **Hecho**: `ng update @angular/cli@22 @angular/core@22` en `Basic.UI`: Angular 22.1.7, CLI 22.1.8, TypeScript 6.0. Migraciones automáticas: `changeDetection: ChangeDetectionStrategy.Eager` en cada componente existente (en v22 el default pasa a OnPush; Eager conserva el comportamiento de antes), `withXhr()` en `provideHttpClient` (v22 usa fetch por default), diagnósticos `nullishCoalescingNotNullable`/`optionalChainNotNullable` suprimidos en `tsconfig.app.json`. `engines.node` en `package.json` (`^22.22.3 || ^24.15.0`) para que Oryx elija un Node que Angular 22 acepte. `ng build` verde; 25 tests de UI verdes (el spec del guard se reescribió para el guard nuevo: token en memoria o refresh por cookie).
- **Ojo en el primer deploy**: si el build de la Static Web App falla por versión de Node, la salida es compilar en el runner (`actions/setup-node` 24 + `skip_app_build`), como ya hace `prod-hostinger-ui.yml` de Turbo.
- **Siguiente (cuando lo pidas)**: reglas de Angular 22 como archivo de reglas por repo (la decisión pendiente es standalone vs NgModule; hoy los tres bootstrapean con NgModule) y quitar los `Eager` componente por componente probando en QA.

---
# Handoff — 2026-09-17 (noche) · el demo usa el STS de TurboEmpresa y el API de Spartan IT (`master` = QA)

## Resumen para Héctor
- **Decisión tuya**: un solo STS, el API de TurboEmpresa (`/api/auth`, rama `sts-merge` de TurboEmpresa). `qa-mercenaries-sts` no tiene sentido: borrado su workflow (`master_qa-mercenaries-sts.yml`); borra el App Service cuando quieras.
- **Hecho (`sts-turbo`)**: `environment.prod.ts` → `stsUrl` = `qa-turboempresa-api…`; `environment.ts` → `http://localhost:5169` (TurboEmpresa.API local). Login con **correo** (el STS de Turbo registra por email, mínimo 8 caracteres). `AuthService`: token de acceso solo en memoria (fuera `localStorage`), `login`/`refresh`/`logout` con `withCredentials` (la cookie HttpOnly `refresh` del STS); el guard de `/tasks` y `App` rehacen la sesión desde la cookie al recargar. El interceptor no cambia (Bearer + refresh ante 401). Fuera el widget de Turnstile de la UI (`turnstile.ts`, script): login y registro piden un token invisible de reCAPTCHA Enterprise con la site key de Turbo (`recaptchaSiteKey`), que el STS verifica. `Basic.API/appsettings.json` valida `Issuer`/`Audience` `TurboEmpresa` con la clave de desarrollo de Turbo; `BasicSTS.API` y los tests quedaron con los mismos valores para que el demo local siga siendo consistente. 39 tests verdes, `ng build` verde.
- **`qa-demo-api` ya no existe** (Héctor, 2026-09-17 noche): el Security Demo es parte de Spartan IT. Las tareas viven en `SpartanIT.API` `/api/tasks` (mismo contrato que Basic.API; siembra 3 tareas por usuario la primera vez; rama `demo-tasks` → `main` de SpartanIT) y `environment.prod.ts` apunta a `qa-spartanit-api`. Mergeado a `master` (QA thankful-sea). **Te toca**: `Jwt__Key` en `qa-spartanit-api` (la misma de Turbo) y en Google Cloud → reCAPTCHA → key `6LcyD1Yt…` agregar `thankful-sea-0308a2310.7.azurestaticapps.net` y `localhost`. `Basic.API`, `BasicSTS.API`, `qa-demo-sts` y sus workflows sobran: dime si los borro.
- Ojo: Turbo tiene en CORS a thankful-sea (con y sin `.7.`); `TasksController` sigue con `int.Parse` del id (los ids de Turbo son bigint desde 1: sirve).

## Estado actual
- `master` = QA thankful-sea: UI contra el STS de Turbo (`qa-turboempresa-api`) y el API de Spartan (`qa-spartanit-api`); funciona cuando ambos tengan `Jwt__Key` y el de Spartan esté desplegado.

---
# Handoff — 2026-09-15 (sin BFF: JWT del STS + captcha Turnstile + rate limiting)

## Resumen para Héctor
- **Hecho (`master` = QA thankful-sea + qa-demo-sts + qa-demo-api)**: fuera el BFF (cookie HttpOnly + `ISessionStore` de `7bfb10d`); de vuelta el flujo JWT del STS (access 3 min + refresh 1 día, interceptor Bearer con refresh silencioso), como pediste: cada UI con su API y aparte el STS. Captcha de Cloudflare Turnstile en login y registro (el STS verifica el token; 403 si falla) y rate limiting en las dos APIs (10/min por IP en login/register/refresh, 100/min por IP global; 429). Se conservan el drawer móvil y todo lo visual. 39 tests backend + 24 de UI verdes; `ng build` verde.
- **QA funciona con las claves de PRUEBA de Turnstile** (siempre pasan: site key `1x00000000000000000000AA` en `environment.prod.ts`, secreto en `appsettings.QA.json`). Para que de verdad frene bots: dash.cloudflare.com → Turnstile → «Add widget» (hostname `thankful-sea-0308a2310.7.azurestaticapps.net`, y luego el dominio real) → site key a `environment.prod.ts` y secreto al App Setting `Captcha__Secret` del STS. Fuera de Development el STS no arranca sin secreto.
- **STS compartido `qa-mercenaries-sts`** (`qa-mercenaries-sts-gcexaggxdme7gffs.westus3-01.azurewebsites.net`): ya tiene workflow (`.github/workflows/master_qa-mercenaries-sts.yml`, publica `BasicSTS.API`) y `appsettings.QA.json` admite las tres UIs de QA en CORS (thankful-sea, proud-coast, mango-desert). La UI sigue apuntando a `qa-demo-sts` para no romper el demo mientras el nuevo no despliega. Te toca: (1) Deployment Center del App Service → GitHub Actions (repo Basic-Demo, rama `master`): copia los 3 secrets que genere a los nombres del workflow (`AZUREAPPSERVICE_CLIENTID_QA_MERCENARIES_STS`, `…TENANTID…`, `…SUBSCRIPTIONID…`) o borra mi workflow y deja el de Azure apuntando a `BasicSTS.API/BasicSTS.API.csproj`; (2) App Settings `ASPNETCORE_ENVIRONMENT=QA`, `Captcha__Secret`, `Jwt__Key` (la misma que valide cada API que use sus tokens); (3) con el deploy en verde, cambiar `stsUrl` en `environment.prod.ts` y push. Cuando TurboEmpresa y Spartan usen este STS necesitarán validar sus JWT con esa misma `Jwt__Key` y audiencia: eso entra con tu re-arquitectura.
- **Queda de tu lado además**: revisar el login en QA (widget de Turnstile visible en el drawer; el botón se habilita cuando pasa), probar 11 logins fallidos seguidos → «Too many attempts».

## Qué se hizo
- Revertidos a la versión JWT (`d9d9794`): `BasicSTS.API/Program.cs` y `Controllers/AuthController.cs`, `Basic.API/Program.cs`, `appsettings.json` de ambas, `Basic.UI/src/app/auth.service.ts`, `auth.interceptor.ts`, `auth.guard.ts` (+ spec; redirigen a `/`, ya no hay `/login`), `app.ts` (sin `restore()`), `Basic.Test/EndpointTests.cs`. Borrados: `SessionAuthenticationHandler.cs` (×2), `Basic.Core/Entities/Session.cs`, `Basic.Core/Repositories/ISessionStore.cs`, `Basic.Data/EfSessionStore.cs`; `AppDbContext`/`DataExtensions` sin `Sessions`.
- Nuevos: `BasicSTS.API/CaptchaVerifier.cs`, `Basic.UI/src/app/turnstile.ts` (directiva `appTurnstile`, render explícito, reset tras cada envío), `.github/workflows/master_qa-mercenaries-sts.yml`. `CredentialsRequest` con `CaptchaToken`; `[EnableRateLimiting("auth")]` en el controlador (`/me` exento); `UseRateLimiter` + `UseForwardedHeaders` (XForwardedFor+Proto, fuera de Development) en ambas APIs; `Captcha:Secret` en `appsettings*.json`; `turnstileSiteKey` en `environment*.ts`; script de Turnstile en `index.html`; `login.ts|html` con token, mensajes para 403/429 y botón bloqueado hasta pasar el captcha. Docs: README, `requirements-coverage.md`, `REQ-…md`, `AGENT-OPERATING-NOTES.md`, `home.html` (fact-card de seguridad).
- Tests: `TestApp.Create(captchaSecret)` apaga el captcha por defecto; nuevos `Login_and_register_without_captcha_token_are_forbidden_when_captcha_is_configured` y `Login_is_rate_limited_after_10_attempts_per_minute`; fuera el test rojo del BFF.

## Estado actual
- `master` pusheado (SWA + qa-demo-sts + qa-demo-api se redeployan solos). QA usa claves de prueba de Turnstile. `qa-mercenaries-sts` sin secrets ni App Settings → su workflow falla hasta el paso (1).

## Next steps (en orden)
1. Héctor: Turnstile real + secrets/App Settings de `qa-mercenaries-sts` + cambio de `stsUrl`.
2. Fase 2 (Docker/K8s) y BFF/Redis siguen en `AGENT-OPERATING-NOTES.md`, diferidos hasta que todo corra en Azure.

---

# Handoff — 2026-08-08

## Qué se hizo

Todo en `Basic.UI` (showcase para el ejercicio técnico "Security Demo"), sin commitear todavía:

- Rebrand completo: identidad de cliente original → "Security Demo" en título, header y eyebrow (`index.html`, `app.html`, `home.html`, `app.spec.ts` actualizado).
- Copy condensado en las 6 fact-cards y en "Requirements coverage" (de párrafos a una línea cada uno).
- **Requirements coverage ahora es MD-driven**: contenido vive en `public/requirements-coverage.md`, se fetchea y parsea en runtime (`home/requirements.ts`, parser propio sin dependencias, con `requirements.spec.ts`). Editar el `.md` es suficiente para cambiar la página — no hace falta tocar el template. Verificado contra `REQ-flagship-demo-technical-exercise.md` (el brief real del ejercicio) — se agregó la línea de "Presentation & code review" que faltaba.
- Sección `#about` nueva con bio real del dueño del sitio (ya no es placeholder) y link a `https://proud-coast-051f45010.7.azurestaticapps.net/en/about` (Spartan IT, vía `environment.ts`/`environment.prod.ts` — local apunta a `localhost:55112`, prod a la URL real).
- **Bug de gradiente cortado**: causado por `overflow:hidden` en `.hero` cortando el glow que intencionalmente se sale del cuadro. Arreglado moviendo el overflow-hidden a `body` (solo el eje horizontal).
- **Bug del headline (dos veces roto, revertido)**: se intentó un split por palabra para animación staggered; falló dos veces (palabras pegadas, luego palabras cada una en su línea). Revertido por completo a texto plano simple — no reintentar la técnica sin evidencia sólida de por qué fallaba.
- Animación: fondo con 3 blobs de aura (drift lento), glassmorphism en cards, glow al hover, reveals por scroll (`home/reveal.ts`, reemplaza animación que solo corría una vez al cargar), spotlight que sigue el cursor (`home/spotlight.ts`, solo con mouse real, respeta `prefers-reduced-motion`), splash de entrada con parallax (`home/splash-parallax.ts`) que lleva al hero actual.
- Paleta: naranja (`#fe5a0b`) reemplazado por cyan→violeta (`#22d3ee` → `#7c3aed`), contraste verificado (~10.4:1).
- `AGENT-OPERATING-NOTES.md` (nuevo, raíz del repo): reglas de gobierno para agentes (economía de tokens, máx. 2 reintentos y escalar) + esqueleto del plan "Step 2" (Docker/K8s).

## Estado actual

- **No commiteado nada de esta sesión.** Todo vive en el working directory.
- Tests de `Basic.UI` en verde en la última verificación confirmada (24/24). Build limpio.
- **Pendiente sin terminar** (última tarea en curso cuando se hizo el hard-stop, no confirmada): páginas de detalle por card (`/details/:id`) con la intención real de cada decisión + código citado del repo — pedido, no verificado si se alcanzó a implementar. Revisar `git status`/`git diff` en `Basic.UI/src/app/home/` y buscar rutas nuevas antes de asumir que no se hizo.
- **Chart de economía de tokens** (pedido para la card "Built with GenAI", números reales de esta sesión ~831K tokens en 6 pasadas) — mismo caso, pedido pero no confirmado si se completó.
- Bases de datos: `Basic.Data/AppDbContext.cs` tiene un cambio de 1 línea sin verificar qué es — revisar antes de commitear.

## Next steps (orden de prioridad)

1. `git diff Basic.UI/src/app/home/` completo para confirmar si las páginas de detalle y el chart de economía se llegaron a implementar antes del hard-stop.
2. Correr `dotnet test`/`ng test` frescos en ambos repos para confirmar estado real (no asumir el último número reportado).
3. Decidir si se commitea todo junto o se separa (ver nota de riesgo abajo).
4. Fase 2 (una vez esto esté commiteado y estable): Docker/Kubernetes — plan en `AGENT-OPERATING-NOTES.md`.
5. Backend BFF/Redis en `BasicSTS.API` (mover tokens del front end a sesión server-side) — diseño ya acordado con el usuario, implementación NO empezó (solo un test en rojo escrito en `Basic.Test/EndpointTests.cs`, bloqueado porque `Basic.API`/`BasicSTS.API` estaban corriendo localmente y bloqueaban el build de test).

## Riesgo a tener en cuenta antes de push

Nada de esto se ha pusheado. Si se pushea a `main`, dispara el deploy de Basic.UI vía Azure Static Web Apps (workflow ya existente) — confirmar con el usuario antes, ya que incluye cambios de branding/contenido que él debe aprobar visualmente primero.

## Pendientes/backlog acordado

- Redis + BFF en `BasicSTS.API` (diseño acordado, no implementado).
- CV real para el flujo de Spartan IT (ver handoff de ese repo) — este repo solo enlaza a Spartan IT, no sirve el CV directamente.

## Cómo retomar

- Repo: `C:\Ballast Lane Repo\Basic Demo`. `Basic.UI` es Angular 21 (`ng serve`, puerto 58906), `Basic.API`/`BasicSTS.API` son .NET (`dotnet run`, puertos 5216/5143).
- El usuario corre los procesos, no los agentes — solo pedir que reinicie si algo no conecta.
- `AGENT-OPERATING-NOTES.md` tiene las reglas de gobierno vigentes (máx. 2 reintentos, economía de tokens).
