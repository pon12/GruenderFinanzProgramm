using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework.Internal;

public class SaveTest1 : MonoBehaviour
{
    private DataBase userAuthDB;
    
    private void Start()
    {
        // Initialize central user database
        userAuthDB = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("userData");
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
        userAuthDB.createUser("Nutzer1", "PassKeyHash1", "RecoveryPassKeyHash1");
        userAuthDB.createUser("Nutzer2", "PassKeyHash2", "RecoveryPassKeyHash2");
        
        Debug.Log("Sample users created in central database.");
    }

    private void CreateUserDatabases()
    {
        // Get all users from central userAuthDB
        List<UserDB> allUsers = userAuthDB.getAllUsers();
        
        foreach (UserDB user in allUsers)
        {
            // Create individual database for each user
            DataBase userDatabase = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>(user.name);
            userDatabase.setupDatabase();
            
            // Create companies for this user
            CreateCompaniesForUser(user.name, userDatabase);
        }
    }
private void CreateCompaniesForUser(string userName, DataBase userDatabase)
{
    if (userName == "Nutzer1")
    {
        userDatabase.insert(new Company
        {
            name = "Firma1",
            legalForm = 1, // GmbH
            industry = 1,  // IT & Software
            location = "Berlin1"
        });

        userDatabase.insert(new Company
        {
            name = "Firma2",
            legalForm = 3, // AG
            industry = 2,  // Handel
            location = "Berlin2"
        });
    }
    else if (userName == "Nutzer2")
    {
        userDatabase.insert(new Company
        {
            name = "Firma1-2",
            legalForm = 2, // KG
            industry = 3,  // Produktion
            location = "Berlin1-2"
        });

        userDatabase.insert(new Company
        {
            name = "Firma2-2",
            legalForm = 6, // UG
            industry = 4,  // Dienstleistung
            location = "Berlin2-2"
        });
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
        DataBase userDatabase = GlobalDatabaseManager.Instance.GetDatabase<DataBase>(user.name);

        if (userDatabase != null)
        {
            // Get lookup values
            string[] legalForms = userDatabase.getLookupValues("LegalForm");
            string[] industries = userDatabase.getLookupValues("Industry");

            // Get all companies
            List<Company> userCompanies = userDatabase.getAll<Company>();

            Debug.Log($"Companies count: {userCompanies.Count}");

            foreach (Company company in userCompanies)
            {
                Debug.Log("----------------------------------");
                Debug.Log($"ID: {company.id}");
                Debug.Log($"Name: {company.name}");

                Debug.Log($"Rechtsform: {legalForms[company.legalForm - 1]}");

                Debug.Log($"Branche: {industries[company.industry - 1]}");

                Debug.Log($"Standort: {company.location}");
            }
        }
        else
        {
            Debug.LogWarning($"Database not found for user: {user.name}");
        }
    }
}

    private void OnDestroy()
    {
        GlobalDatabaseManager.Instance.CloseAllDatabases();
    }
}
