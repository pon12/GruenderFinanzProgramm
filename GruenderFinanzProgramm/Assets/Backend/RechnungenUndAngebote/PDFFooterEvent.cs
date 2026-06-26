using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;

public class PdfFooterEvent : PdfPageEventHelper
{
    private readonly string firma;
    private readonly string adresse;
    private readonly iTextSharp.text.Font footerFont;

    private readonly bool showBankData;

public PdfFooterEvent(string firma, string adresse, bool showBankData)
{
    this.firma = firma;
    this.adresse = adresse;
    this.showBankData = showBankData;

    footerFont = FontFactory.GetFont(
        FontFactory.HELVETICA,
        8,
        iTextSharp.text.Color.BLACK
    );
}

    public override void OnEndPage(PdfWriter writer, Document document)
    {
        PdfPTable footerTable = new PdfPTable(3);
        footerTable.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
        footerTable.SetWidths(new float[] { 1.4f, 1.4f, 1.2f });

        AddFooterCell(
            footerTable,
            firma + "\n" + adresse,
            footerFont
        );

        string bankText =
            "Bankverbindung\n" +
            PlayerPrefs.GetString("settings_kreditinstitut", "") + "\n" +
            "IBAN: " + PlayerPrefs.GetString("settings_iban", "") + "\n" +
            "BIC: " + PlayerPrefs.GetString("settings_bic", "") + "\n" +
            "Konto-Inh.: " + PlayerPrefs.GetString("settings_kontoinhaber", "");

        AddFooterCell(
            footerTable,
            bankText,
            footerFont
        );

        AddFooterCell(
            footerTable,
            "Steuerdaten\n" +
            "Steuer-Nr.: " + PlayerPrefs.GetString("settings_steuernummer", "") + "\n" +
            "USt-IdNr.: " + PlayerPrefs.GetString("settings_ustidnr", ""),
            footerFont
        );

        PdfContentByte canvas = writer.DirectContent;

        float lineY = document.BottomMargin - 5;
        canvas.SetLineWidth(0.8f);
        canvas.MoveTo(document.LeftMargin, lineY);
        canvas.LineTo(document.PageSize.Width - document.RightMargin, lineY);
        canvas.Stroke();

        footerTable.WriteSelectedRows(
            0,
            -1,
            document.LeftMargin,
            document.BottomMargin - 12,
            canvas
        );
    }

    private static void AddFooterCell(
        PdfPTable table,
        string text,
        iTextSharp.text.Font font
    )
    {
        PdfPCell cell = new PdfPCell(new Phrase(text, font));
        cell.Border = Rectangle.NO_BORDER;
        cell.PaddingTop = 4;
        cell.PaddingRight = 10;
        cell.HorizontalAlignment = Element.ALIGN_LEFT;
        table.AddCell(cell);
    }
}