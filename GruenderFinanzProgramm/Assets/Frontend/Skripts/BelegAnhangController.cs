using System;
using System.Collections.Generic;
using UnityEngine;

public static class BelegAnhangController
{
    public static readonly List<string> AnhangSchluessel = new List<string>
    {
        "AGB",
        "Disclaimer",
        "Barzahlung",
        "Überweisung",
        "Bezahlweise"
    };

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
                string titelKlein = (doc.title ?? "").ToLowerInvariant();

                if (titelKlein.Contains("agb") || titelKlein.Contains("allgemeine geschäftsbedingungen"))
                    ergebnis["AGB"] = true;

                if (titelKlein.Contains("disclaimer") || titelKlein.Contains("haftungsausschluss"))
                    ergebnis["Disclaimer"] = true;

                if (doc.category == "Bezahlweise" && titelKlein.Contains("bar"))
                    ergebnis["Barzahlung"] = true;

                if (doc.category == "Bezahlweise" &&
                    (titelKlein.Contains("überweisung") || titelKlein.Contains("uberweisung")
                     || titelKlein.Contains("konto") || titelKlein.Contains("iban")
                     || titelKlein.Contains("zahlung")))
                    ergebnis["Überweisung"] = true;

                if (doc.category == "Bezahlweise")
                    ergebnis["Bezahlweise"] = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BelegAnhang] Dokumente konnten nicht geladen werden: " + e.Message);
        }

        return ergebnis;
    }

    public static DocumentDashboard.DocumentData HoleDokument(string schluessel)
    {
        try
        {
            var alle = DocumentDashboard.GetSavedDocuments();
            if (alle?.savedDocs == null) return null;

            foreach (var doc in alle.savedDocs)
            {
                string titelKlein = (doc.title ?? "").ToLowerInvariant();

                switch (schluessel)
                {
                    case "AGB":
                        if (titelKlein.Contains("agb") || titelKlein.Contains("allgemeine geschäftsbedingungen"))
                            return doc;
                        break;
                    case "Disclaimer":
                        if (titelKlein.Contains("disclaimer") || titelKlein.Contains("haftungsausschluss"))
                            return doc;
                        break;
                    case "Barzahlung":
                        if (doc.category == "Bezahlweise" && titelKlein.Contains("bar"))
                            return doc;
                        break;
                    case "Überweisung":
                        if (doc.category == "Bezahlweise" &&
                            (titelKlein.Contains("überweisung") || titelKlein.Contains("uberweisung")
                             || titelKlein.Contains("konto") || titelKlein.Contains("iban")
                             || titelKlein.Contains("zahlung")))
                            return doc;
                        break;
                    case "Bezahlweise":
                        if (doc.category == "Bezahlweise")
                            return doc;
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BelegAnhang] Dokument nicht gefunden: " + e.Message);
        }
        return null;
    }

    public static void SchreibeAnhaenge(
        iTextSharp.text.Document document,
        List<string> ausgewaehlteSchluessel)
    {
        if (ausgewaehlteSchluessel == null || ausgewaehlteSchluessel.Count == 0) return;

        var titelFont = iTextSharp.text.FontFactory.GetFont(
            iTextSharp.text.FontFactory.HELVETICA_BOLD, 14);
        var textFont = iTextSharp.text.FontFactory.GetFont(
            iTextSharp.text.FontFactory.HELVETICA, 11);
        var subFont = iTextSharp.text.FontFactory.GetFont(
            iTextSharp.text.FontFactory.HELVETICA_OBLIQUE, 9);

        foreach (string schluessel in ausgewaehlteSchluessel)
        {
            var doc = HoleDokument(schluessel);
            if (doc == null)
            {
                Debug.LogWarning("[BelegAnhang] Kein Dokument für Schlüssel: " + schluessel);
                continue;
            }

            try
            {
                document.NewPage();
                document.Add(new iTextSharp.text.Paragraph(schluessel, titelFont));
                document.Add(new iTextSharp.text.Paragraph(
                    "Kategorie: " + doc.category + "  |  " + DateTime.Now.ToString("dd.MM.yyyy"), subFont));
                document.Add(new iTextSharp.text.Paragraph(" "));
                var linie = new iTextSharp.text.pdf.draw.LineSeparator();
                document.Add(new iTextSharp.text.Chunk(linie));
                document.Add(new iTextSharp.text.Paragraph(" "));

                string inhalt = !string.IsNullOrWhiteSpace(doc.inhalt) ? doc.inhalt : doc.title;
                string[] zeilen = inhalt.Split(new[] { '\n', '\r' }, StringSplitOptions.None);

                foreach (string zeile in zeilen)
                {
                    if (string.IsNullOrEmpty(zeile.Trim()))
                        document.Add(new iTextSharp.text.Paragraph(" "));
                    else
                        document.Add(new iTextSharp.text.Paragraph(zeile, textFont));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BelegAnhang] Fehler beim Schreiben von " + schluessel + ": " + e.Message);
            }
        }
    }
}