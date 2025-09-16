# ATR Server (Azure Functions)

This project hosts the minimal APIs, ingestion triggers, and Durable Functions that power
Awakening to Reality.

## Running locally

1. Copy `local.settings.sample.json` to `local.settings.json` and populate the placeholders.
2. Ensure Azure Storage Emulator or Azurite is running if you are using the default storage
   connection string.
3. Run `func start` or `dotnet run` from this directory.

## HTTP endpoints

| Route | Description |
|-------|-------------|
| `POST /api/chat` | Processes synchronous chat requests using the standard LLM flow. |

## Durable workflows

- `ThinkingOrchestrator`: Example workflow orchestrating the "thinking" model pipeline.

## Next steps

- Replace placeholder LLM calls with production integrations (OpenAI, Gemini, etc.).
- Enforce per-tier quotas using durable entities or Cosmos DB counters.
- Add ingestion triggers for Blob Storage to populate Azure AI Search automatically.
