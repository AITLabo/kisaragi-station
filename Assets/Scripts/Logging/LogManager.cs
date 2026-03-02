using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

// きさらぎ駅ループ – プレイヤー行動ログ保存
// JSON形式で保存。将来のBrain自動生成の学習データになる。
public class LogManager : MonoBehaviour
{
    public static LogManager Instance { get; private set; }

    [Header("Log Settings")]
    [SerializeField] private string logFileName = "kisaragi_log.json";

    private List<LogEntry> sessionLog = new List<LogEntry>();
    private string logPath;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // WebGLではpersistentDataPathへ保存
        logPath = Path.Combine(Application.persistentDataPath, logFileName);
    }

    // ──────────────────────────────────────
    // GameManagerから呼ばれる
    // ──────────────────────────────────────
    public void SaveLog(string actionID, string result)
    {
        if (GameManager.Instance == null) return;

        var entry = new LogEntry
        {
            actionID       = actionID,
            result         = result,
            timestamp      = Time.time,
            distortionLevel = GameManager.Instance.currentDistortion.ToString(),
            progress       = GameManager.Instance.currentProgress
        };

        sessionLog.Add(entry);
        FlushToDisk();
    }

    private void FlushToDisk()
    {
        try
        {
            var wrapper = new LogWrapper { entries = sessionLog };
            string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
            File.WriteAllText(logPath, json);
        }
        catch (Exception e)
        {
            // WebGLではファイルIO不可の場合があるため握り潰す
            Debug.LogWarning($"[LogManager] ログ保存失敗（WebGL制限の可能性）: {e.Message}");
        }
    }

    // ──────────────────────────────────────
    // データ構造
    // ──────────────────────────────────────
    [Serializable]
    public class LogEntry
    {
        public string actionID;
        public string result;         // "correct" or "wrong"
        public float  timestamp;
        public string distortionLevel;
        public int    progress;
    }

    [Serializable]
    private class LogWrapper
    {
        public List<LogEntry> entries;
    }
}
