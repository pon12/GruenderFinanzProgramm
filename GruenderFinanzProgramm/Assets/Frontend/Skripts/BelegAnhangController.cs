using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;

// Eindeutige Aliase für iTextSharp
using ITextFont = iTextSharp.text.Font;
using ITextDocument = iTextSharp.text.Document;
using ITextParagraph = iTextSharp.text.Paragraph;

public static class BelegAnhangController
{
    // ============================================================
    // AUSWÄHLBARE ANHÄNGE
    // Kontodaten sind NICHT auswählbar, da sie immer ausgegeben
    // werden sollen.
    // ============================================================

    public static readonly List<string> AnhangSchluessel =
        new List<string>
        {
            "Zahlungsbedingungen",
            "AGB",
            "Disclaimer",
            "SEPA-Basislastschrift-Mandat",
            "Widerrufsbelehrung",
            "Barzahlung",
            "Überweisung",
            "Mahnverfahren",
            "Ratenzahlungsbestimmungen",
            "Copyright Hinweis",
            "Lizenzhinweis Einfach",
            "Lizenzhinweis Erweitert",
            "Datenschutzerklärung",
            "Vertraulichkeitserklärung",
            "Kundenzufriedenheitsumfrage",
            "Dienstleistungskatalog"
        };


    // ============================================================
    // BEZAHLWEISE
    // ============================================================

    private static readonly HashSet<string> BezahlweiseGruppe =
        new HashSet<string>
        {
            "Barzahlung",
            "Überweisung"
        };


    // ============================================================
    // VERFÜGBARE ANHÄNGE PRÜFEN
    // ============================================================

    public static Dictionary<string, bool> HoleVerfuegbareAnhaenge()
    {
        var ergebnis =
            new Dictionary<string, bool>();

        try
        {
            var alle =
                DocumentDashboard.GetSavedDocuments();

            if (alle == null ||
                alle.savedDocs == null)
            {
                return ergebnis;
            }


            foreach (string schluessel in AnhangSchluessel)
            {
                var dokument =
                    FindeDokument(
                        alle.savedDocs,
                        schluessel
                    );

                ergebnis[schluessel] =
                    dokument != null &&
                    HatDokumentInhalt(dokument);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "[BelegAnhang] Fehler beim Prüfen der Anhänge: " +
                e.Message
            );
        }

        return ergebnis;
    }


    // ============================================================
    // KONTODATEN IMMER AUSGEBEN
    // Kontodaten sind nicht auswählbar, werden aber in jedem
    // Rechnungs-/Angebots-PDF ausgegeben.
    // ============================================================

    public static void SchreibeKontodaten(ITextDocument document)
    {
        if (document == null)
            return;

        try
        {
            var konto = DocumentDashboard.GetKontodatenFelder();

            ITextFont titelFont = FontFactory.GetFont(
                FontFactory.HELVETICA_BOLD, 14);

            ITextFont textFont = FontFactory.GetFont(
                FontFactory.HELVETICA, 10);

            ITextFont feldLabelFont = FontFactory.GetFont(
                FontFactory.HELVETICA_BOLD, 10);

            document.NewPage();
            document.Add(new ITextParagraph("Bankverbindung", titelFont));
            document.Add(new ITextParagraph(" "));
            document.Add(new Chunk(new LineSeparator()));
            document.Add(new ITextParagraph(" "));

            SchreibeFeld(document, "Kontoinhaber",
                HoleDictionaryWert(konto, "kontoinhaber"),
                textFont, feldLabelFont);

            SchreibeFeld(document, "Kreditinstitut",
                HoleDictionaryWert(konto, "bank"),
                textFont, feldLabelFont);

            SchreibeFeld(document, "IBAN",
                HoleDictionaryWert(konto, "iban"),
                textFont, feldLabelFont);

            SchreibeFeld(document, "BIC",
                HoleDictionaryWert(konto, "bic"),
                textFont, feldLabelFont);
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[BelegAnhang] Fehler beim Schreiben der Kontodaten: " + e);
        }
    }


    // ============================================================
    // ANHÄNGE IN PDF SCHREIBEN
    // ============================================================

    public static void SchreibeAnhaenge(
        ITextDocument document,
        List<string> ausgewaehlt,
        string status = "",
        string belegTyp = "Rechnung")
    {
        if (document == null)
            return;


        try
        {
            var alle =
                DocumentDashboard.GetSavedDocuments();

            if (alle == null ||
                alle.savedDocs == null)
            {
                Debug.LogWarning(
                    "[BelegAnhang] Keine Dokumentdaten gefunden."
                );

                return;
            }


            // ========================================================
            // SCHRIFTEN
            // ========================================================

            ITextFont titelFont =
                FontFactory.GetFont(
                    FontFactory.HELVETICA_BOLD,
                    14
                );

            ITextFont textFont =
                FontFactory.GetFont(
                    FontFactory.HELVETICA,
                    10
                );

            ITextFont feldLabelFont =
                FontFactory.GetFont(
                    FontFactory.HELVETICA_BOLD,
                    10
                );

            LineSeparator linie =
                new LineSeparator();


            // ========================================================
            // 1. BEZAHLWEISE
            // ========================================================

            var bezahlweisen =
                (ausgewaehlt ?? new List<string>())
                .Where(
                    x => BezahlweiseGruppe.Contains(x)
                )
                .ToList();


            if (bezahlweisen.Count > 0)
            {
                document.NewPage();

                document.Add(
                    new ITextParagraph(
                        "Bezahlweise",
                        titelFont
                    )
                );

                document.Add(
                    new ITextParagraph(" ")
                );

                document.Add(
                    new Chunk(linie)
                );

                document.Add(
                    new ITextParagraph(" ")
                );


                bool hatBarzahlung =
                    bezahlweisen.Contains(
                        "Barzahlung"
                    );

                bool hatUeberweisung =
                    bezahlweisen.Contains(
                        "Überweisung"
                    );


                string zahlungswege;

                if (hatBarzahlung &&
                    hatUeberweisung)
                {
                    zahlungswege =
                        "Barzahlung oder Überweisung";
                }
                else if (hatBarzahlung)
                {
                    zahlungswege =
                        "Barzahlung";
                }
                else
                {
                    zahlungswege =
                        "Überweisung";
                }


                document.Add(
                    new ITextParagraph(
                        "Zahlungsweise: " +
                        zahlungswege,
                        textFont
                    )
                );

                document.Add(
                    new ITextParagraph(" ")
                );


                // ----------------------------------------------------
                // BARZAHLUNG
                // Inhalt kommt aus dem Dokumente-Screen
                // ----------------------------------------------------

                if (hatBarzahlung)
                {
                    var barDok =
                        FindeDokument(
                            alle.savedDocs,
                            "Barzahlung"
                        );

                    SchreibeStrukturDokument(
                        document,
                        barDok,
                        textFont,
                        feldLabelFont
                    );
                }


                // ----------------------------------------------------
                // ÜBERWEISUNG
                // Inhalt kommt aus dem Dokumente-Screen
                // ----------------------------------------------------

                if (hatUeberweisung)
                {
                    var ueberweisungDok =
                        FindeDokument(
                            alle.savedDocs,
                            "Überweisung"
                        );

                    SchreibeStrukturDokument(
                        document,
                        ueberweisungDok,
                        textFont,
                        feldLabelFont
                    );
                }
            }

            // ========================================================
            // 3. NORMALE ANHÄNGE
            //
            // AGB, Disclaimer, Zahlungsbedingungen usw.
            // kommen ausschließlich aus dem Dokumente-Screen.
            // ========================================================

            var normaleAnhaenge =
                (ausgewaehlt ?? new List<string>())
                .Where(
                    x =>
                        !BezahlweiseGruppe.Contains(x) &&
                        x != "Kontodaten (IBAN/BIC)"
                )
                .ToList();


            foreach (string schluessel in normaleAnhaenge)
            {
                var dokument =
                    FindeDokument(
                        alle.savedDocs,
                        schluessel
                    );


                if (dokument == null)
                {
                    Debug.LogWarning(
                        "[BelegAnhang] Dokument nicht gefunden: " +
                        schluessel
                    );

                    continue;
                }


                if (!HatDokumentInhalt(dokument))
                {
                    Debug.LogWarning(
                        "[BelegAnhang] Dokument hat keinen Inhalt: " +
                        schluessel
                    );

                    continue;
                }


                document.NewPage();


                document.Add(
                    new ITextParagraph(
                        schluessel,
                        titelFont
                    )
                );

                document.Add(
                    new ITextParagraph(" ")
                );

                document.Add(
                    new Chunk(linie)
                );

                document.Add(
                    new ITextParagraph(" ")
                );


                // ----------------------------------------------------
                // Strukturierte Dokumente
                // ----------------------------------------------------

                if (dokument.strukturFelder != null &&
                    dokument.strukturFelder.Count > 0)
                {
                    SchreibeStrukturDokument(
                        document,
                        dokument,
                        textFont,
                        feldLabelFont
                    );
                }
                else
                {
                    // ------------------------------------------------
                    // Normales Freitext-Dokument
                    // ------------------------------------------------

                    document.Add(
                        new ITextParagraph(
                            dokument.inhalt ?? "",
                            textFont
                        )
                    );
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[BelegAnhang] Fehler beim Schreiben der Anhänge: " +
                e
            );
        }
    }
    // ============================================================
    // DOKUMENT SUCHEN
    // ============================================================

    private static DocumentDashboard.DocumentData FindeDokument(
        List<DocumentDashboard.DocumentData> dokumente,
        string titel)
    {
        if (dokumente == null || string.IsNullOrWhiteSpace(titel))
            return null;

        // Zuerst exakt nach dem sichtbaren Titel suchen.
        // Dadurch ist die Kategorie egal und alle Dokumente aus dem
        // Dokumente-Screen können gefunden werden.
        var dokument = dokumente.FirstOrDefault(
            d => d != null && d.title == titel
        );

        if (dokument != null)
            return dokument;

        // Bekannte Anzeigenamen -> tatsächliche Dokumenttitel.
        if (titel == "Datenschutzerklärung")
        {
            return dokumente.FirstOrDefault(
                d => d != null &&
                     d.title == "Datenschutzerklärung (DSGVO)"
            );
        }

        if (titel == "Dienstleistungskatalog")
        {
            return dokumente.FirstOrDefault(
                d => d != null &&
                     d.title == "Dienstleistungskatalog / Preisliste"
            );
        }

        return null;
    }
    // ============================================================
    // PRÜFEN, OB DOKUMENT INHALT HAT
    // ============================================================

    private static bool HatDokumentInhalt(
        DocumentDashboard.DocumentData dokument)
    {
        if (dokument == null)
            return false;


        if (!string.IsNullOrWhiteSpace(
            dokument.inhalt))
        {
            return true;
        }


        if (dokument.strukturFelder != null)
        {
            return dokument.strukturFelder.Any(
                f =>
                    !string.IsNullOrWhiteSpace(
                        f.wert
                    )
            );
        }


        return false;
    }


    // ============================================================
    // STRUKTURIERTES DOKUMENT AUSGEBEN
    // ============================================================

    private static void SchreibeStrukturDokument(
        ITextDocument document,
        DocumentDashboard.DocumentData dokument,
        ITextFont textFont,
        ITextFont feldLabelFont)
    {
        if (dokument == null)
            return;


        // ------------------------------------------------------------
        // Strukturfelder
        // ------------------------------------------------------------

        if (dokument.strukturFelder != null)
        {
            foreach (
                var feld
                in dokument.strukturFelder)
            {
                if (string.IsNullOrWhiteSpace(
                    feld.wert))
                {
                    continue;
                }


                string label =
                    HoleFeldLabel(
                        dokument.title,
                        feld.key
                    );


                SchreibeFeld(
                    document,
                    label,
                    feld.wert,
                    textFont,
                    feldLabelFont
                );
            }
        }


        // ------------------------------------------------------------
        // Falls zusätzlich Freitext vorhanden ist
        // ------------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
            dokument.inhalt))
        {
            document.Add(
                new ITextParagraph(
                    dokument.inhalt ?? "",
                    textFont
                )
            );
        }
    }


    // ============================================================
    // EINZELNES FELD AUSGEBEN
    // ============================================================

    private static void SchreibeFeld(
        ITextDocument document,
        string label,
        string wert,
        ITextFont textFont,
        ITextFont feldLabelFont)
    {
        if (string.IsNullOrWhiteSpace(wert))
            return;


        ITextParagraph paragraph =
            new ITextParagraph();


        paragraph.Add(
            new Chunk(
                label + ": ",
                feldLabelFont
            )
        );


        paragraph.Add(
            new Chunk(
                wert,
                textFont
            )
        );


        document.Add(paragraph);

        document.Add(
            new ITextParagraph(" ")
        );
    }


    // ============================================================
    // LESBARES LABEL FÜR STRUKTURFELD
    // ============================================================

    private static string HoleFeldLabel(
        string dokumentTitel,
        string key)
    {
        switch (key)
        {
            case "iban":
                return "IBAN";

            case "bic":
                return "BIC";

            case "bank":
                return "Bank";

            case "kontoinhaber":
                return "Kontoinhaber";

            case "zahlungsfrist":
                return "Zahlungsfrist";

            case "skontoSatz":
                return "Skonto-Satz";

            case "skontoZeitraum":
                return "Skonto-Zeitraum";

            case "verzugszins":
                return "Verzugszins";

            case "zusatzhinweise":
                return "Zusatzhinweise";

            case "leistungsbereich":
                return "Leistungsbereich";

            case "widerrufsfrist":
                return "Widerrufsfrist";

            case "zahlungsziel":
                return "Zahlungsziel";

            case "verzugszinssatz":
                return "Verzugszinssatz";

            case "mahngebuehr":
                return "Mahngebühr";

            case "abnahmefrist":
                return "Abnahmefrist";

            case "lizenzmodell":
                return "Lizenzmodell";

            case "schadensersatzfaktor":
                return "Schadensersatzfaktor";

            case "ndaDauer":
                return "NDA-Dauer";

            case "gerichtsstand":
                return "Gerichtsstand";

            case "geltungsbereich":
                return "Geltungsbereich";

            case "inhaltlichePruefung":
                return "Inhaltliche Prüfung";

            case "externeVerweise":
                return "Externe Verweise";

            case "urheberrechtshinweis":
                return "Urheberrechtshinweis";

            case "gueltigkeitsbereich":
                return "Gültigkeitsbereich";

            case "zusatzhinweis":
                return "Zusatzhinweis";

            case "ausfuehrung":
                return "Ausführung";

            case "glaeubigerId":
                return "Gläubiger-Identifikationsnummer";

            case "artZahlung":
                return "Art der Zahlung";

            case "zusatzangaben":
                return "Zusatzangaben";

            case "wertersatzklausel":
                return "Wertersatz-Klausel";

            case "kontakt":
                return "Kontakt";

            case "vorzeitigesErloeschen":
                return "Vorzeitiges Erlöschen";

            case "bearbeitungspauschale":
                return "Bearbeitungspauschale";

            case "intervallMahnstufen":
                return "Intervall der Mahnstufen";

            case "mindestauftragswert":
                return "Mindestauftragswert";

            case "maxLaufzeit":
                return "Max. Laufzeit";

            case "bearbeitungsgebuehr":
                return "Bearbeitungsgebühr";

            case "schutzumfang":
                return "Schutzumfang";

            case "referenzklausel":
                return "Referenzklausel";

            case "zusatzangabe":
                return "Zusatzangabe";

            case "preisErweiterteNutzung":
                return "Preis für erweiterte Nutzung";

            default:
                return key;
        }
    }


    // ============================================================
    // DICTIONARY-WERT AUSLESEN
    // ============================================================

    private static string HoleDictionaryWert(
        Dictionary<string, string> daten,
        string key)
    {
        if (daten == null)
            return "";


        if (daten.TryGetValue(
            key,
            out string wert))
        {
            return wert ?? "";
        }


        return "";
    }
public static void FuegeDokumentPdfAn(
        string zielPdf,
        List<string> ausgewaehlteAnhaenge)
    {
        if (string.IsNullOrWhiteSpace(zielPdf))
            return;

        if (ausgewaehlteAnhaenge == null ||
            ausgewaehlteAnhaenge.Count == 0)
            return;

        string tempPdf = zielPdf + ".base.pdf";

        try
        {
            if (!File.Exists(zielPdf))
            {
                Debug.LogWarning(
                    "[BelegAnhang] Haupt-PDF nicht gefunden: " + zielPdf);
                return;
            }

            if (File.Exists(tempPdf))
                File.Delete(tempPdf);

            File.Move(zielPdf, tempPdf);

            var alle = DocumentDashboard.GetSavedDocuments();

            if (alle?.savedDocs == null)
            {
                File.Move(tempPdf, zielPdf);
                return;
            }

            using (FileStream fs = new FileStream(
                zielPdf, FileMode.Create, FileAccess.Write))
            {
                ITextDocument document = new ITextDocument();
                PdfCopy copy = new PdfCopy(document, fs);
                document.Open();

                // --------------------------------------------------
                // 1. Haupt-PDF (Rechnung / Angebot)
                // --------------------------------------------------
                PdfReader mainReader = null;

                try
                {
                    mainReader = new PdfReader(tempPdf);

                    for (int page = 1;
                         page <= mainReader.NumberOfPages;
                         page++)
                    {
                        copy.AddPage(
                            copy.GetImportedPage(
                                mainReader,
                                page
                            )
                        );
                    }
                }
                finally
                {
                    if (mainReader != null)
                        mainReader.Close();
                }

                // --------------------------------------------------
                // 2. Ausgewählte Dokument-PDFs
                //    Jedes Dokument kommt als vollständige PDF hinein.
                // --------------------------------------------------
                foreach (string titel in ausgewaehlteAnhaenge.Distinct())
                {
                    if (string.IsNullOrWhiteSpace(titel))
                        continue;

                    // Kontodaten sind nicht auswählbar und werden bereits
                    // im Haupt-PDF ausgegeben.
                    if (titel == "Kontodaten (IBAN/BIC)")
                        continue;

                    var dokument = FindeDokument(alle.savedDocs, titel);

                    if (dokument == null)
                    {
                        Debug.LogWarning(
                            "[BelegAnhang] Dokument nicht gefunden: " + titel);
                        continue;
                    }

                    // Die echte, bereits für den Dokumente-Screen vorgesehene
                    // PDF erzeugen bzw. deren Pfad erhalten.
                    string dokumentPdf =
                        DokumentPdfGenerator.ErstellePdfFuerDokument(dokument);

                    if (string.IsNullOrWhiteSpace(dokumentPdf))
                    {
                        Debug.LogWarning(
                            "[BelegAnhang] Keine PDF für Dokument: " + titel);
                        continue;
                    }

                    if (!File.Exists(dokumentPdf))
                    {
                        Debug.LogWarning(
                            "[BelegAnhang] PDF-Datei nicht gefunden: " +
                            dokumentPdf);
                        continue;
                    }

                    Debug.Log(
                        "[BelegAnhang] Füge vollständige Dokument-PDF an: " +
                        titel + " -> " + dokumentPdf);

                    PdfReader dokumentReader = null;

try
{
    dokumentReader = new PdfReader(dokumentPdf);

    for (int page = 1;
         page <= dokumentReader.NumberOfPages;
         page++)
    {
        copy.AddPage(
            copy.GetImportedPage(
                dokumentReader,
                page
            )
        );
    }
}
finally
{
    if (dokumentReader != null)
        dokumentReader.Close();
}
                }

                document.Close();
            }

            if (File.Exists(tempPdf))
                File.Delete(tempPdf);

            Debug.Log(
                "[BelegAnhang] PDF erfolgreich mit Dokument-PDFs zusammengeführt: " +
                zielPdf);
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[BelegAnhang] PDF-Anhänge konnten nicht zusammengeführt werden: " +
                e);

            try
            {
                if (File.Exists(zielPdf))
                    File.Delete(zielPdf);

                if (File.Exists(tempPdf))
                    File.Move(tempPdf, zielPdf);
            }
            catch (Exception restoreException)
            {
                Debug.LogError(
                    "[BelegAnhang] Wiederherstellung der PDF fehlgeschlagen: " +
                    restoreException);
            }
        }
    }

}