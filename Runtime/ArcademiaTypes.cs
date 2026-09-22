using System;

namespace Arcademia.Leaderboards
{
    public enum ArcademiaMode
    {
        Sandbox,
        Launcher,
    }

    [Serializable]
    public class ArcademiaSettings
    {
        public string apiBase = "https://manager.arcademia.ac";
        public string apiKey = "";
    }

    public class ScoreResult
    {
        public bool Success;
        public string Status;
        public string ScoreId;
        public long? Rank;
        public bool Duplicate;
        public string Message;
        public ArcademiaMode Mode;

        public override string ToString() =>
            $"[{Mode}] {Status}"
            + (Success ? "" : " (failed)")
            + (Rank.HasValue ? $" rank #{Rank}" : "")
            + (Duplicate ? " duplicate" : "")
            + (string.IsNullOrEmpty(Message) ? "" : $" - {Message}")
            + (string.IsNullOrEmpty(ScoreId) ? "" : $" [{ScoreId}]");
    }

    public class PingResult
    {
        public bool Success;
        public ArcademiaMode Mode;
        public int GameId;
        public string GameName;
        public string Environment;
        public string Message;

        public override string ToString() =>
            Success
                ? $"[{Mode}] OK - {Environment}" + (string.IsNullOrEmpty(GameName) ? "" : $" - game \"{GameName}\" (#{GameId})")
                : $"[{Mode}] failed - {Message}";
    }

    public class BoardScore
    {
        public int Rank;
        public string PlayerName;
        public long Value;
        public string AchievedAt;
    }

    public class TestScoresResult
    {
        public bool Success;
        public string Message;
        public string BoardSlug;
        public string BoardName;
        public int Total;
        public BoardScore[] Scores = new BoardScore[0];
    }

    public class ClaimResult
    {
        public bool Success;
        public string Status;
        public string Message;
        public ArcademiaMode Mode;

        public override string ToString() =>
            $"[{Mode}] {Status}" + (string.IsNullOrEmpty(Message) ? "" : $" - {Message}");
    }
}
