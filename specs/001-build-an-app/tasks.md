# Tasks: Telegram Channel URL Monitor with yt-dlp Integration

**Feature**: 001-build-an-app  
**Branch**: `feature/001-build-an-app`  
**Input**: Design documents from `/specs/001-build-an-app/`  
**Prerequisites**: ✅ plan.md, research.md, data-model.md, contracts/, quickstart.md

---

## Execution Summary

**Total Tasks**: 38  
**Phases**: 6 (Setup → Tests → Core → Integration → Polish → Pre-PR Gates)  
**Parallelizable**: 19 tasks marked [P]  
**Technology Stack**: C# 12, .NET 8, Telegram.Bot, Microsoft.Data.Sqlite, Serilog, xUnit

---

## Path Conventions

**Source Code**: `src/TelegramYtDlpBot/`  
**Tests**: `tests/TelegramYtDlpBot.Tests/`  
**Scripts**: `scripts/powershell/`  
**Docker**: `docker/`  
**DevContainer**: `.devcontainer/`

---

## Phase 3.1: Setup & Infrastructure

### T001: [X] Create project structure
**Type**: Setup  
**Description**: Create .NET solution and project files following single-project structure from plan.md  
**Files to create**:
- `src/TelegramYtDlpBot/TelegramYtDlpBot.csproj`
- `tests/TelegramYtDlpBot.Tests/TelegramYtDlpBot.Tests.csproj`
- `TelegramYtDlpBot.sln`
- `src/TelegramYtDlpBot/Models/` (directory)
- `src/TelegramYtDlpBot/Services/` (directory)
- `src/TelegramYtDlpBot/Configuration/` (directory)
- `src/TelegramYtDlpBot/Persistence/` (directory)
- `src/TelegramYtDlpBot/Health/` (directory)
- `tests/TelegramYtDlpBot.Tests/Unit/` (directory)
- `tests/TelegramYtDlpBot.Tests/Integration/` (directory)
- `tests/TelegramYtDlpBot.Tests/E2E/` (directory)

**Commands**:
```powershell
dotnet new sln -n TelegramYtDlpBot
dotnet new console -n TelegramYtDlpBot -o src/TelegramYtDlpBot -f net8.0
dotnet new xunit -n TelegramYtDlpBot.Tests -o tests/TelegramYtDlpBot.Tests -f net8.0
dotnet sln add src/TelegramYtDlpBot/TelegramYtDlpBot.csproj
dotnet sln add tests/TelegramYtDlpBot.Tests/TelegramYtDlpBot.Tests.csproj
dotnet add tests/TelegramYtDlpBot.Tests reference src/TelegramYtDlpBot
```

**Acceptance**: Solution builds with `dotnet build`

---

### T002: [X] Install NuGet dependencies
**Type**: Setup  
**Description**: Add all required NuGet packages per research.md technology stack  
**Files to modify**:
- `src/TelegramYtDlpBot/TelegramYtDlpBot.csproj`
- `tests/TelegramYtDlpBot.Tests/TelegramYtDlpBot.Tests.csproj`

**Commands**:
```powershell
# Production dependencies
dotnet add src/TelegramYtDlpBot package Telegram.Bot --version 19.*
dotnet add src/TelegramYtDlpBot package Microsoft.Data.Sqlite --version 8.*
dotnet add src/TelegramYtDlpBot package Serilog --version 3.*
dotnet add src/TelegramYtDlpBot package Serilog.Sinks.Console --version 5.*
dotnet add src/TelegramYtDlpBot package Serilog.Formatting.Compact --version 2.*
dotnet add src/TelegramYtDlpBot package Polly --version 8.*
dotnet add src/TelegramYtDlpBot package Microsoft.Extensions.Hosting --version 8.*
dotnet add src/TelegramYtDlpBot package Microsoft.Extensions.Configuration.Json --version 8.*
dotnet add src/TelegramYtDlpBot package Microsoft.Extensions.Options.ConfigurationExtensions --version 8.*
dotnet add src/TelegramYtDlpBot package Microsoft.AspNetCore.Diagnostics.HealthChecks --version 8.*

# Test dependencies
dotnet add tests/TelegramYtDlpBot.Tests package Moq --version 4.*
dotnet add tests/TelegramYtDlpBot.Tests package FluentAssertions --version 6.*
dotnet add tests/TelegramYtDlpBot.Tests package Testcontainers --version 3.*
dotnet add tests/TelegramYtDlpBot.Tests package coverlet.collector --version 6.*
```

**Acceptance**: `dotnet restore` succeeds without errors

---

### T003: [X] [P] Configure linting and code quality tools
**Type**: Setup  
**Description**: Set up dotnet format, EditorConfig, nullable reference types, code analysis  
**Files to create**:
- `.editorconfig`
- `src/TelegramYtDlpBot/GlobalUsings.cs`

**Files to modify**:
- `src/TelegramYtDlpBot/TelegramYtDlpBot.csproj` (add `<Nullable>enable</Nullable>`, analyzers)

**Content for .editorconfig**:
```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Naming conventions
dotnet_naming_rule.interfaces_should_begin_with_i.severity = warning
dotnet_naming_rule.interfaces_should_begin_with_i.symbols = interface
dotnet_naming_rule.interfaces_should_begin_with_i.style = begins_with_i
dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.capitalization = pascal_case

# Code quality
dotnet_diagnostic.CA1062.severity = warning  # Validate arguments
dotnet_diagnostic.CA2007.severity = none     # Allow ConfigureAwait(false)
```

**Acceptance**: `dotnet format --verify-no-changes` passes

---

### T004: [X] [P] Create appsettings.json configuration files
**Type**: Setup  
**Description**: Create configuration file structure per research.md 12-factor pattern  
**Files to create**:
- `src/TelegramYtDlpBot/appsettings.json`
- `src/TelegramYtDlpBot/appsettings.Development.json` (gitignored)

**Content for appsettings.json**:
```json
{
  "Telegram": {
    "BotToken": "",
    "ChannelId": 0,
    "PollingIntervalSeconds": 2
  },
  "YtDlp": {
    "Mode": "Local",
    "ExecutablePath": "/usr/local/bin/yt-dlp",
    "RemoteApiUrl": "",
    "RemoteApiKey": "",
    "Quality": "bestvideo+bestaudio/best",
    "OutputTemplate": "%(uploader)s/%(upload_date)s/%(title)s.%(ext)s",
    "ConfigFilePath": "",
    "TimeoutMinutes": 60
  },
  "Storage": {
    "DownloadPath": "/data/downloads",
    "DatabasePath": "/data/state.db",
    "MinimumFreeSpaceGB": 1
  },
  "Emojis": {
    "Seen": "👀",
    "Processing": "⚙️",
    "Complete": "✅",
    "Error": "❌"
  },
  "Logging": {
    "MinimumLevel": "Information"
  }
}
```

**Acceptance**: Files exist and are valid JSON

---

### T005: [X] [P] Initialize SQLite schema script
**Type**: Setup  
**Description**: Create SQL migration script from data-model.md schema  
**Files to create**:
- `src/TelegramYtDlpBot/Persistence/schema.sql`

**Content**: Per data-model.md SQLite Schema section (3 tables: ProcessedMessages, DownloadJobs, AppState)

**Acceptance**: Script executes without errors in `sqlite3 :memory:`

---

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3

**CRITICAL: All test tasks below MUST be written and MUST FAIL before ANY implementation tasks in Phase 3.3**

### T006: [X] [P] Contract test for ITelegramMonitor
**Type**: Contract Test  
**Description**: Write tests for ITelegramMonitor interface per contracts/ITelegramMonitor.md  
**Files to create**:
- `tests/TelegramYtDlpBot.Tests/Unit/Services/TelegramMonitorTests.cs`

**Test Cases** (from contract):
- `StartMonitoring_WithValidConfig_RaisesMessageReceivedEvent()`
- `StartMonitoring_WithInvalidToken_ThrowsInvalidOperationException()`
- `StartMonitoring_WithNetworkError_RetriesWithBackoff()`
- `SetReaction_WithValidMessage_ReturnsTrue()`
- `SetReaction_WithInvalidMessage_ReturnsFalse()`
- `SetReaction_WithRateLimit_RetriesOnce()`

**Acceptance**: Tests compile, all fail with "Not implemented"

---

### T007: [X] [P] Contract test for IUrlExtractor
**Type**: Contract Test  
**Description**: Write tests for IUrlExtractor interface per contracts/IUrlExtractor.md  
**Files to create**:
- `tests/TelegramYtDlpBot.Tests/Unit/Services/UrlExtractorTests.cs`

**Test Cases** (from contract):
- `ExtractUrls_WithSingleUrl_ReturnsSingleUrl()`
- `ExtractUrls_WithMultipleUrls_ReturnsAll()`
- `ExtractUrls_WithDuplicateUrls_ReturnsDeduplicated()`
- `ExtractUrls_WithNoUrls_ReturnsEmptyList()`
- `ExtractUrls_WithMixedValidInvalid_ReturnsOnlyValid()`
- `IsValidUrl_WithValidHttp_ReturnsTrue()`
- `IsValidUrl_WithValidHttps_ReturnsTrue()`
- `IsValidUrl_WithFtpScheme_ReturnsFalse()`
- `IsValidUrl_WithRelativeUrl_ReturnsFalse()`
- `IsValidUrl_WithMalformed_ReturnsFalse()`

**Acceptance**: Tests compile, all fail with "Not implemented"

---

### T008: [X] [P] Contract test for IDownloadQueue
**Type**: Contract Test  
**Description**: Write tests for IDownloadQueue interface per contracts/IDownloadQueue.md  
**Files to create**:
- `tests/TelegramYtDlpBot.Tests/Unit/Services/DownloadQueueTests.cs`

**Test Cases** (from contract):
- `EnqueueAsync_WithValidJob_InsertsToDatabase()`
- `DequeueAsync_WithEmptyQueue_ReturnsNull()`
- `DequeueAsync_WithQueuedJobs_ReturnsFIFO()`
- `MarkInProgressAsync_WithQueuedJob_UpdatesStatus()`
- `MarkCompletedAsync_WithInProgressJob_SetsOutputPath()`
- `MarkFailedAsync_WithInProgressJob_SetsErrorMessage()`
- `RetryJobAsync_WithFailedJob_RequeuesIfRetriesRemain()`
- `RetryJobAsync_WithMaxRetries_ReturnsFalse()`
- `GetStatsAsync_WithMixedJobs_ReturnsAccurateCounts()`

**Note**: Use in-memory SQLite (`:memory:`) for test isolation

**Acceptance**: Tests compile, all fail with "Not implemented"

---

### T009: [X] [P] Contract test for IYtDlpExecutor
**Type**: Contract Test  
**Description**: Write tests for IYtDlpExecutor interface per contracts/IYtDlpExecutor.md  
**Files to create**:
- `tests/TelegramYtDlpBot.Tests/Unit/Services/YtDlpExecutorTests.cs`

**Test Cases** (from contract - local mode):
- `DownloadAsync_WithValidUrl_ReturnsFilePath()`
- `DownloadAsync_WithInvalidUrl_ThrowsYtDlpException()`
- `DownloadAsync_WithTimeout_ThrowsYtDlpException()`
- `DownloadAsync_WithCancellation_ThrowsOperationCanceledException()`
- `HealthCheck_WithValidExecutable_ReturnsTrue()`
- `HealthCheck_WithMissingExecutable_ReturnsFalse()`

**Test Cases** (remote mode):
- `RemoteDownload_WithSuccessResponse_DownloadsFile()`
- `RemoteDownload_With404_ThrowsYtDlpException()`
- `RemoteDownload_With500_ThrowsYtDlpException()`

**Note**: Mock `HttpClient` for remote tests, mock `Process` or use integration tests for local

**Acceptance**: Tests compile, all fail with "Not implemented"

---

### T010: [X] [P] Integration test for message-to-queue workflow
**Type**: Integration Test  
**Description**: E2E test from quickstart.md scenario: message received → URL extracted → job queued → emoji applied  
**Files to create**:
- `tests/TelegramYtDlpBot.Tests/Integration/MessageProcessingWorkflowTests.cs`

**Test Scenario** (from quickstart.md):
1. Mock Telegram message with URL received
2. Verify UrlExtractor called
3. Verify DownloadQueue.EnqueueAsync called
4. Verify TelegramMonitor.SetReactionAsync called with "👀"
5. Assert job exists in queue with Status = Queued

**Acceptance**: Test compiles, fails with "Not implemented"

---

### T011: [X] [P] Integration test for download workflow
**Type**: Integration Test  
**Description**: E2E test from quickstart.md: job dequeued → yt-dlp executes → status updated → emoji progresses  
**Files to create**:
- `tests/TelegramYtDlpBot.Tests/Integration/DownloadWorkflowTests.cs`

**Test Scenario** (from quickstart.md):
1. Seed queue with test job
2. Worker dequeues job
3. Mock yt-dlp execution (success)
4. Verify status transitions: Queued → InProgress → Completed
5. Verify emoji reactions: 👀 → ⚙️ → ✅
6. Verify file path recorded

**Acceptance**: Test compiles, fails with "Not implemented"

---

### T012: [X] [P] Integration test for error handling and retry
**Type**: Integration Test  
**Description**: E2E test for failed download with retry logic  
**Files to create**:
- `tests/TelegramYtDlpBot.Tests/Integration/ErrorHandlingWorkflowTests.cs`

**Test Scenario**:
1. Seed queue with test job
2. Mock yt-dlp execution (failure)
3. Verify status transitions: Queued → InProgress → Failed
4. Verify RetryJobAsync called
5. Verify job re-queued (RetryCount incremented)
6. Verify max 3 retries, then permanent failure
7. Verify ❌ emoji applied after final failure

**Acceptance**: Test compiles, fails with "Not implemented"

---

## Phase 3.3: Core Implementation (ONLY after tests are failing)

**PREREQUISITE**: T006-T012 must be complete and failing before starting T013

---

### T013: [P] Create Message model
**Type**: Model  
**Description**: Implement Message in-memory model per data-model.md  
**Files to create**:
- `src/TelegramYtDlpBot/Models/Message.cs`
- `src/TelegramYtDlpBot/Models/ProcessingState.cs` (enum)

**Acceptance**: Model compiles with init-only properties, matches data-model.md spec

---

### T014: [P] Create DownloadJob entity model
**Type**: Model  
**Description**: Implement DownloadJob database entity per data-model.md  
**Files to create**:
- `src/TelegramYtDlpBot/Models/DownloadJob.cs`
- `src/TelegramYtDlpBot/Models/JobStatus.cs` (enum)

**Acceptance**: Model compiles, includes all fields from data-model.md

---

### T015: [P] Create Configuration models
**Type**: Model  
**Description**: Implement configuration POCOs per data-model.md Configuration section  
**Files to create**:
- `src/TelegramYtDlpBot/Configuration/BotConfiguration.cs`
- `src/TelegramYtDlpBot/Configuration/TelegramConfig.cs`
- `src/TelegramYtDlpBot/Configuration/YtDlpConfig.cs`
- `src/TelegramYtDlpBot/Configuration/StorageConfig.cs`
- `src/TelegramYtDlpBot/Configuration/EmojiConfig.cs`
- `src/TelegramYtDlpBot/Configuration/LoggingConfig.cs`
- `src/TelegramYtDlpBot/Configuration/YtDlpMode.cs` (enum)

**Include**: Data annotations for validation (`[Required]`, `[Range]`)

**Acceptance**: Models compile, include Validate() method on EmojiConfig

---

### T016: [P] Create StateManager for SQLite operations
**Type**: Service  
**Description**: Implement SQLite database access layer per data-model.md data access patterns  
**Files to create**:
- `src/TelegramYtDlpBot/Persistence/StateManager.cs`
- `src/TelegramYtDlpBot/Persistence/DatabaseInitializer.cs`

**Methods** (from data-model.md):
- `SaveJobAsync(DownloadJob job)`
- `GetNextQueuedJobAsync()`
- `UpdateJobStatusAsync(Guid jobId, JobStatus status, ...)`
- `IsMessageProcessedAsync(long messageId)`
- `SaveProcessedMessageAsync(long messageId, int urlCount)`
- `GetLastMessageIdAsync()`
- `UpdateLastMessageIdAsync(long messageId)`

**Acceptance**: T008 tests start passing (queue operations work)

---

### T017: Implement UrlExtractor service
**Type**: Service  
**Description**: Implement IUrlExtractor per contracts/IUrlExtractor.md  
**Files to create**:
- `src/TelegramYtDlpBot/Services/UrlExtractor.cs`

**Implementation**:
- Regex pattern: `https?://[^\s]+(?<![.,;:!?)])`
- `IsValidUrl` using `Uri.TryCreate`
- Deduplication logic
- URL length validation (≤2048 chars)

**Acceptance**: T007 tests pass (all 10 test cases green)

---

### T018: Implement DownloadQueue service
**Type**: Service  
**Description**: Implement IDownloadQueue per contracts/IDownloadQueue.md  
**Files to create**:
- `src/TelegramYtDlpBot/Services/DownloadQueue.cs`

**Implementation**:
- Use StateManager for persistence
- FIFO queue logic via CreatedAt ordering
- Status transition validation
- Retry count management

**Acceptance**: T008 tests pass (all 9 test cases green)

---

### T019: Implement LocalYtDlpExecutor
**Type**: Service  
**Description**: Implement local mode of IYtDlpExecutor per contracts/IYtDlpExecutor.md  
**Files to create**:
- `src/TelegramYtDlpBot/Services/LocalYtDlpExecutor.cs`
- `src/TelegramYtDlpBot/Services/YtDlpException.cs`

**Implementation**:
- `System.Diagnostics.Process` for yt-dlp CLI
- Stdout/stderr capture
- Timeout handling (configurable, default 60 min)
- Output file path parsing

**Acceptance**: T009 local mode tests pass

---

### T020: [P] Implement RemoteYtDlpExecutor
**Type**: Service  
**Description**: Implement remote mode of IYtDlpExecutor per contracts/IYtDlpExecutor.md  
**Files to create**:
- `src/TelegramYtDlpBot/Services/RemoteYtDlpExecutor.cs`

**Implementation**:
- `HttpClient` for REST API calls
- Authorization header (Bearer token if configured)
- File download from response URL
- Error response parsing

**Acceptance**: T009 remote mode tests pass

---

### T021: Implement TelegramMonitor service
**Type**: Service  
**Description**: Implement ITelegramMonitor per contracts/ITelegramMonitor.md  
**Files to create**:
- `src/TelegramYtDlpBot/Services/TelegramMonitor.cs`
- `src/TelegramYtDlpBot/Services/MessageReceivedEventArgs.cs`

**Implementation**:
- `ITelegramBotClient` from Telegram.Bot
- Long polling via `StartReceiving`
- MessageReceived event pattern
- Emoji reaction via `SetMessageReactionAsync`
- Exponential backoff on network errors

**Acceptance**: T006 tests pass (all 6 test cases green)

---

### T022: Implement background worker for download processing
**Type**: Service  
**Description**: Create worker that processes queue sequentially  
**Files to create**:
- `src/TelegramYtDlpBot/Services/DownloadWorker.cs`

**Implementation**:
- Implement `BackgroundService` from Microsoft.Extensions.Hosting
- Poll queue every 5 seconds
- Sequential processing (one download at a time)
- Update job status and apply emoji reactions
- Retry logic with exponential backoff (Polly)

**Acceptance**: T011 integration test passes (download workflow works)

---

### T023: Implement application orchestration
**Type**: Integration  
**Description**: Wire up all services in Program.cs with dependency injection  
**Files to modify**:
- `src/TelegramYtDlpBot/Program.cs`

**Implementation**:
- Configure Serilog (structured JSON logging to stdout)
- Load configuration (appsettings.json + env overrides)
- Register services in DI container:
  - `ITelegramMonitor` → `TelegramMonitor`
  - `IUrlExtractor` → `UrlExtractor`
  - `IDownloadQueue` → `DownloadQueue`
  - `IYtDlpExecutor` → `LocalYtDlpExecutor` or `RemoteYtDlpExecutor` (strategy)
  - `StateManager`
  - `DownloadWorker` (hosted service)
- Initialize SQLite database on startup
- Subscribe to MessageReceived event
- Start monitoring and worker

**Acceptance**: Application starts, logs "Monitoring started", T010 integration test passes

---

### T024: Implement health check endpoint
**Type**: Integration  
**Description**: Add /health endpoint per research.md health check design  
**Files to create**:
- `src/TelegramYtDlpBot/Health/TelegramHealthCheck.cs`
- `src/TelegramYtDlpBot/Health/DatabaseHealthCheck.cs`
- `src/TelegramYtDlpBot/Health/DiskSpaceHealthCheck.cs`
- `src/TelegramYtDlpBot/Health/QueueHealthCheck.cs`

**Files to modify**:
- `src/TelegramYtDlpBot/Program.cs` (add health check middleware)

**Implementation**:
- ASP.NET Core minimal API on port 8080
- Health checks:
  - Telegram: Call `GetMeAsync()` with timeout
  - Database: Open connection, execute test query
  - Disk Space: Check DownloadPath has >1GB free
  - Queue: Check last job completion time <10 min
- Return HTTP 200 (healthy) or 503 (unhealthy)

**Acceptance**: `curl http://localhost:8080/health` returns 200 with JSON status

---

## Phase 3.4: Error Handling & Resilience

### T025: Add Polly retry policies
**Type**: Polish  
**Description**: Configure retry policies per research.md (exponential backoff, max 3 attempts)  
**Files to modify**:
- `src/TelegramYtDlpBot/Services/DownloadWorker.cs`
- `src/TelegramYtDlpBot/Services/TelegramMonitor.cs`

**Implementation**:
- Polly retry policy for transient errors (HTTP 5xx, network timeouts)
- Retry schedule: 30s, 60s, 120s (exponential base 30s)
- Do NOT retry: HTTP 4xx, format errors, unsupported URLs
- Circuit breaker for Telegram API (open after 5 consecutive failures)

**Acceptance**: T012 integration test passes (error handling and retry works)

---

### T026: Add comprehensive logging
**Type**: Polish  
**Description**: Add structured logging at all key points per research.md logging strategy  
**Files to modify**: All service files

**Log Points**:
- Application startup (config values, sanitized)
- Message received (messageId, channelId, timestamp)
- URL extracted (count, URLs)
- Job queued (jobId, url, messageId)
- Download started (jobId, url)
- Download completed (jobId, duration, outputPath)
- Download failed (jobId, error, retryCount)
- Emoji reaction applied (messageId, emoji, success)
- Health check results (all checks)

**Log Levels**:
- ERROR: Download failures, API errors, SQLite exceptions
- WARN: Retry attempts, unsupported URLs, rate limits
- INFO: Message processing, lifecycle events
- DEBUG: Config values, queue state

**Acceptance**: Logs are structured JSON, all key events logged

---

## Phase 3.5: DevContainer & Docker

### T027: [P] Create VS Code devcontainer configuration
**Type**: Infrastructure  
**Description**: Set up devcontainer per constitution requirement  
**Files to create**:
- `.devcontainer/devcontainer.json`
- `.devcontainer/Dockerfile`

**Configuration**:
- Base image: `mcr.microsoft.com/dotnet/sdk:8.0`
- Install: git, yt-dlp, sqlite3
- Extensions: C# Dev Kit, PowerShell, Docker
- Port forwarding: 8080 (health check)

**Acceptance**: `Reopen in Container` works, `dotnet --version` returns 8.x

---

### T028: Create production Dockerfile
**Type**: Infrastructure  
**Description**: Multi-stage Docker build per research.md and constitution  
**Files to create**:
- `docker/Dockerfile`

**Stages**:
1. **Build**: SDK image, restore + build + test
2. **Runtime**: Minimal runtime image (Alpine or Debian slim)
3. Install yt-dlp in runtime
4. Copy published app
5. Non-root user (`appuser`)
6. Health check: `curl http://localhost:8080/health`
7. Entrypoint: `/app/TelegramYtDlpBot`

**Acceptance**: `docker build` succeeds, image size <150MB

---

### T029: [P] Create Unraid template
**Type**: Infrastructure  
**Description**: Community Applications XML template per research.md  
**Files to create**:
- `docker/unraid-template.xml`

**Template Fields**:
- Container name, icon, description
- Environment variables: `Telegram__BotToken`, `Telegram__ChannelId`, `YtDlp__Mode`
- Volume mappings: `/data/downloads`, `/data` (for SQLite)
- Port: 8080 (health check)
- Network: bridge mode
- Repository: `ghcr.io/taldelarosa/telegram-ytdlp-bot`

**Acceptance**: Template validates against Unraid schema

---

## Phase 3.6: Pre-PR Quality Gates (REQUIRED before PR creation)

**CRITICAL: ALL tasks in this phase MUST pass before creating pull request**

---

### T030: Run dotnet format linter
**Type**: Quality Gate  
**Description**: Execute dotnet format to check code formatting compliance  
**Command**: `dotnet format --verify-no-changes`

**Acceptance**: No formatting violations found

---

### T031: Auto-fix linting issues
**Type**: Quality Gate  
**Description**: Apply automatic fixes for all fixable linting violations  
**Command**: `dotnet format`

**Acceptance**: `dotnet format --verify-no-changes` passes after fixes

---

### T032: Run PSScriptAnalyzer on PowerShell scripts
**Type**: Quality Gate  
**Description**: Lint PowerShell scripts per constitution  
**Command**: `Invoke-ScriptAnalyzer -Path scripts/ -Recurse -Fix`

**Acceptance**: No PSScriptAnalyzer warnings or errors

---

### T033: Run complete test suite
**Type**: Quality Gate  
**Description**: Execute all unit, integration, and E2E tests  
**Command**: `dotnet test --verbosity normal`

**Acceptance**: All tests pass (0 failed), output shows test count

---

### T034: Verify test coverage threshold
**Type**: Quality Gate  
**Description**: Ensure code coverage meets ≥80% requirement per constitution  
**Command**: `dotnet test --collect:"XPlat Code Coverage"`

**Analysis**: Use `coverlet` report or ReportGenerator

**Acceptance**: Total coverage ≥80% for core logic (Models, Services)

---

### T035: Build Docker image (smoke test)
**Type**: Quality Gate  
**Description**: Verify Dockerfile builds successfully  
**Command**: `docker build -t telegram-ytdlp-bot:test -f docker/Dockerfile .`

**Acceptance**: Build completes without errors, image created

---

### T036: Run Docker health check
**Type**: Quality Gate  
**Description**: Start container and verify health endpoint responds  
**Commands**:
```powershell
docker run -d --name bot-test telegram-ytdlp-bot:test
Start-Sleep 5
docker exec bot-test curl http://localhost:8080/health
docker stop bot-test
docker rm bot-test
```

**Acceptance**: Health check returns HTTP 200 with JSON

---

### T037: Review and commit linting fixes
**Type**: Quality Gate  
**Description**: Stage and commit all auto-fixed files  
**Command**: `git add . && git commit -m "Apply linting fixes"`

**Acceptance**: Git status clean (no uncommitted linting changes)

---

### T038: Final verification before PR
**Type**: Quality Gate  
**Description**: Final check that all quality gates passed  

**Checklist**:
- [ ] T030-T034: All linting and tests pass
- [ ] T035-T036: Docker build and health check pass
- [ ] T037: All fixes committed
- [ ] No uncommitted changes in git
- [ ] Branch up to date with develop

**Acceptance**: Ready to create PR to `develop` branch

---

## Dependencies Graph

```
Setup Phase (T001-T005)
    ↓
Tests Phase (T006-T012) - All [P] parallelizable
    ↓
Models Phase (T013-T015) - All [P] parallelizable
    ↓
T016 (StateManager) ← required by T018
    ↓
Services Phase:
    T017 (UrlExtractor) [P]
    T018 (DownloadQueue) ← depends on T016
    T019-T020 (YtDlp executors) [P]
    T021 (TelegramMonitor) [P]
    ↓
T022 (DownloadWorker) ← depends on T018, T019/T020, T021
    ↓
T023 (Program.cs integration) ← depends on all services
    ↓
T024 (Health checks) [P with T025-T026]
T025 (Polly policies)
T026 (Logging)
    ↓
Infrastructure Phase (T027-T029) - All [P]
    ↓
Pre-PR Quality Gates (T030-T038) - Sequential
```

---

## Parallel Execution Examples

### Example 1: Contract Tests (Phase 3.2)
```powershell
# All contract tests can run simultaneously (different files)
# Launch in separate terminals or use task runner

# Terminal 1
Task: "Write ITelegramMonitor contract tests in tests/.../TelegramMonitorTests.cs"

# Terminal 2
Task: "Write IUrlExtractor contract tests in tests/.../UrlExtractorTests.cs"

# Terminal 3
Task: "Write IDownloadQueue contract tests in tests/.../DownloadQueueTests.cs"

# Terminal 4
Task: "Write IYtDlpExecutor contract tests in tests/.../YtDlpExecutorTests.cs"
```

### Example 2: Model Creation (Phase 3.3)
```powershell
# Models are independent, create in parallel

# Terminal 1
Task: "Create Message model in src/.../Models/Message.cs"

# Terminal 2
Task: "Create DownloadJob model in src/.../Models/DownloadJob.cs"

# Terminal 3
Task: "Create Configuration models in src/.../Configuration/"
```

### Example 3: Infrastructure (Phase 3.5)
```powershell
# Infrastructure files are independent

# Terminal 1
Task: "Create devcontainer configuration in .devcontainer/"

# Terminal 2
Task: "Create production Dockerfile in docker/Dockerfile"

# Terminal 3
Task: "Create Unraid template in docker/unraid-template.xml"
```

---

## Validation Checklist

**Pre-Implementation**:
- [x] All contracts have corresponding test tasks (T006-T009)
- [x] All entities have model creation tasks (T013-T015)
- [x] All tests come before implementation (Phase 3.2 before 3.3)
- [x] Parallel tasks marked [P] are truly independent
- [x] Each task specifies exact file paths
- [x] Dependencies documented in graph
- [x] Pre-PR quality gates included (T030-T038)

**Post-Implementation** (checklist for completion):
- [ ] All 38 tasks completed
- [ ] All tests passing (T033)
- [ ] Code coverage ≥80% (T034)
- [ ] Docker image builds (T035)
- [ ] Health check works (T036)
- [ ] All linting passes (T030-T032)
- [ ] Ready for PR to `develop` branch

---

## Notes

- **TDD Enforcement**: Phase 3.2 (tests) MUST complete before Phase 3.3 (implementation)
- **Parallel Safety**: Tasks marked [P] modify different files, safe to run concurrently
- **Commit Frequency**: Commit after each task for atomic changes
- **Test-First**: Verify tests fail before implementing, then verify they pass after
- **Constitution Compliance**: All tasks align with v1.2.0 requirements

---

**Total Estimated Time**: 15-20 hours (sequential), 8-12 hours (with parallelization)

**Next Step**: Begin with T001 (project structure creation)

---

*Generated from plan.md, data-model.md, research.md, contracts/, and quickstart.md*  
*Constitution: v1.2.0 compliant*
