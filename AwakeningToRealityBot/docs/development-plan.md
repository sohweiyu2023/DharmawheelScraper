# Development Roadmap

The Awakening to Reality build schedule is divided into six major phases spanning 21 weeks.
This roadmap reflects the agreed 320-hour estimate and highlights tangible deliverables for
review at the end of each milestone.

| Phase | Weeks | Focus | Key Deliverables |
|-------|-------|-------|------------------|
| 1 | 1-4 | Foundation | Azure infrastructure-as-code, authentication skeleton, base Blazor shell, secure PDF upload entry point |
| 2 | 5-6 | Ingestion Pipeline | PDF parsing function, chunking/embedding library, Azure AI Search indexing script |
| 3 | 7-10 | Core Chat API | Minimal APIs for synchronous chat, tier validation logic, document-filtered retrieval, default LLM integration |
| 4 | 11-14 | Frontend UI | Document selector, responsive chat canvas, PDF.js viewer, dynamic model selector |
| 5 | 15-19 | Advanced Features | Durable Functions workflows, Web PubSub updates, full admin dashboard, App Configuration writers |
| 6 | 20-21 | Testing & Launch | E2E testing, admin QA, load testing, security review, production deployment |

## First 30 days

1. **Week 1**
   - Procure production domain and configure DNS.
   - Deploy core Azure resources (storage, search, App Configuration, Key Vault, Static Web
     Apps, Front Door) via Bicep.
   - Seed App Configuration with a default "Free" tier and model list from
     `infrastructure/appconfig/initial-settings.json`.

2. **Week 2**
   - Implement Azure AD B2C user flows (sign-up, sign-in, password reset).
   - Integrate authentication into the Blazor shell with protected routes for admin areas.

3. **Week 3**
   - Complete the admin PDF upload flow end-to-end (client form, API endpoint, blob storage
     persistence, ingestion trigger event).

4. **Week 4**
   - Focus on the ingestion worker: deterministic chunking, metadata generation, embedding
     calls, and Azure AI Search indexing.
   - Validate ingestion with at least one Dharma text and confirm search relevance with
     hybrid queries.

## Engineering ceremonies

- **Weekly demo**: Each Friday, demonstrate the latest increment (UI mockups, working APIs,
  ingestion telemetry) to stakeholders.
- **Architecture review**: At the beginning of phases 3 and 5, hold a technical review to
  validate design decisions against scope and budget.
- **Definition of done**: Code reviewed, passing automated tests, updated documentation, and
  deployment pipeline validation where applicable.
