using UnityEngine;

public class FinanceKassenbuchAuswertungTester : MonoBehaviour
{
    private DataBase db;

    private void Start()
    {
        Debug.Log("===== FinanceKassenbuchAuswertungTester gestartet =====");

        db = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("Alex");

        if (db == null)
        {
            Debug.LogError("[FinanceKassenbuchAuswertungTester] Datenbank konnte nicht geladen werden.");
            return;
        }

        db.setupDatabase();

        Debug.Log("[FinanceKassenbuchAuswertungTester] Test läuft direkt auf Alex.db");

        CreateTestData();
        TestAuswertungen();

        Debug.Log("===== FinanceKassenbuchAuswertungTester fertig =====");
    }

    private void CreateTestData()
    {
        Debug.Log("===== TESTDATEN: Kassenbuch-Auswertung =====");

        db.createAusgaben(
            200f,
            "Instagram Werbung",
            "24.06.2026",
            "Ausgabe",
            "Marketing"
        );

        db.createAusgaben(
            150f,
            "Flyer Druck",
            "24.06.2026",
            "Ausgabe",
            "Marketing"
        );

        db.createAusgaben(
            500f,
            "Laptop Zubehör",
            "24.06.2026",
            "Ausgabe",
            "Betriebsausgaben"
        );

        db.createEinkommen(
            1000f,
            "Webdesign Auftrag",
            "24.06.2026",
            "Einnahme",
            "Dienstleistungen"
        );

        Debug.Log("[FinanceKassenbuchAuswertungTester] Testdaten erstellt.");
    }

    private void TestAuswertungen()
    {
        Debug.Log("===== TEST: Kassenbuch-Auswertungen =====");

        float einnahmenGesamt =
            FinanceKassenbuchAuswertungService.GetSummeEinnahmenGesamt(db);

        float ausgabenGesamt =
            FinanceKassenbuchAuswertungService.GetSummeAusgabenGesamt(db);

        float saldo =
            FinanceKassenbuchAuswertungService.GetSaldo(db);

        float marketingAusgaben =
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(
                db,
                "Marketing"
            );

        float betriebsausgaben =
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(
                db,
                "Betriebsausgaben"
            );

        float dienstleistungen =
            FinanceKassenbuchAuswertungService.GetSummeEinnahmenNachKategorie(
                db,
                "Dienstleistungen"
            );

        Debug.Log("Einnahmen Gesamt: " + einnahmenGesamt);
        Debug.Log("Ausgaben Gesamt: " + ausgabenGesamt);
        Debug.Log("Saldo: " + saldo);

        Debug.Log("Marketingausgaben: " + marketingAusgaben);
        Debug.Log("Betriebsausgaben: " + betriebsausgaben);
        Debug.Log("Dienstleistungen Einnahmen: " + dienstleistungen);
    }
}