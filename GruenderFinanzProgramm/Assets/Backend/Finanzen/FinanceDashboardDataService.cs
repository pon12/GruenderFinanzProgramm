using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class FinanceDashboardDataService
{
    private static readonly string[] MonthNames =
    {
        "Januar",
        "Februar",
        "März",
        "April",
        "Mai",
        "Juni",
        "Juli",
        "August",
        "September",
        "Oktober",
        "November",
        "Dezember"
    };

    public static List<FinanzMonatswert> GetMonthlyFinanceData(
        DataBase db,
        int year
    )
    {
        List<FinanzMonatswert> monthlyData = CreateEmptyMonthlyFinanceData();

        if (db == null)
        {
            Debug.LogError("[FinanceDashboardDataService] Datenbank ist null.");
            return monthlyData;
        }

        List<Einkommen> einkommenList = db.getAllEinkommenEntries();
        List<Ausgaben> ausgabenList = db.getAllAusgabenEntries();

        if (einkommenList != null)
        {
            foreach (Einkommen einkommen in einkommenList)
            {
                if (!TryParseKassenbuchDate(einkommen.Datum, out DateTime datum))
                {
                    continue;
                }

                if (datum.Year != year)
                {
                    continue;
                }

                int monthIndex = datum.Month - 1;

                if (monthIndex < 0 || monthIndex > 11)
                {
                    continue;
                }

                monthlyData[monthIndex].einnahmen += einkommen.Amount;
            }
        }

        if (ausgabenList != null)
        {
            foreach (Ausgaben ausgabe in ausgabenList)
            {
                if (!TryParseKassenbuchDate(ausgabe.Datum, out DateTime datum))
                {
                    continue;
                }

                if (datum.Year != year)
                {
                    continue;
                }

                int monthIndex = datum.Month - 1;

                if (monthIndex < 0 || monthIndex > 11)
                {
                    continue;
                }

                monthlyData[monthIndex].ausgaben += ausgabe.Amount;
            }
        }

        for (int i = 0; i < monthlyData.Count; i++)
        {
            monthlyData[i].gewinn =
                monthlyData[i].einnahmen - monthlyData[i].ausgaben;
        }

        return monthlyData;
    }

    public static List<FinanzMonatswert> GetCurrentYearMonthlyFinanceData(
        DataBase db
    )
    {
        return GetMonthlyFinanceData(db, DateTime.Now.Year);
    }

    public static List<float> GetMonthlyIncome(
        DataBase db,
        int year
    )
    {
        List<FinanzMonatswert> data = GetMonthlyFinanceData(db, year);
        List<float> values = new List<float>();

        foreach (FinanzMonatswert month in data)
        {
            values.Add(month.einnahmen);
        }

        return values;
    }

    public static List<float> GetMonthlyExpenses(
        DataBase db,
        int year
    )
    {
        List<FinanzMonatswert> data = GetMonthlyFinanceData(db, year);
        List<float> values = new List<float>();

        foreach (FinanzMonatswert month in data)
        {
            values.Add(month.ausgaben);
        }

        return values;
    }

    public static List<float> GetMonthlyProfit(
        DataBase db,
        int year
    )
    {
        List<FinanzMonatswert> data = GetMonthlyFinanceData(db, year);
        List<float> values = new List<float>();

        foreach (FinanzMonatswert month in data)
        {
            values.Add(month.gewinn);
        }

        return values;
    }

    public static List<DienstleistungMonatsUmsatz> GetMonthlyServiceRevenue(
    DataBase db,
    int year
)
    {
        List<DienstleistungMonatsUmsatz> serviceRevenueData =
            new List<DienstleistungMonatsUmsatz>();

        if (db == null)
        {
            Debug.LogError("[FinanceDashboardDataService] Datenbank ist null.");
            return serviceRevenueData;
        }

        List<Service> services = db.getAllServices();
        List<Invoice> invoices = db.getAllInvoices();

        if (services == null || services.Count == 0)
        {
            return serviceRevenueData;
        }

        foreach (Service service in services)
        {
            if (service == null || string.IsNullOrWhiteSpace(service.name))
            {
                continue;
            }

            serviceRevenueData.Add(
                new DienstleistungMonatsUmsatz(service.name)
            );
        }

        if (invoices == null || invoices.Count == 0)
        {
            return serviceRevenueData;
        }

        foreach (Invoice invoice in invoices)
        {
            if (invoice == null)
            {
                continue;
            }

            if (!IsRelevantInvoiceStatus(invoice.status))
            {
                continue;
            }

            if (!TryParseKassenbuchDate(invoice.date, out DateTime invoiceDate))
            {
                continue;
            }

            if (invoiceDate.Year != year)
            {
                continue;
            }

            int monthIndex = invoiceDate.Month - 1;

            if (monthIndex < 0 || monthIndex > 11)
            {
                continue;
            }

            List<InvoiceItem> items = db.getItemsByInvoice(invoice.id);

            if (items == null || items.Count == 0)
            {
                continue;
            }

            foreach (InvoiceItem item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.articleNumber))
                {
                    continue;
                }

                DienstleistungMonatsUmsatz serviceData =
                    FindServiceRevenueData(serviceRevenueData, item.articleNumber);

                if (serviceData == null)
                {
                    continue;
                }

                float amount = (float)(item.quantity * item.unitPrice);
                serviceData.AddUmsatz(monthIndex, amount);
            }
        }

        return serviceRevenueData;
    }

    public static List<DienstleistungMonatsUmsatz> GetCurrentYearMonthlyServiceRevenue(
        DataBase db
    )
    {
        return GetMonthlyServiceRevenue(db, DateTime.Now.Year);
    }

    public static List<float> GetMonthlyRevenueForService(
        DataBase db,
        int year,
        string serviceName
    )
    {
        List<float> values = CreateEmptyFloatMonthList();

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return values;
        }

        List<DienstleistungMonatsUmsatz> allServiceRevenue =
            GetMonthlyServiceRevenue(db, year);

        DienstleistungMonatsUmsatz serviceData =
            FindServiceRevenueData(allServiceRevenue, serviceName);

        if (serviceData == null)
        {
            return values;
        }

        return serviceData.monatsUmsaetze;
    }

    private static List<FinanzMonatswert> CreateEmptyMonthlyFinanceData()
    {
        List<FinanzMonatswert> monthlyData = new List<FinanzMonatswert>();

        for (int i = 0; i < 12; i++)
        {
            monthlyData.Add(new FinanzMonatswert(i, MonthNames[i]));
        }

        return monthlyData;
    }

    private static List<float> CreateEmptyFloatMonthList()
    {
        List<float> values = new List<float>();

        for (int i = 0; i < 12; i++)
        {
            values.Add(0f);
        }

        return values;
    }

    private static DienstleistungMonatsUmsatz FindServiceRevenueData(
        List<DienstleistungMonatsUmsatz> serviceRevenueData,
        string serviceName
    )
    {
        if (serviceRevenueData == null || string.IsNullOrWhiteSpace(serviceName))
        {
            return null;
        }

        string normalizedServiceName = serviceName.Trim().ToLower();

        foreach (DienstleistungMonatsUmsatz data in serviceRevenueData)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.dienstleistungName))
            {
                continue;
            }

            if (data.dienstleistungName.Trim().ToLower() == normalizedServiceName)
            {
                return data;
            }
        }

        return null;
    }

    private static bool IsRelevantInvoiceStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        string normalizedStatus = status.Trim().ToLower();

        return normalizedStatus == "angenommen"
            || normalizedStatus == "bezahlt";
    }

    private static bool TryParseKassenbuchDate(
        string dateText,
        out DateTime date
    )
    {
        return DateTime.TryParseExact(
            dateText,
            "dd.MM.yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date
        );
    }
}