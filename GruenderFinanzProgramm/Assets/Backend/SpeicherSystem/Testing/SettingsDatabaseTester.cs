using UnityEngine;

public class SettingsDatabaseTester : MonoBehaviour
{
    private DataBase db;

    [Header("Test User")]
    public int testUserId = 1;

    private void Start()
    {
        Debug.Log("===== SettingsDatabaseTester gestartet =====");

        db = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("Alex");

        if (db == null)
        {
            Debug.LogError("[SettingsDatabaseTester] Datenbank konnte nicht geladen werden.");
            return;
        }

        db.setupDatabase();

        Debug.Log("[SettingsDatabaseTester] Test läuft direkt auf Alex.db");

        TestSettingsCreateReadUpdate();

        Debug.Log("===== SettingsDatabaseTester fertig =====");
    }

    private void TestSettingsCreateReadUpdate()
    {
        Debug.Log("===== TEST: Settings erstellen / lesen / speichern =====");

        Settings settings = db.getOrCreateSettingsForUser(testUserId);

        if (settings == null)
        {
            Debug.LogError("[SettingsDatabaseTester] Settings konnten nicht erstellt oder geladen werden.");
            return;
        }

        Debug.Log("[TEST] Settings geladen für UserId: " + settings.userId);
        Debug.Log("[TEST] Rechnungspräfix vorher: " + settings.rechnungsNrPräfix);
        Debug.Log("[TEST] Zahlungsziel vorher: " + settings.zahlungsziel);

        settings.rechnungsNrPräfix = "RE-TEST-";
        settings.zahlungsziel = "30";
        settings.iban = "DE_TEST_IBAN";
        settings.bic = "TESTBIC";
        settings.kontoInhaber = "Test Kontoinhaber";
        settings.kreditinstitut = "Test Bank";

        settings.barzahlung = "Barzahlung Test";
        settings.ueberweisung = "Überweisung Test";
        settings.agb = "AGB Test";
        settings.disclaimer = "Disclaimer Test";
        settings.zahlungshinweis = "Bitte überweisen Sie den Betrag auf das angegebene Konto.";

        db.updateSettingsForUser(settings);

        Debug.Log("[TEST] Settings wurden gespeichert.");

        Settings loadedSettings = db.getSettingsByUserId(testUserId);

        if (loadedSettings == null)
        {
            Debug.LogError("[SettingsDatabaseTester] Settings konnten nach dem Speichern nicht erneut geladen werden.");
            return;
        }

        Debug.Log("[TEST] Settings erneut geladen.");
        Debug.Log("[TEST] Rechnungspräfix nachher: " + loadedSettings.rechnungsNrPräfix);
        Debug.Log("[TEST] Zahlungsziel nachher: " + loadedSettings.zahlungsziel);
        Debug.Log("[TEST] IBAN nachher: " + loadedSettings.iban);
        Debug.Log("[TEST] BIC nachher: " + loadedSettings.bic);
        Debug.Log("[TEST] Kontoinhaber nachher: " + loadedSettings.kontoInhaber);
        Debug.Log("[TEST] Kreditinstitut nachher: " + loadedSettings.kreditinstitut);
        Debug.Log("[TEST] Barzahlung nachher: " + loadedSettings.barzahlung);
        Debug.Log("[TEST] Überweisung nachher: " + loadedSettings.ueberweisung);
        Debug.Log("[TEST] AGB nachher: " + loadedSettings.agb);
        Debug.Log("[TEST] Disclaimer nachher: " + loadedSettings.disclaimer);
        Debug.Log("[TEST] Zahlungshinweis nachher: " + loadedSettings.zahlungshinweis);
    }
}