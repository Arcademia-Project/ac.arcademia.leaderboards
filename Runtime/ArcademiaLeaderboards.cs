using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace Arcademia.Leaderboards
{
    public static class ArcademiaLeaderboards
    {
        private const string SettingsFileName = "arcademia.json";

        private static ArcademiaSettings _settings;
        private static LauncherTransport _launcher;
        private static bool _initialised;

        public static ArcademiaMode Mode
        {
            get
            {
                EnsureInitialised();
                return _launcher != null ? ArcademiaMode.Launcher : ArcademiaMode.Sandbox;
            }
        }

        public static string SessionId
        {
            get
            {
                EnsureInitialised();
                return _launcher?.SessionId;
            }
        }

        public static ArcademiaSettings Settings
        {
            get
            {
                EnsureInitialised();
                return _settings;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Shutdown();
        }

        public static void Configure(ArcademiaSettings settings)
        {
            EnsureInitialised();
            _settings = settings ?? new ArcademiaSettings();
        }

        public static void Shutdown()
        {
            _launcher?.Dispose();
            _launcher = null;
            _settings = null;
            _initialised = false;
        }

        private static void EnsureInitialised()
        {
            if (_initialised)
                return;

            _initialised = true;
            _launcher = LauncherTransport.TryCreateFromEnvironment();
            _settings = LoadSettingsFile() ?? new ArcademiaSettings();
        }

        private static ArcademiaSettings LoadSettingsFile()
        {
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, SettingsFileName);
                if (!File.Exists(path))
                    return null;

                return JsonUtility.FromJson<ArcademiaSettings>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Arcademia] Could not read " + SettingsFileName + ": " + ex.Message);
                return null;
            }
        }

        public static async Task<PingResult> PingAsync()
        {
            EnsureInitialised();

            try
            {
                if (_launcher != null)
                {
                    var raw = await _launcher.SendAsync("hello", null);
                    var dto = JsonUtility.FromJson<LauncherResponseDto>(raw);
                    return new PingResult
                    {
                        Success = dto.ok,
                        Mode = ArcademiaMode.Launcher,
                        Environment = "Live (via launcher)",
                        Message = dto.ok ? null : dto.message ?? dto.error,
                    };
                }

                var response = await SandboxTransport.SendAsync(
                    HttpMethod.Get, _settings.apiBase, "/api/Sdk/Ping", _settings.apiKey, null);

                if (!response.IsSuccess)
                    return new PingResult
                    {
                        Success = false,
                        Mode = ArcademiaMode.Sandbox,
                        Message = Describe(response),
                    };

                var ping = JsonUtility.FromJson<PingResponseDto>(response.Body);
                return new PingResult
                {
                    Success = true,
                    Mode = ArcademiaMode.Sandbox,
                    GameId = ping.gameId,
                    GameName = ping.gameName,
                    Environment = ping.environment + " (sandbox)",
                };
            }
            catch (Exception ex)
            {
                return new PingResult { Success = false, Mode = Mode, Message = ex.Message };
            }
        }

        public static async Task<ScoreResult> SubmitScoreAsync(
            string boardSlug,
            long value,
            string playerName = null,
            string metadataJson = null,
            Guid? scoreId = null)
        {
            EnsureInitialised();

            var id = (scoreId ?? Guid.NewGuid()).ToString();
            var valueText = value.ToString(CultureInfo.InvariantCulture);

            try
            {
                if (_launcher != null)
                    return await SubmitViaLauncherAsync(boardSlug, valueText, playerName, metadataJson, id);

                return await SubmitViaSandboxAsync(boardSlug, valueText, playerName, metadataJson, id);
            }
            catch (Exception ex)
            {
                return new ScoreResult
                {
                    Success = false,
                    Status = "error",
                    ScoreId = id,
                    Message = ex.Message,
                    Mode = Mode,
                };
            }
        }

        private static async Task<ScoreResult> SubmitViaLauncherAsync(
            string boardSlug, string valueText, string playerName, string metadataJson, string scoreId)
        {
            var fields =
                "\"boardSlug\":" + ArcademiaJson.Quote(boardSlug)
                + ",\"apiKey\":" + ArcademiaJson.Quote(_settings.apiKey ?? "")
                + "," + ArcademiaJson.ScoreFields(valueText, playerName, scoreId, metadataJson);

            var raw = await _launcher.SendAsync("submitScore", fields);
            var dto = JsonUtility.FromJson<LauncherResponseDto>(raw);

            if (!dto.ok)
                return new ScoreResult
                {
                    Success = false,
                    Status = "error",
                    ScoreId = scoreId,
                    Message = dto.message ?? dto.error,
                    Mode = ArcademiaMode.Launcher,
                };

            return new ScoreResult
            {
                Success = dto.status == "submitted" || dto.status == "queued",
                Status = dto.status,
                ScoreId = string.IsNullOrEmpty(dto.scoreId) ? scoreId : dto.scoreId,
                Rank = dto.rank >= 0 ? dto.rank : (long?)null,
                Duplicate = dto.duplicate,
                Message = dto.message,
                Mode = ArcademiaMode.Launcher,
            };
        }

        private static async Task<ScoreResult> SubmitViaSandboxAsync(
            string boardSlug, string valueText, string playerName, string metadataJson, string scoreId)
        {
            var body = "{" + ArcademiaJson.ScoreFields(valueText, playerName, scoreId, metadataJson) + "}";
            var path = "/api/Sdk/Leaderboards/" + Uri.EscapeDataString(boardSlug) + "/scores";

            var response = await SandboxTransport.SendAsync(
                HttpMethod.Post, _settings.apiBase, path, _settings.apiKey, body);

            if (!response.IsSuccess)
                return new ScoreResult
                {
                    Success = false,
                    Status = "rejected",
                    ScoreId = scoreId,
                    Message = Describe(response),
                    Mode = ArcademiaMode.Sandbox,
                };

            var dto = JsonUtility.FromJson<SubmitResponseDto>(response.Body);
            return new ScoreResult
            {
                Success = true,
                Status = "submitted",
                ScoreId = string.IsNullOrEmpty(dto.scoreId) ? scoreId : dto.scoreId,
                Rank = dto.rank >= 0 ? dto.rank : (long?)null,
                Duplicate = dto.duplicate,
                Mode = ArcademiaMode.Sandbox,
            };
        }

        public static async Task<TestScoresResult> GetTestScoresAsync(
            string boardSlug, int limit = 25, int offset = 0)
        {
            EnsureInitialised();

            if (_launcher != null)
                return new TestScoresResult
                {
                    Success = false,
                    Message = "Test scores are only available in sandbox mode.",
                };

            try
            {
                var path = "/api/Sdk/Leaderboards/" + Uri.EscapeDataString(boardSlug)
                    + "/scores?limit=" + limit.ToString(CultureInfo.InvariantCulture)
                    + "&offset=" + offset.ToString(CultureInfo.InvariantCulture);

                var response = await SandboxTransport.SendAsync(
                    HttpMethod.Get, _settings.apiBase, path, _settings.apiKey, null);

                if (!response.IsSuccess)
                    return new TestScoresResult { Success = false, Message = Describe(response) };

                var dto = JsonUtility.FromJson<TestScoresDto>(response.Body);
                var rows = dto.scores ?? new ScoreRowDto[0];
                var scores = new BoardScore[rows.Length];
                for (var i = 0; i < rows.Length; i++)
                    scores[i] = new BoardScore
                    {
                        Rank = rows[i].rank,
                        PlayerName = rows[i].playerName,
                        Value = rows[i].value,
                        AchievedAt = rows[i].achievedAt,
                    };

                return new TestScoresResult
                {
                    Success = true,
                    BoardSlug = dto.board?.slug,
                    BoardName = dto.board?.name,
                    Total = dto.total,
                    Scores = scores,
                };
            }
            catch (Exception ex)
            {
                return new TestScoresResult { Success = false, Message = ex.Message };
            }
        }

        private const int ClaimResponseTimeoutMs = 6 * 60 * 1000;

        public static async Task<ClaimResult> RequestClaimAsync(string scoreId)
        {
            EnsureInitialised();

            if (string.IsNullOrEmpty(scoreId))
                return new ClaimResult { Success = false, Status = "error", Message = "scoreId is required.", Mode = Mode };

            if (_launcher == null)
                return new ClaimResult
                {
                    Success = false,
                    Status = "rejected",
                    Message = "Claiming a score requires running through the Arcademia launcher.",
                    Mode = ArcademiaMode.Sandbox,
                };

            try
            {
                var fields = "\"scoreId\":" + ArcademiaJson.Quote(scoreId)
                    + ",\"apiKey\":" + ArcademiaJson.Quote(_settings.apiKey ?? "");

                var raw = await _launcher.SendAsync("requestClaim", fields, ClaimResponseTimeoutMs);
                var dto = JsonUtility.FromJson<LauncherResponseDto>(raw);

                if (!dto.ok)
                    return new ClaimResult
                    {
                        Success = false,
                        Status = "error",
                        Message = dto.message ?? dto.error,
                        Mode = ArcademiaMode.Launcher,
                    };

                return new ClaimResult
                {
                    Success = dto.status == "saved",
                    Status = dto.status,
                    Message = dto.message,
                    Mode = ArcademiaMode.Launcher,
                };
            }
            catch (Exception ex)
            {
                return new ClaimResult { Success = false, Status = "error", Message = ex.Message, Mode = ArcademiaMode.Launcher };
            }
        }

        private static string Describe(SandboxResponse response)
        {
            var body = string.IsNullOrWhiteSpace(response.Body) ? "" : response.Body.Trim('"');
            return "HTTP " + response.StatusCode + (body.Length > 0 ? ": " + body : "");
        }
    }
}
