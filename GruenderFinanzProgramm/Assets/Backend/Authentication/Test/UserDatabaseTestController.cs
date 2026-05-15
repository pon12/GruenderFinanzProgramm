using UnityEngine;

public class UserDatabaseTestController : MonoBehaviour
{
    private UserDatabaseService userDatabaseService;

    private void Start()
    {
        userDatabaseService = new UserDatabaseService();
    }

    public void testDatabaseAccess()
    {
        userDatabaseService.canAccessCurrentUserDatabase();
    }

    public void printActiveDatabasePath()
    {
        string path = userDatabaseService.getActiveDatabasePath();

        if (!string.IsNullOrWhiteSpace(path))
        {
            Debug.Log("Frontend würde automatisch diese Datenbank verwenden: " + path);
        }
    }
}