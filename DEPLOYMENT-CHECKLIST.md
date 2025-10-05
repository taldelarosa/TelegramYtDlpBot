# 🚀 Deployment Checklist

## Pre-Deployment

- [ ] Bot token obtained from @BotFather
- [ ] Channel ID identified (use @userinfobot)
- [ ] Bot added to channel as admin
- [ ] Docker and Docker Compose installed
- [ ] Sufficient disk space (recommend 50+ GB for videos)

## Local Testing (Optional but Recommended)

- [ ] Clone/download repository
- [ ] Create `.env` file from `.env.example`
- [ ] Run `docker-compose build` successfully
- [ ] Run `docker-compose up -d`
- [ ] Check logs: `docker-compose logs -f`
- [ ] See "Connected as bot: <your_bot_name>"
- [ ] Post a test video URL
- [ ] Verify reactions appear (👀 → 🔥 → 👍)
- [ ] Verify video downloads to `./downloads/`
- [ ] Stop: `docker-compose down`

## Unraid Deployment

### Method 1: Docker Compose (Recommended)

#### Step 1: Transfer Files
- [ ] SSH into Unraid server
- [ ] Navigate to: `cd /mnt/user/appdata`
- [ ] Clone repo or copy files
- [ ] Verify files present: `ls -la TelegramYtDlpBot`

#### Step 2: Configuration
- [ ] Create `.env` file: `cp .env.example .env`
- [ ] Edit `.env`: `nano .env`
- [ ] Set `BOT_TOKEN`
- [ ] Set `CHANNEL_ID`
- [ ] Save and exit (Ctrl+X, Y, Enter)

#### Step 3: Create Directories
- [ ] `mkdir -p data downloads`
- [ ] `chmod -R 777 data downloads`

#### Step 4: Build and Deploy
- [ ] `cd /mnt/user/appdata/TelegramYtDlpBot`
- [ ] `docker-compose build` (takes 2-3 minutes)
- [ ] `docker-compose up -d`

#### Step 5: Verify
- [ ] `docker-compose logs -f`
- [ ] See "Connected as bot" message
- [ ] See "Starting Telegram monitoring" message
- [ ] No error messages
- [ ] Press Ctrl+C to exit logs (container keeps running)

#### Step 6: Test
- [ ] Post video URL to Telegram channel
- [ ] Check logs: `docker-compose logs --tail=50`
- [ ] See "Received message" log
- [ ] See "Processing job" log
- [ ] See "completed successfully" log
- [ ] Verify file in `/mnt/user/appdata/TelegramYtDlpBot/downloads/`

### Method 2: Unraid Docker UI

#### Step 1: Build Image
- [ ] SSH into Unraid
- [ ] `cd /mnt/user/appdata/TelegramYtDlpBot`
- [ ] `docker build -t telegram-ytdlp-bot:latest .`
- [ ] Wait for build to complete

#### Step 2: Create Container
- [ ] Open Unraid web UI
- [ ] Go to **Docker** tab
- [ ] Click **Add Container**

#### Step 3: Basic Settings
- [ ] **Name**: `telegram-ytdlp-bot`
- [ ] **Repository**: `telegram-ytdlp-bot:latest`
- [ ] **Network Type**: `bridge`
- [ ] **Console shell command**: `bash`

#### Step 4: Volume Mappings
Add two paths:

- [ ] **Path 1**:
  - Container Path: `/app/data`
  - Host Path: `/mnt/user/appdata/telegram-ytdlp-bot/data`
  - Access Mode: Read/Write

- [ ] **Path 2**:
  - Container Path: `/app/downloads`
  - Host Path: `/mnt/user/downloads/telegram-videos`
  - Access Mode: Read/Write

#### Step 5: Environment Variables
Add these:

- [ ] `DOTNET_ENVIRONMENT` = `Production`
- [ ] `BotConfiguration__BotToken` = `<your_bot_token>`
- [ ] `BotConfiguration__ChannelId` = `<your_channel_id>`
- [ ] `BotConfiguration__YtDlp__ExecutablePath` = `/usr/local/bin/yt-dlp`

#### Step 6: Create and Verify
- [ ] Click **Apply**
- [ ] Wait for container to start (green icon)
- [ ] Click container icon → **Logs**
- [ ] Verify "Connected as bot" message

## Post-Deployment

### Verification

- [ ] Bot shows online in Telegram
- [ ] Container shows running: `docker ps`
- [ ] Logs show no errors
- [ ] Test download works
- [ ] Emoji reactions appear on test message
- [ ] File appears in downloads directory
- [ ] Database created: `ls -lh data/state.db`

### Monitoring Setup

- [ ] Bookmark logs command: `docker-compose logs -f`
- [ ] Note downloads location for media server
- [ ] Set up disk space monitoring (if needed)
- [ ] Consider backing up database weekly

### Integration (Optional)

- [ ] Add downloads folder to Plex/Jellyfin/Emby
- [ ] Set up SMB share for downloads
- [ ] Configure automated cleanup (if desired)
- [ ] Add to startup array (Unraid auto-starts containers)

## Maintenance Tasks

### Daily
- [ ] Check container is running: `docker ps | grep telegram`

### Weekly
- [ ] Review logs for errors: `docker-compose logs --tail=100`
- [ ] Check disk space: `df -h /mnt/user/downloads`
- [ ] Backup database: `cp data/state.db data/state.db.backup`

### Monthly
- [ ] Update bot: `git pull && docker-compose up -d --build`
- [ ] Update yt-dlp: `docker-compose build --no-cache && docker-compose up -d`
- [ ] Review downloaded files, cleanup old videos if needed

## Troubleshooting Checklist

### Container Won't Start

- [ ] Check logs: `docker logs telegram-ytdlp-bot`
- [ ] Verify `.env` file exists and has correct values
- [ ] Check BOT_TOKEN is valid (no spaces, correct format)
- [ ] Verify CHANNEL_ID is negative number
- [ ] Check directory permissions: `ls -la data downloads`
- [ ] Try manual permissions fix: `chmod -R 777 data downloads`

### Bot Not Responding

- [ ] Verify bot is admin in channel
- [ ] Check channel ID is correct (use @userinfobot)
- [ ] Verify bot token hasn't been revoked
- [ ] Check Telegram API status (telegram.org/status)
- [ ] Restart container: `docker-compose restart`

### Downloads Failing

- [ ] Check yt-dlp works: `docker exec telegram-ytdlp-bot /usr/local/bin/yt-dlp --version`
- [ ] Test manual download: `docker exec telegram-ytdlp-bot /usr/local/bin/yt-dlp <test_url> -o /tmp/test.mp4`
- [ ] Check disk space: `df -h`
- [ ] Verify download directory is writable
- [ ] Review yt-dlp errors in logs

### Duplicate Detection Not Logging

- [ ] Check log level is Info or Debug
- [ ] Post same URL twice
- [ ] Review logs: `docker-compose logs | grep "Duplicate"`
- [ ] Verify file exists from first download

### Performance Issues

- [ ] Check CPU usage: `docker stats telegram-ytdlp-bot`
- [ ] Check RAM usage: `docker stats telegram-ytdlp-bot`
- [ ] Review resource limits in docker-compose.yml
- [ ] Consider increasing limits if constrained
- [ ] Check for large video downloads (4K, etc.)

## Rollback Procedure

If something goes wrong:

- [ ] Stop container: `docker-compose down`
- [ ] Restore database backup: `cp data/state.db.backup data/state.db`
- [ ] Restore previous docker-compose.yml (if modified)
- [ ] Rebuild: `docker-compose build --no-cache`
- [ ] Start: `docker-compose up -d`
- [ ] Verify: `docker-compose logs -f`

## Success Criteria

✅ All checks passed when:

1. **Container Running**
   ```bash
   $ docker ps | grep telegram-ytdlp-bot
   telegram-ytdlp-bot   Up 5 minutes
   ```

2. **Logs Clean**
   ```
   info: Connected as bot: auralist_bot
   info: Starting Telegram monitoring for channel -1001234567890
   ```

3. **Download Works**
   - Post URL → See 👀
   - Wait 2-5 seconds → See 🔥
   - Wait for download → See 👍
   - File appears in downloads folder

4. **Duplicate Detection**
   - Post same URL again
   - Logs show: "Duplicate video request detected"
   - Still gets 👍 emoji
   - No re-download

5. **Persistence**
   - Stop container: `docker-compose down`
   - Start container: `docker-compose up -d`
   - Previous messages not reprocessed
   - Database intact

## Documentation Reference

- **README.md** - General overview and development
- **DOCKER.md** - Complete Docker reference
- **UNRAID.md** - Unraid-specific guide
- **DOCKER-SUMMARY.md** - Quick reference
- **This file** - Deployment checklist

## Support Commands

```bash
# View logs
docker-compose logs -f

# Restart container
docker-compose restart

# Stop container
docker-compose down

# Rebuild and restart
docker-compose up -d --build

# Check container status
docker ps | grep telegram

# Execute command in container
docker exec -it telegram-ytdlp-bot bash

# Check yt-dlp version
docker exec telegram-ytdlp-bot /usr/local/bin/yt-dlp --version

# View stats
docker stats telegram-ytdlp-bot
```

## Emergency Contacts

- Docker Issues: Check DOCKER.md
- Unraid Issues: Check UNRAID.md  
- Bot Issues: Check README.md
- Telegram API: https://telegram.org/status

---

**Ready to deploy?** Start with "Local Testing" section, then proceed to "Unraid Deployment"!
