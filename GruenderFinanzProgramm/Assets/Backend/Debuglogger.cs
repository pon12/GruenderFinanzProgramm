using UnityEngine;
using System;
using System.IO;
public class DebugLogger : MonoBehaviour
{
    private static DebugLogger instance;
    private static bool isEnabled = false;
    private static string logFilePath = "";
    private static string settingsFilePath = "";
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
    
        string basePath = Application.persistentDataPath;
        settingsFilePath = Path.Combine(basePath, "Settings.txt");
        // Settings.txt anlegen falls nicht vorhanden
        if (!File.Exists(settingsFilePath))
        {
            File.WriteAllText(settingsFilePath, "DEBUGLOG=false");
            Debug.Log($"Settings.txt erstellt unter: {settingsFilePath}");
        }
    
        isEnabled = ParseDebugSetting();
        if (isEnabled)
        {
            // Debug-Ordner anlegen
            string debugFolder = Path.Combine(basePath, "Debug");
            Directory.CreateDirectory(debugFolder);
            // Dateiname mit Datum und Zeit des Programmstarts
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(debugFolder, $"DebugLog_{timestamp}.txt");
            // Log-Datei 
            File.WriteAllText(logFilePath, $"=== DebugLog gestartet: {DateTime.Now} ===\n\n");
            // Unity Log-Handler registrieren
            Application.logMessageReceived += HandleLog;
            Debug.Log("✅ DebugLogger aktiv.");
        }
        else
        {
            Debug.Log("DebugLogger ist deaktiviert (DEBUGLOG=false in Settings.txt).");
        }
    }
    private bool ParseDebugSetting()
    {
        try
        {
            string[] lines = File.ReadAllLines(settingsFilePath);
            foreach (string line in lines)
            {
                // Leerzeilen und Kommentare überspringen
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                // Nach DEBUGLOG= suchen
                if (line.StartsWith("DEBUGLOG="))
                {
                    string value = line.Substring("DEBUGLOG=".Length).Trim().ToLower();
                    return value == "true";
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Fehler beim Parsen der Settings.txt: {ex.Message}");
        }
        return false;
    }
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (!isEnabled || string.IsNullOrEmpty(logFilePath)) return;
        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logTypeLabel = type switch
            {
                LogType.Error     => "[ERROR]",
                LogType.Warning   => "[WARNING]",
                LogType.Exception => "[EXCEPTION]",
                LogType.Assert    => "[ASSERT]",
                _                 => "[LOG]"
            };
            string entry = $"[{timestamp}] {logTypeLabel} {logString}";
  
            if (type == LogType.Error || type == LogType.Exception)
                entry += $"\n  StackTrace: {stackTrace}";
            File.AppendAllText(logFilePath, entry + "\n");
        }
        catch (Exception ex)
        {
            // Nicht nochmal loggen sonst endlos
            Console.WriteLine($"DebugLogger Fehler: {ex.Message}");
        }
    }
    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
}