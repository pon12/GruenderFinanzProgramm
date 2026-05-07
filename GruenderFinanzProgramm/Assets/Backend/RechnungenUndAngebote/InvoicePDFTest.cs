using System.Collections.Generic;
using UnityEngine;

public class InvoicePdfTest : MonoBehaviour
{
    void Start()
    {
        // Beispielrechnung erstellen
        Invoice invoice = new Invoice
        {
            invoiceNumber = "R-2026-001",
            companyId = 1001,

            // WICHTIG:
            // date ist jetzt string und nicht DateTime
            date = System.DateTime.Now.ToString("dd.MM.yyyy"),

            dueDate = "20.05.2026",
            status = "offen",

            subtotal = 2500,
            tax = 475,
            total = 2975,

            notes = "Zahlbar innerhalb von 14 Tagen. Vielen Dank für Ihren Auftrag!"
        };

        // Beispiel-Positionen
        List<InvoiceItem> items = new List<InvoiceItem>
        {
            new InvoiceItem
            {
                description = "Webdesign-Paket",
                quantity = 1,
                unitPrice = 2000
            },

            new InvoiceItem
            {
                description = "Hosting (12 Monate)",
                quantity = 1,
                unitPrice = 500
            }
        };

        // Export starten
        InvoicePdfExporter.ExportInvoiceToPdf(invoice, items);
    }
}