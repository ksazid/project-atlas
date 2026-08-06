<p align="center">
  <img src="docs/assets/pes-overview.png" alt="Product Engineering Starter Overview" width="100%">
</p>

# Product Engineering Starter Mobile

An open-source governance layer for turning an approved **PRD** and **TRD** into a traceable, secure, cost-controlled web product.

Product Engineering Starter (PES) decides **what is approved and safe to build**. [Superpowers](https://github.com/obra/superpowers) provides the default methodology for planning, implementing, reviewing, debugging and finishing an approved slice.

## Atlas implementation status

The current product implementation uses PES without restructuring the framework.

- VS-01: identity and Business foundation
- VS-02: Business profile, goals and context
- VS-03: modular Knowledge Pack foundation in PR #7

VS-03 introduces `KnowledgePack -> KnowledgePackVersion -> KnowledgeSection[]`, immutable published versions, explicit exact-version business assignment, lifecycle and audit APIs, optimistic concurrency, and a unified mobile Knowledge Pack view with secure offline caching. It does not introduce AI execution, embeddings, vector storage, production deployment, production credentials, or paid services.

See `docs/slices/VS-03.md`, `docs/architecture.md`, and `docs/domain-model.md`.

## What PES provides

- PRD/TRD intake and conflict detection
- source-linked requirements and traceability
- roadmaps, milestones, epics and vertical slices
- architecture, design and security governance
- typed approvals, structured decisions and governed lifecycle transitions
- focused active-slice context and protected paths
- risk-based impact, ownership, release, rollback and post-release contracts
- Loop Engineering-inspired state, budgets, gates, run history and stop conditions
- deterministic preflight, certification evidence and exact-SHA release controls
- a generated read-only delivery dashboard with computed action notifications
- optional memory, knowledge, brevity, deployment and delivery-graph integrations
- human-controlled merge, release and production enablement

## Default mobile stack

- Next.js + TypeScript
- ASP.NET Core
- PostgreSQL + EF Core
- OpenAPI
- xUnit and Playwright
- Docker Compose
- GitHub Actions

## Mobile application baseline

- Expo SDK 57 / React Native 0.86 / React 19.2
- Expo Router and typed routes
- EAS development, preview and production build profiles
- SecureStore, Notifications, Updates, Localization and Network state
- TanStack Query and Zod
- Jest Expo and React Native Testing Library
- Android and iOS release gates bound to the exact certified SHA

The static `dashboard/` remains the read-only PES governance dashboard. It is not a web product application.

## Prerequisites

- Git 2.40+
- Node.js 24 LTS with npm
- .NET SDK 10.x
- Docker with Compose v2
- A supported coding-agent harness for Superpowers
- Optional: GitHub CLI, Python 3.9+, Codex Security, NotebookLM, MemPalace and Caveman

## Install

Use this repository as a GitHub template, or clone it directly:

```bash
git clone https://github.com/ksazid/product-engineering-starter.git my-product
cd my-product
npm install
npm run preflight:structure
npx expo install --fix --cwd apps/mobile
npm run preflight
```

## Start a product

1. Complete `product/PRD.md`.
2. Complete `product/TRD.md`.
3. Add design rules to `product/DESIGN.md`.
4. Define terminology in `product/GLOSSARY.md`.
5. Approve the source documents.
6. Run intake and planning.

```bash
npm run product:intake
npm run planning:generate
npm run planning:validate
npm run engineering:advise
```

The intake process blocks missing sections, unresolved draft status, conflicts and unsupported assumptions rather than inventing policy.

## Delivery workflow

```text
PRD + TRD
→ product, technical and security intake
→ source-linked requirements
→ roadmap, milestones, epics and vertical slices
→ typed scope and policy decisions
→ human plan and implementation approval
→ activate one vertical slice
→ focused context pack
→ Superpowers planning, TDD, implementation and review
→ deterministic preflight
→ risk-triggered security or delivery graph when justified
→ exact-SHA certification approval
→ release contract and rollback readiness
→ human release and production-enable approval
→ production verification
→ post-release outcome review
```

```bash
npm run slice:activate -- VS-01
npm run slice:status
npm run slice:transition -- implementing
npm run slice:validate
```

## End-to-end governance

PES records independent approval types for scope, policy, implementation, certification, release and production enablement. It also records product decisions and the exact gates each unresolved decision blocks.

The canonical lifecycle is:

```text
proposed
→ discovery
→ decision-pending
→ approved
→ ready-for-implementation
→ implementing
→ testing
→ certification
→ certified
→ release-pending
→ released
→ observed
→ validated
```

Exception states include `blocked`, `rejected`, `deferred`, `superseded` and `rolled-back`.

Implementation permission is explicit:

```text
specification-only
contracts-only
runtime-disabled
runtime-enabled
production-enabled
```

Run the governance validator:

```bash
npm run governance:validate
```

See `docs/governance/END-TO-END.md`.

## Delivery dashboard

PES includes a static read-only dashboard generated from the authoritative delivery files. It shows slice lifecycle, gate progress, approvals, pending decisions, blockers, certification, releases, rollback history and computed notifications.

```bash
npm run dashboard:build
npm run dashboard:serve
```

Open `http://127.0.0.1:4173`. GitHub Actions also uploads the generated dashboard as the `pes-dashboard` artifact.

The dashboard deliberately has no database, authentication or editable approvals. Repository files remain authoritative.

## Operating modes

PES begins in **Lite** mode and adds complexity only when evidence shows it will reduce risk or rework.

| Mode | Intended use | Delivery execution |
| --- | --- | --- |
| **Lite** | MVPs, solo developers and low-risk work | Single-agent Superpowers execution |
| **Standard** | Growing products, multiple modules and formal releases | Optional risk-triggered review graph |
| **Enterprise** | High-risk, regulated or multi-team products | Optional full specialist delivery graph |

## Risk-triggered delivery graph

Standard and Enterprise modes can activate bounded specialist review when risk justifies it. The graph cannot change approved scope, accept security risk, merge code, or deploy.

```bash
npm run delivery-graph:check
```

Configuration: `.engineering/DELIVERY-GRAPH.json`  
Guidance: `docs/integrations/DELIVERY-GRAPH.md`

## Responsibility boundary

| PES | Superpowers |
| --- | --- |
| Product and technical authority | Feature-level clarification |
| Requirement IDs and traceability | Implementation planning |
| Roadmap and vertical slices | Worktrees and execution |
| Typed approvals and decisions | No approval authority |
| Architecture and security policy | TDD and debugging |
| Protected paths and human gates | Spec and code-quality review |
| Preflight and certification | Branch completion workflow |
| Release and production approval | No release authority |

## Deployment strategy

PES treats frontend, API and database as one coordinated release. Deployment requires a certified exact SHA and explicit human release and production approval.

## UI workflow

Use the approved design baseline first, then only relevant installed skills. Product features must extend the existing PES visual and technical framework rather than replace it.

## Security model

Use deterministic secret scanning, dependency validation, authorization tests, security headers and protected-path rules. Codex Security remains optional and risk-triggered.

## Main commands

```bash
npm run product:intake
npm run planning:generate
npm run planning:validate
npm run governance:validate
npm run slice:activate -- VS-01
npm run slice:transition -- <state>
npm run slice:status
npm run slice:validate
npm run delivery:status
npm run dashboard:build
npm run dashboard:serve
npm run delivery-graph:check
npm run deployment:advise
npm run security:classify -- <changed-files>
npm run knowledge:export
npm run memory:doctor
npm run optimize:context
npm run preflight
npm run certify
npm run engineering:advise
npm run profile:show
```

## Deliberate exclusions

No Kubernetes default, microservice generation, event-sourcing default, generic repositories, uncontrolled agent swarms, autonomous merge, autonomous production deployment, general-purpose project-management system, mandatory hosting provider or mandatory Ruflo dependency.

## License

MIT — see [LICENSE](LICENSE).
