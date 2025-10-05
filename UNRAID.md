# Deploying to Unraid Server

This guide explains how to deploy the Telegram YT-DLP Bot on your Unraid server.

## Method 1: Using Docker Compose (Recommended)

### Step 1: Prepare the Server

1. SSH into your Unraid server or use the terminal in the Unraid web UI
2. Navigate to your appdata directory:
   ```bash
   cd /mnt/user/appdata
   ```

3. Clone or copy the repository:
   ```bash
   git clone https://github.com/yourusername/TelegramYtDlpBot.git
   cd TelegramYtDlpBot
   ```

### Step 2: Configure Environment

1. Create your `.env` file:
   ```bash
   cp .env.example .env
   nano .env
   ```

2. Set your values:
   ```bash
   BOT_TOKEN=your_bot_token_here
   CHANNEL_ID=your_channel_id_here
   ```

3. Save and exit (Ctrl+X, Y, Enter)

### Step 3: Create Required Directories

```bash
mkdir -p data downloads
chmod -R 777 data downloads
```

### Step 4: Build and Run

```bash
docker-compose up -d
```

### Step 5: Verify It's Running

```bash
docker-compose logs -f
```

You should see:
```
info: Connected as bot: your_bot_name
info: Starting Telegram monitoring for channel -1001234567890
```

### Managing the Container

**View logs:**
```bash
docker-compose logs -f telegram-ytdlp-bot
```

**Restart:**
```bash
docker-compose restart
```

**Stop:**
```bash
docker-compose down
```

**Update and restart:**
```bash
git pull
docker-compose up -d --build
```

## Method 2: Using Unraid Docker UI

### Step 1: Build the Image

First, you need to build the Docker image. SSH into Unraid and run:

```bash
cd /mnt/user/appdata/TelegramYtDlpBot
docker build -t telegram-ytdlp-bot:latest .
```

### Step 2: Add Container in Unraid

1. Go to the **Docker** tab in Unraid web UI
2. Click **Add Container**
3. Configure the following settings:

#### Basic Settings
- **Name**: `telegram-ytdlp-bot`
- **Repository**: `telegram-ytdlp-bot:latest`
- **Network Type**: `bridge`
- **Console shell command**: `bash`

#### Port Mappings
No ports needed (bot doesn't expose any services)

#### Path Mappings
Add two paths:

| Container Path | Host Path | Access Mode |
|---------------|-----------|-------------|
| `/app/data` | `/mnt/user/appdata/telegram-ytdlp-bot/data` | Read/Write |
| `/app/downloads` | `/mnt/user/downloads/telegram-videos` | Read/Write |

#### Environment Variables
Add these variables:

| Key | Value |
|-----|-------|
| `DOTNET_ENVIRONMENT` | `Production` |
| `BotConfiguration__BotToken` | `your_bot_token_here` |
| `BotConfiguration__ChannelId` | `your_channel_id_here` |
| `BotConfiguration__YtDlp__ExecutablePath` | `/usr/local/bin/yt-dlp` |

4. Click **Apply**

### Step 3: Verify

1. Click on the container icon in the Docker tab
2. Select **Logs**
3. You should see the bot connect and start monitoring

## Troubleshooting on Unraid

### Container Won't Start

Check the logs in the Unraid Docker UI:
```bash
docker logs telegram-ytdlp-bot
```

Common issues:
- Invalid bot token or channel ID
- Missing or incorrect environment variables
- Permission issues with mounted volumes

### Fix Permission Issues

```bash
chmod -R 777 /mnt/user/appdata/telegram-ytdlp-bot
chmod -R 777 /mnt/user/downloads/telegram-videos
```

### Container Keeps Restarting

View real-time logs:
```bash
docker logs -f telegram-ytdlp-bot
```

Check for error messages about:
- Database access (check `/mnt/user/appdata/telegram-ytdlp-bot/data` permissions)
- Download directory access (check `/mnt/user/downloads/telegram-videos` exists)
- Network connectivity (ensure Unraid has internet access)

### Updating yt-dlp in Container

The container downloads yt-dlp during build. To update:

1. Rebuild the container:
   ```bash
   cd /mnt/user/appdata/TelegramYtDlpBot
   docker-compose down
   docker-compose build --no-cache
   docker-compose up -d
   ```

### Monitoring Downloads

Watch downloads in real-time:
```bash
ls -lh /mnt/user/downloads/telegram-videos
```

Or use the Unraid shares browser in the web UI.

## Resource Usage

Typical resource usage:
- **CPU**: 0.5-2% idle, 20-50% during downloads
- **RAM**: ~100-200 MB idle, 200-500 MB during downloads
- **Disk**: Depends on video sizes (set in docker-compose.yml limits)

You can adjust resource limits in `docker-compose.yml`:

```yaml
deploy:
  resources:
    limits:
      cpus: '2'
      memory: 2G
```

## Backup Recommendations

### What to Backup

1. **Database**: `/mnt/user/appdata/telegram-ytdlp-bot/data/state.db`
   - Contains job history and message tracking
   - Small file (< 1 MB typically)

2. **Downloads**: `/mnt/user/downloads/telegram-videos`
   - Your actual video files
   - Size varies based on usage

3. **Configuration**: `.env` file or environment variables

### Unraid Backup

Include these paths in your Unraid backup:
- `/mnt/user/appdata/telegram-ytdlp-bot/`

Use the CA Backup plugin or Unraid backup tools.

## Accessing Downloaded Videos

### Via Unraid Shares

1. Open your Unraid shares in Windows Explorer: `\\your-unraid-ip\downloads\telegram-videos`
2. Videos will appear here as they're downloaded

### Via SMB/NFS

Configure your share settings in Unraid:
- Go to **Shares** tab
- Click on `downloads` share
- Enable SMB/NFS as needed
- Set appropriate permissions

### Via Plex/Jellyfin/Emby

Point your media server to `/mnt/user/downloads/telegram-videos` as a library folder.

## Security Notes

1. **Bot Token**: Keep your `.env` file secure, never commit it to git
2. **File Permissions**: Container runs as non-root user (UID 1000)
3. **Network**: Bot only needs outbound HTTPS to Telegram API
4. **Updates**: Regularly rebuild to get latest yt-dlp version

## Community Apps Template (Future)

If this bot is published to Community Applications, you can install it with one click:

1. Go to **Apps** tab
2. Search for "telegram ytdlp bot"
3. Click **Install**
4. Fill in your bot token and channel ID
5. Choose download directory
6. Click **Apply**
