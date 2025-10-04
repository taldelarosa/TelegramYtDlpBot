# Feature Specification: Telegram Channel URL Monitor with yt-dlp Integration

**Feature Branch**: `feature/001-build-an-app`  
**Created**: 2025-10-04  
**Status**: Draft  

---

## 1. Problem Statement

Users manually monitor private Telegram channels for shared URLs (videos, audio, media), then manually copy those URLs to yt-dlp for downloading. This process is tedious, error-prone, and provides no feedback mechanism to indicate processing status to the channel participants.

**Target Users**: 
- Telegram channel administrators who manage media archival workflows
- Content curators who aggregate media from specific channels
- Users with self-hosted yt-dlp instances who want automated intake

**Success Criteria**:
- Zero manual intervention required once bot is configured and monitoring
- Real-time emoji feedback visible to all channel members (Seen → Processing → Complete)
- 100% of posted URLs are captured and queued for download within 5 seconds
- Graceful handling of unsupported URLs (emoji indicator without crash)

---

## 2. Functional Requirements

### Core Functionality

**FR-001: Channel Monitoring**
- The app MUST continuously monitor a configured private Telegram channel for new messages
- Message polling or webhook updates MUST occur with ≤5 second latency from post time
- Authentication MUST use Telegram Bot API Token (created via BotFather)
- Bot account MUST be added to target private channel by an administrator before monitoring can begin

**FR-002: URL Extraction**
- The app MUST parse message text and extract all valid URLs (http/https schemes)
- Multiple URLs in a single message MUST be treated as separate download jobs
- URL validation MUST occur before queuing (basic scheme/format check, not content verification)

**FR-003: yt-dlp Integration**
- The app MUST support both local yt-dlp execution and remote self-hosted yt-dlp API calls
- Configuration MUST support both config file and environment variables, with environment variables taking precedence (12-factor app pattern)
- Download requests MUST include configurable quality settings (e.g., best video+audio)
- Downloads MUST be processed sequentially (one at a time) from the queue

**FR-004: Emoji Reaction Feedback**
- **Seen Reaction**: MUST be applied immediately when URL is detected in message (within 1 second)
- **Processing Reaction**: MUST replace "Seen" when download begins (atomic update)
- **Complete Reaction**: MUST replace "Processing" when download finishes successfully (atomic update)
- NEEDS CLARIFICATION: Emoji choices (user-configurable vs hardcoded defaults)
**FR-005: Error Handling**
- Failed downloads (unsupported URL, network error, yt-dlp error) MUST receive distinct error emoji reaction
- NEEDS CLARIFICATION: Retry mechanism (automatic retry with backoff vs manual intervention)
- Errors MUST be logged with full context (URL, error message, timestamp, message ID)

**FR-006: Download Management**
- Download destination MUST be configurable via path setting (local filesystem or Docker volume mount point)
- App treats all destinations as local filesystem paths (user responsible for mounting network shares/storage as Docker volumes)
- Downloaded files MUST be organized by configurable scheme (e.g., `{date}/{channel}/{filename}` pattern)
- NEEDS CLARIFICATION: Post-download actions (notification, file move, metadata tagging)

### Configuration & Control

**FR-007: Channel Configuration**
- User MUST be able to configure target channel ID/username via configuration file or environment variable
- Multi-channel monitoring is OUT OF SCOPE for initial release
- Configuration changes MUST be applied at app restart (hot-reload is OUT OF SCOPE)

**FR-008: yt-dlp Configuration**
- User MUST configure yt-dlp executable path (local mode) or API endpoint URL (remote mode)
- Download quality/format preferences MUST be configurable via yt-dlp arguments
- NEEDS CLARIFICATION: Support for yt-dlp config file pass-through vs explicit arguments

**FR-009: Credential Management**
- Telegram Bot API Token MUST be stored securely (environment variables or Docker secrets)
- NEEDS CLARIFICATION: Secrets management strategy for yt-dlp remote API credentials (if required)

### Operational Requirements

**FR-010: Logging**
- All message processing events MUST be logged (timestamp, message ID, URLs extracted, reactions applied)
- yt-dlp output MUST be captured and logged (stdout/stderr)
- Log verbosity MUST be configurable (ERROR, WARN, INFO, DEBUG)
- NEEDS CLARIFICATION: Log format (structured JSON vs plaintext) and destination (file vs stdout vs both)

**FR-011: State Persistence**
- Download queue MUST be persisted to SQLite database to survive restarts without data loss
- Processed message IDs MUST be tracked in SQLite to prevent duplicate downloads after restart
- Last processed message offset MUST be persisted to resume monitoring from correct position
- Database file MUST be stored in a Docker volume for persistence across container recreations

**FR-012: Health Monitoring**
- App MUST expose health check endpoint for container orchestration (HTTP /health or similar)
- NEEDS CLARIFICATION: Metrics exposure (Prometheus format, custom JSON endpoint, or none)

---

## 3. Non-Functional Requirements

### Performance
**NFR-001**: Message detection latency MUST NOT exceed 5 seconds under normal network conditions  
**NFR-002**: Emoji reactions MUST be applied within 1 second of state transition  
**NFR-003**: The app MUST handle at least 100 queued downloads without performance degradation  

### Security
**NFR-004**: All Telegram API credentials MUST be stored outside source code (environment variables minimum)  
**NFR-005**: Downloaded files MUST have restricted permissions (non-world-readable in Unix environments)  
**NFR-006**: Network requests to yt-dlp API MUST support HTTPS with certificate validation  

### Reliability
**NFR-007**: The app MUST recover gracefully from network disconnections (Telegram API or yt-dlp)  
**NFR-008**: Transient errors MUST NOT crash the application (retry with exponential backoff)  
**NFR-009**: State persistence MUST ensure zero data loss on clean shutdown (SIGTERM handling)  

### Usability
**NFR-010**: Configuration MUST be documentable in ≤10 steps for non-technical users  
**NFR-011**: Error messages in logs MUST include actionable troubleshooting guidance  
**NFR-012**: The app MUST provide clear startup logs indicating configuration validation results  
---

## 4. User Stories

**US-001**: As a channel admin, I want the bot to automatically detect URLs so I don't have to manually copy them  
**US-002**: As a channel member, I want to see emoji reactions so I know when my shared URL is being processed  
**US-003**: As a media curator, I want downloads to happen in the background so I can continue other work  
**US-004**: As a self-hoster, I want to use my existing yt-dlp server so I don't duplicate infrastructure  
**US-005**: As a user, I want the app to survive restarts without losing queued downloads  

---

## 5. Out of Scope

- **Multi-channel monitoring**: Only one channel per bot instance (run multiple containers if needed)
- **User command interface**: No bot commands like `/start`, `/help`, `/status` (pure automation)
- **File serving**: Bot does not re-upload files to Telegram or expose HTTP download links
- **Advanced scheduling**: No cron-like delayed processing or rate limiting beyond inherent yt-dlp throttling
- **External database servers**: SQLite used for simplicity (Postgres/MySQL deferred to future if needed)
- **Web UI**: Configuration is file/environment-based only
- **Telegram message editing**: Bot does not modify or delete channel messages
- **Multi-format outputs**: yt-dlp handles format conversion; bot does no transcoding

---

## 6. Assumptions

- **Telegram Access**: User has created Telegram Bot via BotFather and obtained Bot API Token
- **Channel Permissions**: Bot account has been added to target channel with message read + reaction permissions
- **yt-dlp Availability**: yt-dlp executable is installed (local mode) or remote API is accessible and compatible
- **Storage Space**: Sufficient disk space exists for downloads (no quota enforcement in bot logic)
- **Network Stability**: Host environment has stable internet connectivity (retries handle transient issues only)
- **Docker Environment**: App will run in Docker with VS Code devcontainer for development
- **Unraid Deployment**: Production deployment target is Unraid Docker environment

---

## 7. Dependencies

### External Services
- **Telegram Bot API**: For channel monitoring and emoji reactions
- **yt-dlp**: CLI tool (local) or API-compatible remote service (self-hosted instance)

### Infrastructure
- **Docker**: Container runtime for packaging
- **Unraid**: Target deployment platform
- **.NET 8 SDK**: For C# development (per constitution)
- **PowerShell 7+**: For scripting components (per constitution)

### Libraries (Tentative - finalized in plan phase)
- **Telegram.Bot (C#)**: Official Telegram Bot API client library
- **Microsoft.Data.Sqlite**: SQLite database provider for .NET
- **System.Text.Json**: Configuration and logging
- **Serilog**: Structured logging framework

---

## 8. Success Metrics

- **Automation Rate**: 100% of URLs in monitored channel are processed without manual intervention
- **Feedback Latency**: Emoji reactions applied within 1 second of state change (seen/processing/complete)
- **Reliability**: 99%+ uptime over 30-day period (excluding planned restarts)
- **Error Recovery**: Zero crashes from transient network errors or malformed URLs
- **User Satisfaction**: Channel admins report reduced manual workload (qualitative feedback post-deployment)

---

## 9. Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Telegram API rate limits hit during high message volume | High | Medium | Implement request queuing with rate limit backoff; monitor API quotas |
| yt-dlp format support changes break downloads | Medium | Low | Pin yt-dlp version in Dockerfile; test against sample URLs in CI |
| Private channel access revoked mid-operation | High | Low | Implement graceful auth failure detection; alert via logs + health check failure |
| Large download queue exhausts disk space | High | Medium | Add disk space monitoring; pause queue if free space < threshold |
| Emoji reactions fail silently (API error) | Low | Medium | Log all reaction API calls; periodic validation that reactions were applied |
---

## 10. Validation & Testing Strategy

### Test Coverage Requirements (per constitution)
- **Unit Tests**: All URL extraction, queue management, state persistence logic (≥80% coverage)
- **Integration Tests**: Telegram API mocking (sent messages → reactions applied)
- **End-to-End Tests**: Full workflow with test Telegram channel + mock yt-dlp server
- **Security Tests**: Credential handling, file permissions validation
- **Performance Tests**: Queue processing under load (100+ URLs)

### Manual Validation Scenarios
1. Post single URL to test channel → verify Seen → Processing → Complete emoji progression
2. Post multiple URLs in one message → verify all are queued and processed independently
3. Post unsupported URL (e.g., `http://invalid`) → verify error emoji without crash
4. Restart app mid-download → verify queue resumes (if persistence implemented)
5. Disconnect network during download → verify retry logic engages

---

## 11. Entity Definitions

### Message
- **MessageId** (int64): Telegram message unique identifier
- **ChannelId** (int64): Source channel identifier
- **Text** (string): Raw message content
- **Timestamp** (DateTime): When message was posted
- **ExtractedUrls** (List<string>): URLs found in message text
- **ProcessingState** (enum): Seen | Processing | Complete | Error

### Download Job
- **JobId** (Guid): Unique job identifier (internal)
- **SourceMessageId** (int64): Reference to originating Telegram message
- **Url** (string): Target URL for yt-dlp
- **Status** (enum): Queued | InProgress | Completed | Failed
- **CreatedAt** (DateTime): Job creation timestamp
- **CompletedAt** (DateTime?): Job completion timestamp (null if not finished)
- **ErrorMessage** (string?): Error details if Status = Failed
- **OutputPath** (string?): Filesystem path to downloaded file

### Configuration
- **TelegramBotToken** (string): Bot API authentication token
- **ChannelId** (int64): Target channel to monitor
- **YtDlpMode** (enum): Local | Remote
- **YtDlpPath** (string): Local executable path or remote API URL
- **DownloadPath** (string): Base directory for saved files
- **EmojiSeen** (string): Emoji for "URL detected" reaction
- **EmojiProcessing** (string): Emoji for "download in progress" reaction
- **EmojiComplete** (string): Emoji for "download finished" reaction
- **EmojiError** (string): Emoji for "download failed" reaction

---

## Clarifications

### Session 2025-10-04

**Q1: Telegram Authentication Mechanism**
- **Question**: The bot needs to connect to Telegram. Should it use a Bot API Token (created via BotFather), authenticate as a User Session (using phone number + 2FA), or support both modes?
- **Answer**: Bot API Token
- **Impact**: Simplifies authentication flow (single token in environment variable). Bot must be explicitly added to private channel by admin. Bot can react to messages using Telegram Bot API reaction endpoints. No need for user session persistence or 2FA handling. Requirements updated: FR-001 (authentication method specified), FR-009 (credentials scope clarified), Assumptions section (user session reference removed).

**Q2: Download Strategy**
- **Question**: When multiple URLs are posted (either in one message or across multiple messages), how should yt-dlp downloads be processed?
- **Answer**: Sequential (process one download at a time)
- **Impact**: Simplifies implementation and avoids resource contention. Queue is straightforward FIFO. No concurrency management needed. Throughput is limited to one download at a time, but suitable for typical channel usage patterns. Requirements updated: FR-003 (download strategy specified as sequential processing).

**Q3: yt-dlp Mode Configuration**
- **Question**: The app needs to know whether to use local yt-dlp execution or call a remote self-hosted API. How should this mode be configured?
- **Answer**: Both (environment variable overrides config file - 12-factor app pattern)
- **Impact**: Maximum flexibility for different deployment scenarios. Config file provides defaults and documentation. Environment variables allow container-specific overrides without rebuilding images. Follows cloud-native best practices. Requires configuration loading logic that merges both sources with proper precedence. Requirements updated: FR-003 (configuration approach specified), FR-007 and FR-008 (config mechanism clarified).

**Q4: State Persistence Mechanism**
- **Question**: The app needs to handle restarts gracefully. What should be persisted and how?
- **Answer**: Filesystem with SQLite database
- **Impact**: Reliable queue recovery after restarts. No duplicate downloads on restart (processed message IDs tracked). Simple embedded database with zero external dependencies. Database file must be in Docker volume for persistence across container recreations. Adds SQLite library dependency and schema migration considerations. Requirements updated: FR-011 (persistence mechanism specified), Out of Scope (clarified SQLite vs external DB), Dependencies (added Microsoft.Data.Sqlite library).

**Q5: Download Destination**
- **Question**: Where should downloaded files be saved?
- **Answer**: Configurable path (user handles mounting network shares as Docker volumes)
- **Impact**: Clean separation of concerns - app writes to configured local path, infrastructure layer (Docker/Unraid) handles mounting network/cloud storage if needed. Simplifies implementation (no network protocol handling). Maximum flexibility for users (local, NFS, SMB, cloud mounts all work). Requires clear documentation on Docker volume configuration. Requirements updated: FR-006 (download destination approach specified), Assumptions (added storage mounting responsibility).

### Clarification Coverage Summary

| Category | Status | Count | Notes |
|----------|--------|-------|-------|
| **Resolved** | ✅ | 5 | Authentication, download strategy, config approach, persistence, storage destination |
| **Deferred to Plan** | 🔄 | 7 | Emoji config, retry logic, post-download actions, yt-dlp config file, secrets mgmt, log format, metrics |
| **Total Original** | | 12 | Initial ambiguities identified |

**Deferred Items** (to be addressed in plan phase based on technical constraints):
- Emoji configuration approach (hardcoded defaults vs user-configurable)
- Retry mechanism for failed downloads
- Post-download actions/notifications
- yt-dlp config file pass-through support
- Remote yt-dlp API credential management
- Log format and destination
- Metrics/monitoring endpoint format

**Recommendation**: Proceed to `/plan` phase - critical architectural decisions resolved. Remaining items are implementation details that can be determined during technical design.

---

**Constitution Reference**: Version 1.2.0