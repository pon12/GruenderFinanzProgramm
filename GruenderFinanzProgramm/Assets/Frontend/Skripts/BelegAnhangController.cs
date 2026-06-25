using System;
using System.Collections.Generic;
using System.Linq;
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

    // Gibt alle Titel aus der Bezahlweise-Kategorie zurück (dynamisch aus dem Pool)
    public static List<string> HoleAlleBezahlweiseTitel()
    {
        var titel = new List<string>();
        try
        {
            var alle = DocumentDashboard.GetSavedDocuments();
            if (alle?.savedDocs == null) return titel;
            foreach (var doc in alle.savedDocs)
            {
                if (doc.category == "Bezahlweise" && !string.IsNullOrEmpty(doc.title))
                    if (!titel.Contains(doc.title))
                        titel.Add(doc.title);
        }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BelegAnhang] Titel konnten nicht geladen werden: " + e.Message);
        }
        return titel;
    }

    // Prüft welche Anhänge im Pool vorhanden und mit Inhalt gefüllt sind.
    // Pflichtdokumente mit Strukturfeldern gelten als vorhanden wenn
    // mindestens ein Strukturfeld einen Wert enthält.
    public static Dictionary<string, bool> HoleVerfuegbareAnhaenge()
    {
        var ergebnis = new Dictionary<string, bool>();
        try
        {
            var alle = DocumentDashboard.GetSavedDocuments();
            if (alle?.savedDocs == null) return ergebnis;

            foreach (var doc in alle.savedDocs)
            {
                if (doc.category != "Bezahlweise") continue;
                if (string.IsNullOrEmpty(doc.title)) continue;

                bool hatInhalt = !string.IsNullOrWhiteSpace(doc.inhalt);

                bool hatStrukturfelder = doc.strukturFelder != null
                    && doc.strukturFelder.Count > 0
                    && doc.strukturFelder.Any(f => !string.IsNullOrWhiteSpace(f.wert));

                ergebnis[doc.title] = hatInhalt || hatStrukturfelder;
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
                var doc = alle.savedDocs.Find(d => d.category == "Bezahlweise" && d.title == key);
                if (doc == null) continue;

                // Inhalt aus Freitext oder Strukturfeldern zusammenbauen
                string inhaltText = "";
                if (!string.IsNullOrWhiteSpace(doc.inhalt))
                {
                    inhaltText = doc.inhalt;
                }
                else if (doc.strukturFelder != null && doc.strukturFelder.Count > 0)
                {
                    var zeilen = new System.Text.StringBuilder();
                    foreach (var feld in doc.strukturFelder)
                    {
                        if (!string.IsNullOrWhiteSpace(feld.wert))
                            zeilen.AppendLine(feld.key + ": " + feld.wert);
                    }
                    inhaltText = zeilen.ToString().Trim();
                }

                if (string.IsNullOrWhiteSpace(inhaltText)) continue;

                document.NewPage();
                document.Add(new Paragraph(doc.title, titelFont));
                document.Add(new Paragraph(" "));
                document.Add(new Chunk(linie));
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(inhaltText, textFont));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BelegAnhang] PDF-Anhang fehlgeschlagen: " + e.Message);
        }
    }
}
