using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AuthService
{
    private PassKeyStorage passKeyStorage = new PassKeyStorage();

    public PassKeyRecord registerUser(string username, string companyName)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("Registrierung fehlgeschlagen: Kein Nutzername eingegeben.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(companyName))
        {
            Debug.LogError("Registrierung fehlgeschlagen: Kein Firmenname eingegeben.");
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

        PassKeyRecord record = new PassKeyRecord(userId, username, companyName, passKey, recoveryKey);

        passKeyStorage.saveRecord(record);

        Debug.Log("Registrierung erfolgreich.");
        Debug.Log("Nutzer: " + username);
        Debug.Log("Firma: " + companyName);
        Debug.Log("PassKey: " + passKey);
        Debug.Log("RecoveryKey: " + recoveryKey);
        Debug.Log("Gespeichert unter: " + passKeyStorage.getStoragePath());

        return record;
    }

    public PassKeyRecord loginWithPassKey(string enteredPassKey)
    {
        if (string.IsNullOrWhiteSpace(enteredPassKey))
        {
            Debug.LogError("Login fehlgeschlagen: Kein Passkey eingegeben.");
            return null;
        }

        List<PassKeyRecord> records = passKeyStorage.getAllRecords();

        foreach (PassKeyRecord record in records)
        {
            if (record.passKey == enteredPassKey)
            {
                Debug.Log("Willkommen zurück, " + record.username + ".");
                Debug.Log("Login erfolgreich für Firma: " + record.companyName);
                return record;
            }
        }

        Debug.LogError("Login fehlgeschlagen: Passkey wurde nicht gefunden.");
        return null;
    }

    public string resetPassKeyWithRecoveryKey(string enteredRecoveryKey)
    {
        if (string.IsNullOrWhiteSpace(enteredRecoveryKey))
        {
            Debug.LogError("Reset fehlgeschlagen: Kein RecoveryKey eingegeben.");
            return null;
        }

        List<PassKeyRecord> records = passKeyStorage.getAllRecords();

        foreach (PassKeyRecord record in records)
        {
            if (record.recoveryKey == enteredRecoveryKey)
            {
                string newPassKey = generateUniquePassKey();
                record.passKey = newPassKey;

                passKeyStorage.overwriteAllRecords(records);

                Debug.Log("Passkey erfolgreich zurückgesetzt.");
                Debug.Log("Nutzer: " + record.username);
                Debug.Log("Neuer PassKey: " + newPassKey);

                return newPassKey;
            }
        }

        Debug.LogError("Reset fehlgeschlagen: RecoveryKey ungültig.");
        return null;
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

    private string generateUniquePassKey()
    {
        List<PassKeyRecord> records = passKeyStorage.getAllRecords();

        string passKey;

        do
        {
            passKey = Random.Range(1000, 10000).ToString();
        }
        while (passKeyExists(passKey, records));

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
        while (recoveryKeyExists(recoveryKey, records));

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

    private bool passKeyExists(string passKey, List<PassKeyRecord> records)
    {
        foreach (PassKeyRecord record in records)
        {
            if (record.passKey == passKey)
            {
                return true;
            }
        }

        return false;
    }

    private bool recoveryKeyExists(string recoveryKey, List<PassKeyRecord> records)
    {
        foreach (PassKeyRecord record in records)
        {
            if (record.recoveryKey == recoveryKey)
            {
                return true;
            }
        }

        return false;
    }

    public string getStoragePath()
    {
        return passKeyStorage.getStoragePath();
    }
}