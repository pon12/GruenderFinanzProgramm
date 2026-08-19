using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using System.IO;
using System.Linq;

public class AuthService
{
    private AuthDatabaseService authDatabaseService = new AuthDatabaseService();

    public string passkeyGlobal;
    public string recoveryPassKeyGlobal;


    public string getRecoveryPassKey()
    {
        var result = new StringBuilder();
        string tempRecoveryKey = recoveryPassKeyGlobal;
        for (int i = 0; i < tempRecoveryKey.Length; i++)
        {
            result.Append(tempRecoveryKey[i]);
            if ((i + 1) % 4 == 0 && i != tempRecoveryKey.Length - 1)
            {
                result.Append(' ');
            }
        }
        return result.ToString();
    }

    public PassKeyRecord registerUser(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("Registrierung fehlgeschlagen: Kein Nutzername eingegeben.");
            return null;
        }

        if (authDatabaseService.usernameExists(username))
        {
            Debug.LogError("Registrierung fehlgeschlagen: Nutzername existiert bereits.");
            return null;
        }

        string userId = "user_" + System.DateTime.Now.Ticks;

        string passKey = generateUniquePassKey();
        passkeyGlobal = passKey;
        string recoveryKey = generateUniqueRecoveryKey();
        recoveryPassKeyGlobal = recoveryKey;

        Debug.Log(getRecoveryPassKey());

        string passKeyHash = hashValue(passKey);
        string recoveryKeyHash = hashValue(recoveryKey);

        string databaseName = authDatabaseService.createUserDatabase(username);

        PassKeyRecord record = new PassKeyRecord(
            userId,
            username,
            passKeyHash,
            recoveryKeyHash,
            databaseName
        );

        authDatabaseService.createAuthUser(record);

        Debug.Log("Registrierung erfolgreich.");
        Debug.Log("Nutzer: " + username);
        Debug.Log("PassKey: " + passKey);
        Debug.Log("RecoveryKey: " + recoveryKey);
        Debug.Log("Nutzerdatenbank: " + databaseName);
        Debug.Log("Hinweis: In der userData.db werden nur Hashwerte gespeichert.");
        Debug.Log("Auth-Datenbank gespeichert unter: " + authDatabaseService.getAuthDatabasePath());

        return record;
    }

    public PassKeyRecord loginWithPassKey(string enteredPassKey)
    {
        if (string.IsNullOrWhiteSpace(enteredPassKey))
        {
            Debug.LogError("Login fehlgeschlagen: Kein PassKey eingegeben.");
            return null;
        }

        passkeyGlobal = "Kein Passkey Gespeichert";
        recoveryPassKeyGlobal = "Kein Recovery Key Gespeichert";

        string enteredPassKeyHash = hashValue(enteredPassKey);

        PassKeyRecord user = authDatabaseService.getUserByPassKeyHash(enteredPassKeyHash);

        if (user == null)
        {
            Debug.LogError("Login fehlgeschlagen: PassKey ist ungültig.");
            return null;
        }

        Debug.Log("Willkommen zurück, " + user.username + ".");
        Debug.Log("Aktive Nutzerdatenbank: " + user.databaseName);

        return user;
    }

    public string resetPassKeyWithRecoveryKey(string enteredRecoveryKey)
    {
        if (string.IsNullOrWhiteSpace(enteredRecoveryKey))
        {
            Debug.LogError("Reset fehlgeschlagen: Kein RecoveryKey eingegeben.");
            return null;
        }

        string enteredRecoveryKeyHash = hashValue(enteredRecoveryKey);

        PassKeyRecord user = authDatabaseService.getUserByRecoveryKeyHash(enteredRecoveryKeyHash);

        if (user == null)
        {
            Debug.LogError("Reset fehlgeschlagen: RecoveryKey ist ungültig.");
            return null;
        }

        string newPassKey = generateUniquePassKey();
        string newPassKeyHash = hashValue(newPassKey);

        authDatabaseService.updatePassKeyHash(user, newPassKeyHash);

        Debug.Log("PassKey erfolgreich zurückgesetzt.");
        Debug.Log("Nutzer: " + user.username);
        Debug.Log("Neuer PassKey: " + newPassKey);
        Debug.Log("Hinweis: Neuer PassKey wurde nur als Hash gespeichert.");

        return newPassKey;
    }

    public bool deleteLocalProfileWithRecoveryKey(string enteredRecoveryKey, PassKeyRecord expectedUser)
    {
        if (string.IsNullOrWhiteSpace(enteredRecoveryKey))
        {
            Debug.LogError("Löschen fehlgeschlagen: Kein RecoveryKey eingegeben.");
            return false;
        }

        string cleanedRecoveryKey = new string(enteredRecoveryKey.Where(char.IsDigit).ToArray());

        if (cleanedRecoveryKey.Length != 16)
        {
            Debug.LogError("Löschen fehlgeschlagen: RecoveryKey muss 16 Ziffern enthalten.");
            return false;
        }

        string recoveryKeyHash = hashValue(cleanedRecoveryKey);
        PassKeyRecord user = authDatabaseService.getUserByRecoveryKeyHash(recoveryKeyHash);

        if (user == null)
        {
            Debug.LogError("Löschen fehlgeschlagen: RecoveryKey ist ungültig.");
            return false;
        }

        if (expectedUser == null)
        {
            Debug.LogError("Löschen fehlgeschlagen: Kein aktiver Nutzer vorhanden.");
            return false;
        }

        if (user.databaseName != expectedUser.databaseName)
        {
            Debug.LogError("Löschen fehlgeschlagen: RecoveryKey gehört nicht zum aktuell angemeldeten Nutzer.");
            return false;
        }

        string userDatabasePath = Path.Combine(Application.persistentDataPath, user.databaseName + ".db");

        try
        {
            GlobalDatabaseManager.Instance.CloseDatabase(user.databaseName);
        }
        catch
        {
            // Wenn die DB nicht offen oder nicht registriert ist, ist das kein Blocker.
        }

        bool databaseDeleted = true;

        try
        {
            if (File.Exists(userDatabasePath))
            {
                File.Delete(userDatabasePath);
                Debug.Log("Nutzer-Datenbank gelöscht: " + userDatabasePath);
            }
            else
            {
                Debug.LogWarning("Nutzer-Datenbank war nicht vorhanden: " + userDatabasePath);
            }
        }
        catch (System.Exception ex)
        {
            databaseDeleted = false;
            Debug.LogError("Nutzer-Datenbank konnte nicht gelöscht werden: " + ex.Message);
        }

        bool authDeleted = authDatabaseService.deleteAuthUserByRecoveryKeyHash(recoveryKeyHash);

        if (!authDeleted)
        {
            Debug.LogError("Lokalprofil nicht vollständig gelöscht: User-Eintrag konnte nicht entfernt werden.");
            return false;
        }

        if (!databaseDeleted)
        {
            Debug.LogError("Lokalprofil nicht vollständig gelöscht: Nutzer-Datenbank konnte nicht entfernt werden.");
            return false;
        }

        Debug.Log("Lokalprofil vollständig gelöscht: " + user.username);
        return true;
    }

    private string generateUniquePassKey()
    {
        string passKey;
        string passKeyHash;

        do
        {
            passKey = Random.Range(1000, 10000).ToString();
            passKeyHash = hashValue(passKey);
        }
        while (authDatabaseService.passKeyHashExists(passKeyHash));

        return passKey;
    }

    private string generateUniqueRecoveryKey()
    {
        string recoveryKey;
        string recoveryKeyHash;

        do
        {
            recoveryKey = generateNumericKey(16);
            recoveryKeyHash = hashValue(recoveryKey);
        }
        while (authDatabaseService.recoveryKeyHashExists(recoveryKeyHash));

        return recoveryKey;
    }

    private string generateNumericKey(int length)
    {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < length; i++)
        {
            int digit = Random.Range(0, 10);
            builder.Append(digit);
        }

        return builder.ToString();
    }

    private string hashValue(string value)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(value);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            StringBuilder builder = new StringBuilder();

            foreach (byte b in hashBytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }

    public string getStoragePath()
    {
        return authDatabaseService.getAuthDatabasePath();
    }
}