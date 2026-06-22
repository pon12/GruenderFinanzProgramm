using System.Collections.Generic;
using System.IO;
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

            checkUserPDFFolder(user);

            Debug.Log("NutzerDB erkannt/erstellt: " + user.name + ".db");
        }
    }

    private void checkUserPDFFolder(UserDB user)
    {
        string pdfFolderPath = System.IO.Path.Combine(
            Application.persistentDataPath,
            "PDFs",
            "User_" + user.id
        );

        if (!System.IO.Directory.Exists(pdfFolderPath))
        {
            System.IO.Directory.CreateDirectory(pdfFolderPath);
            Debug.Log("PDF-Ordner erstellt: " + pdfFolderPath);
        }
        else
        {
            Debug.Log("PDF-Ordner erkannt: " + pdfFolderPath);
        }
    }
}