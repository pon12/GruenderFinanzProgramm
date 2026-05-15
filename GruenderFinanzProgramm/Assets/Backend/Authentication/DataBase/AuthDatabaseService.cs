using System.IO;
using UnityEngine;

public class AuthDatabaseService
{
    private const string authDatabaseName = "userData";

    private DataBase authDatabase;

    public AuthDatabaseService()
    {
        authDatabase = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>(authDatabaseName);
        authDatabase.setupAuthDatabase();

        Debug.Log("Auth-Datenbank bereit: " + authDatabase.getDatabasePath());
    }


    public bool usernameExists(string username)
    {
        return authDatabase.authUsernameExistsExact(username);
    }

    public bool passKeyHashExists(string passKeyHash)
    {
        return authDatabase.authPassKeyHashExists(passKeyHash);
    }

    public bool recoveryKeyHashExists(string recoveryKeyHash)
    {
        return authDatabase.authRecoveryKeyHashExists(recoveryKeyHash);
    }

    public void createAuthUser(PassKeyRecord record)
    {
        authDatabase.createAuthUser(
            record.userId,
            record.username,
            record.passKeyHash,
            record.recoveryKeyHash,
            record.databaseName
        );
    }

    public PassKeyRecord getUserByPassKeyHash(string passKeyHash)
    {
        AuthUserDB user = authDatabase.getAuthUserByPassKeyHash(passKeyHash);

        if (user == null)
        {
            return null;
        }

        return convertToPassKeyRecord(user);
    }

    public PassKeyRecord getUserByRecoveryKeyHash(string recoveryKeyHash)
    {
        AuthUserDB user = authDatabase.getAuthUserByRecoveryKeyHash(recoveryKeyHash);

        if (user == null)
        {
            return null;
        }

        return convertToPassKeyRecord(user);
    }

    public void updatePassKeyHash(PassKeyRecord record, string newPassKeyHash)
    {
        AuthUserDB user = authDatabase.getAuthUserByRecoveryKeyHash(record.recoveryKeyHash);

        if (user == null)
        {
            Debug.LogError("PassKey konnte nicht aktualisiert werden: Nutzer wurde nicht gefunden.");
            return;
        }

        authDatabase.updateAuthUserPassKeyHash(user, newPassKeyHash);
    }

    public string createUserDatabase(string username)
    {
        string databaseName = createSafeDatabaseName(username);

        DataBase userDatabase = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>(databaseName);
        userDatabase.setupUserDatabase();

        Debug.Log("Nutzerdatenbank erstellt/bereit: " + userDatabase.getDatabasePath());

        return databaseName;
    }

    public string getAuthDatabasePath()
    {
        return authDatabase.getDatabasePath();
    }

    private PassKeyRecord convertToPassKeyRecord(AuthUserDB user)
    {
        return new PassKeyRecord(
            user.userId,
            user.username,
            user.passKeyHash,
            user.recoveryKeyHash,
            user.databaseName
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