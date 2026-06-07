using UnityEngine;
using System;
using System.IO;
public class DebugLogger : MonoBehaviour
{
    private static DebugLogger instance;
    private static bool isEnabled = false;
    private static string logFilePath = "";
    public static DebugLogger Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("DebugLogger");
                instance = obj.AddComponent<DebugLogger>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        // Settings laden (SettingsParser kümmert sich um alles)
        SettingsParser.Initialize();
        // DebugLog-Status aus Settings lesen
        isEnabled = SettingsParser.GetBool("DEBUGLOG");
        if (isEnabled)
        {
            string debugFolder = Path.Combine(Application.persistentDataPath, "Debug");
            Directory.CreateDirectory(debugFolder);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(debugFolder, $"DebugLog_{timestamp}.txt");
            File.WriteAllText(logFilePath, $"=== DebugLog gestartet: {DateTime.Now} ===\n\n");
            Application.logMessageReceived += HandleLog;
            Debug.Log("DebugLogger aktiv.");
        }
        else
        {
            Debug.Log("DebugLogger deaktiviert (DEBUGLOG=false).");
        }
    }
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (!isEnabled || string.IsNullOrEmpty(logFilePath)) return;
        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string label = type switch
            {
                LogType.Error     => "[ERROR]",
                LogType.Warning   => "[WARNING]",
                LogType.Exception => "[EXCEPTION]",
                LogType.Assert    => "[ASSERT]",
                _                 => "[LOG]"
            };
            string entry = $"[{timestamp}] {label} {logString}";
            if (type == LogType.Error || type == LogType.Exception)
                entry += $"\n  StackTrace: {stackTrace}";
            File.AppendAllText(logFilePath, entry + "\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DebugLogger Fehler: {ex.Message}");
        }
    }
    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
}