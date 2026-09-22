using System;
using System.Collections.Generic;
using System.Text;
using Arcademia.Leaderboards;
using UnityEngine;

namespace Arcademia.Leaderboards.Samples
{
    public class ArcademiaQuickStart : MonoBehaviour
    {
        private const string PrefApiBase = "arcademia.quickstart.apiBase";
        private const string PrefApiKey = "arcademia.quickstart.apiKey";
        private const string PrefBoard = "arcademia.quickstart.board";
        private const string PrefPlayerName = "arcademia.quickstart.playerName";
        private const int MaxLogLines = 200;
        private const float DesignHeight = 720f;

        private string _apiBase;
        private string _apiKey;
        private string _boardSlug;
        private string _valueText = "1000";
        private string _playerName = "Player One";
        private string _metadata = "";

        private bool _busy;
        private string _lastScoreId;
        private Vector2 _logScroll;
        private readonly List<string> _log = new List<string>();

        private void Start()
        {
            var settings = ArcademiaLeaderboards.Settings;
            _apiBase = PlayerPrefs.GetString(PrefApiBase, settings.apiBase);
            _apiKey = string.IsNullOrEmpty(settings.apiKey)
                ? PlayerPrefs.GetString(PrefApiKey, "")
                : settings.apiKey;
            _boardSlug = PlayerPrefs.GetString(PrefBoard, "highscore");
            _playerName = PlayerPrefs.GetString(PrefPlayerName, "Player One");

            ApplySettings();
            Append("Mode: " + ArcademiaLeaderboards.Mode
                + (ArcademiaLeaderboards.Mode == ArcademiaMode.Launcher
                    ? " (session " + ArcademiaLeaderboards.SessionId + ")"
                    : " - scores go to the Test area"));

            if (!string.IsNullOrEmpty(_apiKey))
                RunPing();
        }

        private void OnDestroy()
        {
            ArcademiaLeaderboards.Shutdown();
        }

        private void ApplySettings()
        {
            ArcademiaLeaderboards.Configure(new ArcademiaSettings
            {
                apiBase = _apiBase,
                apiKey = _apiKey,
            });

            PlayerPrefs.SetString(PrefApiBase, _apiBase ?? "");
            PlayerPrefs.SetString(PrefApiKey, _apiKey ?? "");
            PlayerPrefs.SetString(PrefBoard, _boardSlug ?? "");
            PlayerPrefs.SetString(PrefPlayerName, _playerName ?? "");
            PlayerPrefs.Save();
        }

        private void Append(string line)
        {
            _log.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + line);
            if (_log.Count > MaxLogLines)
                _log.RemoveAt(0);
            _logScroll.y = float.MaxValue;
            Debug.Log("[Arcademia] " + line);
        }

        private async void RunPing()
        {
            if (_busy) return;
            _busy = true;
            ApplySettings();
            Append("Ping...");
            try { Append((await ArcademiaLeaderboards.PingAsync()).ToString()); }
            finally { _busy = false; }
        }

        private async void RunSubmit(long value)
        {
            if (_busy) return;
            _busy = true;
            ApplySettings();
            Append("Submitting " + value + " to \"" + _boardSlug + "\"...");
            try
            {
                var metadata = string.IsNullOrWhiteSpace(_metadata) ? null : _metadata;
                var result = await ArcademiaLeaderboards.SubmitScoreAsync(_boardSlug, value, _playerName, metadata);
                if (result.Success && !string.IsNullOrEmpty(result.ScoreId))
                    _lastScoreId = result.ScoreId;
                Append(result.ToString());
            }
            finally { _busy = false; }
        }

        private async void RunClaim()
        {
            if (_busy) return;
            if (string.IsNullOrEmpty(_lastScoreId))
            {
                Append("Submit a score first.");
                return;
            }
            _busy = true;
            ApplySettings();
            Append("Requesting claim for " + _lastScoreId + "...");
            try { Append((await ArcademiaLeaderboards.RequestClaimAsync(_lastScoreId)).ToString()); }
            finally { _busy = false; }
        }

        private async void RunFetch()
        {
            if (_busy) return;
            _busy = true;
            ApplySettings();
            Append("Fetching test scores for \"" + _boardSlug + "\"...");
            try
            {
                var result = await ArcademiaLeaderboards.GetTestScoresAsync(_boardSlug, 10, 0);
                if (!result.Success)
                {
                    Append("Failed - " + result.Message);
                    return;
                }

                var sb = new StringBuilder();
                sb.Append(result.BoardName).Append(" (").Append(result.Total).Append(" test scores)");
                foreach (var s in result.Scores)
                    sb.Append("\n    #").Append(s.Rank).Append("  ").Append(s.PlayerName).Append("  ").Append(s.Value);
                Append(sb.ToString());
            }
            finally { _busy = false; }
        }

        private void OnGUI()
        {
            var scale = Screen.height / DesignHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            var width = Screen.width / scale;
            var height = Screen.height / scale;

            var label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            var field = new GUIStyle(GUI.skin.textField) { fontSize = 16 };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 16 };
            var heading = new GUIStyle(label) { fontSize = 22, fontStyle = FontStyle.Bold };

            GUILayout.BeginArea(new Rect(20, 16, width - 40, height - 32));

            GUILayout.Label("Arcademia Leaderboards Quick Start", heading);
            GUILayout.Label(
                "Mode: " + ArcademiaLeaderboards.Mode
                + (ArcademiaLeaderboards.Mode == ArcademiaMode.Launcher
                    ? "  |  session " + ArcademiaLeaderboards.SessionId
                    : "  |  sandbox (Test area only)"),
                label);

            GUILayout.Space(6);
            _apiBase = Row("API base", _apiBase, label, field);
            _apiKey = Row("API key", _apiKey, label, field);
            _boardSlug = Row("Board slug", _boardSlug, label, field);
            _valueText = Row("Score", _valueText, label, field);
            _playerName = Row("Player name", _playerName, label, field);
            _metadata = Row("Metadata JSON", _metadata, label, field);

            GUILayout.Space(8);
            GUI.enabled = !_busy;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping", button, GUILayout.Height(34))) RunPing();
            if (GUILayout.Button("Submit score", button, GUILayout.Height(34)))
            {
                if (long.TryParse(_valueText, out var value)) RunSubmit(value);
                else Append("Score must be a whole number.");
            }
            if (GUILayout.Button("Submit random", button, GUILayout.Height(34)))
                RunSubmit(UnityEngine.Random.Range(100, 100000));
            if (GUILayout.Button("Fetch test scores", button, GUILayout.Height(34))) RunFetch();
            if (GUILayout.Button("Claim last score", button, GUILayout.Height(34))) RunClaim();
            GUI.enabled = true;
            if (GUILayout.Button("Clear log", button, GUILayout.Height(34))) _log.Clear();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            _logScroll = GUILayout.BeginScrollView(_logScroll, GUI.skin.box);
            foreach (var line in _log)
                GUILayout.Label(line, label);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private static string Row(string name, string value, GUIStyle label, GUIStyle field)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, label, GUILayout.Width(130));
            var result = GUILayout.TextField(value ?? "", field);
            GUILayout.EndHorizontal();
            return result;
        }
    }
}
