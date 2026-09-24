using System;

namespace Arcademia.Leaderboards
{
    public enum ArcademiaMode
    {
        Sandbox,
        Launcher,
    }

    public enum LeaderboardScope
    {
        Local,
        Institutional,
        Country,
        Global,
    }

    public class ScoreQuery
    {
        public LeaderboardScope Scope = LeaderboardScope.Global;
        public bool BestPerPlayer = true;
        public string Ranks;
        public string PlayerScoreId;
        public int Before;
        public int After;

        public static ScoreQuery For(LeaderboardScope scope) => new ScoreQuery { Scope = scope };

        public ScoreQuery Top(int count)
        {
            Ranks = count > 0 ? "1-" + count : "none";
            return this;
        }

        public ScoreQuery Range(int from, int to)
        {
            var part = from == to ? from.ToString() : from + "-" + to;
            Ranks = string.IsNullOrEmpty(Ranks) || Ranks == "none" ? part : Ranks + "," + part;
            return this;
        }

        public ScoreQuery Rank(int rank) => Range(rank, rank);

        public ScoreQuery WithRanks(string ranks)
        {
            Ranks = ranks;
            return this;
        }

        public ScoreQuery NoRanks()
        {
            Ranks = "none";
            return this;
        }

        public ScoreQuery AroundPlayer(string scoreId, int before = 0, int after = 0)
        {
            PlayerScoreId = scoreId;
            Before = before;
            After = after;
            return this;
        }

        public ScoreQuery EveryScore()
        {
            BestPerPlayer = false;
            return this;
        }

        public ScoreQuery WithScope(LeaderboardScope scope)
        {
            var copy = (ScoreQuery)MemberwiseClone();
            copy.Scope = scope;
            return copy;
        }
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
        public bool Claimed;
        public bool IsPlayer;
        public string MachineName;
        public string SiteName;
        public string Country;
        public string Metadata;
    }

    public class ScoresResult
    {
        public bool Success;
        public string Message;
        public ArcademiaMode Mode;
        public string BoardSlug;
        public string BoardName;
        public LeaderboardScope Scope;
        public bool BestPerPlayer;
        public int Total;
        public BoardScore[] Scores = new BoardScore[0];
        public BoardScore Player;
        public BoardScore[] Around = new BoardScore[0];

        public override string ToString() =>
            Success
                ? $"[{Mode}] {BoardName} ({Scope}) - {Scores.Length} of {Total} scores"
                    + (Player != null ? $", player #{Player.Rank}" : "")
                : $"[{Mode}] {Scope} failed - {Message}";
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
        public string ScoreId;
        public string PlayerName;
        public string ClaimUrl;
        public string Message;
        public ArcademiaMode Mode;

        public override string ToString() =>
            $"[{Mode}] {Status}"
            + (string.IsNullOrEmpty(PlayerName) ? "" : $" as \"{PlayerName}\"")
            + (string.IsNullOrEmpty(Message) ? "" : $" - {Message}");
    }

    public class NameResult
    {
        public bool Success;
        public string Status;
        public string ScoreId;
        public string PlayerName;
        public string Message;
        public ArcademiaMode Mode;

        public override string ToString() =>
            $"[{Mode}] {Status}"
            + (string.IsNullOrEmpty(PlayerName) ? "" : $" \"{PlayerName}\"")
            + (string.IsNullOrEmpty(Message) ? "" : $" - {Message}");
    }
}
