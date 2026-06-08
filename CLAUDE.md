# CLAUDE.md

## Project Identity

The project name is **ConduitAI**. Do not write it as "Conduit AI" in code, UI, documentation, comments, namespaces, page titles, README content, seed data, or tests unless quoting older material that is being corrected.

ConduitAI is an AI-assisted lead intelligence and follow-up platform for real-estate sales teams. It should feel like a realistic internal business application, not a demo chatbot.

Use `PLAN.md` as the primary product and architecture guide. This file adds operating rules for coding agents.

## Non-Negotiable Constraints

- The project must be free to run, free to develop, and suitable for a public repository.
- Security is the top priority from the start.
- Do not add authentication, user roles, Docker, microservices, Redis, queues, cloud infrastructure, or CI/CD pipelines.
- Do not introduce paid APIs, paid SaaS dependencies, proprietary SDKs, hosted LLM services, or commercial-only assets.
- Do not require secrets for the core app to run locally.
- Keep the app realistic for a single developer to complete in roughly 4 days.

## Required Stack

Backend:

- C#
- ASP.NET Core MVC
- Entity Framework Core
- SQLite

Frontend:

- Razor views
- HTML
- CSS
- JavaScript
- jQuery

AI:

- Ollama on localhost
- Preferred local model: `qwen2.5-coder:7b`
- AI calls must be explicit user actions, not automatic page-load behavior.

## Architecture Rules

- Build a single MVC monolith with a clear service layer.
- Keep controllers thin. Business logic belongs in services.
- Use ViewModels for pages and forms. Do not bind database entities directly to broad public forms when a focused ViewModel is safer.
- Use EF Core LINQ and parameterized queries. Do not concatenate SQL from user input.
- Store AI outputs in SQLite. Do not regenerate summaries or recommendations on every page load.
- Keep AI response contracts structured and parseable as JSON.
- Prefer simple, maintainable code over abstractions that do not reduce real complexity.

Core service areas:

- `LeadService`
- `TimelineService`
- `AiAnalysisService`
- `MeetingNotesService`
- `DashboardService`
- `OllamaClient`
- `AiPromptBuilder`
- `AiResponseParser`

## Security Requirements

Because this is a public repository, assume all code, config, seed data, and docs will be reviewed publicly.

Required precautions:

- Never commit secrets, API keys, passwords, private tokens, local absolute secrets paths, or private customer data.
- Add and maintain `.gitignore` entries for local databases, logs, user secrets, build outputs, and environment-specific files.
- Keep `appsettings.json` safe for public defaults. Prefer environment-variable overrides for local settings so tracked config files do not drift.
- Validate all user input server-side with DataAnnotations and service-level checks.
- Use ASP.NET Core anti-forgery validation for POST forms.
- Preserve Razor's default HTML encoding. Do not render user notes or AI output as raw HTML.
- Treat AI output as untrusted text. Encode it in views and validate it before storage.
- Avoid logging raw meeting notes, phone numbers, emails, or full lead notes.
- Use clear error messages for users, but do not expose stack traces, raw prompts, connection strings, or internal exception details.
- Prevent over-posting by using ViewModels and explicit mapping.
- Include safe delete confirmation flows for destructive lead and timeline operations.
- Keep dependencies minimal and current. Do not add libraries without checking license, maintenance status, and security value.

AI-specific security:

- Prompts must instruct the model not to invent facts.
- Prompts must avoid legal, financial, protected-class, or discriminatory recommendations.
- The app must tolerate malformed model output.
- If Ollama is unavailable or parsing fails, fail gracefully and do not store partial AI analysis.

## Cost and Licensing Rules

- Use only free and open-source packages compatible with public portfolio use.
- Prefer Microsoft/ASP.NET Core packages, EF Core SQLite, jQuery, and locally hosted Ollama.
- Do not add paid UI kits, icon packs, hosted analytics, external enrichment APIs, hosted databases, or cloud-only services.
- Seed data must be fictional.
- Any copied sample code or assets must have a license that allows public repository use.

## Product Scope

Implement the features described in `PLAN.md`:

- Lead CRUD
- Lead timeline
- Stored AI lead analysis
- Meeting notes assistant
- Dashboard metrics
- Search and filtering

Do not expand scope unless explicitly asked. Optional follow-up tables or extra history screens should come after the core workflows are working.

## UX Direction

- Build a polished internal real-estate CRM experience.
- Avoid chatbot-first layouts.
- Use compact dashboard metrics, lead tables, practical forms, and detail pages.
- jQuery should progressively enhance server-rendered pages; the app should remain understandable without heavy frontend complexity.
- Display AI as embedded lead intelligence: summary, score, urgency, intent, and next action.

## Development Workflow

- Read `PLAN.md` before implementing feature work.
- Keep changes focused and small enough to review.
- Add migrations when entity schema changes.
- Add tests around services, filters, and AI parsing where practical.
- Run formatting/build/tests before finalizing work when the project has been scaffolded.
- Update docs when setup steps, config, model names, or workflows change.

## Completion Standard

A change is not complete until:

- It follows the architecture in `PLAN.md`.
- It preserves the free/local/public-repo constraints.
- It handles validation and expected failure paths.
- It does not leak secrets or sensitive local data.
- It can be explained clearly in an interview as backend business software design.
