# REQ — .NET Technical Interview Exercise (BLA, V6)

> Fuente: `Net - BLA - Technical Interview Exercise - V6.pdf` (3 páginas).

## Overview

Desarrollar una aplicación web simple con API y capa de datos usando **.NET C#, ASP.NET MVC, Web API** y una base de datos o data store, aplicando **Clean Architecture** y **TDD**.

- El desarrollo debe partir de una **user story informal** creada por el candidato (se incluye en la presentación).
- La app permite **CRUD** de registros vía endpoints de la API.
- Además: crear un usuario, hacer login con él, y persistir la información del usuario en los datos.

## Requerimientos

### Backend

**Database**
- Base de datos (u otro storage) con al menos **una tabla/objeto/contenedor** para los datos de la app y **otro adicional para usuarios**.
- La tabla principal debe tener **identificador único (PK)** y al menos **dos campos más**.

**API**
- ASP.NET Web API con endpoints CRUD sobre los datos.
- Cada endpoint con verbos HTTP, parámetros y valores de retorno apropiados.
- Una **segunda API** con endpoints de: creación de usuario, login, y endpoints **autorizados y no autorizados**.

**Data layer**
- Capa de acceso a datos que interactúa con el storage y provee las operaciones CRUD para la API.

**Business logic layer**
- Capa de lógica de negocio con todas las reglas de negocio y validaciones.
- Debe ser **independiente** de la capa de datos y de la API.

**Unit tests**
- Tests unitarios para **todos** los componentes: data access layer, business logic layer y endpoints de la API.

### Frontend

- Integrar el backend con un framework frontend a elección (React, Vue, etc.).
- Criterios clave:
  - Responsive y user-friendly.
  - CRUD completo asociado al caso de uso implementado.
  - Código estructurado: componentes y estado organizados limpiamente.

### Submission

- **README** con instrucciones de setup y documentación necesaria.
- La app debe tener **datos y credenciales seed** para demo.

### Generative AI tools (sección del ejercicio)

Escenario: generar una RESTful API para un sistema simple de gestión de tareas:
- CRUD de tasks.
- Cada task tiene: `title`, `description`, `status`, `due_date`.
- Las tasks se asocian a un usuario (asumir que existe un modelo User básico).

Entregables del candidato:
- El **prompt** usado con su herramienta GenAI preferida (Cursor, Claude Code, Copilot, etc.) para generar el scaffold o la implementación.
- El **código generado** (o muestra representativa).
- Descripción de cómo se: **validaron** las sugerencias de la IA, **corrigió/mejoró** el output, y **manejaron** edge cases, autenticación y validaciones.

### Presentación y code review

- Presentación al panel técnico (Google Meet/Zoom, screen share de GitHub o IDE): user story, decisiones de diseño, arquitectura técnica y demo funcional.
- Después, code review con el panel: explicar decisiones de código y responder preguntas.

## Criterios de evaluación

| Criterio | Detalle |
|---|---|
| Clean Architecture | Separación de responsabilidades e independencia de componentes |
| Testing | Cobertura suficiente; TDD preferible |
| Code quality | Organizado, legible, best practices |
| Functionality | Sin errores ni bugs; deseable: sin warnings en consola del browser |
| Presentation | Clara, concisa; dominio de best practices backend y frontend |
| GenAI tools | Fluidez con herramientas GenAI y prompt engineering; pensamiento crítico al evaluar código generado por IA |

---

## Estado de implementación — v0.3 (2026-07-17)

### Cambios v0.3 (2026-07-17)
- UI renombrada a "Ballastlane - .NET - Technical Interview Exercise" (title, topbar, README).
- Login convertido en landing para revisores: tema Ballast Lane (fondo negro, acento #FE5A0B, serif + gradiente), tarjetas "how it was built" (stack, arquitectura, TDD, SDLC, GenAI), sección **Requirements coverage** que mapea cada requerimiento del brief a su implementación (pendientes marcados como tales), credenciales demo visibles y animaciones de entrada (stagger, ease-out fuerte, gated por `prefers-reduced-motion` y `hover: hover`).
- Seguridad anti-enumeración: registro responde `201` o `400` vacío (`SuppressMapClientErrors`), regla de contraseña validada en la UI, test de regresión `Register_rejection_reveals_no_reason` (32 tests backend).
- Deploy QA: workflow `master_qa-demo-sts.yml` (generado por Azure) apuntado a `BasicSTS.API.csproj`; `environment.prod.ts` con orígenes reales `https://` de ambos App Services.

### Entregado
- **User story:** "Como usuario registrado quiero crear, ver, editar y borrar mis tareas personales (title, description, status, due date) para organizar mi trabajo; solo yo puedo ver y gestionar mis tareas."
- **Clean Architecture:** `Basic.Core` (entidades + reglas de negocio, cero dependencias) ← `Basic.Data` (EF Core + SQLite, repositorios) ← `Basic.API` / `BasicSTS.API`.
- **DB:** archivo SQLite `basic-demo.db`, auto-creado y con seed al arrancar cualquiera de las APIs. Tablas `Users` (Id PK, Username único, PasswordHash) y `Tasks` (Id PK, Title, Description, Status, DueDate, UserId FK).
- **BasicSTS.API** (`:5143`): `POST /api/auth/register`, `POST /api/auth/login` (JWT HS256, 8h), `GET /api/auth/me` autorizado — cubre endpoints autorizados y no autorizados.
- **Basic.API** (`:5216`): CRUD `/api/tasks` con `[Authorize]` + ownership por usuario, `GET /api/tasks/statuses` anónimo, CORS para la UI.
- **Basic.UI** (Angular 21, `:58906`): login/registro, guard de ruta, interceptor JWT, CRUD de tareas responsive.
- **Tests (TDD):** 32 backend — servicios con fakes, repositorios con SQLite in-memory, endpoints HTTP reales con `WebApplicationFactory` — más 4 de UI (vitest).
- **README** con setup, credenciales seed (`demo` / `Password123!`) y tabla de endpoints.

### Decisiones
- SQLite en lugar de "archivo MySQL": MySQL requiere servidor; SQLite es la base de datos de archivo abrible on demand.
- **Supabase (Postgres) elegido sobre Cosmos DB** para el segundo storage: con EF Core es solo provider Npgsql + connection string, mismos repositorios; Cosmos exigiría una implementación distinta.
- OpenAPI excluido de ambas APIs (decisión 2026-07-17): las versiones parcheadas de `Microsoft.OpenApi` (CVE GHSA-v5pm-xwqc-g5wc) rompen el source generator de .NET 10; los endpoints quedan documentados en README y archivos `.http`.
- `EnsureCreated()` con guard anti-carrera (ambas APIs comparten el archivo SQLite y pueden arrancar a la vez) en lugar de migraciones.
- Anti user-enumeration (2026-07-17): el registro responde creado o `400` sin cuerpo ni motivo (nunca "username ya existe"); la regla de contraseña se valida en la UI. Aplica a todo diseño futuro.

### Pendiente
1. Segundo repositorio de datos: Supabase vía `Npgsql.EntityFrameworkCore.PostgreSQL`, selección por config (`"Database": "sqlite" | "supabase"`), sin tocar `Basic.Core`.
2. Sección GenAI del ejercicio: documentar prompts usados, cómo se validó/corrigió el código generado y manejo de edge cases/auth.
3. Presentación: guion de user story, arquitectura y demo.
