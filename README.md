# ConduitAI

ConduitAI is an ASP.NET Core MVC portfolio project for real-estate lead intelligence and follow-up. It is designed as a compact internal CRM that demonstrates practical C# business software: lead records, timeline history, dashboard metrics, stored AI analysis, and meeting-note processing.

The implementation guide for this repository is [PLAN.md](PLAN.md).

## Project Status

This repository currently contains the architecture and implementation plan. The MVC application should be built from that plan as a single ASP.NET Core MVC project using Entity Framework Core, SQLite, Razor views, JavaScript, jQuery, and local Ollama integration.

## Goals

- Track real-estate buyer and seller leads.
- Search and filter leads by status, source, location, and priority.
- Record timeline interactions such as calls, emails, meetings, property tours, and follow-ups.
- Generate and store AI lead analysis only when explicitly requested.
- Process meeting notes into structured summaries, key facts, risks, and next actions.
- Show a compact dashboard with lead counts, high-priority leads, upcoming follow-ups, and recently updated records.

## Technology Stack

- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQLite
- Razor views
- HTML/CSS
- JavaScript
- jQuery
- Ollama at `http://localhost:11434`
- Default local model: `qwen2.5-coder:7b`

## Core Workflows

ConduitAI is intended to support these workflows first:

- Lead create, edit, delete, list, and detail views
- Lead search and filtering
- Lead timeline entries
- Stored AI lead analysis
- Meeting notes assistant
- Dashboard metrics

Additional features should stay secondary to these workflows.

## Architecture

ConduitAI should remain a single MVC application. Controllers handle HTTP concerns, services contain business workflows, Entity Framework Core manages persistence, and ViewModels are used for form binding and display.

Expected service areas:

- `LeadService`
- `TimelineService`
- `DashboardService`
- `AiAnalysisService`
- `MeetingNotesService`
- `OllamaClient`
- `AiPromptBuilder`
- `AiResponseParser`

AI output is stored in SQLite and should never be regenerated automatically on page load. Users explicitly trigger AI actions from the UI.

## Local AI

ConduitAI uses Ollama locally. The app should remain usable when Ollama is not running, with clear user-facing errors for AI actions.

Expected Ollama configuration:

```text
Base URL: http://localhost:11434
Model: qwen2.5-coder:7b
```

To prepare the local model:

```bash
ollama pull qwen2.5-coder:7b
ollama serve
```

## Security Principles

This project is intended for a public repository, so security and privacy are part of the design:

- Do not commit secrets, tokens, passwords, real customer data, or private notes.
- Keep configuration public-safe by default.
- Use fictional seed data only.
- Validate input server-side.
- Use anti-forgery tokens for POST forms.
- Bind forms to ViewModels instead of broad database entities.
- Use EF Core LINQ and parameterized queries.
- Do not render user input or AI output as raw HTML.
- Treat AI output as untrusted text.
- Do not log full raw notes, emails, phone numbers, prompts containing private data, or AI responses containing lead PII.

## Planned Data Model

The planned SQLite schema includes:

- `Leads`
- `LeadInteractions`
- `LeadAnalyses`
- `MeetingNotes`

Lead analysis and meeting-note results are stored for auditability instead of being overwritten or regenerated automatically.

## Development Constraints

ConduitAI should be free to develop, free to run, and realistic for a single developer portfolio project.

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

## Getting Started

After the ASP.NET Core MVC project is scaffolded, the expected local workflow will be:

```bash
dotnet restore
dotnet ef database update
dotnet run
```

Then open the local URL printed by `dotnet run`.

AI features require Ollama to be running locally. Non-AI CRM features should work without Ollama.

## Quality Bar

Before considering implementation work complete:

- The project builds successfully.
- EF Core migrations match entity changes.
- Core workflows are manually testable.
- Parser and service tests are added where practical.
- Public-repository safety has been reviewed.
- Documentation matches the implemented behavior.

