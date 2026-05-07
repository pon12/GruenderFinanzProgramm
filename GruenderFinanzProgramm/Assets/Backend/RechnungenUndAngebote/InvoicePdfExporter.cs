using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;
public static class InvoicePdfExporter
{
    public static string ExportInvoiceToPdf(Invoice invoice, List<InvoiceItem> items)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "Rechnungen");
        Directory.CreateDirectory(folderPath);
        string filePath = Path.Combine(folderPath,
            "Rechnung_" + invoice.invoiceNumber + ".pdf");
        try
        {
            Document doc = new Document(PageSize.A4, 50, 50, 50, 50);
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                PdfWriter.GetInstance(doc, fs);
                doc.AddAuthor("Deine Firma");
                doc.AddTitle("Rechnung " + invoice.invoiceNumber);
                doc.Open();
                // Fonts – iTextSharp.text.Font explizit um Konflikt mit UnityEngine.Font zu vermeiden
                iTextSharp.text.Font titleFont = FontFactory.GetFont(
                    FontFactory.HELVETICA_BOLD, 18
                );
                iTextSharp.text.Font headerFont = FontFactory.GetFont(
                    FontFactory.HELVETICA_BOLD, 12
                );
                iTextSharp.text.Font normalFont = FontFactory.GetFont(
                    FontFactory.HELVETICA, 12
                );
                iTextSharp.text.Font grayFont = FontFactory.GetFont(
                    FontFactory.HELVETICA_OBLIQUE, 10,
                   new iTextSharp.text.Color(128, 128, 128)
                );
                // Titel
                Paragraph title = new Paragraph(
                    "Rechnung Nr. " + invoice.invoiceNumber, titleFont
                );
                doc.Add(title);
                doc.Add(new Paragraph(" "));
                // Rechnungsdaten
                doc.Add(new Paragraph("Firma-ID: " + invoice.companyId, normalFont));
                doc.Add(new Paragraph("Datum: " + invoice.date.ToString("dd.MM.yyyy"), normalFont));
                doc.Add(new Paragraph("Fällig bis: " + invoice.dueDate, normalFont));
                doc.Add(new Paragraph("Status: " + invoice.status, normalFont));
                doc.Add(new Paragraph(" "));
                // Artikeltabelle
                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100f;
                table.SetWidths(new float[] { 3.5f, 1f, 1.2f, 1.2f });
                AddHeaderCell(table, "Beschreibung", headerFont);
                AddHeaderCell(table, "Menge",         headerFont);
                AddHeaderCell(table, "Einzel (€)",    headerFont);
                AddHeaderCell(table, "Gesamt (€)",    headerFont);
                foreach (InvoiceItem item in items)
                {
                    AddBodyCell(table, item.description,                normalFont);
                    AddBodyCell(table, item.quantity.ToString(),        normalFont);
                    AddBodyCell(table, item.unitPrice.ToString("0.00"), normalFont);
                    AddBodyCell(table, item.total.ToString("0.00"),     normalFont);
                }
                doc.Add(table);
                doc.Add(new Paragraph(" "));
                // Summenblock
                PdfPTable totals = new PdfPTable(2);
                totals.WidthPercentage     = 50f;
                totals.HorizontalAlignment = Element.ALIGN_RIGHT;
                AddBodyCell(totals, "Zwischensumme:",                           normalFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, invoice.subtotal.ToString("0.00") + " €",   normalFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, "MwSt:",                                    normalFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, invoice.tax.ToString("0.00") + " €",        normalFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, "Gesamt:",                                  headerFont, Element.ALIGN_RIGHT);
                AddBodyCell(totals, invoice.total.ToString("0.00") + " €",      headerFont, Element.ALIGN_RIGHT);
                doc.Add(totals);
                // Notizen
                if (!string.IsNullOrEmpty(invoice.notes))
                {
                    doc.Add(new Paragraph(" "));
                    doc.Add(new Paragraph("Anmerkungen:", headerFont));
                    doc.Add(new Paragraph(invoice.notes, normalFont));
                }
                // Footer
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(" "));
                Paragraph footer = new Paragraph(
                    "Erstellt am " + DateTime.Now.ToString("dd.MM.yyyy"), grayFont
                );
                footer.Alignment = Element.ALIGN_CENTER;
                doc.Add(footer);
                doc.Close();
            }
            Debug.Log("PDF gespeichert: " + filePath);
            Application.OpenURL("file://" + filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            Debug.LogError("Fehler beim PDF-Export: " + ex.Message);
            return null;
        }
    }
    private static void AddHeaderCell(PdfPTable table, string text, iTextSharp.text.Font font)
    {
        PdfPCell cell = new PdfPCell(new Phrase(text, font));
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        cell.BackgroundColor = new iTextSharp.text.Color(230, 230, 230);
        table.AddCell(cell);
    }
    private static void AddBodyCell(PdfPTable table, string text, iTextSharp.text.Font font, int alignment)
    {
        PdfPCell cell = new PdfPCell(new Phrase(text, font));
        cell.HorizontalAlignment = alignment;
        table.AddCell(cell);
    }
    private static void AddBodyCell(PdfPTable table, string text, iTextSharp.text.Font font)
    {
        AddBodyCell(table, text, font, Element.ALIGN_LEFT);
    }
}