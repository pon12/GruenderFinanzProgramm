
// Damit kann Frontend später einfach sagen:
// DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
// bekommt automatisch:
// Alex eingeloggt -> Alex.db
// Micha eingeloggt -> Micha.db


using UnityEngine;

public static class UserDatabaseAccess
{
    public static DataBase getCurrentUserDatabase()
    {
        if (StateManager.Instance == null)
        {
            Debug.LogError("UserDatabaseAccess: Kein StateManager gefunden.");
            return null;
        }

        if (!StateManager.Instance.isLoggedIn())
        {
            Debug.LogError("UserDatabaseAccess: Kein Nutzer ist eingeloggt.");
            return null;
        }

        PassKeyRecord currentUser = StateManager.Instance.getCurrentUser();

        if (currentUser == null)
        {
            Debug.LogError("UserDatabaseAccess: currentUser ist null.");
            return null;
        }

        DataBase userDatabase = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>(currentUser.databaseName);
        userDatabase.setupDatabase();

        Debug.Log("UserDatabaseAccess: Aktive Nutzerdatenbank = " + currentUser.databaseName);

        return userDatabase;
    }

    public static string getCurrentDatabaseName()
    {
        if (StateManager.Instance == null || !StateManager.Instance.isLoggedIn())
        {
            return null;
        }

        PassKeyRecord currentUser = StateManager.Instance.getCurrentUser();

        if (currentUser == null)
        {
            return null;
        }

        return currentUser.databaseName;
    }
}