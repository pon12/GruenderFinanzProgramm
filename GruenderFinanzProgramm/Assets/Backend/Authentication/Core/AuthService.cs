using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class AuthService
{
    private PassKeyStorage passKeyStorage = new PassKeyStorage();

    public PassKeyRecord registerUser(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("Registrierung fehlgeschlagen: Kein Nutzername eingegeben.");
            return null;
        }

        if (usernameExists(username))
        {
            Debug.LogError("Registrierung fehlgeschlagen: Nutzername existiert bereits.");
            return null;
        }

        string userId = "user_" + System.DateTime.Now.Ticks;

        string passKey = generateUniquePassKey();
        string recoveryKey = generateUniqueRecoveryKey();

        string passKeyHash = hashValue(passKey);
        string recoveryKeyHash = hashValue(recoveryKey);

        PassKeyRecord record = new PassKeyRecord(userId, username, passKeyHash, recoveryKeyHash);

        passKeyStorage.saveRecord(record);

        Debug.Log("Registrierung erfolgreich.");
        Debug.Log("Nutzer: " + username);
        Debug.Log("PassKey: " + passKey);
        Debug.Log("RecoveryKey: " + recoveryKey);
        Debug.Log("Hinweis: In der Datei werden nur Hashwerte gespeichert.");
        Debug.Log("Gespeichert unter: " + passKeyStorage.getStoragePath());

        return record;
    }

    public PassKeyRecord loginWithPassKey(string enteredPassKey)
    {
        if (string.IsNullOrWhiteSpace(enteredPassKey))
        {
            Debug.LogError("Login fehlgeschlagen: Kein PassKey eingegeben.");
            return null;
        }

        string enteredPassKeyHash = hashValue(enteredPassKey);
        List<PassKeyRecord> records = passKeyStorage.getAllRecords();

        foreach (PassKeyRecord record in records)
        {
            if (record.passKeyHash == enteredPassKeyHash)
            {
                Debug.Log("Willkommen zurück, " + record.username + ".");
                return record;
            }
        }

        Debug.LogError("Login fehlgeschlagen: PassKey ist ungültig.");
        return null;
    }

    public string resetPassKeyWithRecoveryKey(string enteredRecoveryKey)
    {
        if (string.IsNullOrWhiteSpace(enteredRecoveryKey))
        {
            Debug.LogError("Reset fehlgeschlagen: Kein RecoveryKey eingegeben.");
            return null;
        }

        string enteredRecoveryKeyHash = hashValue(enteredRecoveryKey);
        List<PassKeyRecord> records = passKeyStorage.getAllRecords();

        foreach (PassKeyRecord record in records)
        {
            if (record.recoveryKeyHash == enteredRecoveryKeyHash)
            {
                string newPassKey = generateUniquePassKey();
                record.passKeyHash = hashValue(newPassKey);

                passKeyStorage.overwriteAllRecords(records);

                Debug.Log("PassKey erfolgreich zurückgesetzt.");
                Debug.Log("Nutzer: " + record.username);
                Debug.Log("Neuer PassKey: " + newPassKey);
                Debug.Log("Hinweis: Neuer PassKey wurde nur als Hash gespeichert.");

                return newPassKey;
            }
        }

        Debug.LogError("Reset fehlgeschlagen: RecoveryKey ist ungültig.");
        return null;
    }

    private string generateUniquePassKey()
    {
        List<PassKeyRecord> records = passKeyStorage.getAllRecords();
        string passKey;

        do
        {
            passKey = Random.Range(1000, 10000).ToString();
        }
        while (passKeyHashExists(hashValue(passKey), records));

        return passKey;
    }

    private string generateUniqueRecoveryKey()
    {
        List<PassKeyRecord> records = passKeyStorage.getAllRecords();
        string recoveryKey;

        do
        {
            recoveryKey = generateNumericKey(16);
        }
        while (recoveryKeyHashExists(hashValue(recoveryKey), records));

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

    private bool usernameExists(string username)
    {
        List<PassKeyRecord> records = passKeyStorage.getAllRecords();

        foreach (PassKeyRecord record in records)
        {
            if (record.username == username)
            {
                return true;
            }
        }

        return false;
    }

    private bool passKeyHashExists(string passKeyHash, List<PassKeyRecord> records)
    {
        foreach (PassKeyRecord record in records)
        {
            if (record.passKeyHash == passKeyHash)
            {
                return true;
            }
        }

        return false;
    }

    private bool recoveryKeyHashExists(string recoveryKeyHash, List<PassKeyRecord> records)
    {
        foreach (PassKeyRecord record in records)
        {
            if (record.recoveryKeyHash == recoveryKeyHash)
            {
                return true;
            }
        }

        return false;
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
        return passKeyStorage.getStoragePath();
    }
}