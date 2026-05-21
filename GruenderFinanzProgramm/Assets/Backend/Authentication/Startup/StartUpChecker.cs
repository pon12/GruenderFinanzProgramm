using System.Collections.Generic;
using UnityEngine;

public class StartUpChecker : MonoBehaviour
{
    [SerializeField] private string version = "0.1.0";

    private void Start()
    {
        Debug.Log("===== StartUpChecker gestartet =====");
        Debug.Log("Version: " + version);

        checkUserDataDatabase();
        checkUserDatabases();

        Debug.Log("===== StartUpChecker fertig =====");
    }

    private void checkUserDataDatabase()
    {
        DataBase userDataDB = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("userData");
        userDataDB.setupAuthDB();

        Debug.Log("userData.db erkannt/erstellt: " + userDataDB.getDatabasePath());
    }

    private void checkUserDatabases()
    {
        DataBase userDataDB = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("userData");
        List<UserDB> users = userDataDB.getAllUsers();

        Debug.Log("Gefundene Nutzer in userData.db: " + users.Count);

        foreach (UserDB user in users)
        {
            DataBase userDatabase = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>(user.name);
            userDatabase.setupDatabase();

            Debug.Log("NutzerDB erkannt/erstellt: " + user.name + ".db");
        }
    }
}