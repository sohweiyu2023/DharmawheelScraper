# Awakening to Reality AI Chat Bot (ATR v2.5)

The **Awakening to Reality** project modernises the historical Dharmawheel chatbot into a
cloud-first Retrieval Augmented Generation (RAG) platform.  This repository contains a
full-stack implementation plan and working scaffolding for the Blazor WebAssembly client,
Azure Functions powered API, ingestion workers, and the surrounding Azure infrastructure.

The implementation follows the specification provided by the Dharmawheel team and focuses
on:

- A mobile-friendly, citation-rich chat experience that allows users to focus their search
  on one or more curated Dharma documents.
- Multi-model orchestration, including real-time and "thinking" workflows with Azure Durable
  Functions and Azure Web PubSub.
- End-to-end governance with monetisation tiers, centralised configuration, audit-friendly
  secret storage, and an admin dashboard for managing documents, pricing, and usage.
- Serverless, pay-as-you-go infrastructure designed for reliability, observability, and
  operational efficiency.

## Repository layout

```
AwakeningToRealityBot/
├── README.md                    # Project overview
├── docs/                        # Architecture, UX, and roadmap documentation
├── infrastructure/              # Infrastructure-as-code (Bicep) and default configuration
├── src/                         # .NET 8 solution scaffolding
│   ├── Client/                  # Blazor WebAssembly front-end
│   ├── Server/                  # Azure Functions minimal API & orchestrations
│   ├── Shared/                  # Shared contracts and DTOs
│   └── Infrastructure/          # Common services for storage, search, and configuration
└── tests/                       # Test projects (unit, integration, load)
```

Each part of the tree already includes starter source code, configuration, and detailed
documentation so contributors can quickly get productive.

## Getting started

1. Review the [architecture overview](docs/architecture.md) and the [development plan](docs/development-plan.md).
2. Provision Azure resources using the [Bicep templates](infrastructure/bicep/README.md).
3. Update `infrastructure/appconfig/initial-settings.json` with environment-specific values
   (e.g., tier pricing) before importing into Azure App Configuration.
4. Open the solution in Visual Studio or VS Code with the C# and Azure Functions
   extensions.  Restore dependencies with `dotnet restore`.
5. Follow the instructions in `src/Server/README.md` and `src/Client/README.md` to run the
   API and the web client locally.

## Contributing

- Adhere to the coding guidelines in `docs/contributing.md`.
- Avoid committing secrets, production configuration, or generated binaries (bin/obj).
- Validate changes by running the unit and integration test suites under the `tests/`
  directory, and ensure the infrastructure templates pass `bicep build`.

The project uses conventional commits and GitHub pull requests.  Please include screenshots
when modifying the UI.
