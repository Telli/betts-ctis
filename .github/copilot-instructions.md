## Copilot / AI Agent Instructions for Betts CTIS

Quick, focused guidance to help an AI coding assistant be productive in this repository.

- Big picture: this repo contains a .NET 9 backend (clean-architecture) under `Betts/BettsTax` and a Next.js frontend under `Betts/sierra-leone-ctis`.
- Backend key folders: `Betts/BettsTax/BettsTax.Web` (API), `Betts/BettsTax/BettsTax.Core` (domain/services), `Betts/BettsTax/BettsTax.Data` (EF models/migrations).
- Frontend key folders: `Betts/sierra-leone-ctis/app`, `components`, `lib/services` and `styles` (shadcn/ui + TailwindCSS patterns).

- Common patterns to follow:
  - Clean Architecture: controllers -> Core services -> Data repositories. Prefer DI and interfaces from `BettsTax.Core`.
  - DTOs + AutoMapper for external models. Validation uses FluentValidation (look for validators next to DTOs).
  - EF Core migrations live under `BettsTax.Data` and are applied with `--project BettsTax.Data --startup-project BettsTax.Web`.

- Local dev commands (from repo root):
  - Backend: `cd Betts/BettsTax && dotnet restore && cd BettsTax.Web && dotnet run`
  - DB migrations: `dotnet ef migrations add <Name> --project BettsTax.Data --startup-project BettsTax.Web` and `dotnet ef database update --project BettsTax.Data --startup-project BettsTax.Web`
  - Tests: `dotnet test` (or run specific projects under `*Tests` directories)
  - Frontend: `cd Betts/sierra-leone-ctis && pnpm install && pnpm dev`

- Important files & scripts to reference:
  - `Betts/BettsTax/BettsTax.Web/Program.cs` (API startup, ports, middleware)
  - `Betts/BettsTax/BettsTax.Data` (models, migrations)
  - `Betts/scripts` and top-level `scripts/deploy.sh` (deployment flow)
  - `Betts/sierra-leone-ctis/env.example.md` for frontend env var patterns (NEXT_PUBLIC_API_URL)

- External integrations to be cautious about:
  - Mobile money gateways (Orange Money, Africell) — pay attention to provider-specific payloads and webhooks.
  - PostgreSQL, Redis, Prometheus/Grafana, Loki — do not hardcode credentials; use env vars.

- Auth & security conventions:
  - JWT-based auth + ASP.NET Identity. Roles include Client, Associate, Admin, SystemAdmin.
  - Field-level encryption, audit logging and strict validation are used across the codebase.

- When changing behavior:
  - Add/update EF migrations under `BettsTax.Data` and test locally before committing.
  - Update AutoMapper profiles when adding DTOs.
  - Add FluentValidation rules when changing input contracts.

- How to propose changes in PRs:
  - Keep backend changes limited to service/controller/repository boundaries.
  - Include migration scripts and small sample data for QA where appropriate.
  - Run `dotnet test` and `pnpm lint` (frontend) before requesting review.

If any of this is unclear or you'd like the file to include shortcuts for running the full dev stack (Docker compose, advanced debugging steps, or example queries), tell me which area to expand.
