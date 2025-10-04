# Quickstart Guide: Telegram Channel URL Monitor

**Feature**: 001-build-an-app  
**Date**: 2025-10-04  
**Audience**: Developers implementing this feature

---

## Prerequisites

1. **.NET 8 SDK** installed
2. **Docker** installed (for devcontainer and deployment)
3. **VS Code** with Remote-Containers extension
4. **Telegram Bot Token** from BotFather
5. **Private Telegram Channel** with bot added as admin

---

## Setup Steps

### 1. Create Telegram Bot

```bash
# In Telegram, start chat with @BotFather
/newbot
# Follow prompts:
# - Bot name: MyDownloadBot
# - Username: my_download_bot

# Save the bot token: 123456789:ABCdefGHIjklMNOpqrsTUVwxyz

# Add bot to your private channel:
# 1. Open channel settings
# 2. Administrators > Add Administrator
# 3. Search for @my_download_bot
# 4. Grant "Post Messages" permission (for reactions)
```

### 2. Get Channel ID

```bash
# Post a message in your channel mentioning the bot
# Then visit: https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates
# Find "chat":{"id":-1001234567890,...} in JSON response
# Save the channel ID: -1001234567890
```

### 3. Clone Repository and Open in Devcontainer

```powershell
git clone https://github.com/taldelarosa/TelegramYtDlpBot.git
cd TelegramYtDlpBot
git checkout feature/001-build-an-app

# Open in VS Code
code .

# When prompted: "Reopen in Container" → Click
# Wait for devcontainer to build (first time: ~5 minutes)
```

### 4. Configure Application

Create `src/TelegramYtDlpBot/appsettings.Development.json`:

```json
{
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "ChannelId": -1001234567890,
    "PollingIntervalSeconds": 2
  },
  "YtDlp": {
    "Mode": "Local",
    "ExecutablePath": "/usr/local/bin/yt-dlp",
    "Quality": "bestvideo+bestaudio/best",
    "OutputTemplate": "%(uploader)s/%(upload_date)s/%(title)s.%(ext)s",
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
    "MinimumLevel": "Debug"
  }
}
```

**Security**: Never commit `appsettings.Development.json` to git (already in `.gitignore`)

### 5. Run Tests (TDD Workflow)

```powershell
# Inside devcontainer terminal
cd /workspace

# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Expected output (after implementation):
# Tests: Passed: 45, Failed: 0, Skipped: 0
# Coverage: 82% (target: ≥80%)
```

### 6. Run Application Locally

```powershell
# Inside devcontainer
cd /workspace/src/TelegramYtDlpBot

# Run with Development settings
dotnet run --environment Development

# Expected console output:
# [12:34:56 INF] Starting Telegram Channel URL Monitor
# [12:34:56 INF] Bot: @my_download_bot
# [12:34:56 INF] Channel ID: -1001234567890
# [12:34:56 INF] YtDlp Mode: Local
# [12:34:56 INF] Health check: Telegram ✓, YtDlp ✓, Database ✓
# [12:34:56 INF] Monitoring started
```

### 7. Test End-to-End

1. **Post a test URL** in your Telegram channel:
   ```
   Check this out: https://www.youtube.com/watch?v=dQw4w9WgXcQ
   ```

2. **Observe emoji reactions**:
   - Immediately: 👀 (Seen)
   - After ~2 seconds: ⚙️ (Processing)
   - After download completes: ✅ (Complete)

3. **Verify downloaded file**:
   ```powershell
   ls /data/downloads
   # Should see: RickAstley/20091025/Never_Gonna_Give_You_Up.mp4
   ```

4. **Check logs**:
   ```powershell
   # Look for structured JSON logs in console
   # Example:
   # {"@t":"2025-10-04T12:35:10Z","@l":"INF","message":"URL extracted","url":"https://youtube...","messageId":123}
   # {"@t":"2025-10-04T12:35:15Z","@l":"INF","message":"Download completed","jobId":"abc-123","outputPath":"/data/downloads/..."}
   ```

---

## Common Issues

### Issue: Bot not receiving messages

**Symptoms**: No `MessageReceived` events in logs

**Diagnosis**:
```powershell
# Check bot has channel access
curl "https://api.telegram.org/bot<YOUR_TOKEN>/getUpdates"

# Should see channel in "chat" field
# If empty: Bot not added to channel or not admin
```

**Solution**:
1. Verify bot is added to channel as administrator
2. Grant "Post Messages" permission (required for reactions)
3. Send a test message after adding bot
4. Restart application

---

### Issue: yt-dlp command not found

**Symptoms**: `YtDlpException: yt-dlp executable not found`

**Diagnosis**:
```powershell
which yt-dlp
# Should output: /usr/local/bin/yt-dlp
```

**Solution**:
```powershell
# Install yt-dlp in devcontainer
pip install yt-dlp

# Or use APK (Alpine)
apk add yt-dlp

# Verify installation
yt-dlp --version
```

---

### Issue: SQLite database locked

**Symptoms**: `Microsoft.Data.Sqlite.SqliteException: database is locked`

**Diagnosis**: WAL mode not enabled

**Solution**:
```powershell
# Enable WAL mode (should be automatic, but verify)
sqlite3 /data/state.db "PRAGMA journal_mode=WAL;"
```

---

### Issue: Disk space error

**Symptoms**: `Health check failed: Insufficient disk space`

**Diagnosis**:
```powershell
df -h /data/downloads
# Check available space
```

**Solution**:
- Free up disk space
- Adjust `Storage.MinimumFreeSpaceGB` in config
- Mount larger volume

---

## Development Workflow

### TDD Cycle (Red-Green-Refactor)

1. **Red**: Write failing test
   ```csharp
   [Fact]
   public async Task ExtractUrls_WithMultipleUrls_ReturnsAllUrls()
   {
       var extractor = new UrlExtractor();
       var urls = extractor.ExtractUrls("Check https://youtube.com and https://example.com");
       Assert.Equal(2, urls.Count);
   }
   ```

2. **Green**: Implement minimum code to pass
   ```csharp
   public List<string> ExtractUrls(string text)
   {
       var regex = new Regex(@"https?://[^\s]+");
       return regex.Matches(text)
           .Select(m => m.Value)
           .Where(IsValidUrl)
           .Distinct()
           .ToList();
   }
   ```

3. **Refactor**: Improve code quality
   - Extract regex to const
   - Add XML documentation
   - Optimize performance

4. **Repeat** for next requirement

---

### Pre-PR Checklist

Before creating a Pull Request:

```powershell
# 1. Run linting and auto-fix
dotnet format

# 2. Run all tests
dotnet test

# 3. Check code coverage
dotnet test --collect:"XPlat Code Coverage"
# Verify coverage ≥ 80%

# 4. Run PowerShell linting (if scripts modified)
Invoke-ScriptAnalyzer -Path scripts/ -Recurse -Fix

# 5. Build Docker image (smoke test)
docker build -t telegram-ytdlp-bot:test -f docker/Dockerfile .

# 6. Run health check
docker run --rm telegram-ytdlp-bot:test /app/health-check.sh
```

All checks must pass before PR creation (per constitution v1.2.0).

---

## Docker Deployment

### Build Production Image

```powershell
docker build -t telegram-ytdlp-bot:latest -f docker/Dockerfile .
```

### Run Locally with Docker

```powershell
docker run -d \
  --name telegram-bot \
  -e Telegram__BotToken="YOUR_TOKEN" \
  -e Telegram__ChannelId="-1001234567890" \
  -e YtDlp__Mode="Local" \
  -v /mnt/user/downloads:/data/downloads \
  -v /mnt/user/appdata/telegram-bot:/data \
  --restart unless-stopped \
  telegram-ytdlp-bot:latest
```

### View Logs

```powershell
docker logs -f telegram-bot

# Expected output (JSON structured logs):
# {"@t":"...","@l":"INF","message":"Application started"}
# {"@t":"...","@l":"INF","message":"Monitoring channel","channelId":-1001234567890}
```

### Health Check

```powershell
curl http://localhost:8080/health

# Expected response:
# {"status":"Healthy","checks":{"telegram":"ok","database":"ok","disk":"ok","queue":"ok"}}
```

---

## Unraid Deployment

### Install from Community Applications

1. Open Unraid Web UI
2. Navigate to **Apps** tab
3. Search: "Telegram YtDlp Bot"
4. Click **Install**
5. Configure:
   - **Bot Token**: Your Telegram bot token
   - **Channel ID**: Your channel ID
   - **Download Path**: `/mnt/user/downloads` (or custom)
   - **App Data**: `/mnt/user/appdata/telegram-bot`
6. Click **Apply**

### Manual Template Installation

1. Copy `docker/unraid-template.xml` to `/boot/config/plugins/community.applications/templates/`
2. Refresh Apps page
3. Follow install wizard

### Monitor via Unraid UI

- **Container logs**: Docker > telegram-bot > Logs
- **Resource usage**: Docker > telegram-bot > Stats
- **Health status**: Green checkmark if `/health` returns 200

---

## Architecture Overview

```
┌─────────────────┐
│  TelegramMonitor│◄─── Telegram Bot API (long polling)
└────────┬────────┘
         │ MessageReceived event
         ▼
┌─────────────────┐
│   UrlExtractor  │──► Extract URLs from message text
└────────┬────────┘
         │ URLs
         ▼
┌─────────────────┐
│  DownloadQueue  │◄─── Persist to SQLite
└────────┬────────┘
         │ Dequeue next job
         ▼
┌─────────────────┐
│ YtDlpExecutor   │──► Execute yt-dlp (local or remote)
└────────┬────────┘
         │ Output file path
         ▼
┌─────────────────┐
│  StateManager   │──► Update job status, apply emoji
└─────────────────┘
```

**Key Flows**:
1. **Message → Queue**: TelegramMonitor receives message → UrlExtractor finds URLs → DownloadQueue enqueues jobs → Apply 👀 emoji
2. **Queue → Download**: Worker dequeues job → Apply ⚙️ emoji → YtDlpExecutor downloads → Apply ✅ emoji
3. **Error**: Any failure → Log error → Apply ❌ emoji → Retry logic (max 3 attempts)

---

## Next Steps

After completing quickstart:

1. **Implement Phase 2 Tasks**: Run `/tasks` command to generate detailed task list
2. **Follow TDD**: Write tests first, implement features incrementally
3. **Iterate**: Review constitution compliance after each task group
4. **Deploy**: Build Docker image and deploy to Unraid when MVP complete

---

**Quickstart Complete** ✅  
**Questions?** Check contracts documentation or run `/tasks` for detailed implementation steps.
