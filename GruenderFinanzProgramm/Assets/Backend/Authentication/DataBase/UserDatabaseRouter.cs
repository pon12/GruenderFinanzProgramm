using System.IO;
using UnityEngine;


// Erkennt Nutzer über StateManager



public class UserDatabaseRouter
{
    private string databaseFolderPath;

    public UserDatabaseRouter()
    {
        databaseFolderPath = Path.Combine(Application.dataPath, "Backend/Databases");

        if (!Directory.Exists(databaseFolderPath))
        {
            Directory.CreateDirectory(databaseFolderPath);
        }
    }

    public string getCurrentUserDatabasePath()
    {
        if (StateManager.Instance == null)
        {
            Debug.LogError("Datenbankzugriff fehlgeschlagen: StateManager existiert nicht.");
            return null;
        }

        if (!StateManager.Instance.isLoggedIn())
        {
            Debug.LogError("Datenbankzugriff fehlgeschlagen: Kein Nutzer ist eingeloggt.");
            return null;
        }

        PassKeyRecord currentUser = StateManager.Instance.getCurrentUser();

        if (currentUser == null)
        {
            Debug.LogError("Datenbankzugriff fehlgeschlagen: Kein aktueller Nutzer gefunden.");
            return null;
        }

        string safeUsername = sanitizeFileName(currentUser.username);
        string databaseName = safeUsername + ".db";

        string databasePath = Path.Combine(databaseFolderPath, databaseName);

        Debug.Log("Aktive Nutzerdatenbank: " + databasePath);

        return databasePath;
    }

    private string sanitizeFileName(string fileName)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar.ToString(), "");
        }

        return fileName;
    }
}