using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class AuthDatabaseService
{
    private const string authDatabaseName = "userData";

    private DataBase authDatabase;

    public AuthDatabaseService()
    {
        authDatabase = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>(authDatabaseName);
        authDatabase.setupAuthDB();

        Debug.Log("Auth-Datenbank bereit: " + authDatabase.getDatabasePath());
    }

    public bool usernameExists(string username)
    {
        List<UserDB> users = authDatabase.getAllUsers();

        foreach (UserDB user in users)
        {
            if (user.name == username)
            {
                return true;
            }
        }

        return false;
    }

    public bool passKeyHashExists(string passKeyHash)
    {
        return getUserDBByPassKeyHash(passKeyHash) != null;
    }

    public bool recoveryKeyHashExists(string recoveryKeyHash)
    {
        return getUserDBByRecoveryKeyHash(recoveryKeyHash) != null;
    }

    public void createAuthUser(PassKeyRecord record)
    {
        UserDB newUser = new UserDB
        {
            name = record.username,
            passKeyHash = record.passKeyHash,
            recoveryPassKeyHash = record.recoveryKeyHash
        };

        authDatabase.insert(newUser);
    }

    public PassKeyRecord getUserByPassKeyHash(string passKeyHash)
    {
        UserDB user = getUserDBByPassKeyHash(passKeyHash);

        if (user == null)
        {
            return null;
        }

        return convertToPassKeyRecord(user);
    }

    public PassKeyRecord getUserByRecoveryKeyHash(string recoveryKeyHash)
    {
        UserDB user = getUserDBByRecoveryKeyHash(recoveryKeyHash);

        if (user == null)
        {
            return null;
        }

        return convertToPassKeyRecord(user);
    }

    public void updatePassKeyHash(PassKeyRecord record, string newPassKeyHash)
    {
        UserDB user = getUserDBByRecoveryKeyHash(record.recoveryKeyHash);

        if (user == null)
        {
            Debug.LogError("PassKey konnte nicht aktualisiert werden: Nutzer wurde nicht gefunden.");
            return;
        }

        user.passKeyHash = newPassKeyHash;
        authDatabase.updateUser(user);
    }

    public string createUserDatabase(string username)
    {
        string databaseName = createSafeDatabaseName(username);

        DataBase userDatabase = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>(databaseName);
        userDatabase.setupDatabase();

        Debug.Log("Nutzerdatenbank erstellt/bereit: " + userDatabase.getDatabasePath());

        return databaseName;
    }

    public string getAuthDatabasePath()
    {
        return authDatabase.getDatabasePath();
    }

    private UserDB getUserDBByPassKeyHash(string passKeyHash)
    {
        List<UserDB> users = authDatabase.getAllUsers();

        foreach (UserDB user in users)
        {
            if (user.passKeyHash == passKeyHash)
            {
                return user;
            }
        }

        return null;
    }

    private UserDB getUserDBByRecoveryKeyHash(string recoveryKeyHash)
    {
        List<UserDB> users = authDatabase.getAllUsers();

        foreach (UserDB user in users)
        {
            if (user.recoveryPassKeyHash == recoveryKeyHash)
            {
                return user;
            }
        }

        return null;
    }

    private PassKeyRecord convertToPassKeyRecord(UserDB user)
    {
        return new PassKeyRecord(
            "user_" + user.id,
            user.name,
            user.passKeyHash,
            user.recoveryPassKeyHash,
            user.name
        );
    }

    private string createSafeDatabaseName(string username)
    {
        string safeName = username.Trim();

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidChar.ToString(), "");
        }

        safeName = safeName.Replace(" ", "_");

        return safeName;
    }
}