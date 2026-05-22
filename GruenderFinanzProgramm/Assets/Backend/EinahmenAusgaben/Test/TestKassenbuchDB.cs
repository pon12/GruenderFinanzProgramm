using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using System.Linq;
public class TestKassenbuchDB : MonoBehaviour
{

    private DataBase KassenbuchDB;

    private void Start()
    {
        // Initialize central user database
        KassenbuchDB = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("KassenbuchDatabase");

        KassenbuchDB.setupKassenbuchTable();

        createSampleKassenbuchEntries();

        getAllKassenbuchEntries();

        deleteEntries();

        calculateDifference();
        
    }

    private void createSampleKassenbuchEntries()
    {
        // Create sample Kassenbuch entries
        KassenbuchDB.createEinkommen(1000, "Gehalt");
        KassenbuchDB.createEinkommen(200, "Freelance Projekt");
        KassenbuchDB.createAusgaben(300, "Miete");
        KassenbuchDB.createAusgaben(150, "Lebensmittel");

        Debug.Log("Beispiel-Einträge hinzugefügt.");
    }

    private void getAllKassenbuchEntries()
    {
    List<Einkommen> einkommenEntries = KassenbuchDB.getAllEinkommenEntries();
    foreach (Einkommen entry in einkommenEntries)
    {
        Debug.Log($"Einkommen: {entry.Amount} {entry.Description}");
    }

    List<Ausgaben> ausgabenEntries = KassenbuchDB.getAllAusgabenEntries();
    foreach (Ausgaben entry in ausgabenEntries)
    {
        Debug.Log($"Ausgaben: {entry.Amount} {entry.Description}");
    }
    }

    public void deleteEntries()
    {
       KassenbuchDB.deleteEinkommen(1); 
       KassenbuchDB.deleteAusgaben(1); 
       Debug.Log("Die erste Einkommen- und Ausgaben-Eintrag wurden gelöscht.");
    
    }

    public float calculateDifference()
    {
    float totalEinkommen = KassenbuchDB.getAllEinkommenEntries().Sum(e => e.Amount);
    float totalAusgaben = KassenbuchDB.getAllAusgabenEntries().Sum(a => a.Amount);
    float difference = totalEinkommen - totalAusgaben;

    Debug.Log($"Die Differenz zwischen Gesamteinkommen und Gesamtausgaben beträgt: {difference}");
    return difference;
    }


    

    private void OnDestroy()
    {
        GlobalDatabaseManager.Instance.CloseAllDatabases();
    }

}
