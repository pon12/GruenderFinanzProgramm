using UnityEngine;

public class UserDatabaseService
{
    private UserDatabaseRouter userDatabaseRouter;

    public UserDatabaseService()
    {
        userDatabaseRouter = new UserDatabaseRouter();
    }

    public string getActiveDatabasePath()
    {
        return userDatabaseRouter.getCurrentUserDatabasePath();
    }

    public bool canAccessCurrentUserDatabase()
    {
        string databasePath = getActiveDatabasePath();

        if (string.IsNullOrWhiteSpace(databasePath))
        {
            Debug.LogError("Zugriff auf Nutzerdatenbank nicht möglich.");
            return false;
        }

        Debug.Log("Zugriff auf Nutzerdatenbank erlaubt: " + databasePath);
        return true;
    }
}