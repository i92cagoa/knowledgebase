# Knowledge Base

Desktop application to save and organize a knowledge base of notes, topics and knowledge pieces.
Notes are written in Markdown, grouped by workspaces/projects, related through tags, indexed for
search and rendered visually (graph view, like Obsidian).

## Tech Stack

| Layer | Technology |
|---|---|
| Backend services | .NET 10 - Minimal APIs - EF Core 10 - FluentValidation - Scalar (OpenAPI) |
| Database | SQLite (dev/default) or PostgreSQL - provider abstraction via EF Core |
| Observability | OpenTelemetry |
| Desktop app | AvaloniaUI (https://avaloniaui.net/) |
| Tests | xUnit - AwesomeAssertions - SpecFlow (E2E) - WebApplicationFactory (contract) |

## Structure

```
src/
  Api/             Minimal API endpoints, DI composition, Scalar + OpenAPI, health
  Application/     Feature services (I<Feature>Service) + FluentValidation validators, Feature DTOs
  Domain/          Entities, business rules, Result<T> (zero external dependencies)
  Infrastructure/  EF Core DbContext, entity configs, database provider abstraction, file storage, OpenTelemetry
  Desktop/         AvaloniaUI desktop app (tree view + note editor + markdown render)
tests/
  UnitTests/       Unit tests for Domain/Application features
  ContractTests/   API contract tests (routes, status codes, schemas via WebApplicationFactory)
  IntegrationTests/ DB operations + storage against a real provider (SQLite on disk)
  E2ETests/        SpecFlow/Gherkin end-to-end scenarios against the hosted API
```

## Architecture Rules

- **Domain has ZERO external dependencies.** It only contains entities, rules and `Result<T>`.
- **All data access through `AppDbContext`.** No repository pattern, no UnitOfWork.
- **`Result<T>` for business errors**, not exceptions.
- **Application exposes feature services** (`IWorkspaceService`, `INoteService`, ...) implemented by
  classes that take `IAppDbContext` + FluentValidation validators via primary constructors and are
  called directly by the endpoints (no MediatR/mediator pipeline).
- **Records for DTOs**, primary constructors for DI.
- **CancellationToken flows through every async chain.**
- The desktop app consumes the APIs exposed by `src/Api`.
- The desktop app uses **AvaloniaUI**.
- Contract tests for APIs, integration tests for DB operations, unit tests for features.
- SpecFlow E2E tests for features.
- All documentation lives in this `README.md`.

### Never Suggest
- AutoMapper — write explicit mappings.
- Repository / UnitOfWork on top of EF Core.
- MediatR — use direct feature service classes instead.
- Swashbuckle — we use Scalar for OpenAPI.

## Database Provider Abstraction

The model is provider-agnostic: the same `AppDbContext` and entities run on **SQLite** and
**PostgreSQL**. The provider is selected through configuration, so switching requires only
changing `appsettings.json` (no code changes).

`src/Infrastructure/Persistence/DatabaseProviderConfiguration.cs` is the single place that maps a
`DatabaseProvider` (Sqlite | Postgres) to the EF Core provider:

```csharp
options.UseDatabaseProvider(new DatabaseOptions
{
    Provider = DatabaseProvider.Postgres,
    ConnectionString = "Host=localhost;Database=knowledgebase;Username=postgres;Password=..."
});
```

Configuration (defaults):

```json
"Database": {
  "Provider": "Sqlite",                      // "Sqlite" or "Postgres"
  "ConnectionString": "Data Source=knowledgebase.db"
}
```

### Why not an abstraction on top of EF Core?
EF Core is already a multi-provider abstraction. Adding a custom data-access layer on top of it
would mean reinventing what EF does. The provider abstraction lives in DI/configuration, exactly
as EF Core intends.

### Migrations per provider
EF Core migrations are provider-specific. They are currently stored in
`src/Infrastructure/Migrations/` (SQLite-based). To also support PostgreSQL, generate a second set of
Postgres migrations:

```
dotnet ef migrations add X --project src/Infrastructure --startup-project src/Api
```

> Note: the scaffold ships the SQLite migration (auto-applied on API startup, see `Program.cs`).
> A Postgres migration is added once a Postgres instance is available, using the env vars below.

### Design-time factory
`AppDbContextFactory` reads `KB_DATABASE_PROVIDER` (`sqlite` | `postgres`) and
`KB_CONNECTION_STRING` from the environment so `dotnet ef` and multi-provider migrations work.

## Run the API

```bash
# run API (applies migrations on startup)
dotnet run --project src/Api
```

OpenAPI at `/openapi/v1.json`, Scalar UI at `/scalar/v1`, health at `/health`.

### Run the desktop app

```bash
# 1. start the API
dotnet run --project src/Api

# 2. in another terminal, start the desktop app
dotnet run --project src/Desktop
```

The desktop app reads its API URL from `src/Desktop/appsettings.json`:

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5124"
  }
}
```

The app icon is a lightbulb representing knowledge, generated from
`tools/generate_icon.py` into `src/Desktop/Assets/bulb.{ico,png}`. It is used as the window/executable
icon (`<ApplicationIcon>` + `Icon="/Assets/bulb.ico"`) and shown in the app toolbar. Regenerate with:

```bash
python3 tools/generate_icon.py
cp tools/bulb.png tools/bulb.ico src/Desktop/Assets/
```

## API surface

The API is **RESTful**: resources are nouns, related resources are nested under their parent, the
resource identifier lives in the URL path, and HTTP methods map to actions.

| Method | Route | Description |
|---|---|---|
| GET | `/api/workspaces` | List workspaces |
| GET | `/api/workspaces?embed=notes` | Workspaces with notes + tag names (tree view source) |
| GET | `/api/workspaces/{id}` | Get one workspace |
| POST | `/api/workspaces` | Create workspace |
| PUT | `/api/workspaces/{id}` | Update workspace (name/description) |
| DELETE | `/api/workspaces/{id}` | Delete workspace (cascades notes/attachments) |
| GET | `/api/workspaces/{id}/notes` | List note summaries in a workspace |
| POST | `/api/workspaces/{id}/notes` | Create note (title, markdown, tags, source url/summary) |
| GET | `/api/notes/{id}` | Get one note with tags + attachments |
| GET | `/api/notes?titleQuery={text}&tags={t1,t2}&page={n}&pageSize={n}` | Search/paginate notes by title/content and tags |
| PUT | `/api/notes/{id}` | Update note + replace its tags |
| DELETE | `/api/notes/{id}` | Delete note |
| GET | `/api/tags` | List all tags (with per-tag note counts) |
| POST | `/api/tags` | Create tag |
| PUT | `/api/tags/{id}` | Update tag (rename/recolor) |
| DELETE | `/api/tags/{id}` | Delete tag |
| POST | `/api/tag-merges` | Merge a source tag into a target tag (reassigns notes) |
| POST | `/api/notes/{noteId}/attachments?kind={1,2}` | Upload picture (1) / canvas (2) to a note |
| GET | `/api/attachments/{id}` | Stream attachment content |
| DELETE | `/api/attachments/{id}` | Delete attachment |
| GET | `/api/graph?workspaceId={id}` | Graph nodes (notes) + edges (shared tags), optionally scoped to a workspace |
| GET | `/health` | Health check |

Error model: business errors are returned as `Result<T>` → JSON with `{ code, description }` and an
HTTP status (`404 NotFound`, `409 Conflict`, `400 Invalid`); success mutations return `204`, creates
return `201` with the id.

## Tests

```bash
dotnet test
```

| Suite | What it covers | Location |
|---|---|---|
| Unit | Domain entities, feature services, validators, Result\<T\> | `tests/UnitTests` |
| Contract | API routes, status codes, error mapping, OpenAPI document | `tests/ContractTests` |
| Integration | EF Core persistence + disk storage against real SQLite file | `tests/IntegrationTests` |
| E2E (SpecFlow) | Gherkin scenarios against hosted API | `tests/E2ETests` |

### E2E tests (SpecFlow)

Located in `tests/E2ETests/Features`. Each scenario runs against a fresh SQLite database via
`WebApplicationFactory<Program>`.

| Feature | Scenarios | What it verifies |
|---|---|---|
| `Health.feature` | Healthy service is reported | `/health` responds `Healthy` |
| `Notes.feature` | Create note with tags in workspace; reuse tag keeps a single tag; search finds notes by title and tag | note CRUD, tree, tag reuse, search |
| `Tags.feature` | Rename a tag; merge a tag into another reassigns notes | tag rename/recolor, merge, note counts |
| `Graph.feature` | Graph connects notes sharing tags | `/api/graph` nodes + edges |

Run them with: `dotnet test tests/E2ETests`

## Feature Plan & Status

> Status legend: **done** — implemented and tested · **in progress** — partially implemented ·
> **planned** — specified, not started · **blocked** — waiting on a dependency.

### Foundation

| # | Item | Status | Notes |
|---|---|---|---|
| F0.1 | Solution scaffold (Domain/Application/Infrastructure/Api/tests) | **done** | .NET 10, minimal APIs |
| F0.2 | Database provider abstraction (SQLite ↔ PostgreSQL) | **done** | `DatabaseProviderConfiguration`, DI-driven |
| F0.3 | EF Core model + initial SQLite migration | **done** | `20260920091732_InitialCreate` |
| F0.4 | CI-friendly test projects (unit/contract/integration/E2E) | **done** | all green |
| F0.5 | Scalar + OpenAPI + health endpoint | **done** | `/scalar/v1`, `/openapi/v1.json`, `/health` |
| F0.6 | OpenTelemetry wiring | **done** | tracing + metrics sources in Infrastructure |
| F0.7 | AvaloniaUI desktop app | **done** | tree view + editor, connects to `src/Api` |

### Feature 1 — Notes, tags, workspaces, attachments
Add/edit/delete a note/topic/knowledge channel: title, date, Markdown body (rendered), optional
pictures/canvas, tags. Group entries by workspaces/projects shown in a left tree view.

| Task | Status | Notes |
|---|---|---|
| 1.1 Workspace CRUD + API | **done** | validators, conflict on duplicate name |
| 1.2 Note CRUD + API (title, date, markdown, source url/summary) | **done** | markdown content + render in desktop |
| 1.3 Tag entity + API | **done** | unique by name, hex color |
| 1.4 Attach picture/canvas to note (storage + API) | **done** | disk storage, upload + stream content |
| 1.5 Tag assignment on notes (relate notes by shared tags) | **done** | create-or-reuse policy, replace on update |
| 1.6 Group notes under workspaces (tree view data source) | **done** | `/api/workspaces?embed=notes` |
| 1.7 Avalonia tree view + note editor with Markdown rendering | **done** | `Markdown.Avalonia` preview, tags, CRUD commands |

### Feature 2 — Global search (Spotlight-like)
Keyboard shortcut to search notes by title/topic/tag across the whole knowledge base with a quick,
Spotlight-style overlay and paginated results.

| Task | Status | Notes |
|---|---|---|
| 2.1 Search API (title/tag/full-text) | **done** | `GET /api/notes?titleQuery=&tags=&page=&pageSize=` |
| 2.2 Paginated search results | **done** | `PagedResult<T>` — items, page, pageSize, totalCount, totalPages |
| 2.3 Spotlight overlay in Avalonia (global hotkey) | **done** | `Ctrl/Cmd+K`, debounced-as-you-type search |
| 2.4 Open result → navigate to note | **done** | Enter/double-click loads the note into the editor |

### Feature 3 — Tag management
Define and manage the tag vocabulary used to relate and search notes.

| Task | Status | Notes |
|---|---|---|
| 3.1 Tag CRUD + rename/merge | **done** | create/update/delete + `POST /api/tag-merges` |
| 3.2 Tag existence validation on note save | **done** | FluentValidation on create/update/merge + note save |
| 3.3 Reuse existing tag OR create-on-assign policy | **done** | notes create-or-reuse tags by name |
| 3.4 Tag management UI | **done** | Avalonia "Tags" window: add, rename, recolor, merge, delete, note counts |

### Feature 4 — Notes graph
Graph visualization like Obsidian where notes (nodes) are connected by shared tags / relations.
Hovering/clicking a node opens the note.

| Task | Status | Notes |
|---|---|---|
| 4.1 Graph model: nodes = notes, edges = shared tags | **done** | deduplicated undirected edges |
| 4.2 Graph data API (nodes + edges) | **done** | `GET /api/graph?workspaceId=` |
| 4.3 Graph rendering in Avalonia | **done** | custom `GraphView` control, force-directed layout |
| 4.4 Node click → open note; drag/pan/zoom | **done** | click opens note in editor, drag pans, wheel zooms |

### Feature 5 — Link to article → new note
Add a URL that becomes a note. Analyze the link with an LLM to extract topics/tags and a short
summary.

| Task | Status | Notes |
|---|---|---|
| 5.1 Import article endpoint (fetch URL) | **planned** | |
| 5.2 LLM analysis (topic/tag extraction + summary) | **planned** | |
| 5.3 Create note from analysis result | **planned** | reuses 1.2/1.5 |
| 5.4 LLM provider abstraction (pluggable) | **planned** | |

### Feature 6 — CSV link import
Provide a CSV file with links and add them exactly as in Feature 5.

| Task | Status | Notes |
|---|---|---|
| 6.1 CSV import parser + validation | **planned** | |
| 6.2 Batch article analysis (rate limiting, progress) | **planned** | reuses 5.2 |
| 6.3 Import report (succeeded/failed rows) | **planned** | |

## Committing Convention Notes (repo hygiene)
- Never commit `*.db` or secrets (`Database.ConnectionString` for Postgres goes to user-secrets/env).
- Postgres connection strings belong in environment variables, not `appsettings.json`.