# AGENTS.md

## Mission

Build and maintain **ConduitAI**, an ASP.NET Core MVC application for real-estate lead intelligence and follow-up. It is an internal CRM built on C#, EF Core, SQLite, server-rendered MVC with jQuery enhancement, and local AI integration via Ollama.

This file and `CLAUDE.md` define scope, architecture, and operating rules. Follow them unless the user explicitly changes direction.

## Canonical Naming

- Correct: `ConduitAI`
- Incorrect: `Conduit AI`

Use `ConduitAI` in namespaces, UI copy, docs, seed data, tests, page titles, and comments.

## Hard Constraints

- Free to run.
- Free to develop.
- Safe for a public repository.
- Security-first implementation.
- Minimal scope, maintainable by a single developer.

Do not add:

- Authentication or roles
- Docker
- Microservices
- Redis
- Message queues
- Cloud infrastructure
- CI/CD pipelines
- Paid APIs or hosted LLM services
- Paid assets or commercial-only libraries

## Technology Boundaries

Use:

- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQLite
- Razor views
- HTML/CSS
- JavaScript
- jQuery
- Ollama at `http://localhost:11434`
- Local model default: `qwen2.5-coder:7b`

Avoid introducing new frameworks unless the user asks and the benefit is concrete.

## Architecture Expectations

- Keep a single MVC application.
- Use controllers for HTTP concerns only.
- Put business workflows in services.
- Use ViewModels for form binding and display.
- Use EF Core entities for persistence.
- Use migrations for schema changes.
- Store AI analysis and meeting-note output in the database.
- Never regenerate AI output on page load.
- Prefer server-rendered MVC first; use jQuery as enhancement.

Expected service areas:

- Leads
- Timeline/interactions
- Dashboard metrics
- Ollama client
- AI prompt construction
- AI JSON parsing and validation
- Meeting notes processing

## Security Rules

Security is the first priority because this repository will be public.

Required practices:

- Never commit secrets, tokens, passwords, real customer data, or private notes.
- Keep config public-safe by default.
- Use `.gitignore` for local databases, logs, build outputs, and environment-specific files.
- Validate input server-side.
- Use anti-forgery tokens for POST forms.
- Avoid over-posting by binding ViewModels, not broad entities.
- Use EF Core LINQ/parameterization. Do not build SQL from user strings.
- Do not render user input or AI output with raw HTML.
- Treat AI output as untrusted text.
- Do not log full raw notes, emails, phone numbers, prompts containing private data, or model responses containing lead PII.
- Show safe user-facing errors; keep stack traces and internals out of UI.
- Check dependency licenses and avoid unnecessary packages.

AI-specific precautions:

- Ollama must be local and optional to start the app.
- If Ollama is down, show a clean error and keep the app usable.
- Require structured JSON responses from the model.
- Validate and normalize model output before storing.
- Retry malformed JSON once if implemented; do not store invalid partial output.
- Prompts must tell the model not to invent facts or make discriminatory/protected-class recommendations.

## Product Focus

Implement these core workflows before adding extras:

- Lead create/edit/delete/view
- Lead list search and filtering
- Lead timeline entries
- Stored AI lead analysis
- Meeting notes assistant
- Dashboard with total leads, new leads, high-priority leads, upcoming follow-ups, and recently updated leads

Keep the domain real estate:

- Buyer budget
- Location
- Relocation
- Schools as a stated buyer preference
- Property tours
- Lead source
- Follow-up actions

Use fictional seed data only.

## UI Guidance

- Make it feel like an internal CRM, not a marketing site.
- Avoid chatbot-centered UI.
- Prioritize useful tables, filters, detail pages, and concise status panels.
- Keep dashboards compact and business-focused.
- Use clear loading and failure states for AI actions.
- Ensure forms work through normal MVC posts; AJAX is progressive enhancement.

## Quality Bar

Before considering work complete:

- Build succeeds.
- EF migrations match entity changes.
- Core flows are manually testable.
- Service and parser tests are added where practical.
- Public repo safety has been considered.
- Documentation stays accurate.

Useful test targets:

- Lead CRUD and filtering
- Timeline creation and lead `UpdatedAt`
- Dashboard query behavior
- AI parser valid/malformed JSON
- AI analysis storage
- Meeting notes storage

## When Unsure

Choose the simpler implementation that:

- Matches the architecture and scope in this document and `CLAUDE.md`
- Stays free/local
- Reduces security risk
- Keeps controllers thin and business logic in services
- Avoids unnecessary infrastructure
