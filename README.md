# Ballastlane - .NET - Technical Interview Exercise

Simple task management app built for the BLA technical exercise: Clean Architecture, TDD, ASP.NET Core Web API, SQLite, and Angular.

**User story:** *As a registered user, I want to create, view, edit, and delete my personal tasks (with title, description, status, and due date) so I can keep track of my work. Only I can see and manage my own tasks.*

## Architecture

```
Basic.UI       Angular 21 UI (login + tasks CRUD)
Basic.API      Tasks CRUD Web API (JWT-protected)        ─┐
BasicSTS.API   Auth Web API: register / login / me       ─┤→ Basic.Data → Basic.Core
Basic.Data     EF Core + SQLite repositories, seed        │
Basic.Core     Entities, business rules, validations — no dependencies
Basic.Test     xUnit: services (fakes), repositories (in-memory SQLite), endpoints (WebApplicationFactory)
```

- **Basic.Core** is the business logic layer: it has zero package/project dependencies. Services validate input (required title, valid status, password rules, unique username) and enforce ownership (a user can only touch their own tasks). Passwords are hashed with PBKDF2 (BCL only).
- **Basic.Data** implements the `ITaskRepository` / `IUserRepository` interfaces defined in Core using EF Core + SQLite. Swapping storage (e.g. Supabase/Postgres) is a provider + connection-string change.
- The database is a single file, `basic-demo.db`, created and seeded automatically at first API startup. Open it on demand with any SQLite viewer. Tables: `Users` (Id PK, Username unique, PasswordHash) and `Tasks` (Id PK, Title, Description, Status, DueDate, UserId FK).

## Run it

Prerequisites: .NET 10 SDK, Node 20+.

```powershell
# Terminal 1 — auth API (http://localhost:5143)
dotnet run --project BasicSTS.API

# Terminal 2 — tasks API (http://localhost:5216)
dotnet run --project Basic.API

# Terminal 3 — UI (http://localhost:58906)
cd Basic.UI
npm install
npm start
```

Open http://localhost:58906 and log in with the seed credentials:

| Username | Password |
|---|---|
| `demo` | `Password123!` |

The seed user comes with three example tasks. You can also register a new account from the login page.

## Tests

```powershell
dotnet test            # 30 backend tests: services, repositories, API endpoints
cd Basic.UI; npm test  # UI tests (vitest)
```

## API summary

**BasicSTS.API** (`http://localhost:5143`)

| Verb | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | anonymous | Create user (min 8-char password) |
| POST | `/api/auth/login` | anonymous | Returns JWT (8h) |
| GET | `/api/auth/me` | Bearer | Current user info |

**Basic.API** (`http://localhost:5216`)

| Verb | Route | Auth | Description |
|---|---|---|---|
| GET | `/api/tasks/statuses` | anonymous | Available status values |
| GET | `/api/tasks` | Bearer | Current user's tasks |
| GET | `/api/tasks/{id}` | Bearer | One task (404 if not yours) |
| POST | `/api/tasks` | Bearer | Create task |
| PUT | `/api/tasks/{id}` | Bearer | Update task |
| DELETE | `/api/tasks/{id}` | Bearer | Delete task |

## Notes

- The JWT signing key in `appsettings.json` is a dev-only value shared by both APIs; in a real deployment it would live in a secret store.
- `EnsureCreated()` is used instead of migrations — appropriate for a file-based demo DB, switch to migrations when the schema evolves.
- Planned second storage backend: Supabase (Postgres) via the Npgsql EF Core provider — same repositories, config-selected.
