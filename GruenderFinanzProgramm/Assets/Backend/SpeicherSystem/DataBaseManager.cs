using UnityEngine;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


/// Generic database manager - inherit from this for specific database implementations

public abstract class DatabaseManager : MonoBehaviour
{
    protected SQLiteConnection database;
    protected string databasePath;
    protected string databaseName;

    // Initialize the database with a custom name and optional path

    public virtual void initializeDatabase(string databaseName, string customPath = null)
    {
        this.databaseName = databaseName;

        if (string.IsNullOrEmpty(customPath))
        {
            databasePath = Path.Combine(Application.persistentDataPath, $"{databaseName}.db");
        }
        else
        {
            databasePath = Path.Combine(customPath, $"{databaseName}.db");
        }

        databasePath = Path.GetFullPath(databasePath);

        try
        {
            database = new SQLiteConnection(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            Debug.Log($"Database '{this.databaseName}' initialized at: {databasePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize database '{this.databaseName}': {ex.Message}");
        }
    }


    // Create a table for a specific data type

    public virtual void createTable<T>() where T : new()
    {
        if (database == null)
        {
            Debug.LogError($"Database '{databaseName}' is not initialized. Cannot create table '{typeof(T).Name}'.");
            return;
        }

        try
        {
            database.CreateTable<T>();
            Debug.Log($"Table for type '{typeof(T).Name}' created successfully in database '{databaseName}'");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create table for type '{typeof(T).Name}': {ex.Message}");
        }
    }


    // Insert a single record

    public virtual int insert<T>(T item) where T : new()
    {
        if (database == null)
        {
            Debug.LogError($"Database '{databaseName}' is not initialized. Cannot insert '{typeof(T).Name}'.");
            return -1;
        }

        try
        {
            int result = database.Insert(item);
            Debug.Log($"Inserted record into '{typeof(T).Name}' table. Result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to insert record: {ex.Message}");
            return -1;
        }
    }


    // Insert multiple records

    public virtual int insertAll<T>(List<T> items) where T : new()
    {
        if (database == null)
        {
            Debug.LogError($"Database '{databaseName}' is not initialized. Cannot insert records into '{typeof(T).Name}'.");
            return -1;
        }

        try
        {
            int result = database.InsertAll(items);
            Debug.Log($"Inserted {result} records into '{typeof(T).Name}' table");
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to insert records: {ex.Message}");
            return -1;
        }
    }


    // Retrieve all records of a type

    public virtual List<T> getAll<T>() where T : new()
    {
        if (database == null)
        {
            Debug.LogError($"Database '{databaseName}' is not initialized. Cannot retrieve records from '{typeof(T).Name}'.");
            return new List<T>();
        }

        try
        {
            List<T> results = database.Table<T>().ToList();
            return results;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to retrieve records: {ex.Message}");
            return new List<T>();
        }
    }


    // Retrieve a record by ID

    public virtual T getById<T>(int id) where T : new()
    {
        if (database == null)
        {
            Debug.LogError($"Database '{databaseName}' is not initialized. Cannot retrieve '{typeof(T).Name}' with ID {id}.");
            return default;
        }

        try
        {
            T result = database.Find<T>(id);
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to retrieve record with ID {id}: {ex.Message}");
            return default;
        }
    }


    // Update a record

    public virtual int update<T>(T item) where T : new()
    {
        if (database == null)
        {
            Debug.LogError($"Database '{databaseName}' is not initialized. Cannot update '{typeof(T).Name}'.");
            return -1;
        }

        try
        {
            int result = database.Update(item);
            Debug.Log($"Updated record in '{typeof(T).Name}' table. Result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to update record: {ex.Message}");
            return -1;
        }
    }


    // Delete a record by ID

    public virtual int delete<T>(int id) where T : new()
    {
        if (database == null)
        {
            Debug.LogError($"Database '{databaseName}' is not initialized. Cannot delete '{typeof(T).Name}' with ID {id}.");
            return -1;
        }

        try
        {
            T item = getById<T>(id);
            if (item != null)
            {
                int result = database.Delete(item);
                Debug.Log($"Deleted record from '{typeof(T).Name}' table. Result: {result}");
                return result;
            }

            Debug.LogWarning($"No '{typeof(T).Name}' record found with ID {id}. Nothing deleted.");
            return 0;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to delete record: {ex.Message}");
            return -1;
        }
    }


    public virtual List<T> query<T>(string queryString) where T : new()
    {
        if (database == null)
        {
            Debug.LogError($"Database '{databaseName}' is not initialized. Cannot execute query for '{typeof(T).Name}'.");
            return new List<T>();
        }

        try
        {
            List<T> results = database.Query<T>(queryString);
            return results;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to execute query: {ex.Message}");
            return new List<T>();
        }
    }


    public virtual void close()
    {
        if (database != null)
        {
            database.Close();
            Debug.Log($"Database '{databaseName}' closed");
        }
    }


    public virtual void deleteDatabase()
    {
        close();
        try
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
                Debug.Log($"Database file deleted: {databasePath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to delete database: {ex.Message}");
        }
    }


    public virtual long getDatabaseSize()
    {
        try
        {
            if (File.Exists(databasePath))
            {
                FileInfo fileInfo = new FileInfo(databasePath);
                return fileInfo.Length;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get database size: {ex.Message}");
        }
        return 0;
    }


    public string getDatabasePath() => databasePath;


    public string getDatabaseName() => databaseName;
}
