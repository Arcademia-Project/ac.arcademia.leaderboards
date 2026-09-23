using System;
using System.Globalization;
using System.Text;

namespace Arcademia.Leaderboards
{
    internal static class ArcademiaJson
    {
        public static string Quote(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        public static string ScoreFields(
            string value,
            string playerName,
            string scoreId,
            string metadataJson
        )
        {
            var sb = new StringBuilder();
            sb.Append("\"value\":").Append(value);
            if (!string.IsNullOrEmpty(playerName))
                sb.Append(",\"playerName\":").Append(Quote(playerName));
            sb.Append(",\"scoreId\":").Append(Quote(scoreId));
            if (!string.IsNullOrWhiteSpace(metadataJson))
                sb.Append(",\"metadata\":").Append(metadataJson);
            return sb.ToString();
        }
    }

    [Serializable]
    internal class SubmitResponseDto
    {
        public string scoreId;
        public long rank = -1;
        public bool duplicate;
        public string environment;
    }

    [Serializable]
    internal class LauncherResponseDto
    {
        public string id;
        public bool ok;
        public string status;
        public string scoreId;
        public long rank = -1;
        public bool duplicate;
        public string message;
        public string error;
        public string mode;
        public string sessionId;
    }

    [Serializable]
    internal class PingResponseDto
    {
        public int gameId;
        public string gameName;
        public string environment;
    }

    [Serializable]
    internal class BoardDto
    {
        public string slug;
        public string name;
    }

    [Serializable]
    internal class ScoreRowDto
    {
        public int rank;
        public string playerName;
        public long value;
        public string achievedAt;
        public bool claimed;
        public bool isPlayer;
        public string machineName;
        public string siteName;
        public string country;
    }

    [Serializable]
    internal class TestScoresDto
    {
        public BoardDto board;
        public int total;
        public ScoreRowDto[] scores;
    }

    [Serializable]
    internal class ScoresDto
    {
        public bool ok = true;
        public string error;
        public string message;
        public BoardDto board;
        public string scope;
        public string mode;
        public int total;
        public ScoreRowDto[] scores;
        public ScoreRowDto player;
        public ScoreRowDto[] around;
    }
}
