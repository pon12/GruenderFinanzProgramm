using UnityEngine;
using SQLite;
using System.IO;

public class SQLiteConnectionTest : MonoBehaviour
{
    private void Start()
    {
        string testPath = Path.Combine(Application.persistentDataPath, "sqlite_test.db");
        testPath = Path.GetFullPath(testPath);

        Debug.Log("SQLite Testpfad: " + testPath);

        try
        {
            SQLiteConnection connection = new SQLiteConnection(
                testPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create
            );

            connection.CreateTable<SQLiteTestTable>();
            connection.Close();

            Debug.Log("SQLite Verbindungstest erfolgreich.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SQLite Verbindungstest fehlgeschlagen: " + ex);
        }
    }
}

public class SQLiteTestTable
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public string testText { get; set; }
}