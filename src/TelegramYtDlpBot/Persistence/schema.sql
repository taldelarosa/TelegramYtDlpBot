-- Telegram Channel URL Monitor - SQLite Schema
-- Version: 1.0.0
-- Date: 2025-10-04

-- Enable foreign keys and WAL mode for concurrency
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

-- Processed messages from Telegram
CREATE TABLE IF NOT EXISTS ProcessedMessages (
    MessageId INTEGER PRIMARY KEY,
    ChannelId INTEGER NOT NULL,
    ProcessedAt TEXT NOT NULL,
    UrlCount INTEGER NOT NULL DEFAULT 0
);

-- Download job queue
CREATE TABLE IF NOT EXISTS DownloadJobs (
    JobId TEXT PRIMARY KEY,
    MessageId INTEGER NOT NULL,
    Url TEXT NOT NULL,
    Status TEXT NOT NULL CHECK(Status IN ('Queued','InProgress','Completed','Failed')),
    CreatedAt TEXT NOT NULL,
    CompletedAt TEXT,
    ErrorMessage TEXT,
    OutputPath TEXT,
    RetryCount INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (MessageId) REFERENCES ProcessedMessages(MessageId)
);

CREATE INDEX IF NOT EXISTS idx_jobs_status ON DownloadJobs(Status);
CREATE INDEX IF NOT EXISTS idx_jobs_created ON DownloadJobs(CreatedAt);

-- Application state key-value store
CREATE TABLE IF NOT EXISTS AppState (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);

-- Bootstrap initial state
INSERT OR IGNORE INTO AppState (Key, Value) VALUES ('LastMessageId', '0');
INSERT OR IGNORE INTO AppState (Key, Value) VALUES ('ConfigVersion', '1');
