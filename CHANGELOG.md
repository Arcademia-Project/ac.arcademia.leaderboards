# Changelog

All notable changes to the Arcademia Leaderboards SDK are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.0.0] - 2026-09-22

### Added
- `ArcademiaLeaderboards` static facade: `PingAsync`, `SubmitScoreAsync`, `GetTestScoresAsync`, `RequestClaimAsync`.
- Automatic launcher detection via environment variables (`ARCADEMIA_PIPE`, `ARCADEMIA_NONCE`, `ARCADEMIA_SESSION_ID`) with a transparent fallback to sandbox (direct HTTPS + API key) mode when they are absent.
- `arcademia.json` config file support (read from `StreamingAssets`), for setting a default API key/base URL without touching code.
- Packaged as a UPM package (`ac.arcademia.leaderboards`) with a `QuickStart` sample.
