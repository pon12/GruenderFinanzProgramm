using UnityEngine;
using System.Collections.Generic;

public class SaveTest1 : MonoBehaviour
{
    private DataBase gameDatabase;

    private void Start()
    {
        // Initialize the database
        gameDatabase = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("GameData");
        //gameDatabase.setupDatabase();

        // Create sample data
        //createSampleData();
        
        // Retrieve and display data
        retrieveAndDisplayData();
    }

    private void createSampleData()
    {
        // Create users
        gameDatabase.createUser("Max Musterman", 1234, 5678, true);
        gameDatabase.createUser("Pontus", 9999, 8888, false);

        // Create companies
        gameDatabase.createCompany("Zaibatsu", 1);
        gameDatabase.createCompany("GeileFirma", 2);
    }

    private void retrieveAndDisplayData()
    {
        // Get all users
        List<User> allUsers = gameDatabase.getAllUsers();
        Debug.Log($"Total users: {allUsers.Count}");

        foreach (User user in allUsers)
        {
            Debug.Log($"User: {user.name}, Logged In: {user.isLoggedIn}");
        }

        // Get logged-in users
        List<User> loggedInUsers = gameDatabase.getLoggedInUsers();
        Debug.Log($"Logged-in users: {loggedInUsers.Count}");

        // Get all companies
        List<Company> allCompanies = gameDatabase.getAllCompanies();
        Debug.Log($"Total companies: {allCompanies.Count}");

        foreach (Company company in allCompanies)
        {
            Debug.Log($"Company: {company.name}, Legal Form: {company.legalForm}");
        }

        // Find by name
        List<User> aliceUsers = gameDatabase.findUsersByName("Alice");
        Debug.Log($"Found users matching 'Alice': {aliceUsers.Count}");
    }

    private void OnDestroy()
    {
        GlobalDatabaseManager.Instance.CloseAllDatabases();
    }
}
