using UnityEngine;
using System.Collections.Generic;

public class SaveTest1 : MonoBehaviour
{
    private DataBase gameDatabase;
    private void Start()
    {
        // Datenbank initialisieren
        gameDatabase = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("GameData");
        
        //Dataenbank löschen auskommentieren für testzwecke
        //gameDatabase.deleteDatabase();
        

        gameDatabase.setupDatabase();
        // Beispieldaten anlegen
        CreateSampleData();

        // Daten abrufen und anzeigen
        RetrieveAndDisplayData();
    }
    private void CreateSampleData()
    {
        // Benutzer
        gameDatabase.createUser("Max Mustermann");
        gameDatabase.createUser("Pontus");
        // Firmen (neue Signatur mit Branche + Standort)
        gameDatabase.createCompany("Zaibatsu GmbH", 1, 0, "Berlin");
        gameDatabase.createCompany("GeileFirma AG", 2, 3, "Hamburg");
        Debug.Log("Beispielbenutzer und Firmen wurden angelegt.");
    }
    private void RetrieveAndDisplayData()
    {
        // Alle Benutzer
        List<UserDB> allUsers = gameDatabase.getAllUsers();
        Debug.Log($"Gesamtanzahl Benutzer: {allUsers.Count}");
        foreach (UserDB user in allUsers)
            Debug.Log($"Benutzer: {user.name}");
        
        
        // Alle Firmen
        List<Company> allCompanies = gameDatabase.getAllCompanies();
        Debug.Log($"Gesamtanzahl Firmen: {allCompanies.Count}");
        foreach (Company c in allCompanies)
        {
            Debug.Log("----------------------------------");
            Debug.Log($"ID: {c.id}");
            Debug.Log($"Name: {c.name}");
            Debug.Log($"Rechtsform: {c.LegalFormName}");
            Debug.Log($"Branche: {c.IndustryName}");
            Debug.Log($"Standort: {c.location}");
        }
        
        // Suche nach Benutzername
        List<UserDB> pontusUsers = gameDatabase.findUsersByName("Pontus");
        Debug.Log($"Gefundene Benutzer mit 'Pontus': {pontusUsers.Count}");
    }
    private void OnDestroy()
    {
        GlobalDatabaseManager.Instance.CloseAllDatabases();
    }
}