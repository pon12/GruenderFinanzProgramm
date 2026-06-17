using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;

public static class OfferPdfExporter
{
    public static UserPDFDocument ExportOfferToPdf(
        Offer offer,
        List<OfferItem> items,
        int userId,
        DataBase db
    )
    {
        string folderPath = Path.Combine(
            Application.persistentDataPath,
            "PDFs",
            StateManager.Instance.getCurrentUser().username,
            "Angebote"
        );

        Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(
            folderPath,
            "Angebot_" + offer.offerNumber + ".pdf"
        );

        try
        {
            Document doc = new Document(PageSize.A4, 50, 50, 50, 50);

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                PdfWriter.GetInstance(doc, fs);

                doc.AddAuthor("Ventoriq");
                doc.AddTitle("Angebot " + offer.offerNumber);

                doc.Open();

                iTextSharp.text.Font titleFont =
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);

                iTextSharp.text.Font headerFont =
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);

                iTextSharp.text.Font normalFont =
                    FontFactory.GetFont(FontFactory.HELVETICA, 12);

                iTextSharp.text.Font grayFont =
                    FontFactory.GetFont(
                        FontFactory.HELVETICA_OBLIQUE,
                        10,
                        new iTextSharp.text.Color(128, 128, 128)
                    );

                Paragraph title = new Paragraph(
                    "ANGEBOT " + offer.offerNumber,
                    titleFont
                );

                title.Alignment = Element.ALIGN_LEFT;
                doc.Add(title);
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph("Absender:", headerFont));
                if (!string.IsNullOrEmpty(offer.companyName))
                {
                    doc.Add(new Paragraph(offer.companyName, normalFont));
                }
                if (!string.IsNullOrEmpty(offer.companyAddress))
                {
                    doc.Add(new Paragraph(offer.companyAddress, normalFont));
                }
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph("Kunde:", headerFont));
                if (!string.IsNullOrEmpty(offer.customerName))
                {
                    doc.Add(new Paragraph(offer.customerName, normalFont));
                }
                if (!string.IsNullOrEmpty(offer.customerAddress))
                {
                    doc.Add(new Paragraph(offer.customerAddress, normalFont));
                }
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph("Datum: " + offer.date, normalFont));
                doc.Add(new Paragraph("Status: " + offer.status, normalFont));
                doc.Add(new Paragraph(" "));

                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100f;
                table.SetWidths(new float[] { 1.4f, 3.0f, 1f, 1.2f, 1.2f  });

                AddHeaderCell(table, "Artikelnummer", headerFont);
                AddHeaderCell(table, "Beschreibung", headerFont);
                AddHeaderCell(table, "Menge", headerFont);
                AddHeaderCell(table, "Einzel (€)", headerFont);
                AddHeaderCell(table, "Gesamt (€)", headerFont);

                foreach (OfferItem item in items)
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

            double zwischensumme = 0;

            foreach (OfferItem item in items)
                {
                    zwischensumme += item.calculatedTotal;
                }
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
                    offer.tax.ToString("0.00") + " €",
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
                    offer.total.ToString("0.00") + " €",
                    headerFont,
                    Element.ALIGN_RIGHT
                    );

                doc.Add(totals);

                if (!string.IsNullOrEmpty(offer.notes))
                {
                    doc.Add(new Paragraph(" "));
                    doc.Add(new Paragraph("Anmerkungen:", headerFont));
                    doc.Add(new Paragraph(offer.notes, normalFont));
                }

                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(" "));

                Paragraph footer = new Paragraph(
                    "Erstellt am " + DateTime.Now.ToString("dd.MM.yyyy"),
                    grayFont
                );

                footer.Alignment = Element.ALIGN_CENTER;
                doc.Add(footer);

                doc.Close();
            }

            Debug.Log("Angebots-PDF gespeichert: " + filePath);

            UserPDFDocument document =
                PDFStorage.RegisterExistingPDF(
                    filePath,
                    userId,
                    db,
                    "Angebote"
                );

            Application.OpenURL("file://" + filePath);

            return document;
        }
        catch (Exception ex)
        {
            Debug.LogError("Fehler beim Angebots-PDF-Export: " + ex);
            return null;
        }
    }

    private static void AddHeaderCell(
        PdfPTable table,
        string text,
        iTextSharp.text.Font font
    )
    {
        PdfPCell cell = new PdfPCell(new Phrase(text, font));
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        cell.BackgroundColor = new iTextSharp.text.Color(230, 230, 230);
        table.AddCell(cell);
    }

    private static void AddBodyCell(
        PdfPTable table,
        string text,
        iTextSharp.text.Font font,
        int alignment = Element.ALIGN_LEFT
    )
    {
        PdfPCell cell = new PdfPCell(new Phrase(text, font));
        cell.HorizontalAlignment = alignment;
        table.AddCell(cell);
    }
}