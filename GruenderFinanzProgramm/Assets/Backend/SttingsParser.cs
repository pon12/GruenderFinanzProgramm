using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
public static class SettingsParser
{
    private static string settingsFilePath = "";
    private static Dictionary<string, string> settings = new Dictionary<string, string>();
    // Standard-Werte falls Settings.txt fehlt oder Eintrag nicht vorhanden
    private static readonly Dictionary<string, string> defaults = new Dictionary<string, string>
    {
        { "DEBUGLOG", "false" },
        { "THEME",    "0"     }
        // Weitere Settings hier einfach ergänzen
    };
    
    // Initialisierung //
  
    public static void Initialize()
    {
        settingsFilePath = Path.Combine(Application.persistentDataPath, "Settings.txt");
        // Settings.txt anlegen falls nicht vorhanden
        if (!File.Exists(settingsFilePath))
        {
            CreateDefaultSettings();
        }
        // Einlesen
        Parse();
    }
    
    // Settings.txt erstellen //
  
    private static void CreateDefaultSettings()
    {
        var lines = new List<string>
        {
            "# GruenderFinanz Settings",
            "# Aenderungen werden beim naechsten Programmstart wirksam",
            "",
            "# Debug-Log aktivieren (true/false)",
            "DEBUGLOG=false",
            "",
            "# Theme: 0 = Dark, 1 = White (weitere Werte fuer zukuenftige Themes)",
            "THEME=0"
            // Weitere Settings hier ergaenzen
        };
        File.WriteAllLines(settingsFilePath, lines);
        Debug.Log($"Settings.txt erstellt unter: {settingsFilePath}");
    }
  
    // Parsen //
    
    private static void Parse()
    {
        settings.Clear();
        try
        {
            string[] lines = File.ReadAllLines(settingsFilePath);
            foreach (string line in lines)
            {
                // Kommentare und Leerzeilen überspringen
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                // Key=Value aufteilen
                int separatorIndex = line.IndexOf('=');
                if (separatorIndex < 0) continue;
                string key   = line.Substring(0, separatorIndex).Trim().ToUpper();
                string value = line.Substring(separatorIndex + 1).Trim();
                settings[key] = value;
            }
            Debug.Log($"Settings.txt geladen: {settings.Count} Einträge.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Fehler beim Parsen der Settings.txt: {ex.Message}");
        }
    }
   
    // Getter //
   
    public static string GetString(string key)
    {
        key = key.ToUpper();
        if (settings.ContainsKey(key))
            return settings[key];
        // Fallback auf Default-Wert
        if (defaults.ContainsKey(key))
            return defaults[key];
        return "";
    }
    public static bool GetBool(string key)
    {
        return GetString(key).ToLower() == "true";
    }
    public static int GetInt(string key)
    {
        if (int.TryParse(GetString(key), out int result))
            return result;
        // Fallback auf Default
        if (defaults.ContainsKey(key.ToUpper()) &&
            int.TryParse(defaults[key.ToUpper()], out int defaultResult))
            return defaultResult;
        return 0;
    }

    // Setter (Wert in Datei ändern) //
 
    public static void SetValue(string key, string value)
    {
        key = key.ToUpper();
        settings[key] = value;
        SaveSettings();
    }
    public static void SetValue(string key, bool value)
    {
        SetValue(key, value.ToString().ToLower());
    }
    public static void SetValue(string key, int value)
    {
        SetValue(key, value.ToString());
    }
  
    // In Datei zurückschreiben //
   
    private static void SaveSettings()
    {
        try
        {
            if (!File.Exists(settingsFilePath))
            {
                CreateDefaultSettings();
                return;
            }
            string[] lines = File.ReadAllLines(settingsFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]) || lines[i].StartsWith("#"))
                    continue;
                int separatorIndex = lines[i].IndexOf('=');
                if (separatorIndex < 0) continue;
                string existingKey = lines[i].Substring(0, separatorIndex).Trim().ToUpper();
                if (settings.ContainsKey(existingKey))
                    lines[i] = $"{existingKey}={settings[existingKey]}";
            }
            File.WriteAllLines(settingsFilePath, lines);
            Debug.Log($"Settings.txt gespeichert.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Fehler beim Speichern der Settings.txt: {ex.Message}");
        }
    }
}
