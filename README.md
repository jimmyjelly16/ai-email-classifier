# AI Email Classifier — .NET Worker Service

A .NET background service that automatically classifies incoming customer emails using a Large Language Model, writes structured results back to PostgreSQL, and tracks processing state for reliable, incremental operation.

This project was built from scratch to demonstrate **integration engineering patterns**: external API integration, a provider-agnostic LLM abstraction, a status machine for fault tolerance, crash-safe incremental processing, a human-in-the-loop review feedback loop, and structured logging.

> **Why this project exists:** The value of an integration engineer isn't "calling an API" — it's the design decisions that make a system reliable, observable, and maintainable in production. This README highlights those decisions explicitly.

---

## What it does

1. Reads `Pending` emails from a PostgreSQL `EmailInbox` table
2. Sends subject + body to an LLM (OpenAI GPT-4o-mini), which returns structured JSON: **category, priority, summary, assigned department**
3. Writes the classification back to the database
4. Advances each email through a status machine (`Pending → Processing → Completed / Failed`)
5. Automatically retries transient failures (up to a configurable limit), leaving the rest for human review
6. Records every run in a `ProcessingLog` table for auditing and cost tracking

> **Data source note:** This service assumes emails are already parsed and inserted into the database by an upstream system. Email ingestion (IMAP/POP3) is intentionally out of scope, to keep the focus on the integration and classification pipeline. Test data is based on anonymised real-world business scenarios.

---

## Architecture

```
Worker (Timer, every N seconds)
  → Load Pending emails (since watermark) + retryable Failed emails
  → For each:
        set Processing  →  classify via LLM  →  write result  →  set Completed
        (on error: set Failed, increment RetryCount)
  → Update watermark in DB
  → Write ProcessingLog
```

### LLM provider abstraction

The classification logic is fully **provider-agnostic**. It depends only on an `IChatCompletionProvider` interface, never on a specific vendor SDK.

```
EmailClassifierService  (business logic — knows no vendor)
        │  uses
        ▼
IChatProviderFactory.GetActive()   ← picks provider from config
        │
        ▼
IChatCompletionProvider
        ├── OpenAiChatProvider   (implemented)
        ├── ClaudeChatProvider   (drop-in: add one DI line)
        └── OllamaChatProvider   (drop-in: local LLM for data-residency needs)
```

Switching the LLM provider requires changing **one config value** (`"Llm": { "Provider": "OpenAI" }`) and restarting — no business-logic changes. Adding a new provider requires **one DI registration line**; the factory never changes (open/closed principle).

Vendor differences (e.g. OpenAI puts the system prompt in the messages array, while Claude takes it as a separate parameter) are absorbed inside each provider implementation behind a unified `ChatRole` model.

---

## Key design decisions

| Decision | Rationale |
|---|---|
| Set status to `Processing` **before** calling the API | Prevents duplicate processing if the service crashes mid-call |
| Status machine + watermark as **two independent layers** | Status guarantees correctness; the watermark is only a performance optimisation. A stale watermark causes extra scanning, never reprocessing |
| Watermark **persisted in DB** (`WorkerState` table) | Survives restarts; resumes from the last processed point. ISO-8601 round-trip format preserves UTC kind for Npgsql |
| Retryable `Failed` emails **bypass the watermark** | A failed email may be old; filtering it by watermark would prevent the retry from ever happening |
| `ContactId` nullable, `Company` as a separate table | Real-world normalisation and fault tolerance — unknown senders still get processed |
| Per-email error isolation | One failed email never blocks the rest of the batch |
| Provider-agnostic `IChatCompletionProvider` | No vendor lock-in; switch via config, add providers via one DI line |
| Human-in-the-loop review fields | Captures human-confirmed corrections as the data foundation for a future dynamic few-shot feedback loop |
| `LoggerMessage` source generators | High-performance structured logging — avoids string interpolation and allocations on hot paths |

---

## Resilience: automatic retry

Transient failures (API timeout, network blip) should never permanently lose an email or stop the service.

- The worker picks up `Pending` emails **and** `Failed` emails whose `RetryCount` is below the configured limit (default 3)
- Each retry increments `RetryCount`
- Once the limit is reached, the email stays `Failed` with its `ErrorMessage` preserved for human inspection

This is an **at-least-once** processing design.

---

## Human-in-the-loop review (feedback foundation)

Classification quality improves over time without changing code. Reviewers don't check every email — only the ones that look wrong.

`EmailInbox` carries review fields: `IsReviewed`, `IsCorrect`, `CorrectedCategory`, `ReviewNote`, `ReviewedAt`. When a reviewer flags a misclassification and supplies the correct category, that confirmed example is stored in the database.

`ReviewQueryService` exposes:
- confirmed-correct examples (future few-shot material),
- incorrectly-classified cases (to analyse where the prompt needs work),
- failed cases that have exhausted retries (needing human attention).

> **Roadmap:** a planned dynamic few-shot stage will pull recent confirmed-correct examples into the prompt at classification time, so the model learns from real corrections — moving "improving classification" from the engineer to the people who understand the business best.

---

## Tech stack

- **.NET 8** Worker Service (`BackgroundService`)
- **Entity Framework Core** + **Npgsql** (PostgreSQL)
- **OpenAI API** (GPT-4o-mini)
- **Serilog** with `LoggerMessage` source generators (structured logging)
- **Docker** (PostgreSQL 16)

---

## Getting started

### Prerequisites

- .NET 8 SDK
- Docker (for PostgreSQL)
- An OpenAI API key

### Setup

1. Start PostgreSQL:
   ```bash
   docker start postgres-dev
   ```

2. Set environment variables (never committed to source control):
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

### Configuration

`appsettings.json` (no secrets — keys come from environment variables):

```json
{
  "Llm":    { "Provider": "OpenAI" },
  "OpenAI": { "DefaultModel": "gpt-4o-mini" },
  "Worker": {
    "IntervalSeconds": 30,
    "WatermarkMinutesBack": 1440,
    "MaxRetryCount": 3
  }
}
```

---

## Security

Secrets are handled **secure-by-default**:
- API keys and DB passwords are injected from environment variables, never written to config or source control
- `appsettings.json` and `appsettings.Development.json` are git-ignored; only `*.example` templates are committed
- Configuration values are validated on startup

---

## Roadmap

- [ ] Dynamic few-shot: inject recent human-confirmed examples into the prompt at classification time
- [ ] `ClaudeChatProvider` / `OllamaChatProvider` (provider abstraction already supports both)
- [ ] React front-end with a review UI (surfacing failed + to-be-reviewed cases)
- [ ] Containerisation, CI/CD, and Azure deployment (Container Apps, Key Vault, API Management, Monitor)
- [ ] Polly-based retry + circuit breaker around providers for production-grade resilience

---

## License

MIT
