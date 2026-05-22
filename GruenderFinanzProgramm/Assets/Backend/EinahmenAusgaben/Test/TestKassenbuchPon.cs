using UnityEngine;
using System.Collections.Generic;
using System.Linq;



//wird in TestSzenen/SampleSzene ausgeführt benötigt zum funktionieren zuvor ausgeführt die TestSzenen/LoginSzene - Pon

public class TestKassenbuchPon : MonoBehaviour
{
    void Start()
    {
        UserDatabaseAccess.getCurrentUserDatabase().setupKassenbuchTable();

        createSampleKassenbuchEntries();

        getAllKassenbuchEntries();

        deleteEntries();

        calculateDifference();
    }



    private void createSampleKassenbuchEntries()
    {
        UserDatabaseAccess.getCurrentUserDatabase().createEinkommen(1000, "Gehalt");
        UserDatabaseAccess.getCurrentUserDatabase().createEinkommen(200, "Freelance Projekt");
        UserDatabaseAccess.getCurrentUserDatabase().createEinkommen(500, "Freelance Projekt 2");
        UserDatabaseAccess.getCurrentUserDatabase().createAusgaben(300, "Miete");
        UserDatabaseAccess.getCurrentUserDatabase().createAusgaben(150, "Lebensmittel");
        UserDatabaseAccess.getCurrentUserDatabase().createAusgaben(1500, "Lebensmittel 2");

        Debug.Log("Beispiel-Einträge hinzugefügt.");
    }

    private void getAllKassenbuchEntries()
    {
    List<Einkommen> einkommenEntries = UserDatabaseAccess.getCurrentUserDatabase().getAllEinkommenEntries();
    foreach (Einkommen entry in einkommenEntries)
    {
        Debug.Log($"Einkommen: {entry.Amount} {entry.Description}");
    }

    List<Ausgaben> ausgabenEntries = UserDatabaseAccess.getCurrentUserDatabase().getAllAusgabenEntries();
    foreach (Ausgaben entry in ausgabenEntries)
    {
        Debug.Log($"Ausgaben: {entry.Amount} {entry.Description}");
    }
    }

    public void deleteEntries()
    {
       UserDatabaseAccess.getCurrentUserDatabase().deleteEinkommen(1); 
       UserDatabaseAccess.getCurrentUserDatabase().deleteAusgaben(1); 
       Debug.Log("Die erste Einkommen- und Ausgaben-Eintrag wurden gelöscht.");
    
    }

    public float calculateDifference()
    {
    float totalEinkommen = UserDatabaseAccess.getCurrentUserDatabase().getAllEinkommenEntries().Sum(e => e.Amount);
    float totalAusgaben = UserDatabaseAccess.getCurrentUserDatabase().getAllAusgabenEntries().Sum(a => a.Amount);
    float difference = totalEinkommen - totalAusgaben;

    Debug.Log($"Die Differenz zwischen Gesamteinkommen und Gesamtausgaben beträgt: {difference}");
    return difference;
    }


}
