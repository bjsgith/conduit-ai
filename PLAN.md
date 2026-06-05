# ConduitAI Architecture Plan

## Summary

Build ConduitAI as a single ASP.NET Core MVC application for real-estate sales teams. The app will use SQLite with Entity Framework Core, server-rendered Razor views, JavaScript/jQuery for interactive forms and AJAX actions, and Ollama on `localhost` for local AI analysis.

The project should feel like an internal business application: lead records, timeline history, stored AI analysis, meeting-note extraction, dashboards, and filtering. It will not include authentication, roles, Docker, cloud services, queues, or background workers.

Primary design decisions:

- Use a modular MVC monolith, not layered microservices.
- Store AI outputs in relational tables and regenerate only on explicit user action.
- Keep AI responses structured as JSON to make the app reliable and interview-friendly.
- Use service classes for business logic so controllers stay thin.
- Use SQLite for local persistence and EF Core migrations for schema management.
- Use jQuery only where it improves UX: filtering, AI generation buttons, modal forms, and partial refreshes.

## System Architecture

The application will have five main layers:

1. **MVC Web Layer**
   - Razor views for dashboard, lead pages, timeline, and AI tools.
   - Controllers handle routing, validation, redirects, and JSON responses.
   - jQuery handles AJAX interactions for AI generation, filters, delete confirmations, and inline timeline entry creation.

2. **Application Service Layer**
   - `LeadService`
   - `TimelineService`
   - `AiAnalysisService`
   - `MeetingNotesService`
   - `DashboardService`
   - Services contain workflow logic and coordinate EF Core plus Ollama calls.

3. **Data Access Layer**
   - EF Core `AppDbContext`.
   - SQLite database.
   - Entity classes with relationships and validation attributes.
   - LINQ queries for dashboard metrics and filtering.

4. **AI Integration Layer**
   - `IOllamaClient` wraps HTTP calls to Ollama.
   - `AiPromptBuilder` centralizes prompts.
   - `AiResponseParser` validates and normalizes JSON responses.
   - AI failures are stored/logged gracefully and shown as actionable UI errors.

5. **Presentation Models**
   - ViewModels keep Razor pages separate from database entities.
   - Request DTOs for AJAX endpoints.
   - AI response DTOs mirror expected structured output.

This architecture demonstrates backend design without overengineering. It also keeps implementation feasible in 4 days.

## Database Schema

Use SQLite with EF Core migrations.

### `Leads`

Stores the core lead record.

Fields:

- `Id` integer primary key
- `Name` required string, max 150
- `Email` nullable string, max 200
- `Phone` nullable string, max 50
- `LeadSource` required enum/string
- `Budget` nullable decimal
- `Location` nullable string, max 150
- `Notes` nullable text
- `Status` required enum/string
- `CreatedAt` datetime
- `UpdatedAt` datetime

Recommended statuses:

- `New`
- `Contacted`
- `Qualified`
- `TourScheduled`
- `OfferPending`
- `Closed`
- `Lost`

Recommended lead sources:

- `Website`
- `Referral`
- `OpenHouse`
- `Zillow`
- `RealtorCom`
- `SocialMedia`
- `Other`

### `LeadInteractions`

Stores every timeline event.

Fields:

- `Id` integer primary key
- `LeadId` foreign key
- `OccurredAt` datetime
- `InteractionType` required enum/string
- `Notes` required text
- `CreatedAt` datetime

Recommended interaction types:

- `PhoneCall`
- `Email`
- `Meeting`
- `PropertyTour`
- `FollowUp`
- `Note`

Relationship:

- One `Lead` has many `LeadInteractions`.

### `LeadAnalyses`

Stores AI-generated lead intelligence. Do not regenerate on page load.

Fields:

- `Id` integer primary key
- `LeadId` foreign key
- `Summary` required text
- `LeadScore` integer, 0-100
- `UrgencyLevel` string: `Low`, `Medium`, `High`
- `BuyingIntent` string: `Low`, `Medium`, `High`
- `RecommendedNextAction` required text
- `GeneratedAt` datetime
- `ModelName` string
- `PromptVersion` string

Relationship:

- One `Lead` has many `LeadAnalyses`.
- The UI shows the latest analysis by default and may expose previous analyses later if time allows.

Decision: keep history rather than overwriting. This demonstrates auditability and avoids losing previous AI outputs.

### `MeetingNotes`

Stores pasted salesperson notes and structured AI output.

Fields:

- `Id` integer primary key
- `LeadId` nullable foreign key
- `RawNotes` required text
- `StructuredSummary` required text
- `KeyFactsJson` required text
- `RisksJson` required text
- `RecommendedNextAction` required text
- `CreatedAt` datetime
- `ModelName` string
- `PromptVersion` string

Relationship:

- A meeting note may be attached to a lead.
- If attached, it should also create a `LeadInteraction` of type `Meeting` or `Note`.

Decision: store JSON arrays as text in SQLite for key facts and risks. This keeps the schema simple while preserving structured output.

### Optional `FollowUps`

Add only if time allows after core features are complete.

Fields:

- `Id`
- `LeadId`
- `DueAt`
- `Title`
- `CompletedAt`
- `CreatedAt`

For the 4-day version, upcoming follow-ups can be inferred from `LeadAnalyses.RecommendedNextAction` plus manual timeline entries. A separate table is useful but not required for the core demo.

## Entity Definitions

Core entity classes:

- `Lead`
  - Navigation: `ICollection<LeadInteraction> Interactions`
  - Navigation: `ICollection<LeadAnalysis> Analyses`
  - Navigation: `ICollection<MeetingNote> MeetingNotes`

- `LeadInteraction`
  - Navigation: `Lead Lead`

- `LeadAnalysis`
  - Navigation: `Lead Lead`

- `MeetingNote`
  - Navigation: nullable `Lead Lead`

Enums:

- `LeadStatus`
- `LeadSource`
- `InteractionType`
- `UrgencyLevel`
- `BuyingIntent`

Decision: use C# enums in code and store them as strings with EF Core conversions. This keeps code type-safe and the SQLite database readable during interviews.

## MVC Structure

Recommended routes:

- `/`
  - Dashboard

- `/Leads`
  - Lead list with filters

- `/Leads/Details/{id}`
  - Lead profile, latest AI analysis, timeline, meeting notes

- `/Leads/Create`
  - Create lead

- `/Leads/Edit/{id}`
  - Edit lead

- `/Leads/Delete/{id}`
  - Delete confirmation

- `/Interactions/Create`
  - Add timeline entry, usually from lead details

- `/Ai/AnalyzeLead/{leadId}`
  - Explicitly generate and store lead analysis

- `/MeetingNotes/Create`
  - Paste raw notes, generate structured output, store result

Controllers:

- `HomeController`
  - Dashboard

- `LeadsController`
  - CRUD, search, filtering

- `InteractionsController`
  - Timeline entry creation/deletion

- `AiController`
  - AJAX endpoints for lead analysis

- `MeetingNotesController`
  - Meeting notes assistant workflow

Decision: keep AI and meeting notes in separate controllers. Lead analysis and meeting-note extraction are related but distinct user workflows.

## API Endpoint Design

Because this is MVC, most endpoints return views or partial views. AJAX endpoints return JSON or HTML partials.

### Lead Pages

`GET /Leads`

Query parameters:

- `status`
- `minLeadScore`
- `location`
- `leadSource`
- `search`

Returns:

- Full lead list view

Behavior:

- Filtering should work with normal query strings for reliability.
- jQuery may update the list asynchronously as enhancement.

### Lead Analysis

`POST /Ai/AnalyzeLead/{leadId}`

Returns JSON:

```json
{
  "success": true,
  "analysisId": 12,
  "summary": "Relocating buyer focused on Scottsdale schools...",
  "leadScore": 86,
  "urgencyLevel": "High",
  "buyingIntent": "High",
  "recommendedNextAction": "Schedule a Scottsdale school-district property tour within 48 hours.",
  "generatedAt": "2026-06-04T18:30:00Z"
}
```

Failure response:

```json
{
  "success": false,
  "message": "Ollama is not available. Confirm the local model is running."
}
```

Decision: use explicit generation via button click. Never call the model automatically on page load.

### Meeting Notes Assistant

`POST /MeetingNotes/Create`

Request:

- `leadId` optional
- `rawNotes` required

Returns:

- Redirect to lead details if attached to a lead
- Otherwise show result page

AJAX variant if time allows:

`POST /MeetingNotes/Analyze`

Returns JSON with structured summary, key facts, risks, and next action.

Decision: implement the normal form first. AJAX enhancement is secondary.

### Timeline Entry

`POST /Interactions/Create`

Request:

- `leadId`
- `occurredAt`
- `interactionType`
- `notes`

Returns:

- Redirect to lead details
- AJAX partial refresh optional

## Folder Structure

Use a conventional ASP.NET Core MVC layout:

```text
ConduitAI/
  Controllers/
    HomeController.cs
    LeadsController.cs
    InteractionsController.cs
    AiController.cs
    MeetingNotesController.cs

  Data/
    AppDbContext.cs
    DbInitializer.cs
    Migrations/

  Models/
    Lead.cs
    LeadInteraction.cs
    LeadAnalysis.cs
    MeetingNote.cs
    Enums/

  Services/
    Interfaces/
      ILeadService.cs
      ITimelineService.cs
      IAiAnalysisService.cs
      IMeetingNotesService.cs
      IDashboardService.cs
      IOllamaClient.cs
    LeadService.cs
    TimelineService.cs
    AiAnalysisService.cs
    MeetingNotesService.cs
    DashboardService.cs
    OllamaClient.cs
    AiPromptBuilder.cs
    AiResponseParser.cs

  ViewModels/
    DashboardViewModel.cs
    LeadListViewModel.cs
    LeadDetailsViewModel.cs
    LeadFormViewModel.cs
    LeadFilterViewModel.cs
    InteractionFormViewModel.cs
    MeetingNotesFormViewModel.cs
    MeetingNotesResultViewModel.cs
    AiAnalysisViewModel.cs

  Views/
    Home/
      Index.cshtml
    Leads/
      Index.cshtml
      Details.cshtml
      Create.cshtml
      Edit.cshtml
      Delete.cshtml
      _LeadTable.cshtml
      _LeadForm.cshtml
      _Timeline.cshtml
      _AiAnalysisPanel.cshtml
    MeetingNotes/
      Create.cshtml
      Result.cshtml
    Shared/
      _Layout.cshtml
      _ValidationScriptsPartial.cshtml

  wwwroot/
    css/
      site.css
    js/
      site.js
      leads.js
      ai.js
```

Decision: avoid separate class libraries. A single MVC project is easier to review, run, and complete in 4 days.

## Service Layer Design

### `LeadService`

Responsibilities:

- Create, update, delete leads
- Apply search/filter logic
- Load lead details with timeline and latest analysis
- Maintain `CreatedAt` and `UpdatedAt`

Important behavior:

- Editing a lead updates `UpdatedAt`.
- Adding timeline entries also updates the parent lead's `UpdatedAt`.

### `TimelineService`

Responsibilities:

- Add interactions
- Validate interaction type and notes
- Load interactions ordered newest first

### `AiAnalysisService`

Responsibilities:

- Build lead context from lead fields and timeline history
- Call `IOllamaClient`
- Parse structured AI JSON
- Store `LeadAnalysis`
- Return display-ready result

Important behavior:

- Do not regenerate unless user clicks "Generate Analysis".
- Use the latest stored analysis on lead detail pages.
- If parsing fails, retry once with a stricter "return valid JSON only" prompt.

### `MeetingNotesService`

Responsibilities:

- Accept raw notes
- Call Ollama
- Parse structured output
- Store raw and AI output
- Optionally attach result to a lead
- Create a timeline entry when attached to a lead

### `DashboardService`

Responsibilities:

- Total leads
- New leads
- High priority leads
- Upcoming follow-ups
- Recently updated leads

Dashboard definitions:

- `Total Leads`: count of all leads
- `New Leads`: leads with status `New`
- `High Priority Leads`: latest analysis has score `>= 75` or urgency `High`
- `Upcoming Follow-Ups`: leads whose latest AI recommended next action exists and status is not `Closed` or `Lost`
- `Recently Updated Leads`: top 5 by `UpdatedAt`

Decision: the dashboard should be query-driven from stored data, not AI-generated live.

## AI Integration Design

Use Ollama's local HTTP API.

Default configuration:

- Base URL: `http://localhost:11434`
- Model: `qwen2.5-coder:7b`
- Configurable through `appsettings.json`

Example config:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen2.5-coder:7b"
  }
}
```

Use structured prompts that request JSON only.

### Lead Analysis Output Contract

```json
{
  "summary": "string",
  "leadScore": 0,
  "urgencyLevel": "Low | Medium | High",
  "buyingIntent": "Low | Medium | High",
  "recommendedNextAction": "string"
}
```

Validation rules:

- Clamp `leadScore` to `0-100`.
- Normalize urgency and buying intent to known enum values.
- Reject empty summary or next action.
- Store model name and prompt version.

### Meeting Notes Output Contract

```json
{
  "structuredSummary": "string",
  "keyFacts": ["string"],
  "risks": ["string"],
  "recommendedNextAction": "string"
}
```

Prompting strategy:

- Include lead fields if notes are attached to an existing lead.
- Ask for concise business language.
- Avoid legal, financial, or discriminatory recommendations.
- Instruct the model to avoid inventing facts not present in lead data or notes.

Failure handling:

- If Ollama is down, show a clear local setup message.
- If JSON parsing fails, retry once with a repair prompt.
- If retry fails, do not store partial analysis.
- Log the failure through ASP.NET Core logging.

Decision: use local LLM calls synchronously from button-triggered actions. No background queue is needed for this portfolio scope.

## UI/UX Plan

The app should look like a practical internal CRM.

Pages:

- Dashboard with compact metric cards, recent leads, and follow-up queue.
- Lead list with filter row and sortable-looking table.
- Lead details with three major areas:
  - Lead profile and status
  - AI analysis panel
  - Timeline and meeting notes
- Create/edit forms using standard MVC validation.
- Meeting notes assistant with a large notes textarea and structured result display.

Frontend behavior:

- jQuery handles AI button loading state.
- jQuery updates AI panel after successful analysis.
- jQuery confirms deletes.
- Filters should still work without JavaScript through query strings.

Decision: avoid a chatbot-style interface. AI appears as a business assistant embedded into lead workflows.

## Development Roadmap

### Day 1: Project Foundation and Data Model

- Create ASP.NET Core MVC project.
- Add EF Core SQLite.
- Define entities, enums, DbContext, and initial migration.
- Seed realistic real-estate lead data.
- Build layout, navigation, dashboard skeleton.
- Implement lead CRUD.

Acceptance:

- App runs locally.
- SQLite database is created.
- Leads can be created, edited, viewed, deleted.
- Dashboard shows basic counts.

### Day 2: Timeline and Filtering

- Add lead detail page.
- Add interaction timeline.
- Implement interaction creation.
- Implement lead list search and filters.
- Add dashboard recently updated and high priority placeholders.

Acceptance:

- Every lead can show a chronological interaction history.
- Filters work by status, score, location, and source.
- Timeline entries persist and update lead activity.

### Day 3: Ollama AI Integration

- Add Ollama configuration and client.
- Build AI prompt/response parser.
- Implement lead analysis generation and storage.
- Add latest AI analysis panel to lead details.
- Add meeting notes assistant and storage.

Acceptance:

- User can generate a lead analysis on demand.
- AI output is stored and reused on reload.
- Meeting notes can be converted into structured business output.
- Ollama failure produces a clean UI message.

### Day 4: Polish, Testing, and Interview Readiness

- Improve dashboard.
- Add validation and error handling.
- Add focused tests for services and AI parsing.
- Improve CSS for internal app polish.
- Add README with setup instructions, Ollama requirements, and demo flow.
- Seed demo data that showcases the app well.

Acceptance:

- App can be demonstrated end-to-end in under 5 minutes.
- README explains how to run SQLite migrations and Ollama.
- Core workflows work without manual database edits.

## Testing Plan

Recommended tests:

- `LeadService` creates and updates leads correctly.
- `LeadService` filters by status, location, source, and lead score.
- `TimelineService` adds interactions and updates parent lead timestamp.
- `AiResponseParser` accepts valid model JSON.
- `AiResponseParser` rejects malformed or incomplete output.
- `AiAnalysisService` stores generated analysis and does not regenerate automatically.
- `MeetingNotesService` stores raw notes plus structured output.

Manual acceptance scenarios:

1. Create a new Scottsdale buyer lead.
2. Add a phone call and property tour timeline entry.
3. Generate AI analysis.
4. Refresh the lead detail page and verify stored analysis remains.
5. Paste meeting notes and attach the result to the lead.
6. Filter lead list by high score and location.
7. Confirm dashboard counts update.

Decision: prioritize service and parser tests over controller-heavy tests. That gives better interview value for the time available.

## Risk Assessment

Main risks:

- **Ollama availability**
  - Mitigation: clear config, graceful failure message, README setup steps.

- **LLM JSON inconsistency**
  - Mitigation: strict JSON prompts, parser validation, retry once with repair prompt.

- **Scope creep**
  - Mitigation: no authentication, no roles, no background jobs, no separate API project, no Docker.

- **Weak business feel**
  - Mitigation: real-estate-specific seed data, dashboard metrics, timeline history, stored analysis, and practical follow-up recommendations.

- **Too much frontend complexity**
  - Mitigation: server-rendered MVC first, jQuery as progressive enhancement only.

- **Overly generic AI demo**
  - Mitigation: AI works inside lead details and meeting notes workflows, not as a standalone chatbot.

## Assumptions and Defaults

- Product domain is real estate sales, based on the selected direction.
- Target user is a salesperson or sales analyst managing buyer leads.
- SQLite database file will live inside the app's local data path or project root during development.
- The preferred Ollama model is `qwen2.5-coder:7b`, configurable in `appsettings.json`.
- AI analysis is generated only by explicit user action.
- The latest analysis is displayed by default; old analyses remain stored.
- Follow-ups are represented through AI recommendations and timeline entries for v1, with a dedicated follow-up table considered optional.
- No authentication, authorization, Docker, Redis, queues, cloud infrastructure, or CI/CD will be added.
