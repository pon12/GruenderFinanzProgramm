using System;
using System.Collections.Generic;
using UnityEngine;

public class InvoicePdfTest : MonoBehaviour
{
    void Start()
    {
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

        Invoice invoice = new Invoice
        {
            companyName = "Tolle Firma GmbH",
            companyAddress = "Musterstraße 1, 12345 Mittweida",
            customerName = "Max Mustermann",
            invoiceNumber = "R-2026-001",

            date = DateTime.Now.ToString("dd.MM.yyyy"),

            dueDate = DateTime.Now.AddDays(14).ToString("dd.MM.yyyy"),

            status = "Offen",

            notes = "Zahlbar innerhalb von 14 Tagen."
        };

        invoice.CalculateTotals(items);

        InvoicePdfExporter.ExportInvoiceToPdf(invoice, items);
    }
}