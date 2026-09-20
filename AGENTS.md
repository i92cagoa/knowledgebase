# Knowledge Base app

## Tech Stack
For the backend services: .NET 10 - Minimal APIs - EF Core 10 - Sqlite/Postgres - FluentValidation - xUnit - AwesomeAssertions
For the desktop app: AvaloniaUI - C#

## Structure
src/Api -> endpoints, DI - src/Application -> feature services, validators
src/Domain -> entities, rules - src/Infrastructure -> EF Core, services, Open Telemetry

## Architecture Rules
- Domain has ZERO external dependencies
- All data access through DbContext. No repository pattern
- Result<T> for business errors, not exceptions
- Application layer exposes feature service interfaces (I<Feature>Service) implemented by service classes that take IAppDbContext + FluentValidation validators via primary constructors
- Records for DTOs - primary constructors for DI
- Always pass CancellationToken through async chains
- The Api exposes RESTful endpoints. Rules:
  - Resources are nouns in URL paths (workspaces, notes, tags, attachments), never action verbs (no /tree, /by-workspace, /content)
  - Related resources are nested under their parent resource (e.g. POST /api/workspaces/{id}/notes, POST /api/notes/{id}/attachments)
  - The resource identifier goes in the URL path; query parameters only for optional representation hints (e.g. ?embed=notes) or future filtering/pagination
  - HTTP methods map to actions: GET read, POST create, PUT update, DELETE delete
  - Create returns 201 + Location, update/delete return 204, errors map to 4xx with { code, description }
- The desktop app should use the APIs defined in the structure.
- The desktop app has to use AvaloniaUI: https://avaloniaui.net/
- Contract tests for APIs, Integration tests for db operations, Unit tests for features.
- Add Specflow E2E tests for the features.
- Document everything in README.md file
- Document E2E tests in README.md file

## Never Suggest
- AutoMapper (write explicit mappings)
- Repository /UnitOfWork on top of EF Core
- MediatR (use direct feature service classes instead)
- Swashbuckle (we use Scalar for OpenAPI)