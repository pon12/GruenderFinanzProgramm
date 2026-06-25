using System;
using System.Collections.Generic;
using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Color = UnityEngine.Color;

public static class BelegAnhangController
{
    // Exakte Dokumenttitel wie sie in DocumentDashboard als Pflichtdokumente angelegt sind
    public static readonly List<string> AnhangSchluessel = new List<string>
    {
        "AGB",
        "Disclaimer",
        "Barzahlung",
        "\u00dcberweisung"
    };

    // Prüft welche Anhänge im Pool vorhanden und mit Inhalt gefüllt sind
    public static Dictionary<string, bool> HoleVerfuegbareAnhaenge()
    {
        var ergebnis = new Dictionary<string, bool>();
        foreach (string key in AnhangSchluessel)
            ergebnis[key] = false;

        try
        {
            var alle = DocumentDashboard.GetSavedDocuments();
            if (alle?.savedDocs == null) return ergebnis;

            foreach (var doc in alle.savedDocs)
            {
                if (!AnhangSchluessel.Contains(doc.title)) continue;
                if (!AnhangSchluessel.Contains(doc.title)) continue;
                if (!string.IsNullOrWhiteSpace(doc.inhalt))
                    ergebnis[doc.title] = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BelegAnhang] Dokumente konnten nicht geladen werden: " + e.Message);
        }

        return ergebnis;
    }

    // Schreibt die ausgewählten Anhänge als zusätzliche Seiten in das PDF
    public static void SchreibeAnhaenge(Document document, List<string> ausgewaehlt)
    {
        if (ausgewaehlt == null || ausgewaehlt.Count == 0) return;

        try
        {
            var alle = DocumentDashboard.GetSavedDocuments();
            if (alle?.savedDocs == null) return;

            var titelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var textFont  = FontFactory.GetFont(FontFactory.HELVETICA, 11);
            var linie     = new LineSeparator();

            foreach (string key in ausgewaehlt)
            {
                var doc = alle.savedDocs.Find(d => d.title == key);

                if (doc == null || string.IsNullOrWhiteSpace(doc.inhalt)) continue;

                document.NewPage();
                document.Add(new Paragraph(doc.title, titelFont));
                document.Add(new Paragraph(" "));
                document.Add(new Chunk(linie));
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(doc.inhalt, textFont));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BelegAnhang] PDF-Anhang fehlgeschlagen: " + e.Message);
        }
    }
}
