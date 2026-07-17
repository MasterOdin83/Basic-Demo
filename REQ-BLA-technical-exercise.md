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
