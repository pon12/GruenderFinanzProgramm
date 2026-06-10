using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework.Internal;

public class SaveTest1 : MonoBehaviour
{
    private DataBase userAuthDB;

    private void Start()
    {
        // Initialize central user database
        userAuthDB = GlobalDatabaseManager.Instance
            .GetOrCreateDatabase<DataBase>("userData");

        userAuthDB.setupAuthDB();

        // Create sample users
        CreateSampleUsers();

        // Create dynamic databases for each user and their companies
        CreateUserDatabases();

        // Display all data
        RetrieveAndDisplayData();
    }
    private void CreateSampleUsers()
    {
        // Create users in central userAuthDB
        userAuthDB.createUser(
            "Nutzer1",
            "PassKeyHash1",
            "RecoveryPassKeyHash1"
        );
        userAuthDB.createUser(
            "Nutzer2",
            "PassKeyHash2",
            "RecoveryPassKeyHash2"
        );

        Debug.Log("Sample users created in central database.");
    }

    private void CreateUserDatabases()
    {
        // Get all users from central userAuthDB
        List<UserDB> allUsers = userAuthDB.getAllUsers();

        foreach (UserDB user in allUsers)
        {
            // Create individual database for each user
            DataBase userDatabase = GlobalDatabaseManager.Instance
                .GetOrCreateDatabase<DataBase>(user.name);

            userDatabase.setupDatabase();

            // Create companies for this user
            CreateCompaniesForUser(user.name, userDatabase);
        }
    }
    private void CreateCompaniesForUser(string userName, DataBase userDatabase)
    {
        if (userName == "Nutzer1")
        {
            userDatabase.createCompany(
                "Firma1",
                1,
                1,
                "Berlin1",
                "321123",
                "1990",
                "HRB12345",
                "Musterstraße 1",
                "12345",
                "DE123456789"
            );

            userDatabase.createCompany(
                "Firma2",
                2,
                2,
                "Berlin2",
                "654321",
                "2000",
                "HRB54321",
                "Musterstraße 2",
                "54321",
                "DE987654321"
            );
        }
        else if (userName == "Nutzer2")
        {
            userDatabase.createCompany(
                "Firma1-2",
                1,
                1,
                "Berlin1-2",
                "321123",
                "1990",
                "HRB12345", 
                "Musterstraße 1",
                "12345",
                "DE123456789"
            );

            userDatabase.createCompany(
                "Firma2-2",
                2,
                2,
                "Berlin2-2",                
                "654321",
                "2000",
                "HRB54321",
                "Musterstraße 2",
                "54321",
                "DE987654321"
            );
        }

        Debug.Log($"Companies created for user: {userName}");
    }

    private void RetrieveAndDisplayData()
    {
        // Get all users from central userAuthDB
        List<UserDB> allUsers = userAuthDB.getAllUsers();

        Debug.Log($"\n========== TOTAL USERS: {allUsers.Count} ==========");

        // Iterate through each user
        foreach (UserDB user in allUsers)
        {
            Debug.Log($"\n========== User: {user.name} ==========");

            // Get the user's associated database
            DataBase userDatabase = GlobalDatabaseManager.Instance
                .GetDatabase<DataBase>(user.name);

            if (userDatabase != null)
            {
                // Get all companies from that user's database
                List<Company> userCompanies = userDatabase.getAllCompanies();

                Debug.Log($"Companies count: {userCompanies.Count}");

                foreach (Company company in userCompanies)
                {
                    Debug.Log("----------------------------------");
                    Debug.Log($"ID: {company.id}");
                    Debug.Log($"Name: {company.name}");
                    Debug.Log($"Rechtsform: {company.legalForm}");
                    Debug.Log($"Branche: {company.industry}");
                    Debug.Log($"Standort: {company.location}");
                }
            }
            else
            {
                Debug.LogWarning(
                    $"Database not found for user: {user.name}"
                );
            }
        }
    }
    private void OnDestroy()
    {
        GlobalDatabaseManager.Instance.CloseAllDatabases();
    }
}