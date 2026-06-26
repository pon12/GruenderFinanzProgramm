using System.Collections.Generic;
using UnityEngine;

public class FinanceDashboardDataTester : MonoBehaviour
{
    private DataBase db;

    [Header("Testjahr")]
    public int testYear = 2026;

    private void Start()
    {
        Debug.Log("===== FinanceDashboardDataTester gestartet =====");

        db = GlobalDatabaseManager.Instance.GetOrCreateDatabase<DataBase>("Alex");

        if (db == null)
        {
            Debug.LogError("[FinanceDashboardDataTester] Test-Datenbank konnte nicht geladen werden.");
            return;
        }

        db.setupDatabase();

        Debug.Log("[FinanceDashboardDataTester] Test läuft direkt auf Alex.db");

        CreateFinanceTestData();

        TestMonthlyFinanceData();
        TestMonthlyServiceRevenue();

        Debug.Log("===== FinanceDashboardDataTester fertig =====");
    }

    private void CreateFinanceTestData()
    {
        Debug.Log("===== TESTDATEN: Finanzdashboard =====");

        db.createEinkommen(
            1000f,
            "Test Einnahme Januar Finanzdashboard",
            "15.01." + testYear,
            "Test"
        );

        db.createAusgaben(
            300f,
            "Test Ausgabe Januar Finanzdashboard",
            "20.01." + testYear,
            "Test"
        );

        db.createEinkommen(
            500f,
            "Test Einnahme Februar Finanzdashboard",
            "10.02." + testYear,
            "Test"
        );

        db.createAusgaben(
            100f,
            "Test Ausgabe Februar Finanzdashboard",
            "12.02." + testYear,
            "Test"
        );

        db.createEinkommen(
            250f,
            "Test Einnahme März Finanzdashboard",
            "05.03." + testYear,
            "Test"
        );

        Debug.Log("[FinanceDashboardDataTester] Testdaten wurden erstellt.");
    }

    private void TestMonthlyFinanceData()
    {
        Debug.Log("===== TEST: Monatsdaten Einnahmen / Ausgaben / Gewinn =====");

        List<FinanzMonatswert> monthlyData =
            FinanceDashboardDataService.GetMonthlyFinanceData(db, testYear);

        if (monthlyData == null)
        {
            Debug.LogError("[FinanceDashboardDataTester] monthlyData ist null.");
            return;
        }

        Debug.Log("Anzahl Monatswerte: " + monthlyData.Count);

        foreach (FinanzMonatswert month in monthlyData)
        {
            Debug.Log(
                month.monthIndex + " | " +
                month.monthName + " | " +
                "Einnahmen: " + month.einnahmen + " | " +
                "Ausgaben: " + month.ausgaben + " | " +
                "Gewinn: " + month.gewinn
            );
        }

        List<float> incomeValues =
            FinanceDashboardDataService.GetMonthlyIncome(db, testYear);

        List<float> expenseValues =
            FinanceDashboardDataService.GetMonthlyExpenses(db, testYear);

        List<float> profitValues =
            FinanceDashboardDataService.GetMonthlyProfit(db, testYear);

        Debug.Log("Einnahmen-Liste Länge: " + incomeValues.Count);
        Debug.Log("Ausgaben-Liste Länge: " + expenseValues.Count);
        Debug.Log("Gewinn-Liste Länge: " + profitValues.Count);
    }

    private void TestMonthlyServiceRevenue()
    {
        Debug.Log("===== TEST: Dienstleistungsumsätze pro Monat =====");

        List<DienstleistungMonatsUmsatz> serviceRevenue =
            FinanceDashboardDataService.GetMonthlyServiceRevenue(db, testYear);

        if (serviceRevenue == null)
        {
            Debug.LogError("[FinanceDashboardDataTester] serviceRevenue ist null.");
            return;
        }

        Debug.Log("Anzahl Dienstleistungen: " + serviceRevenue.Count);

        foreach (DienstleistungMonatsUmsatz serviceData in serviceRevenue)
        {
            Debug.Log(
                "Dienstleistung: " + serviceData.dienstleistungName +
                " | Gesamtumsatz: " + serviceData.gesamtUmsatz +
                " | Monatswerte: " + string.Join(", ", serviceData.monatsUmsaetze)
            );
        }
    }
}