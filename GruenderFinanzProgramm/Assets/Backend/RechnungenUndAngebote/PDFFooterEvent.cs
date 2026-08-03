using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;

public class PdfFooterEvent : PdfPageEventHelper
{
    private readonly string firma;
    private readonly string adresse;
    private readonly bool   showBankData;

    private readonly iTextSharp.text.Font footerFont;

    private readonly string kreditinstitut;
    private readonly string iban;
    private readonly string bic;
    private readonly string kontoinhaber;

    private readonly string steuerNr;
    private readonly string ustIdNr;

    public PdfFooterEvent(string firma, string adresse, bool showBankData)
    {
        this.firma        = firma;
        this.adresse      = adresse;
        this.showBankData = showBankData;

        footerFont = FontFactory.GetFont(
            FontFactory.HELVETICA,
            8,
            iTextSharp.text.Color.BLACK
        );

        // Bankdaten nutzerspezifisch aus PlayerPrefs lesen
        kreditinstitut = HoleNutzerPref("settings_kreditinstitut");
        iban           = HoleNutzerPref("settings_iban");
        bic            = HoleNutzerPref("settings_bic");
        kontoinhaber   = HoleNutzerPref("settings_kontoinhaber");

        // Steuerdaten aus DB lesen
        steuerNr = "";
        ustIdNr  = "";
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db != null)
            {
                var companies = db.getAllCompanies();
                if (companies != null && companies.Count > 0)
                {
                    steuerNr = companies[0].steuerNr  ?? "";
                    ustIdNr  = companies[0].ustIdNr   ?? "";

                    if (string.IsNullOrEmpty(this.firma))
                        this.firma = companies[0].name ?? "";

                    if (string.IsNullOrEmpty(this.adresse))
                    {
                        string ort = companies[0].location       ?? "";
                        string str = companies[0].strasseuHausNr ?? "";
                        string plz = companies[0].plz > 0
                            ? companies[0].plz.ToString()
                            : "";
                        this.adresse = str + "\n" + plz + " " + ort;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[PdfFooterEvent] DB-Zugriff fehlgeschlagen: " + e.Message);
        }
    }

    public override void OnEndPage(PdfWriter writer, Document document)
    {
        PdfPTable footerTable = new PdfPTable(3);
        footerTable.TotalWidth =
            document.PageSize.Width - document.LeftMargin - document.RightMargin;
        footerTable.SetWidths(new float[] { 1.4f, 1.4f, 1.2f });

        // Spalte 1: Firmenname + Adresse
        AddFooterCell(footerTable, firma + "\n" + adresse, footerFont);

        // Spalte 2: Bankverbindung
        string bankText =
            "Bankverbindung\n" +
            kreditinstitut + "\n" +
            "IBAN: " + iban + "\n" +
            "BIC: "  + bic  + "\n" +
            "Konto-Inh.: " + kontoinhaber;

        AddFooterCell(footerTable, bankText, footerFont);

        // Spalte 3: Steuerdaten
        string steuerText =
            "Steuerdaten\n" +
            "Steuer-Nr.: " + steuerNr + "\n" +
            "USt-IdNr.: "  + ustIdNr;

        AddFooterCell(footerTable, steuerText, footerFont);

        // Trennlinie
        PdfContentByte canvas = writer.DirectContent;
        float lineY = document.BottomMargin - 5;
        canvas.SetLineWidth(0.8f);
        canvas.MoveTo(document.LeftMargin, lineY);
        canvas.LineTo(document.PageSize.Width - document.RightMargin, lineY);
        canvas.Stroke();

        footerTable.WriteSelectedRows(
            0, -1,
            document.LeftMargin,
            document.BottomMargin - 12,
            canvas
        );
    }

    // Liest einen PlayerPrefs-Wert nutzerspezifisch aus
    private static string HoleNutzerPref(string key, string fallback = "")
    {
        string prefix    = UserDatabaseAccess.getCurrentDatabaseName() ?? "";
        string nutzerKey = string.IsNullOrEmpty(prefix) ? key : prefix + "_" + key;
        return PlayerPrefs.GetString(nutzerKey, PlayerPrefs.GetString(key, fallback));
    }

    private static void AddFooterCell(
        PdfPTable table,
        string text,
        iTextSharp.text.Font font
    )
    {
        PdfPCell cell = new PdfPCell(new Phrase(text, font));
        cell.Border              = Rectangle.NO_BORDER;
        cell.PaddingTop          = 4;
        cell.PaddingRight        = 10;
        cell.HorizontalAlignment = Element.ALIGN_LEFT;
        table.AddCell(cell);
    }
}