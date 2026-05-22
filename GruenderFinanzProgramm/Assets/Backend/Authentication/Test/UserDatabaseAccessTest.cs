using UnityEngine;

public class UserDatabaseAccessTest : MonoBehaviour
{
    public void testCurrentUserDatabase()
    {
        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            Debug.LogError("Test fehlgeschlagen: Keine aktive Nutzerdatenbank gefunden.");
            return;
        }

        Debug.Log("Test erfolgreich: Aktive Nutzerdatenbank = " + db.getDatabaseName());
        Debug.Log("Pfad: " + db.getDatabasePath());
    }
}