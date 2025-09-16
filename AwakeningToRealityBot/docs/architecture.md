# Architecture Overview

This document expands the high-level product vision into a concrete architecture for the
Awakening to Reality (ATR) conversational Dharma study platform.

## System goals

- Deliver fast, contextually grounded answers for Dharma students with verifiable citations.
- Offer flexible monetisation through dynamic model tiers and quotas administered without
  code changes.
- Operate cost-effectively on serverless Azure services while remaining extensible for new
  AI models or document types.

## High-level architecture

```
┌──────────────────┐      ┌──────────────────┐      ┌───────────────────────┐
│  Blazor WASM UI  │────▶︎│ Minimal API Tier │────▶︎│ Azure AI Search Index │
│  (Azure Static   │◀────│ (Azure Functions)│◀────│  (Vector + Keyword)   │
│   Web Apps +     │  ▲   └────┬────────────┘      └─────────┬─────────────┘
│   Azure Front    │  │        │                           │
│   Door CDN)      │  │        │                           │
└────────┬─────────┘  │        │                           │
         │            │        │                           │
         │Web PubSub  │ Durable Functions          Azure Blob Storage
         │Status      │ orchestrate "thinking"      (PDF originals + text
         │Updates     │ model workflows              chunk archives)
         │            │
         ▼            │
   Browser updates    │
   "Thinking..."      │
                      ▼
                 Azure App Configuration
                   + Key Vault for secrets
```

- **Client**: A Blazor WebAssembly single-page app (SPA) hosted on Azure Static Web Apps and
  accelerated worldwide via Azure Front Door.  The app uses SignalR/Azure Web PubSub to
  receive streaming status updates and final answers.
- **API**: A collection of .NET 8 minimal APIs packaged as Azure Functions.  Synchronous
  routes serve standard queries, while Durable Functions orchestrate longer "thinking"
  sessions.  All configuration (tiers, quotas, model availability) is loaded from Azure App
  Configuration, and all secrets come from Azure Key Vault.
- **Knowledge base**: PDFs are uploaded to Azure Blob Storage.  An ingestion function parses
  PDFs, creates overlapping text chunks, embeds them with OpenAI's `text-embedding-3-small`,
  and stores the results and metadata in Azure AI Search.
- **Real-time**: Azure Web PubSub sends orchestration updates to the SPA, eliminating polling.

## Service boundaries

| Project               | Responsibility                                                   |
|-----------------------|------------------------------------------------------------------|
| `Client`              | UI components, chat experience, PDF viewer, admin dashboard UI   |
| `Server`              | HTTP API, Durable orchestrations, security policies              |
| `Infrastructure`      | Adapters for App Config, Blob Storage, AI Search, Embeddings     |
| `Shared`              | DTOs, contracts, telemetry primitives                            |
| `Infrastructure/bicep`| Azure resource provisioning                                      |

## Data flow (RAG pipeline)

1. **Ingestion**
   - Admin uploads PDF via secure portal.
   - Blob trigger launches ingestion function.
   - Text is extracted, chunked (~500-1000 tokens, 10-15% overlap) and annotated with
     metadata (documentId, page, section).
   - Embeddings are generated and stored alongside text and metadata in Azure AI Search.

2. **Standard query**
   - User submits question and document filter via SPA.
   - API validates tier entitlements using settings from Azure App Configuration.
   - Query is embedded and submitted to Azure AI Search with hybrid retrieval and optional
     document filters.
   - Prompt is built using retrieved chunks and sent to the selected standard LLM.
   - Response (with inline citation tokens) is streamed back to the client.

3. **Thinking model query**
   - Durable Functions orchestrate long-running calls to deep-reasoning models.
   - The client receives immediate acknowledgement and listens for final results via Web
     PubSub.
   - Final response includes citations and summary metadata for analytics.

## Security & compliance

- **Authentication**: Azure AD B2C handles user flows and social sign-in.  An alternative
  implementation using ASP.NET Core Identity can be toggled via App Configuration.
- **Authorisation**: Role- and tier-based policies implemented in the Server project.
- **Secrets**: Azure Key Vault stores API keys, connection strings, and admin credentials.
- **Telemetry**: Application Insights collects metrics, traces, and custom events for usage
  and cost analysis.

## Scalability considerations

- Azure Functions scale elastically; Blob triggers and Durable Functions manage workload
  surges without throttling interactive users.
- Azure AI Search partitions handle growth in both document volume and query throughput.
- Static Web Apps automatically integrates with a global CDN, while Azure Front Door ensures
  low latency for interactive users.

## Extensibility roadmap

- Add support for audio/video transcription ingestion by plugging in Azure AI Vision or
  Speech services.
- Allow per-community document collections by extending metadata filters and RBAC.
- Introduce multi-language support with translation pipelines and client localisation.
