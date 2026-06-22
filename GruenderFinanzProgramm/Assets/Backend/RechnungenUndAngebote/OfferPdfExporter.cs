using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
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
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                writer.PageEvent = new PdfFooterEvent(
                offer.companyName,
                offer.companyAddress
            );

                doc.AddAuthor("Ventoriq");
                
                doc.AddTitle("Angebot " + offer.offerNumber);

                doc.Open();

                //Fonts//

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
                


                PdfPTable adressen = new PdfPTable(2);
                adressen.WidthPercentage = 100f;
                adressen.SetWidths(new float[] { 1f, 1f });
                
                PdfPCell kundeCell = new PdfPCell();
                kundeCell.Border = Rectangle.NO_BORDER;

                kundeCell.AddElement(new Paragraph("Kunde", headerFont));

                if (!string.IsNullOrEmpty(offer.customerName))
                {
                    kundeCell.AddElement(new Paragraph(offer.customerName, normalFont));
                }

                if (!string.IsNullOrEmpty(offer.customerAddress))
                {
                    kundeCell.AddElement(new Paragraph(offer.customerAddress, normalFont));
                }

                PdfPCell firmaCell = new PdfPCell();
                firmaCell.Border = Rectangle.NO_BORDER;

                firmaCell.AddElement(new Paragraph("Absender", headerFont));

                if (!string.IsNullOrEmpty(offer.companyName))
                {
                    firmaCell.AddElement(new Paragraph(offer.companyName, normalFont));
                }
                if (!string.IsNullOrEmpty(offer.companyAddress))
                {
                    firmaCell.AddElement(new Paragraph(offer.companyAddress, normalFont));
                }


                adressen.AddCell(kundeCell);
                adressen.AddCell(firmaCell);
                
                doc.Add(adressen);
                doc.Add(new Paragraph(" "));

                Paragraph title = new Paragraph(
                    "ANGEBOT " + offer.offerNumber,
                    titleFont
                );

                title.Alignment = Element.ALIGN_LEFT;
                doc.Add(title);
                doc.Add(new Paragraph(" "));

                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph("Datum: " + offer.date, normalFont));
                doc.Add(new Paragraph("Gültig bis: " + offer.validUntil, normalFont));
                doc.Add(new Paragraph("Status: " + offer.status, normalFont));
                doc.Add(new Paragraph(" "));

                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100f;
                table.SetWidths(new float[] { 1.4f, 3.0f, 1f, 1.2f, 1.2f  });

                AddHeaderCell(table, "Leistung", headerFont);
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
                double positionenGesamt = 0;

                foreach (OfferItem item in items)
                {
                    positionenGesamt += item.calculatedTotal;
                }
                double rabatt = offer.discount;
                double sonstigeKosten = offer.extraCosts;
                int steuersatz = PlayerPrefs.GetInt("settings_steuersatz", 19);
                double mwstSatz = steuersatz / 100.0;
                double zwischensumme = positionenGesamt - rabatt + sonstigeKosten;
                double mwst = zwischensumme * mwstSatz;
                double gesamt = zwischensumme + mwst;
                
                PdfPTable totals = new PdfPTable(2);
                totals.KeepTogether = true;
                totals.WidthPercentage = 50f;
                totals.HorizontalAlignment = Element.ALIGN_RIGHT;
                
                AddBodyCell(
                    totals, "Netto:", 
                    normalFont, 
                    Element.ALIGN_RIGHT
                    );
                AddBodyCell(
                    totals, 
                    positionenGesamt.ToString("0.00") + " €", 
                    normalFont, Element.ALIGN_RIGHT
                    );
                
                AddBodyCell(
                    totals, 
                    "Rabatt:", 
                    normalFont, 
                    Element.ALIGN_RIGHT
                    );
                AddBodyCell(
                    totals, 
                    "+" + sonstigeKosten.ToString("0.00") + " €",
                    normalFont, 
                    Element.ALIGN_RIGHT
                    );

                AddBodyCell(
                    totals, 
                    "Sonstige Kosten:", 
                    normalFont, 
                    Element.ALIGN_RIGHT
                    );
                AddBodyCell(
                    totals, 
                    "+" + sonstigeKosten.ToString("0.00") + " €",
                    normalFont, 
                    Element.ALIGN_RIGHT
                    );

                AddBodyCell(
                    totals, 
                    "Zwischensumme:", 
                    headerFont, 
                    Element.ALIGN_RIGHT
                    );
                AddBodyCell(
                    totals, 
                    zwischensumme.ToString("0.00") + " €", 
                    headerFont, Element.ALIGN_RIGHT
                    );

                AddBodyCell(
                    totals, 
                    "MwSt (" + steuersatz + "%):",
                    normalFont, 
                    Element.ALIGN_RIGHT
                    );
                AddBodyCell(
                    totals, 
                    mwst.ToString("0.00") + " €", 
                    normalFont, 
                    Element.ALIGN_RIGHT
                    );

                AddBodyCell(
                    totals, 
                    "Brutto:", 
                    headerFont, 
                    Element.ALIGN_RIGHT
                    );
                AddBodyCell(
                    totals, 
                    gesamt.ToString("0.00") + " €", 
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
                    footerFont
                );

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