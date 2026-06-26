using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class FinanceDashboardBinder : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private DataBase db;

    private VisualElement root;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }
    }

    private IEnumerator Start()
    {
        Debug.Log("[FinanceUI] Start -> warte auf UI Build...");

        db = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("Alex");

        if (db == null)
        {
            Debug.LogError("[FinanceUI] DB ist NULL");
            yield break;
        }

        db.setupDatabase();

        // 🔥 WICHTIG: UI Toolkit braucht 1 Frame
        yield return null;

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("[FinanceUI] Root VisualElement ist NULL");
            yield break;
        }

        Debug.Log("[FinanceUI] UI geladen -> starte Binding");

        RefreshUI();
    }

    private void RefreshUI()
    {
        Debug.Log("[FinanceUI] RefreshUI gestartet");

        // =========================
        // 🔥 SAFE GET LABEL FUNCTION
        // =========================
        void Set(string name, float value, string suffix = " €")
        {
            var label = root.Q<Label>(name);

            if (label == null)
            {
                Debug.LogError("[FinanceUI] Label NICHT gefunden: " + name);
                return;
            }

            label.text = value.ToString("N2") + suffix;
            Debug.Log("[FinanceUI] gesetzt: " + name + " = " + label.text);
        }

        // =========================
        // 💰 GESAMTWERTE
        // =========================
        float umsatz = FinanceKassenbuchAuswertungService.GetSummeEinnahmenGesamt(db);
        float kosten = FinanceKassenbuchAuswertungService.GetSummeAusgabenGesamt(db);
        float saldo = FinanceKassenbuchAuswertungService.GetSaldo(db);

        float prozent = umsatz > 0 ? (saldo / umsatz) * 100f : 0f;

        Set("Umsatz", umsatz);
        Set("Kosten", kosten);
        Set("Rohgewinn", saldo);
        Set("Prozent", prozent, " %");

        // =========================
        // 📊 ERTRAG
        // =========================
        Set("Dienst1",
            FinanceKassenbuchAuswertungService.GetSummeEinnahmenNachKategorie(db, "Dienstleistungen"));

        Set("Dienst2", 0);
        Set("Dienst3", 0);
        Set("Dienst4", 0);

        // =========================
        // 💸 KOSTEN
        // =========================
        Set("Honorar1",
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(db, "Honorar"));

        Set("Material",
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(db, "Material"));

        Set("Honorar2", 0);
        Set("Honorar3", 0);
        Set("Honorar4", 0);

        // =========================
        // 🏢 BETRIEB
        // =========================
        Set("Auto",
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(db, "Auto"));

        Set("Marketing",
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(db, "Marketing"));

        Set("Reisekosten",
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(db, "Reisekosten"));

        Set("Betriebsausgaben",
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(db, "Betriebsausgaben"));

        // =========================
        // 🏗 INVEST
        // =========================
        Set("Mustermodell", 0);
        Set("Investitionswert", 0);
        Set("Ausstattung", 0);
        Set("SummeSacheinlagen", 0);

        // =========================
        // 🧾 GRÜNDUNG
        // =========================
        Set("CoparateDesigne",
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(db, "Corporate Design"));

        Set("Grundausstattung", 0);

        Set("Homepage",
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(db, "Homepage"));

        Set("SummeGruendungskosten",
            FinanceKassenbuchAuswertungService.GetSummeAusgabenNachKategorie(db, "Gründungskosten"));

        // =========================
        // 💰 KAPITAL
        // =========================
        Set("KapitalbedarfInvestition", 0);
        Set("Sacheinlagen", 0);
        Set("Grunderkosten", 0);
        Set("Kapitalbedarf", 0);
        Set("Liquidservice", 0);
        Set("Gesamtkapitalbedarf", 0);
    }
}