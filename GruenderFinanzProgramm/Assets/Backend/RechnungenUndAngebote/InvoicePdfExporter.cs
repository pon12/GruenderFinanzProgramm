using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;

public static class InvoicePdfExporter
{
    public static UserPDFDocument ExportInvoiceToPdf(
        Invoice invoice,
        List<InvoiceItem> items,
        int userId,
        DataBase db
    )
    {
        string folderPath = Path.Combine(
            Application.persistentDataPath,
            "PDFs",
            StateManager.Instance.getCurrentUser().username,
            "Rechnungen"
        );

        Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(
            folderPath,
            "Rechnung_" + invoice.invoiceNumber + ".pdf"
        );

        try
        {
            Document doc = new Document(
                PageSize.A4,
                50,
                50,
                50,
                50
            );

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                PdfWriter.GetInstance(doc, fs);

                doc.AddAuthor("Ventoriq");
                doc.AddTitle("Rechnung " + invoice.invoiceNumber);

                doc.Open();

                // =========================
                // FONTS
                // =========================

                iTextSharp.text.Font titleFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_BOLD,
                        18
                    );

                iTextSharp.text.Font headerFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_BOLD,
                        12
                    );

                iTextSharp.text.Font normalFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA,
                        12
                    );

                iTextSharp.text.Font grayFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_OBLIQUE,
                        10,
                        new iTextSharp.text.Color(128, 128, 128)
                    );

                // =========================
                // TITEL
                // =========================

                Paragraph invoiceTitle = new Paragraph(
                    "RECHNUNG " + invoice.invoiceNumber,
                    titleFont
                );

                invoiceTitle.Alignment = Element.ALIGN_LEFT;

                doc.Add(invoiceTitle);
                doc.Add(new Paragraph(" "));

                // =========================
                // FIRMA
                // =========================

                doc.Add(new Paragraph(
                    invoice.companyName,
                    headerFont
                ));

                doc.Add(new Paragraph(
                    invoice.companyAddress,
                    normalFont
                ));

                doc.Add(new Paragraph(" "));

                // =========================
                // RECHNUNGSDATEN
                // =========================

                doc.Add(new Paragraph(
                    "Datum: " + invoice.date,
                    normalFont
                ));

                doc.Add(new Paragraph(
                    "Fällig bis: " + invoice.dueDate,
                    normalFont
                ));

                doc.Add(new Paragraph(
                    "Status: " + invoice.status,
                    normalFont
                ));

                doc.Add(new Paragraph(" "));

                // =========================
                // KUNDE
                // =========================

                doc.Add(new Paragraph(
                    "Kunde: " + invoice.customerName,
                    headerFont
                ));

                if (!string.IsNullOrEmpty(invoice.customerAddress))
                {
                    doc.Add(new Paragraph(
                        invoice.customerAddress,
                        normalFont
                    ));
                }

                doc.Add(new Paragraph(" "));

                // =========================
                // TABELLE
                // =========================

                PdfPTable table = new PdfPTable(5);

                table.WidthPercentage = 100f;

                table.SetWidths(
                    new float[]
                    {1.4f, 3.0f, 1f, 1.2f, 1.2f }
                );
                AddHeaderCell(table, "Artikelnummer", headerFont);
                AddHeaderCell(table, "Beschreibung", headerFont);
                AddHeaderCell(table, "Menge", headerFont);
                AddHeaderCell(table, "Einzel (€)", headerFont);
                AddHeaderCell(table, "Gesamt (€)", headerFont);

                foreach (InvoiceItem item in items)
                {
                   AddBodyCell(
                    table, 
                    item.articleNumber, 
                    normalFont
                    );
                   
                    AddBodyCell(
                        table,
                        item.description,
                        normalFont
                    );

                    AddBodyCell(
                        table,
                        item.quantity.ToString(),
                        normalFont,
                        Element.ALIGN_CENTER
                    );

                    AddBodyCell(
                        table,
                        item.unitPrice.ToString("0.00"),
                        normalFont,
                        Element.ALIGN_RIGHT
                    );

                    AddBodyCell(
                        table,
                        item.calculatedTotal.ToString("0.00"),
                        normalFont,
                        Element.ALIGN_RIGHT
                    );
                }

                doc.Add(table);
                doc.Add(new Paragraph(" "));

                // =========================
                // SUMMEN
                // =========================
                
                double zwischensumme = 0;
                foreach (InvoiceItem item in items)
                {
                    zwischensumme += item.calculatedTotal;
                }
                double mwst = zwischensumme * 0.19;
                double gesamt = zwischensumme + mwst;
                
                PdfPTable totals = new PdfPTable(2);

                totals.WidthPercentage = 50f;
                totals.HorizontalAlignment = Element.ALIGN_RIGHT;

                AddBodyCell(
                    totals,
                    "Zwischensumme:",
                    normalFont,
                    Element.ALIGN_RIGHT
                    );

                AddBodyCell(
                    totals,
                    zwischensumme.ToString("0.00") + " €",
                    normalFont,
                    Element.ALIGN_RIGHT
                    );

                AddBodyCell(
                    totals,
                    "MwSt:",
                    normalFont,
                    Element.ALIGN_RIGHT
                );

                AddBodyCell(
                    totals,
                    invoice.tax.ToString("0.00") + " €",
                    normalFont,
                    Element.ALIGN_RIGHT
                );

                AddBodyCell(
                    totals,
                    "Gesamt:",
                    headerFont,
                    Element.ALIGN_RIGHT
                );

                AddBodyCell(
                    totals,
                    invoice.total.ToString("0.00") + " €",
                    headerFont,
                    Element.ALIGN_RIGHT
                );

                doc.Add(totals);

                // =========================
                // NOTIZEN
                // =========================

                if (!string.IsNullOrEmpty(invoice.notes))
                {
                    doc.Add(new Paragraph(" "));

                    doc.Add(new Paragraph(
                        "Anmerkungen:",
                        headerFont
                    ));

                    doc.Add(new Paragraph(
                        invoice.notes,
                        normalFont
                    ));
                }

                // =========================
                // FOOTER
                // =========================

                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(" "));

                Paragraph footer = new Paragraph(
                    "Erstellt am " +
                    DateTime.Now.ToString("dd.MM.yyyy"),
                    grayFont
                );

                footer.Alignment = Element.ALIGN_CENTER;

                doc.Add(footer);

                doc.Close();
            }

            Debug.Log("PDF gespeichert: " + filePath);

            UserPDFDocument document =
                PDFStorage.RegisterExistingPDF(
                    filePath,
                    userId,
                    db,
                    "Rechnungen"
                );

            Application.OpenURL("file://" + filePath);

            return document;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Fehler beim PDF-Export: " + ex
            );

            return null;
        }
    }

    private static void AddHeaderCell(
        PdfPTable table,
        string text,
        iTextSharp.text.Font font
    )
    {
        PdfPCell cell = new PdfPCell(
            new Phrase(text, font)
        );

        cell.HorizontalAlignment =
            Element.ALIGN_CENTER;

        cell.BackgroundColor =
            new iTextSharp.text.Color(230, 230, 230);

        table.AddCell(cell);
    }

    private static void AddBodyCell(
        PdfPTable table,
        string text,
        iTextSharp.text.Font font,
        int alignment = Element.ALIGN_LEFT
    )
    {
        PdfPCell cell = new PdfPCell(
            new Phrase(text, font)
        );

        cell.HorizontalAlignment = alignment;

        table.AddCell(cell);
    }
}

