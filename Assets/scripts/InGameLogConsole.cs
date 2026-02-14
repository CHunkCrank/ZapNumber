using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public sealed class InGameLogConsole : MonoBehaviour
{
    private const int MaxEntries = 300;
    private static InGameLogConsole _instance;

    private readonly List<string> _entries = new List<string>(MaxEntries);
    private readonly object _fileLock = new object();

    private Vector2 _scroll;
    private bool _visible;
    private string _logFilePath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        var go = new GameObject("InGameLogConsole");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<InGameLogConsole>();
    }

    private void Awake()
    {
        _logFilePath = Path.Combine(Application.persistentDataPath, "runtime-log.txt");
        Application.logMessageReceived += OnLogMessageReceived;
        AddEntry("InGameLogConsole started.");
        AddEntry("Log file: " + _logFilePath);
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var line = "[" + timestamp + "][" + type + "] " + condition;

        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            if (!string.IsNullOrEmpty(stackTrace))
            {
                line += "\n" + stackTrace;
            }
        }

        AddEntry(line);
    }

    private void AddEntry(string line)
    {
        if (_entries.Count >= MaxEntries)
        {
            _entries.RemoveAt(0);
        }

        _entries.Add(line);
        AppendLineToFile(line);
    }

    private void AppendLineToFile(string line)
    {
        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Avoid recursive logging if file write fails.
        }
    }

    private void OnGUI()
    {
        var sw = Screen.width;
        var sh = Screen.height;

        if (!_visible)
        {
            if (GUI.Button(new Rect(10, 10, 140, 60), "LOG"))
            {
                _visible = true;
            }

            return;
        }

        GUI.Box(new Rect(10, 10, sw - 20, sh - 20), "Runtime Logs");

        if (GUI.Button(new Rect(sw - 440, 20, 100, 50), "Close"))
        {
            _visible = false;
        }

        if (GUI.Button(new Rect(sw - 330, 20, 100, 50), "Copy"))
        {
            GUIUtility.systemCopyBuffer = BuildCurrentText();
        }

        if (GUI.Button(new Rect(sw - 220, 20, 100, 50), "Clear"))
        {
            _entries.Clear();
            try
            {
                File.WriteAllText(_logFilePath, string.Empty, Encoding.UTF8);
            }
            catch
            {
                // Ignore file errors in debug UI.
            }
        }

        if (GUI.Button(new Rect(sw - 110, 20, 100, 50), "Reload"))
        {
            ReloadFromFile();
        }

        GUI.Label(new Rect(20, 70, sw - 40, 40), _logFilePath);

        var viewRect = new Rect(20, 110, sw - 40, sh - 140);
        var contentRect = new Rect(0, 0, viewRect.width - 25, Mathf.Max(40, _entries.Count * 32));
        _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);

        for (var i = 0; i < _entries.Count; i++)
        {
            GUI.Label(new Rect(0, i * 32, contentRect.width, 32), _entries[i]);
        }

        GUI.EndScrollView();
    }

    private string BuildCurrentText()
    {
        var sb = new StringBuilder(16 * 1024);
        for (var i = 0; i < _entries.Count; i++)
        {
            sb.AppendLine(_entries[i]);
        }

        return sb.ToString();
    }

    private void ReloadFromFile()
    {
        try
        {
            if (!File.Exists(_logFilePath)) return;

            var lines = File.ReadAllLines(_logFilePath);
            _entries.Clear();
            var start = Mathf.Max(0, lines.Length - MaxEntries);
            for (var i = start; i < lines.Length; i++)
            {
                _entries.Add(lines[i]);
            }
        }
        catch
        {
            // Ignore file errors in debug UI.
        }
    }
}
#endif
