// Klasse zum Speichern der Keys in einer Datei


using System.IO;
using UnityEngine;

public class PassKeyStorage
{
    private string filePath;

    public PassKeyStorage()
    {
        filePath = Path.Combine(Application.persistentDataPath, "passkeys.txt");
    }

    public void saveKeys(string passKey, string recoveryKey)
    {
        string content = passKey + "\n" + recoveryKey;
        File.WriteAllText(filePath, content);
    }

    public string getPassKey()
    {
        if (!File.Exists(filePath))
            return null;

        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length < 1)
            return null;

        return lines[0];
    }

    public string getRecoveryKey()
    {
        if (!File.Exists(filePath))
            return null;

        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length < 2)
            return null;

        return lines[1];
    }

    public string getStoragePath()
    {
        return filePath;
    }
}