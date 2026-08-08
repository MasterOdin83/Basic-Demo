# Handoff — 2026-08-08

## Qué se hizo

Todo en `Basic.UI` (showcase para la entrevista técnica de EPAM), sin commitear todavía:

- Rebrand completo: "Ballastlane - .NET - Technical Interview Exercise" → "EPAM" en título, header y eyebrow (`index.html`, `app.html`, `home.html`, `app.spec.ts` actualizado).
- Copy condensado en las 6 fact-cards y en "Requirements coverage" (de párrafos a una línea cada uno).
- **Requirements coverage ahora es MD-driven**: contenido vive en `public/requirements-coverage.md`, se fetchea y parsea en runtime (`home/requirements.ts`, parser propio sin dependencias, con `requirements.spec.ts`). Editar el `.md` es suficiente para cambiar la página — no hace falta tocar el template. Verificado contra `REQ-BLA-technical-exercise.md` (el brief real del ejercicio) — se agregó la línea de "Presentation & code review" que faltaba.
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
