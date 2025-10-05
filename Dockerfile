# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/TelegramYtDlpBot/TelegramYtDlpBot.csproj", "TelegramYtDlpBot/"]
RUN dotnet restore "TelegramYtDlpBot/TelegramYtDlpBot.csproj"

# Copy source code and build
COPY src/TelegramYtDlpBot/ TelegramYtDlpBot/
WORKDIR /src/TelegramYtDlpBot
RUN dotnet publish "TelegramYtDlpBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install yt-dlp and dependencies
RUN apt-get update && apt-get install -y \
    wget \
    python3 \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# Download yt-dlp
RUN wget https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -O /usr/local/bin/yt-dlp \
    && chmod +x /usr/local/bin/yt-dlp

# Copy published app
COPY --from=build /app/publish .

# Create directories for data and downloads
RUN mkdir -p /app/data /app/downloads

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV YtDlp__ExecutablePath=/usr/local/bin/yt-dlp

# Run as non-root user for security
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "TelegramYtDlpBot.dll"]
