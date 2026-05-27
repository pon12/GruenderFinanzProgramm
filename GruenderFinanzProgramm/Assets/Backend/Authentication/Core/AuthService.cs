using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class AuthService
{
    private AuthDatabaseService authDatabaseService = new AuthDatabaseService();

    public string passkeyGlobal;
    public string recoveryPassKeyGlobal;

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