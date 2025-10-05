# 🎯 Quick Reference Card

## Essential Commands

### Docker Compose (Primary Method)

```bash
# Build
docker-compose build

# Start (detached)
docker-compose up -d

# View logs (follow)
docker-compose logs -f

# Stop
docker-compose down

# Restart
docker-compose restart

# Rebuild and restart
docker-compose up -d --build

# View configuration
docker-compose config
```

### Docker CLI (Alternative)

```bash
# List containers
docker ps -a | grep telegram

# View logs
docker logs -f telegram-ytdlp-bot

# Restart
docker restart telegram-ytdlp-bot

# Stop
docker stop telegram-ytdlp-bot

# Remove
docker rm telegram-ytdlp-bot

# Execute command inside container
docker exec -it telegram-ytdlp-bot bash

# Check yt-dlp version
docker exec telegram-ytdlp-bot /usr/local/bin/yt-dlp --version

# View resource usage
docker stats telegram-ytdlp-bot
```

## File Locations

### Local Development (Windows)
```
C:\Users\Ragma\TelegramYtDlpBot\
├── .env                    # Your credentials (gitignored)
├── data\state.db           # SQLite database
├── downloads\              # Downloaded videos
├── docker-compose.yml      # Compose config
└── Dockerfile             # Build instructions
```

### Unraid (Recommended Paths)
```
/mnt/user/appdata/TelegramYtDlpBot/
├── .env                    # Your credentials
├── data/state.db           # SQLite database
└── docker-compose.yml      # Compose config

/mnt/user/downloads/telegram-videos/
└── *.mp4                   # Downloaded videos
```

## Configuration

### Environment Variables (.env file)
```bash
BOT_TOKEN=your_bot_token_here
CHANNEL_ID=-1001234567890
```

### Advanced (docker-compose.yml)
```yaml
environment:
  - BotConfiguration__YtDlp__Quality=1080p
  - BotConfiguration__YtDlp__OutputTemplate=%(title)s.%(ext)s
  - BotConfiguration__Database__Path=/app/data/state.db
  - BotConfiguration__Downloads__OutputPath=/app/downloads
```

## Status Indicators

### Emoji Reactions
- 👀 **Seen** - Message received, URL(s) extracted
- 🔥 **Processing** - Download in progress
- 👍 **Success** - Download completed
- 👎 **Error** - Download failed (after retries)

### Log Messages
```bash
# Successful startup
info: Connected as bot: auralist_bot
info: Starting Telegram monitoring for channel -1001234567890

# Message received
info: Received message 24: https://example.com/video

# Download progress
info: Processing job <uuid> for URL: https://example.com/video

# Success
info: Job <uuid> completed successfully: downloads/video.mp4

# Duplicate detected
info: Duplicate video request detected for URL: <url> - File already exists
```

## Troubleshooting

### Quick Diagnostic
```bash
# Check if running
docker ps | grep telegram

# View recent logs
docker-compose logs --tail=50

# Check disk space
df -h

# Test yt-dlp
docker exec telegram-ytdlp-bot /usr/local/bin/yt-dlp --version
```

### Common Fixes

**Container won't start:**
```bash
docker-compose logs telegram-ytdlp-bot
# Check for: invalid token, wrong channel ID, permission errors
```

**Permission errors:**
```bash
chmod -R 777 data downloads
docker-compose restart
```

**Database locked:**
```bash
docker-compose down
rm data/state.db-wal data/state.db-shm
docker-compose up -d
```

**Update yt-dlp:**
```bash
docker-compose build --no-cache
docker-compose up -d
```

## Resource Usage

### Typical
- **RAM**: 100-200 MB idle, 200-500 MB active
- **CPU**: <1% idle, 20-50% downloading
- **Disk**: Varies (videos can be 10 MB - 1 GB each)

### Unraid Limits (docker-compose.yml)
```yaml
deploy:
  resources:
    limits:
      cpus: '2'
      memory: 2G
```

## URLs & Links

### Documentation
- Main README: `README.md`
- Docker Guide: `DOCKER.md`
- Unraid Guide: `UNRAID.md`
- Deployment: `DEPLOYMENT-CHECKLIST.md`
- Summary: `DOCKER-SUMMARY.md`

### Telegram
- Get bot token: https://t.me/botfather
- Get channel ID: https://t.me/userinfobot
- API Status: https://telegram.org/status

### Tools
- yt-dlp: https://github.com/yt-dlp/yt-dlp
- Docker: https://docs.docker.com/
- Unraid: https://unraid.net/

## Backup & Restore

### Backup
```bash
# Database (important!)
cp data/state.db data/state.db.backup

# Configuration
cp .env .env.backup
cp docker-compose.yml docker-compose.yml.backup

# Optional: Downloads
tar -czf downloads-backup-$(date +%Y%m%d).tar.gz downloads/
```

### Restore
```bash
docker-compose down
cp data/state.db.backup data/state.db
docker-compose up -d
```

## Update Procedure

```bash
# 1. Backup first
cp data/state.db data/state.db.backup

# 2. Get latest code
git pull

# 3. Rebuild and restart
docker-compose up -d --build

# 4. Verify
docker-compose logs -f
```

## Testing

### Test Download
```bash
# Post this to your channel (safe, short video):
https://www.youtube.com/watch?v=dQw4w9WgXcQ

# Watch logs:
docker-compose logs -f

# Check file appears:
ls -lh downloads/
```

### Test Duplicate Detection
```bash
# Post same URL twice
# Second time should log:
# "Duplicate video request detected"
docker-compose logs | grep "Duplicate"
```

## Port Mapping

**None required!** This bot doesn't expose any ports. It only makes outbound connections to Telegram API.

## Network

- **Type**: Bridge (default)
- **Outbound only**: HTTPS to Telegram API
- **No inbound ports needed**

## Security Notes

- ✅ Runs as non-root user (UID 1000)
- ✅ No exposed ports
- ✅ Secrets via environment variables
- ✅ .env file gitignored
- ✅ Minimal container surface

## Quick Start (Copy-Paste)

### First Time Setup
```bash
# Clone project
git clone <repo-url> TelegramYtDlpBot
cd TelegramYtDlpBot

# Configure
cp .env.example .env
nano .env  # Add your BOT_TOKEN and CHANNEL_ID

# Build and run
docker-compose up -d

# Watch logs
docker-compose logs -f
```

### Daily Operations
```bash
# Check status
docker ps | grep telegram

# View logs
docker-compose logs --tail=50

# Restart if needed
docker-compose restart
```

### Maintenance
```bash
# Weekly: Check logs
docker-compose logs --tail=100 | grep -i error

# Monthly: Update
git pull && docker-compose up -d --build

# As needed: Cleanup old videos
rm downloads/old-video.mp4
```

## Success Checklist

- [ ] Container running (`docker ps`)
- [ ] Logs show "Connected as bot"
- [ ] Test URL downloads successfully
- [ ] Emoji reactions appear
- [ ] File saved to downloads/
- [ ] Duplicate detection works
- [ ] Container auto-restarts on failure

## Emergency Stop

```bash
# Stop everything
docker-compose down

# Force stop if unresponsive
docker kill telegram-ytdlp-bot

# Remove everything (nuclear option)
docker-compose down -v  # Warning: Deletes volumes!
```

---

**Need more details?** Check the full documentation files:
- General: README.md
- Docker: DOCKER.md  
- Unraid: UNRAID.md
- Deploy: DEPLOYMENT-CHECKLIST.md
