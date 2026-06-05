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

        calculatetotalEinkommen();

        calculatetotalAusgaben();

        calculateDifference();

        AusgabebyDatum();

        EinkommenbyDatum();

    }



    private void createSampleKassenbuchEntries()
    {
        UserDatabaseAccess.getCurrentUserDatabase().createEinkommen(1000, "Gehalt" , "01.01.2025");
        UserDatabaseAccess.getCurrentUserDatabase().createEinkommen(200, "Freelance Projekt", "12.02.2025");
        UserDatabaseAccess.getCurrentUserDatabase().createEinkommen(500, "Freelance Projekt 2", "01.03.2025");
        UserDatabaseAccess.getCurrentUserDatabase().createAusgaben(300, "Miete", "23.01.2025");
        UserDatabaseAccess.getCurrentUserDatabase().createAusgaben(150, "Lebensmittel", "01.02.2025");
        UserDatabaseAccess.getCurrentUserDatabase().createAusgaben(1500, "Lebensmittel 2", "01.03.2025");

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

    public void calculatetotalEinkommen()
    {
    float totalEinkommen = UserDatabaseAccess.getCurrentUserDatabase().getTotalEinkommen();
    Debug.Log($"Das Gesamteinkommen beträgt: {totalEinkommen}");
    }

    public void calculatetotalAusgaben()
    {
    float totalAusgaben = UserDatabaseAccess.getCurrentUserDatabase().getTotalAusgaben();
    Debug.Log($"Die Gesamtausgaben betragen: {totalAusgaben}");
    }


    public float calculateDifference()
    {
    float difference = UserDatabaseAccess.getCurrentUserDatabase().getDifferenz();
    Debug.Log($"Die Differenz zwischen Gesamteinkommen und Gesamtausgaben beträgt: {difference}");
    return difference;
    }

    public void AusgabebyDatum()
    {
     string datum = "01.02.2025";
    List<Ausgaben> ausgabenByDatum = UserDatabaseAccess.getCurrentUserDatabase().getAusgabebyDatum(datum);
    foreach (Ausgaben entry in ausgabenByDatum)
    {
    Debug.Log($"Die Ausgaben am {datum} betragen: {entry.Amount} {entry.Description}");
    }
    }

    public void EinkommenbyDatum()
    {
        string datum = "01.03.2025";
        List<Einkommen> einkommenByDatum = UserDatabaseAccess.getCurrentUserDatabase().getEinkommenbyDatum(datum);
        foreach (Einkommen entry in einkommenByDatum)
        {
            Debug.Log($"Die Einkommen am {datum} betragen: {entry.Amount} {entry.Description}");
        }
    }

    
    

}
