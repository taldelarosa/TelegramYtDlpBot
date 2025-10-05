# Docker Deployment Summary

This document summarizes the Docker containerization setup for the Telegram YT-DLP Bot.

## Files Created

### 1. `Dockerfile`
Multi-stage Docker build:
- **Build stage**: Uses .NET 8 SDK to compile the application
- **Runtime stage**: Uses .NET 8 ASP.NET runtime (smaller image)
- **Includes**: Automatically downloads yt-dlp, ffmpeg, and Python dependencies
- **Security**: Runs as non-root user (UID 1000)
- **Size**: ~500 MB (optimized with multi-stage build)

### 2. `docker-compose.yml`
Docker Compose configuration for easy deployment:
- **Service name**: `telegram-ytdlp-bot`
- **Restart policy**: `unless-stopped` (auto-restart on failure)
- **Volumes**: 
  - `./data:/app/data` (SQLite database persistence)
  - `./downloads:/app/downloads` (downloaded videos)
- **Environment**: Configured via `.env` file
- **Resources**: Optional CPU and memory limits for Unraid

### 3. `.dockerignore`
Optimizes build context by excluding:
- Build artifacts (bin/, obj/)
- IDE files (.vs/, .vscode/)
- Tests and documentation
- Local data/downloads directories
- Git repository

### 4. `.env.example`
Template for required environment variables:
- `BOT_TOKEN`: Telegram bot token
- `CHANNEL_ID`: Telegram channel ID

### 5. `appsettings.Production.json`
Production configuration:
- Paths optimized for container (`/app/data`, `/app/downloads`)
- yt-dlp path: `/usr/local/bin/yt-dlp`
- Reduced logging verbosity

### 6. `UNRAID.md`
Comprehensive Unraid deployment guide with:
- Docker Compose method
- Unraid UI method
- Troubleshooting section
- Backup recommendations
- Security notes

## Quick Start Commands

### Build the Image
```bash
docker build -t telegram-ytdlp-bot:latest .
```

### Run with Docker Compose
```bash
# Create configuration
cp .env.example .env
nano .env  # Edit with your values

# Start the bot
docker-compose up -d

# View logs
docker-compose logs -f

# Stop the bot
docker-compose down
```

### Run with Docker CLI
```bash
docker run -d \
  --name telegram-ytdlp-bot \
  --restart unless-stopped \
  -e BOT_TOKEN="your_token" \
  -e CHANNEL_ID="-1001234567890" \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/downloads:/app/downloads \
  telegram-ytdlp-bot:latest
```

## Configuration via Environment Variables

The bot supports hierarchical configuration via environment variables using the .NET convention:

```bash
# Telegram settings
BotConfiguration__BotToken=your_token
BotConfiguration__ChannelId=-1001234567890

# yt-dlp settings
BotConfiguration__YtDlp__ExecutablePath=/usr/local/bin/yt-dlp
BotConfiguration__YtDlp__Quality=1080p
BotConfiguration__YtDlp__OutputTemplate=%(title)s.%(ext)s

# Storage settings
BotConfiguration__Database__Path=/app/data/state.db
BotConfiguration__Downloads__OutputPath=/app/downloads
```

## Volume Mounts

### Required Volumes

1. **Database volume** (`/app/data`):
   - Purpose: SQLite database persistence
   - Size: Small (<1 MB typically)
   - Backup: Recommended

2. **Downloads volume** (`/app/downloads`):
   - Purpose: Downloaded video files
   - Size: Varies based on usage
   - Backup: Based on your retention needs

### Optional Volumes

3. **Custom configuration** (`/app/appsettings.Production.json`):
   - Purpose: Override default settings
   - Mount as read-only (`:ro`)
   - Use environment variables instead when possible

## Unraid Integration

### Recommended Paths

For Unraid servers, use these host paths:

| Container Path | Unraid Host Path | Purpose |
|---------------|------------------|---------|
| `/app/data` | `/mnt/user/appdata/telegram-ytdlp-bot/data` | Database storage |
| `/app/downloads` | `/mnt/user/downloads/telegram-videos` | Downloaded videos |

### Resource Limits

Suggested limits for Unraid (adjust based on your server):

```yaml
deploy:
  resources:
    limits:
      cpus: '2'          # Maximum 2 CPU cores
      memory: 2G         # Maximum 2 GB RAM
    reservations:
      cpus: '0.5'        # Reserve 0.5 CPU cores
      memory: 512M       # Reserve 512 MB RAM
```

## Build Arguments

The Dockerfile doesn't currently use build arguments, but you could add them:

```dockerfile
ARG YTDLP_VERSION=latest
RUN wget https://github.com/yt-dlp/yt-dlp/releases/download/${YTDLP_VERSION}/yt-dlp
```

## Security Considerations

1. **Non-root user**: Container runs as UID 1000 (appuser)
2. **Minimal attack surface**: Only required packages installed
3. **No exposed ports**: Bot doesn't listen on any ports
4. **Secrets**: Bot token passed via environment variables, not in image
5. **Read-only filesystem**: Could add `--read-only` flag with tmpfs mounts

## Troubleshooting

### Image Build Fails

**Problem**: Build fails during yt-dlp download
**Solution**: Check internet connectivity, retry build

**Problem**: .NET restore fails
**Solution**: Clear Docker cache: `docker build --no-cache`

### Container Won't Start

**Problem**: Exit code 1 immediately
**Solution**: Check logs for configuration errors

**Problem**: Database permission errors
**Solution**: Fix volume permissions: `chmod -R 777 ./data`

### Downloads Not Working

**Problem**: yt-dlp errors in logs
**Solution**: Rebuild image to get latest yt-dlp: `docker-compose build --no-cache`

**Problem**: Disk space errors
**Solution**: Check available space on mounted volumes

## Maintenance

### Updating the Bot

```bash
# Pull latest code
git pull

# Rebuild and restart
docker-compose up -d --build
```

### Updating yt-dlp

Rebuild the image (yt-dlp is downloaded during build):

```bash
docker-compose build --no-cache
docker-compose up -d
```

### Viewing Logs

```bash
# All logs
docker-compose logs

# Follow logs in real-time
docker-compose logs -f

# Last 100 lines
docker-compose logs --tail=100

# Specific timeframe
docker-compose logs --since 1h
```

### Backup Database

```bash
# Stop the bot
docker-compose down

# Backup database
cp data/state.db data/state.db.backup

# Restart
docker-compose up -d
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Push Docker Image

on:
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Build Docker image
        run: docker build -t telegram-ytdlp-bot:latest .
      
      - name: Run tests
        run: |
          docker run --rm \
            -v $(pwd)/tests:/app/tests \
            telegram-ytdlp-bot:latest \
            dotnet test
      
      - name: Push to registry
        run: |
          echo ${{ secrets.DOCKER_PASSWORD }} | docker login -u ${{ secrets.DOCKER_USERNAME }} --password-stdin
          docker push telegram-ytdlp-bot:latest
```

## Performance Tuning

### Optimize for Unraid

1. **Use cache drive**: Store database on cache for better performance
2. **Download to array**: Store videos directly on array disk
3. **Resource limits**: Set appropriate CPU/RAM limits
4. **Network mode**: Use bridge mode (default) for isolation

### Monitoring

Add health checks to docker-compose.yml:

```yaml
healthcheck:
  test: ["CMD", "dotnet", "--list-runtimes"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

## Future Enhancements

Potential Docker improvements:

1. **Health check endpoint**: Add HTTP health check support
2. **Metrics export**: Prometheus metrics for monitoring
3. **Multi-architecture**: Build for ARM64 (Raspberry Pi, Apple Silicon)
4. **Init system**: Use tini for proper signal handling
5. **Registry publishing**: Publish to Docker Hub or GHCR

## Support

For issues:
1. Check logs: `docker-compose logs`
2. Verify configuration: `docker-compose config`
3. Test connectivity: `docker exec telegram-ytdlp-bot /usr/local/bin/yt-dlp --version`
4. Review UNRAID.md for Unraid-specific issues
