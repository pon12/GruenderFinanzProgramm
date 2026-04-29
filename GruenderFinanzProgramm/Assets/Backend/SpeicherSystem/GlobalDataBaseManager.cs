using System.Collections.Generic;
using UnityEngine;

public class GlobalDatabaseManager : MonoBehaviour
{
    private static GlobalDatabaseManager instance;
    private Dictionary<string, DatabaseManager> databases = new Dictionary<string, DatabaseManager>();

    public static GlobalDatabaseManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("GlobalDatabaseManager");
                instance = obj.AddComponent<GlobalDatabaseManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public T GetOrCreateDatabase<T>(string databaseName) where T : DatabaseManager
    {
        if (databases.ContainsKey(databaseName))
        {
            return databases[databaseName] as T;
        }

        GameObject dbObj = new GameObject($"Database_{databaseName}");
        dbObj.transform.SetParent(transform);
        T manager = dbObj.AddComponent<T>();
        manager.initializeDatabase(databaseName);
        
        databases[databaseName] = manager;
        return manager;
    }


    public T GetDatabase<T>(string databaseName) where T : DatabaseManager
    {
        if (databases.ContainsKey(databaseName))
        {
            return databases[databaseName] as T;
        }
        Debug.LogWarning($"Database '{databaseName}' not found");
        return null;
    }


    public void CloseDatabase(string databaseName)
    {
        if (databases.ContainsKey(databaseName))
        {
            databases[databaseName].close();
            databases.Remove(databaseName);
        }
    }


    public void CloseAllDatabases()
    {
        foreach (var database in databases.Values)
        {
            database.close();
        }
        databases.Clear();
    }

    private void OnDestroy()
    {
        CloseAllDatabases();
    }
}
