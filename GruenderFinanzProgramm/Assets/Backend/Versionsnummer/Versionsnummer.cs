using System.IO;
using UnityEngine;

public class Versionsnummer
{
    private const string FileName = "version.txt";
    private const string FallbackVersion = "Version unbekannt";

    public static void writeVersionToFile(string version)
    {
        string filePath = GetWritableVersionFilePath();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, NormalizeVersion(version));
            Debug.Log("[Versionsnummer] Versionsnummer gespeichert: " + filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Versionsnummer] Versionsnummer konnte nicht geschrieben werden: " + ex.Message);
        }
    }

    public static string getVersion()
    {
        string unityVersion = GetUnityApplicationVersion();

        if (!string.IsNullOrWhiteSpace(unityVersion))
            return unityVersion;

        string version = TryReadVersionFromPath(GetStreamingAssetsVersionFilePath());

        if (!string.IsNullOrWhiteSpace(version))
            return NormalizeVersion(version);

        version = TryReadVersionFromPath(GetEditorAssetsVersionFilePath());

        if (!string.IsNullOrWhiteSpace(version))
            return NormalizeVersion(version);

        version = TryReadVersionFromPath(GetWritableVersionFilePath());

        if (!string.IsNullOrWhiteSpace(version))
            return NormalizeVersion(version);

        Debug.LogWarning("[Versionsnummer] Keine gültige Versionsnummer gefunden. Fallback wird angezeigt.");
        return FallbackVersion;
    }

    private static string GetUnityApplicationVersion()
    {
        string version = Application.version;

        if (string.IsNullOrWhiteSpace(version))
            return null;

        // Unity-Default ignorieren, falls nicht bewusst gesetzt.
        if (version.Trim() == "1.0")
            return null;

        return "Version: " + version.Trim();
    }

    private static string TryReadVersionFromPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {
            if (!File.Exists(filePath))
                return null;

            return File.ReadAllText(filePath).Trim();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Versionsnummer] Konnte Versionsdatei nicht lesen: " + filePath + " | " + ex.Message);
            return null;
        }
    }

    private static string GetStreamingAssetsVersionFilePath()
    {
        return Path.Combine(Application.streamingAssetsPath, FileName);
    }

    private static string GetEditorAssetsVersionFilePath()
    {
        return Path.Combine(Application.dataPath, "Backend", "Versionsnummer", FileName);
    }

    private static string GetWritableVersionFilePath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
    }

    private static string NormalizeVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return FallbackVersion;

        version = version.Trim();

        if (version.StartsWith("Version:"))
            return version;

        return "Version: " + version;
    }
}