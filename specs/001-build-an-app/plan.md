# Implementation Plan: Telegram Channel URL Monitor with yt-dlp Integration

**Branch**: `feature/001-build-an-app` | **Date**: 2025-10-04 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-build-an-app/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from file system structure or context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code, or `AGENTS.md` for all other agents).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary

Automated Telegram bot that monitors a private channel for URLs, downloads them via yt-dlp (local or remote), and provides real-time emoji feedback (👀 Seen → ⚙️ Processing → ✅ Complete). Bot uses Telegram Bot API Token authentication, sequential download processing, SQLite persistence for queue recovery, and configurable storage destinations. Configuration via config file with environment variable overrides (12-factor pattern). Deployed as Docker container to Unraid platform with health checks and volume mounts for data/downloads.

## Technical Context
**Language/Version**: C# 12 / .NET 8 LTS  
**Primary Dependencies**: Telegram.Bot (Bot API client), Microsoft.Data.Sqlite (persistence), Serilog (structured logging)  
**Storage**: SQLite for queue/state persistence, filesystem for downloaded media  
**Testing**: xUnit, Moq (mocking), FluentAssertions, Testcontainers (integration tests)  
**Target Platform**: Docker container on Linux (Unraid), developed in VS Code devcontainer  
**Project Type**: Single (backend service with no frontend)  
**Performance Goals**: <5s message detection latency, <1s emoji reaction response, handle 100+ queued downloads  
**Constraints**: Sequential downloads only, <200MB memory footprint, graceful restart with queue recovery  
**Scale/Scope**: Single channel monitoring, ~10-50 URLs/day typical, designed for 24/7 operation

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Test-Driven Development**: ✅ Unit tests for URL extraction, queue management, state persistence. Integration tests for Telegram API mocking. E2E tests with test channel + mock yt-dlp. TDD enforced via pre-PR quality gates.
- **Code Quality Standards**: ✅ C# nullable reference types enabled, XML docs for public APIs, PSScriptAnalyzer for scripts. EditorConfig for consistent formatting. SonarAnalyzer for code quality metrics.
- **Security First**: ✅ Bot token via environment variable/Docker secrets only. yt-dlp API credentials (if remote mode) stored securely. Input validation on all URLs and message text. No secrets in repository.
- **Performance Requirements**: ✅ <5s message polling latency (Telegram Bot API long polling). <1s emoji reaction updates. Sequential queue prevents resource contention. SQLite indexed queries for O(log n) lookups.
- **User Experience Consistency**: ✅ Predictable emoji progression (👀→⚙️→✅/❌). Clear startup logs with config validation. Comprehensive error messages in logs. Graceful degradation on network failures.
- **Deployment Standards**: ✅ Multi-stage Dockerfile (build + runtime). Health check endpoint (/health HTTP). Docker volumes for SQLite DB and downloads. Unraid Community Applications template. Non-root container user.
- **Pre-PR Quality Gates**: ✅ dotnet format for auto-fix. PSScriptAnalyzer --Fix for PowerShell. All unit/integration tests must pass. Coverage gate ≥80% for core logic.

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
src/
├── TelegramYtDlpBot/
│   ├── Models/              # Entity definitions (Message, DownloadJob, Configuration)
│   ├── Services/            # Core business logic
│   │   ├── TelegramMonitor.cs      # Channel monitoring + emoji reactions
│   │   ├── UrlExtractor.cs         # URL parsing and validation
│   │   ├── DownloadQueue.cs        # Sequential queue management
│   │   ├── YtDlpExecutor.cs        # Local/remote yt-dlp execution
│   │   └── StateManager.cs         # SQLite persistence operations
│   ├── Configuration/       # Config loading (file + env merging)
│   ├── Persistence/         # SQLite schema and migrations
│   ├── Health/              # Health check endpoint
│   └── Program.cs           # Main entry point + DI setup

tests/
├── TelegramYtDlpBot.Tests/
│   ├── Unit/                # Pure logic tests (UrlExtractor, DownloadQueue)
│   ├── Integration/         # Telegram API mocking, SQLite tests
│   └── E2E/                 # Full workflow with Testcontainers

scripts/
└── powershell/              # Build, test, deployment scripts

.devcontainer/
└── devcontainer.json        # VS Code dev environment config

docker/
├── Dockerfile               # Multi-stage production build
└── unraid-template.xml      # Unraid Community Applications template
```

**Structure Decision**: Single project structure chosen (not web/mobile). Backend service only with no user-facing frontend. Clear separation: Models (data), Services (logic), Persistence (storage), Configuration (settings). Tests mirror source structure with unit/integration/E2E distinction.

## Phase 0: Outline & Research

**Status**: ✅ Complete

**Output**: `research.md` with all technical decisions resolved.

**Decisions Made**:
1. Telegram.Bot library (official .NET client) for Bot API integration
2. Strategy pattern for local/remote yt-dlp execution modes
3. SQLite schema with 3 tables (ProcessedMessages, DownloadJobs, AppState)
4. 12-factor configuration: appsettings.json + environment variable overrides
5. ASP.NET Core minimal API for /health endpoint
6. Hardcoded emoji defaults with config file overrides
7. Automatic retry with exponential backoff (max 3 attempts)
8. Structured JSON logging via Serilog to stdout
9. Post-download actions deferred to future (MVP logs only)
10. yt-dlp config file pass-through via --config-location argument
11. Optional remote API key via Authorization header
12. Metrics exposure deferred to future (health check sufficient for MVP)

---

## Phase 1: Design & Contracts

**Status**: ✅ Complete

**Artifacts Generated**:
- `data-model.md`: Entity definitions, SQLite schema, data access patterns
- `contracts/ITelegramMonitor.md`: Channel monitoring and emoji reactions
- `contracts/IUrlExtractor.md`: URL parsing and validation
- `contracts/IDownloadQueue.md`: Sequential queue management with persistence
- `contracts/IYtDlpExecutor.md`: Local/remote download execution
- `quickstart.md`: Developer onboarding and E2E workflow guide

**Design Highlights**:
- Clean separation of concerns (Models, Services, Persistence, Configuration)
- Interface-driven design for testability and DI
- SQLite WAL mode for concurrent reads during writes
- Event-driven architecture (MessageReceived event pattern)
- Strategy pattern for yt-dlp mode switching
- Retry logic with Polly resilience library

---

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
1. Load `.specify/templates/tasks-template.md` as base structure
2. Generate tasks from Phase 1 design artifacts:
   - **Contract Tests** (T001-T005): One test class per interface contract
   - **Model Creation** (T006-T008): Entity classes + SQLite schema setup
   - **Service Implementation** (T009-T018): Implement each interface with TDD
   - **Configuration** (T019-T020): Config loading + validation
   - **Integration** (T021-T023): Wire up DI, health checks, main program
   - **Pre-PR Quality Gates** (T024-T029): Linting, testing, Docker build
   - **Documentation** (T030-T032): README, deployment guides

**Ordering Strategy**:
- **TDD Order**: Tests before implementation (Red-Green-Refactor)
- **Dependency Order**: 
  1. Models (no dependencies)
  2. State/persistence layer
  3. Business services (depend on models/persistence)
  4. Integration/orchestration (depends on all services)
- **Parallelization**: Mark [P] for tasks operating on different files

**Estimated Output**: ~30 numbered, ordered tasks in `tasks.md`

**Task Template Structure** (per constitution):
```
- [ ] T###: [Description]
  - Type: [Contract Test | Unit Test | Implementation | Integration | Infrastructure]
  - Files: [paths to create/modify]
  - Dependencies: [prerequisite task numbers]
  - Acceptance: [how to verify completion]
  - [P] (if parallel-safe)
```

**Example Tasks** (will be fully generated by /tasks):
- T001: Write ITelegramMonitor contract tests [P]
- T006: Create DownloadJob model class [P]
- T009: Implement UrlExtractor service (TDD)
- T021: Wire up dependency injection in Program.cs
- T024: Run dotnet format and verify linting passes

**IMPORTANT**: This phase is executed by the `/tasks` command, NOT by `/plan`

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each contract → contract test task [P]
- Each entity → model creation task [P] 
- Each user story → integration test task
- Implementation tasks to make tests pass

**Ordering Strategy**:
- TDD order: Tests before implementation 
- Dependency order: Models before services before UI
- Mark [P] for parallel execution (independent files)

**Estimated Output**: 25-30 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

**No Complexity Deviations** ✅

All constitutional requirements met:
- TDD enforced (tests before implementation in task order)
- Code quality standards applied (dotnet format, PSScriptAnalyzer)
- Security first (secrets via env vars, input validation)
- Performance requirements achievable (<5s latency, sequential processing)
- UX consistency (emoji progression, clear logging)
- Deployment standards (Docker, health checks, volumes)
- Pre-PR quality gates (linting, testing in Phase 3.6 tasks)

---

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [x] Phase 2: Task planning approach described (/plan command)
- [ ] Phase 3: Tasks generated (/tasks command - NEXT STEP)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved (5/5 answered in spec.md)
- [x] Complexity deviations documented (none required)

**Artifacts Generated**:
- [x] specs/001-build-an-app/research.md (12 research questions answered)
- [x] specs/001-build-an-app/data-model.md (entities, schema, C# models)
- [x] specs/001-build-an-app/contracts/ITelegramMonitor.md
- [x] specs/001-build-an-app/contracts/IUrlExtractor.md
- [x] specs/001-build-an-app/contracts/IDownloadQueue.md
- [x] specs/001-build-an-app/contracts/IYtDlpExecutor.md
- [x] specs/001-build-an-app/quickstart.md
- [ ] specs/001-build-an-app/tasks.md (generated by /tasks command)

---
*Based on Constitution v1.2.0 - See `/memory/constitution.md`*
