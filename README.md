# AI Email Classifier — .NET Worker Service

A .NET background service that automatically classifies incoming customer emails
using an LLM (OpenAI GPT-4o-mini), writes structured results back to PostgreSQL,
and tracks processing state for reliable, incremental operation.

This project demonstrates integration engineering patterns: external API
integration, a status-machine for fault tolerance, incremental processing with
a persistent watermark, and structured logging — built from scratch and fully
open-source.

## What it does

1. Reads `Pending` emails from a PostgreSQL `EmailInbox` table
2. Sends subject + body to GPT-4o-mini, which returns structured JSON
   (category, priority, summary, assigned department)
3. Writes the classification back to the database
4. Advances the email through a status machine
   (`Pending → Processing → Completed / Failed`)
5. Records each run in a `ProcessingLog` table for auditing and cost tracking

> **Data source note:** This service assumes emails are already parsed and
> inserted into the database by an upstream system. Email ingestion (IMAP/POP3)
> is intentionally out of scope to keep the focus on the integration and
> classification pipeline. Test data is based on anonymised real-world business
> scenarios.

## Architecture

```
Worker (Timer, every N seconds)
  → Load Pending emails since watermark
  → For each: set Processing → call LLM → write result → set Completed
              (on error: set Failed, increment RetryCount)
  → Update watermark in DB
  → Write ProcessingLog
```

### Key design decisions

| Decision | Rationale |
|---|---|
| Set status to `Processing` *before* calling the API | Prevents duplicate processing if the service crashes mid-call |
| Status machine + watermark as two independent layers | Status guarantees correctness; watermark is a performance optimisation. A stale watermark only causes extra scanning, never reprocessing |
| Watermark persisted in DB (`WorkerState` table) | Survives restarts; resumes from last processed point |
| `ContactId` nullable, `Company` as separate table | Real-world normalisation and fault tolerance |
| Per-email error isolation | One failed email never blocks the rest of the batch |

## Tech stack

- .NET 10 Worker Service (BackgroundService)
- Entity Framework Core + Npgsql (PostgreSQL)
- OpenAI API (GPT-4o-mini)
- Serilog (structured logging)
- Docker (PostgreSQL 16)

## Getting started

### Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL)
- An OpenAI API key

### Setup

1. Start PostgreSQL:
   ```bash
   docker start postgres-dev
   ```

2. Set environment variables:
   ```bash
   export OPENAI_API_KEY="your-key-here"
   export DEV_DB_PASSWORD="your-db-password"
   ```

3. Copy the settings template:
   ```bash
   cp appsettings.json.example appsettings.json
   ```

4. Apply migrations:
   ```bash
   dotnet ef database update
   ```

5. Run:
   ```bash
   dotnet run
   ```

## Roadmap

- [ ] `IChatCompletionProvider` abstraction (switch between OpenAI / Claude / local Ollama via config)
- [ ] Automatic retry for failed emails (RetryCount < 3)
- [ ] Human review field (`IsReviewed`) for classification feedback loop
- [ ] Containerisation + CI/CD + Azure deployment

## License

MIT
