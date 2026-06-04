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
        string pdfFolder = Path.Combine(
            Application.persistentDataPath,
            "Ventoriq",
            "PDFs",
            $"User_{user.id}"
        );

        if (!Directory.Exists(pdfFolder))
        {
            Directory.CreateDirectory(pdfFolder);
            Debug.Log("PDF-Ordner erstellt: " + pdfFolder);
        }
        else
        {
            Debug.Log("PDF-Ordner erkannt: " + pdfFolder);
        }
    }
}