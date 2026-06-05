# ConduitAI

ConduitAI is an ASP.NET Core MVC application for real-estate lead intelligence and follow-up. It is a compact internal CRM that demonstrates practical C# business software: lead records, timeline history, dashboard metrics, stored AI analysis, and meeting-note processing. AI runs locally through Ollama and is invoked only on explicit user actions.

The architecture and product guide for this repository is [PLAN.md](PLAN.md).

## Features

- **Lead management** — create, edit, delete, list, and detail views with server-side validation and safe delete confirmation.
- **Search and filtering** — filter leads by status, source, location, free-text search, and minimum AI lead score.
- **Lead timeline** — record interactions (calls, emails, meetings, property tours, follow-ups, notes); adding one bumps the lead's last-updated time.
- **Stored AI lead analysis** — on demand, generate a summary, 0–100 lead score, urgency, buying intent, and recommended next action. Results are persisted, never regenerated on page load.
- **Meeting notes assistant** — turn raw notes into a structured summary, key facts, risks, and a next action; optionally attach to a lead, which also records a timeline interaction.
- **Dashboard** — total leads, new leads, high-priority leads, upcoming follow-ups, a recently-updated list, and a follow-up queue.

## Technology Stack

- ASP.NET Core MVC (.NET 8) with Razor views
- C#
- Entity Framework Core 8 with SQLite
- HTML / CSS / JavaScript / jQuery (progressive enhancement)
- Ollama running locally at `http://localhost:11434`

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com/) for AI features (the rest of the app works without it)

## Getting Started

```bash
dotnet restore
dotnet run --project ConduitAI
```

The app applies EF Core migrations and seeds fictional demo data on first run, so a separate `dotnet ef database update` is not required. The SQLite database is created at `ConduitAI/App_Data/conduitai.db` (git-ignored).

Then open the URL printed by `dotnet run` (by default `http://localhost:5229`).

### Enabling AI features

AI actions (lead analysis, meeting notes) require Ollama running locally with the configured model:

```bash
ollama serve
```

In another terminal:

```bash
ollama pull qwen2.5-coder:7b
ollama list
```

If Ollama is unavailable, configured to a non-local URL, or returns unparseable output, AI actions fail gracefully with a user-facing message and nothing partial is stored. All other CRM features remain fully usable.

## Configuration

Ollama settings live in `appsettings.json` under the `Ollama` section:

```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "qwen2.5-coder:7b",
  "TimeoutSeconds": 120
}
```

`appsettings.json` holds public-safe defaults. To use a different local model without changing committed files, override with environment variables:

```bash
Ollama__Model=qwen2.5-coder:14b dotnet run --project ConduitAI
```

AI requests are only sent to loopback Ollama URLs such as `http://localhost:11434` or `http://127.0.0.1:11434`. Non-loopback URLs disable AI features instead of sending private lead data off-machine.

No secrets are required to run the app.

## Project Structure

```
ConduitAI/
  Controllers/    Thin controllers (Home, Leads, Interactions, Ai, MeetingNotes)
  Services/       Business logic + interfaces and AI result types
  Models/         EF Core entities and enums
  Data/           AppDbContext, design-time factory, seed initializer
  Migrations/     EF Core migrations
  ViewModels/     Form and display models (prevent over-posting)
  Views/          Razor views and partials
  Helpers/        Display formatting helpers
  wwwroot/        site.css, site.js, ai.js
ConduitAI.Tests/  xUnit tests (EF Core InMemory)
```

### Service layer

- `LeadService` — lead CRUD and filtering
- `TimelineService` — interaction history
- `DashboardService` — dashboard metrics
- `AiAnalysisService` — generates and stores lead analysis
- `MeetingNotesService` — processes and stores meeting notes
- `OllamaClient` — typed HttpClient wrapper for the Ollama API
- `AiPromptBuilder` / `AiResponseParser` — structured prompt construction and tolerant JSON parsing

### Data model

SQLite tables: `Leads`, `LeadInteractions`, `LeadAnalyses`, `MeetingNotes`. AI results are stored for auditability rather than overwritten or regenerated automatically. Meeting notes attached to a lead are deleted with that lead; unattached notes are only created through the standalone meeting-notes workflow.

## Testing

```bash
dotnet test
```

Tests cover the service layer, lead filtering, AI response parsing, and AI/meeting-note failure paths using the EF Core InMemory provider and hand-rolled fakes.

## Security & Privacy

This project is built for a public repository, so security is part of the design:

- No secrets, tokens, passwords, real customer data, or private notes are committed; local databases, logs, and secrets are git-ignored.
- Input is validated server-side with DataAnnotations and service-level checks.
- POST forms use anti-forgery tokens; forms bind to ViewModels, not database entities.
- Persistence uses EF Core LINQ and parameterized queries.
- User input and AI output are treated as untrusted and rendered with Razor's default HTML encoding (never as raw HTML).
- Prompts instruct the model not to invent facts and to avoid legal, financial, or protected-class recommendations.
- Raw notes, emails, phone numbers, and full AI responses are kept out of logs.

## Constraints

By design, ConduitAI is free to run and develop and adds no authentication, roles, Docker, microservices, Redis, queues, cloud infrastructure, CI/CD, paid APIs, or hosted LLM services. Seed data is fictional.

## License

ConduitAI is licensed under the MIT License. Third-party client libraries in `wwwroot/lib` include their own license files.
