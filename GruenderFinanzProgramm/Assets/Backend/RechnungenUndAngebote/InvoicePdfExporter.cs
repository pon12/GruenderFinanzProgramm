using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
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
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                writer.PageEvent = new PdfFooterEvent(
                invoice.companyName,
                invoice.companyAddress,
                true
                );

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

                iTextSharp.text.Font footerFont =
                FontFactory.GetFont(
                FontFactory.HELVETICA,
                9
                    );

                // =========================
                // KUNDE und firma
                // =========================

                PdfPTable adressen = new PdfPTable(2);
                adressen.WidthPercentage = 100f;
                adressen.SetWidths(new float[] { 1f, 1f });

                PdfPCell kundeCell = new PdfPCell();
                kundeCell.Border = Rectangle.NO_BORDER;
                kundeCell.AddElement(new Paragraph("Kunde", headerFont));

                if (!string.IsNullOrEmpty(invoice.customerAddress))
                {
                    kundeCell.AddElement(new Paragraph(invoice.customerAddress, normalFont));
                }

                PdfPCell firmaCell = new PdfPCell();
                firmaCell.Border = Rectangle.NO_BORDER;
                firmaCell.AddElement(new Paragraph("Absender", headerFont));

                if (!string.IsNullOrEmpty(invoice.companyName))
                {
                    firmaCell.AddElement(new Paragraph(invoice.companyName, normalFont));
                }

                if (!string.IsNullOrEmpty(invoice.companyAddress))
                {
                    firmaCell.AddElement(new Paragraph(invoice.companyAddress, normalFont));
                }

                adressen.AddCell(kundeCell);
                adressen.AddCell(firmaCell);

                doc.Add(adressen);
                doc.Add(new Paragraph(" "));
                
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
                // TABELLE
                // =========================

                PdfPTable table = new PdfPTable(5);

                table.WidthPercentage = 100f;

                table.SetWidths(
                    new float[]
                    {1.4f, 3.0f, 1f, 1.2f, 1.2f }
                );
                AddHeaderCell(table, "Leistung", headerFont);
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
                
                double positionenGesamt = 0;

                foreach (InvoiceItem item in items)
                {
                    positionenGesamt += item.calculatedTotal;
                }
                double rabatt = invoice.discount;
                double skonto = invoice.extraCosts;
                int steuersatz = PlayerPrefs.GetInt("settings_steuersatz", 19);
                double mwstSatz = steuersatz / 100.0;
                double steuerBasis = positionenGesamt - rabatt;
                double mwst = steuerBasis * mwstSatz;
                double zwischensumme = steuerBasis + mwst;
                double gesamt = zwischensumme - skonto;
                
                PdfPTable totals = new PdfPTable(2);
                totals.KeepTogether = true;
                totals.WidthPercentage = 50f;
                totals.HorizontalAlignment = Element.ALIGN_RIGHT;

                AddBodyCell(totals, "Netto:", normalFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, positionenGesamt.ToString("0.00") + " €", normalFont, Element.ALIGN_RIGHT);

                AddBodyCell(totals, "Rabatt:", normalFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, "-" + rabatt.ToString("0.00") + " €", normalFont, Element.ALIGN_RIGHT);

                AddBodyCell(totals, "MwSt (" + steuersatz + "%):", normalFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, mwst.ToString("0.00") + " €", normalFont, Element.ALIGN_RIGHT);

                AddBodyCell(totals, "Zwischenbetrag:", headerFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, zwischensumme.ToString("0.00") + " €", headerFont, Element.ALIGN_RIGHT);

                AddBodyCell(totals, "Skonto:", normalFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, "-" + skonto.ToString("0.00") + " €", normalFont, Element.ALIGN_RIGHT);

                AddBodyCell(totals, "Brutto:", headerFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, gesamt.ToString("0.00") + " €", headerFont, Element.ALIGN_RIGHT);

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

                PdfPTable closingTable = new PdfPTable(1);
                closingTable.WidthPercentage = 100f;
                closingTable.SplitRows = false;
                closingTable.KeepTogether = true;

                PdfPCell closingCell = new PdfPCell();
                closingCell.Border = Rectangle.NO_BORDER;

                closingCell.AddElement(new Paragraph(" "));

                closingCell.AddElement(new Paragraph(
                "Vielen Dank für Ihren Auftrag. Bitte begleichen Sie den Rechnungsbetrag innerhalb der angegebenen Zahlungsfrist.",
                    normalFont));

                closingCell.AddElement(new Paragraph(" "));
                closingCell.AddElement(new Paragraph(" "));

                closingCell.AddElement(new Paragraph(
                    "Mit freundlichen Grüßen",
                    normalFont));

                closingCell.AddElement(new Paragraph(" "));
                closingCell.AddElement(new Paragraph(" "));

                closingTable.AddCell(closingCell);

                doc.Add(closingTable);







                // =========================
                // FOOTER
                // =========================

                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(" "));

                Paragraph footer = new Paragraph(
                    "Erstellt am " +
                    DateTime.Now.ToString("dd.MM.yyyy"),
                    footerFont
                );

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
private static void AddFooterCell(
    PdfPTable table,
    string text,
    iTextSharp.text.Font font
)
{
    PdfPCell cell = new PdfPCell(new Phrase(text, font));
    cell.Border = Rectangle.NO_BORDER;
    cell.PaddingTop = 6;
    cell.PaddingRight = 10;
    cell.HorizontalAlignment = Element.ALIGN_LEFT;
    table.AddCell(cell);
}



}

