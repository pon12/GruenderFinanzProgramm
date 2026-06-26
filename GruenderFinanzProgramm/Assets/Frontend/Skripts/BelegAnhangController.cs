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
    public static readonly List<string> AnhangSchluessel = new List<string>
    {
        "AGB",
        "Disclaimer",
        "Barzahlung",
        "Überweisung"
    };

    // Diese Titel werden zu einer gemeinsamen "Bezahlweise"-Seite zusammengefasst
    private static readonly HashSet<string> BezahlweiseGruppe = new HashSet<string>
    {
        "Barzahlung",
        "Überweisung"
    };

    // Gibt alle Titel aus der Bezahlweise-Kategorie zurück
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
                if (string.IsNullOrEmpty(doc.title)) continue;
                if (!AnhangSchluessel.Contains(doc.title)) continue;

                bool hatInhalt = !string.IsNullOrWhiteSpace(doc.inhalt);

                if (doc.category != "Bezahlweise") continue;
                if (string.IsNullOrEmpty(doc.title)) continue;

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

    // Schreibt die ausgewählten Anhänge als zusätzliche Seiten in das PDF.
    // Barzahlung und Überweisung werden auf einer gemeinsamen "Bezahlweise"-Seite zusammengefasst.
    // belegTyp: "Angebot" oder "Rechnung"
    // status: aktueller Belegstatus z.B. "Bezahlt", "Angenommen"
    public static void SchreibeAnhaenge(Document document, List<string> ausgewaehlt, string status = "", string belegTyp = "Rechnung")
    {
        if (ausgewaehlt == null || ausgewaehlt.Count == 0) return;

        try
        {
            var alle = DocumentDashboard.GetSavedDocuments();

            var titelFont  = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var textFont   = FontFactory.GetFont(FontFactory.HELVETICA, 11);
            var kursivFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 11);
            var linie      = new LineSeparator();

            // Bezahlweise-Gruppe separat behandeln
            var bezahlweiseAusgewaehlt = ausgewaehlt.Where(k => BezahlweiseGruppe.Contains(k)).ToList();

            // Kontodaten erscheinen nur im Footer — nicht als Anhangseite
            var nurFooter = new HashSet<string> { "Kontodaten (IBAN/BIC)", "Zahlungsbedingungen" };
            var einzelAnhaenge = ausgewaehlt
                .Where(k => !BezahlweiseGruppe.Contains(k) && !nurFooter.Contains(k))
                .ToList();

            // Kombinierte Bezahlweise-Seite
            if (bezahlweiseAusgewaehlt.Count > 0)
            {
                bool hatBar          = bezahlweiseAusgewaehlt.Contains("Barzahlung");
                bool hatUeberweisung = bezahlweiseAusgewaehlt.Contains("Überweisung");
                bool istBezahlt      = status == "Bezahlt";
                bool istAngebot      = belegTyp == "Angebot";

                string zahlungswege;
                if (hatBar && hatUeberweisung)
                    zahlungswege = "Barzahlung oder per Banküberweisung";
                else if (hatBar)
                    zahlungswege = "Barzahlung";
                else
                    zahlungswege = "Banküberweisung";

                document.NewPage();
                document.Add(new Paragraph("Bezahlweise", titelFont));
                document.Add(new Paragraph(" "));
                document.Add(new Chunk(linie));
                document.Add(new Paragraph(" "));

                // Einleitungstext je nach Kontext
                string einleitung;
                if (istAngebot)
                    einleitung =
                        "Die angebotene Leistung kann mit folgenden Zahlungsmitteln beglichen werden: " +
                        zahlungswege + ". " +
                        "Bitte teilen Sie uns bei Auftragserteilung Ihre bevorzugte Zahlungsweise mit.";
                else if (istBezahlt)
                    einleitung =
                        "Der Rechnungsbetrag wurde beglichen über: " +
                        zahlungswege + ". " +
                        "Vielen Dank für Ihre Zahlung.";
                else
                    einleitung =
                        "Der angegebene Betrag ist innerhalb der angegebenen Zahlungsfrist zu begleichen über: " +
                        zahlungswege + ". " +
                        "Bei Fragen stehen wir Ihnen gerne zur Verfügung.";

                document.Add(new Paragraph(einleitung, textFont));
                document.Add(new Paragraph(" "));

                // Bankverbindung bei Überweisung ausgeben
                if (hatUeberweisung)
                {
                    string iban           = PlayerPrefs.GetString("settings_iban", "");
                    string bic            = PlayerPrefs.GetString("settings_bic", "");
                    string kreditinstitut = PlayerPrefs.GetString("settings_kreditinstitut", "");
                    string kontoinhaber   = PlayerPrefs.GetString("settings_kontoinhaber", "");

                    if (string.IsNullOrEmpty(iban))
                    {
                        var kontoDok = alle.savedDocs.Find(
                            d => d.category == "Bezahlweise" && d.title == "Kontodaten (IBAN/BIC)");
                        if (kontoDok?.strukturFelder != null)
                        {
                            iban           = HoleFeld(kontoDok, "iban");
                            bic            = HoleFeld(kontoDok, "bic");
                            kreditinstitut = HoleFeld(kontoDok, "bank");
                            kontoinhaber   = HoleFeld(kontoDok, "kontoinhaber");
                        }
                    }

                    document.Add(new Paragraph("Bankverbindung:", titelFont));
                    document.Add(new Paragraph(" "));
                    if (!string.IsNullOrEmpty(kreditinstitut))
                        document.Add(new Paragraph("Kreditinstitut: " + kreditinstitut, textFont));
                    if (!string.IsNullOrEmpty(iban))
                        document.Add(new Paragraph("IBAN: " + iban, textFont));
                    if (!string.IsNullOrEmpty(bic))
                        document.Add(new Paragraph("BIC: " + bic, textFont));
                    if (!string.IsNullOrEmpty(kontoinhaber))
                        document.Add(new Paragraph("Kontoinhaber: " + kontoinhaber, textFont));
                    document.Add(new Paragraph(" "));
                }

                // Zusätzlicher Hinweistext aus Barzahlung-Dokument
                if (hatBar)
                {
                    var barDok = alle.savedDocs.Find(
                        d => d.category == "Bezahlweise" && d.title == "Barzahlung");
                    if (barDok != null && !string.IsNullOrWhiteSpace(barDok.inhalt))
                    {
                        document.Add(new Paragraph(barDok.inhalt, kursivFont));
                        document.Add(new Paragraph(" "));
                    }
                }
            }

            // Einzelne Anhänge (AGB, Disclaimer etc.)
            foreach (string key in einzelAnhaenge)
            {
                var doc = alle.savedDocs.Find(d => d.category == "Bezahlweise" && d.title == key);
                if (doc == null) continue;

                string inhaltText = HoleInhalt(doc);
                if (string.IsNullOrWhiteSpace(inhaltText)) continue;

                document.NewPage();
                document.Add(new Paragraph("Zahlungshinweise", titelFont));
                document.Add(new Paragraph(" "));
                document.Add(new Chunk(linie));
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph(inhaltText, textFont));
            }

            foreach (string key in ausgewaehlt)
            {
                if (key == "Barzahlung" || key == "Überweisung")
                    continue;

                string inhaltText = "";

                var doc = alle?.savedDocs?.Find(d => d.title == key);
                if (doc == null) continue;

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
                document.Add(new Paragraph(key, titelFont));
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

    private static string HoleInhalt(DocumentDashboard.DocumentData doc)
    {
        if (!string.IsNullOrWhiteSpace(doc.inhalt)) return doc.inhalt;
        if (doc.strukturFelder != null && doc.strukturFelder.Count > 0)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var feld in doc.strukturFelder)
                if (!string.IsNullOrWhiteSpace(feld.wert))
                    sb.AppendLine(feld.key + ": " + feld.wert);
            return sb.ToString().Trim();
        }
        return "";
    }

    private static string HoleFeld(DocumentDashboard.DocumentData doc, string key)
    {
        if (doc?.strukturFelder == null) return "";
        var feld = doc.strukturFelder.Find(f => f.key == key);
        return feld?.wert ?? "";
    }
}