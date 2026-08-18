// ================================================================
// DokumentPdfGenerator.cs
//
// Zentrale PDF-Erzeugung für die strukturierten Pflichtdokumente aus dem
// Dokumente-Screen (Gründungsurkunde, Handelsregisterauszug, Gesellschafts-
// vertrag, ...). Vorher gab es dafür GAR KEINEN echten PDF-Export - der
// generische Export-Screen-Code hat nur den TITEL des Dokuments in eine
// PDF geschrieben, nicht die eigentlichen erfassten Werte.
//
// Aufbau: ein generischer Renderer (ErstellePdf) für Titel + Textblöcke +
// Unterschriftenbereich, plus pro Dokumenttyp eine eigene Methode, die aus
// den erfassten Struktur-Feldern den in der Spezifikation vorgegebenen
// Text zusammensetzt.
// ================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using UnityEngine;

public static class DokumentPdfGenerator
{
    // Für "N2"-Formatierung mit deutschem Komma statt Punkt.
    private static readonly System.Globalization.CultureInfo DeKultur =
        System.Globalization.CultureInfo.GetCultureInfo("de-DE");

    // ─────────────────────────────────────────
    // HILFSFUNKTIONEN
    // ─────────────────────────────────────────

    // Holt den Wert eines Struktur-Felds anhand des Keys, oder "" falls nicht gesetzt.
    // Liest eine Zeilenanzahl aus einem Freitext-Feld wie "5" oder
    // "Standard (8 Zeilen)" - nimmt die erste gefundene Zahl, sonst den
    // übergebenen Standardwert. Nach oben gedeckelt, damit ein Tippfehler
    // (z.B. "500") nicht eine unbrauchbar lange PDF erzeugt.
    public static int LeseZeilenAnzahl(string wert, int standard, int max)
    {
        if (string.IsNullOrWhiteSpace(wert)) return standard;
        var treffer = System.Text.RegularExpressions.Regex.Match(wert, @"\d+");
        if (!treffer.Success) return standard;
        if (!int.TryParse(treffer.Value, out int zahl) || zahl <= 0) return standard;
        return Math.Min(zahl, max);
    }

    public static string Feld(List<DocumentDashboard.StrukturFeldWert> felder, string key)
    {
        if (felder == null) return "";
        var eintrag = felder.FirstOrDefault(f => f.key == key);
        return string.IsNullOrWhiteSpace(eintrag?.wert) ? "" : eintrag.wert;
    }

    public struct Firmendaten
    {
        public string Name;
        public string Rechtsform;
        public string Anschrift;
        public string SteuerNr;
        public string UstIdNr;
    }

    public static Firmendaten HoleFirmendaten()
    {
        var ergebnis = new Firmendaten { Name = "", Rechtsform = "", Anschrift = "", SteuerNr = "", UstIdNr = "" };
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            var company = db?.getAllCompanies()?.FirstOrDefault();
            if (company == null) return ergebnis;

            string rechtsformText = (company.legalForm >= 0 && company.legalForm < Company.LegalForms.Count)
                ? Company.LegalForms[company.legalForm]
                : "";

            string strasse = company.strasseuHausNr ?? "";
            string plzOrt = company.plz > 0 ? $"{company.plz} {company.location}".Trim() : (company.location ?? "");
            string anschrift = string.Join(", ", new[] { strasse, plzOrt }.Where(s => !string.IsNullOrWhiteSpace(s)));

            ergebnis.Name = company.name ?? "";
            ergebnis.Rechtsform = rechtsformText;
            ergebnis.Anschrift = anschrift;
            ergebnis.SteuerNr = company.steuerNr ?? "";
            ergebnis.UstIdNr = company.ustIdNr ?? "";
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DokumentPdfGenerator] Firmendaten laden fehlgeschlagen: " + e.Message);
        }
        return ergebnis;
    }

    // Unternehmenszweck kommt laut Spezifikation "automatisch aus den
    // Stammdaten" - das Unternehmensstammdaten-Dokument hat aktuell aber
    // (bewusst unverändert gelassen) gar kein Zweck-Feld. Damit die PDFs
    // trotzdem nicht kaputt aussehen, gibt's hier einen klaren Platzhalter
    // statt eines stillen Lochs im Text.
    public static string HoleUnternehmenszweck()
    {
        try
        {
            var gespeichert = DocumentDashboard.GetSavedDocuments();
            var stammdaten = gespeichert?.savedDocs?.FirstOrDefault(d => d.title == "Unternehmensstammdaten");
            // FIX: suchte bisher nach einem Feld-Key "zweck" - das gibt es
            // in den Unternehmensstammdaten (firma/rechtsform/branche/
            // standort) gar nicht und existiert auch sonst nirgends im
            // Projekt. Griff also immer ins Leere, egal was eingetragen
            // wurde. "Branche" ist das einzige vorhandene Feld, das
            // tatsächlich beschreibt, womit sich das Unternehmen befasst.
            string zweck = Feld(stammdaten?.strukturFelder, "branche");
            return string.IsNullOrWhiteSpace(zweck) ? "[Unternehmenszweck noch nicht in den Stammdaten hinterlegt]" : zweck;
        }
        catch
        {
            return "[Unternehmenszweck noch nicht in den Stammdaten hinterlegt]";
        }
    }

    // ─────────────────────────────────────────
    // GENERISCHER PDF-RENDERER
    // ─────────────────────────────────────────

    // bloecke: (istUeberschrift, text) - Überschriften werden fett/größer
    // dargestellt, normale Blöcke als Fließtext.
    public static bool ErstellePdf(string dateipfad, string titel,
        List<(bool istUeberschrift, string text)> bloecke,
        bool mitUnterschrift = true,
        string unterschriftLinks = "(Ort, Datum)",
        string unterschriftRechts = "(Unterschrift)")
    {
        try
        {
            string ordner = Path.GetDirectoryName(dateipfad);
            if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);

            var firma = HoleFirmendaten();

            Document doc = new Document(PageSize.A4, 50, 50, 60, 75);
            using (FileStream fs = new FileStream(dateipfad, FileMode.Create))
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                writer.PageEvent = new PdfFooterEvent(firma.Name, firma.Anschrift, false);

                doc.AddAuthor("Ventoriq");
                doc.AddTitle(titel);
                doc.Open();

                var titelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 17);
                var ueberschriftFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                var grauFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, new iTextSharp.text.Color(128, 128, 128));

                doc.Add(new Paragraph(titel, titelFont));
                doc.Add(new Paragraph(firma.Name + (string.IsNullOrEmpty(firma.Rechtsform) ? "" : " " + firma.Rechtsform), grauFont));
                doc.Add(new Paragraph("Erstellt am: " + DateTime.Now.ToString("dd.MM.yyyy"), grauFont));
                doc.Add(new Paragraph(" "));

                foreach (var (istUeberschrift, text) in bloecke)
                {
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        doc.Add(new Paragraph(" "));
                        continue;
                    }
                    // Gleicher Zeilenumbruch-Fix wie im erweiterten Renderer:
                    // eingebettetes "\n" zeilenweise mit Chunk.NEWLINE statt
                    // als ein durchgehender String.
                    var p = new Paragraph();
                    var zeilen = text.Split('\n');
                    var schrift = istUeberschrift ? ueberschriftFont : textFont;
                    for (int zi = 0; zi < zeilen.Length; zi++)
                    {
                        p.Add(new Chunk(zeilen[zi], schrift));
                        if (zi < zeilen.Length - 1) p.Add(Chunk.NEWLINE);
                    }
                    p.SpacingAfter = istUeberschrift ? 6f : 10f;
                    doc.Add(p);
                }

                if (mitUnterschrift)
                {
                    doc.Add(new Paragraph(" "));
                    doc.Add(new Paragraph(" "));

                    PdfPTable unterschriftTable = new PdfPTable(2);
                    unterschriftTable.WidthPercentage = 100f;
                    // FIX: eingebettetes "\n" in einer einzelnen Phrase wird
                    // von iTextSharp nicht zuverlässig als echter
                    // Zeilenumbruch gerendert - stattdessen landete alles
                    // in einer Zeile zusammengequetscht. Jetzt mit
                    // Chunk.NEWLINE, dem dafür vorgesehenen Weg.
                    var linksParagraph = new Paragraph();
                    linksParagraph.Add(new Chunk("__________________________", textFont));
                    linksParagraph.Add(Chunk.NEWLINE);
                    linksParagraph.Add(new Chunk(unterschriftLinks, textFont));

                    var rechtsParagraph = new Paragraph();
                    rechtsParagraph.Add(new Chunk("__________________________", textFont));
                    rechtsParagraph.Add(Chunk.NEWLINE);
                    rechtsParagraph.Add(new Chunk(unterschriftRechts, textFont));

                    unterschriftTable.AddCell(new PdfPCell(linksParagraph) { Border = Rectangle.NO_BORDER });
                    unterschriftTable.AddCell(new PdfPCell(rechtsParagraph) { Border = Rectangle.NO_BORDER });
                    doc.Add(unterschriftTable);
                }

                doc.Close();
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[DokumentPdfGenerator] Fehler beim Erstellen von \"" + titel + "\": " + e.Message);
            return false;
        }
    }

    // Variante von ErstellePdf für Dokumente mit ZWEI Unterschriftenzeilen
    // untereinander (z.B. Arbeitsvertrag: Arbeitgeber + Arbeitnehmer),
    // statt der üblichen einen Zeile mit zwei Spalten nebeneinander.
    public static bool ErstellePdfMitDoppelSignatur(string dateipfad, string titel,
        List<(bool istUeberschrift, string text)> bloecke,
        string signatur1Links, string signatur1Rechts,
        string signatur2Links, string signatur2Rechts)
    {
        try
        {
            string ordner = Path.GetDirectoryName(dateipfad);
            if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);

            var firma = HoleFirmendaten();

            Document doc = new Document(PageSize.A4, 50, 50, 60, 75);
            using (FileStream fs = new FileStream(dateipfad, FileMode.Create))
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                writer.PageEvent = new PdfFooterEvent(firma.Name, firma.Anschrift, false);

                doc.AddAuthor("Ventoriq");
                doc.AddTitle(titel);
                doc.Open();

                var titelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 17);
                var ueberschriftFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                var grauFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, new iTextSharp.text.Color(128, 128, 128));

                doc.Add(new Paragraph(titel, titelFont));
                doc.Add(new Paragraph(firma.Name + (string.IsNullOrEmpty(firma.Rechtsform) ? "" : " " + firma.Rechtsform), grauFont));
                doc.Add(new Paragraph("Erstellt am: " + DateTime.Now.ToString("dd.MM.yyyy"), grauFont));
                doc.Add(new Paragraph(" "));

                foreach (var (istUeberschrift, text) in bloecke)
                {
                    if (string.IsNullOrWhiteSpace(text)) { doc.Add(new Paragraph(" ")); continue; }
                    var p = new Paragraph();
                    var zeilen = text.Split('\n');
                    var schrift = istUeberschrift ? ueberschriftFont : textFont;
                    for (int zi = 0; zi < zeilen.Length; zi++)
                    {
                        p.Add(new Chunk(zeilen[zi], schrift));
                        if (zi < zeilen.Length - 1) p.Add(Chunk.NEWLINE);
                    }
                    p.SpacingAfter = istUeberschrift ? 6f : 10f;
                    doc.Add(p);
                }

                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(" "));

                void FuegeSignaturzeileHinzu(string links, string rechts)
                {
                    var linksP = new Paragraph();
                    linksP.Add(new Chunk("__________________________", textFont));
                    linksP.Add(Chunk.NEWLINE);
                    linksP.Add(new Chunk(links, textFont));

                    var rechtsP = new Paragraph();
                    rechtsP.Add(new Chunk("__________________________", textFont));
                    rechtsP.Add(Chunk.NEWLINE);
                    rechtsP.Add(new Chunk(rechts, textFont));

                    var tabelle = new PdfPTable(2);
                    tabelle.WidthPercentage = 100f;
                    tabelle.AddCell(new PdfPCell(linksP) { Border = Rectangle.NO_BORDER, PaddingBottom = 16f });
                    tabelle.AddCell(new PdfPCell(rechtsP) { Border = Rectangle.NO_BORDER, PaddingBottom = 16f });
                    doc.Add(tabelle);
                }

                FuegeSignaturzeileHinzu(signatur1Links, signatur1Rechts);
                FuegeSignaturzeileHinzu(signatur2Links, signatur2Rechts);

                doc.Close();
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[DokumentPdfGenerator] Fehler beim Erstellen von \"" + titel + "\": " + e.Message);
            return false;
        }
    }

    // ─────────────────────────────────────────
    // ERWEITERTER RENDERER MIT TABELLEN-UNTERSTÜTZUNG
    // Für Vorlagen wie Fördermittelübersicht, Darlehensübersicht,
    // Versicherungsübersicht, die echte Tabellen statt nur Fließtext
    // brauchen. Der einfache ErstellePdf-Weg (nur Text/Überschriften)
    // bleibt für alle anderen Dokumente unverändert bestehen.
    // ─────────────────────────────────────────
    public class PdfBlock
    {
        public enum ArtTyp { Ueberschrift, Text, Tabelle }
        public ArtTyp Art;
        public string Text;
        public string[] TabellenSpalten;
        public List<string[]> TabellenZeilen;

        public static PdfBlock Ueberschrift(string text) => new PdfBlock { Art = ArtTyp.Ueberschrift, Text = text };
        public static PdfBlock Absatz(string text) => new PdfBlock { Art = ArtTyp.Text, Text = text };
        public static PdfBlock Tabelle(string[] spalten, List<string[]> zeilen) =>
            new PdfBlock { Art = ArtTyp.Tabelle, TabellenSpalten = spalten, TabellenZeilen = zeilen };
    }

    public static bool ErstellePdfErweitert(string dateipfad, string titel, List<PdfBlock> bloecke,
        bool mitUnterschrift = true, string unterschriftLinks = "(Ort, Datum)", string unterschriftRechts = "(Unterschrift)")
    {
        try
        {
            string ordner = Path.GetDirectoryName(dateipfad);
            if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);

            var firma = HoleFirmendaten();

            Document doc = new Document(PageSize.A4, 45, 45, 60, 75);
            using (FileStream fs = new FileStream(dateipfad, FileMode.Create))
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                writer.PageEvent = new PdfFooterEvent(firma.Name, firma.Anschrift, false);

                doc.AddAuthor("Ventoriq");
                doc.AddTitle(titel);
                doc.Open();

                var titelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 17);
                var ueberschriftFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var textFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                var grauFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 9, new iTextSharp.text.Color(128, 128, 128));
                var tabellenKopfFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, iTextSharp.text.Color.WHITE);
                var tabellenTextFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
                var kopfFarbe = new iTextSharp.text.Color(60, 60, 60);

                doc.Add(new Paragraph(titel, titelFont));
                doc.Add(new Paragraph(firma.Name + (string.IsNullOrEmpty(firma.Rechtsform) ? "" : " " + firma.Rechtsform), grauFont));
                doc.Add(new Paragraph("Erstellt am: " + DateTime.Now.ToString("dd.MM.yyyy"), grauFont));
                doc.Add(new Paragraph(" "));

                foreach (var block in bloecke)
                {
                    switch (block.Art)
                    {
                        case PdfBlock.ArtTyp.Ueberschrift:
                            var u = new Paragraph(block.Text, ueberschriftFont);
                            u.SpacingAfter = 6f;
                            doc.Add(u);
                            break;

                        case PdfBlock.ArtTyp.Text:
                            if (string.IsNullOrWhiteSpace(block.Text)) { doc.Add(new Paragraph(" ")); break; }
                            // FIX: mehrzeiliger Text (eingebettetes "\n")
                            // wird von iTextSharp innerhalb einer einzelnen
                            // Paragraph-Zeichenkette nicht zuverlässig als
                            // Zeilenumbruch gerendert - deshalb zeilenweise
                            // mit Chunk.NEWLINE aufgebaut statt als ein
                            // durchgehender String.
                            var p = new Paragraph();
                            var textZeilen = block.Text.Split('\n');
                            for (int zi = 0; zi < textZeilen.Length; zi++)
                            {
                                p.Add(new Chunk(textZeilen[zi], textFont));
                                if (zi < textZeilen.Length - 1) p.Add(Chunk.NEWLINE);
                            }
                            p.SpacingAfter = 10f;
                            doc.Add(p);
                            break;

                        case PdfBlock.ArtTyp.Tabelle:
                            var tabelle = new PdfPTable(block.TabellenSpalten.Length) { WidthPercentage = 100f };
                            foreach (var spalte in block.TabellenSpalten)
                            {
                                var kopfZelle = new PdfPCell(new Phrase(spalte, tabellenKopfFont))
                                {
                                    BackgroundColor = kopfFarbe,
                                    Padding = 5f,
                                    HorizontalAlignment = Element.ALIGN_LEFT
                                };
                                tabelle.AddCell(kopfZelle);
                            }
                            foreach (var zeile in block.TabellenZeilen)
                            {
                                foreach (var wert in zeile)
                                {
                                    var zelle = new PdfPCell(new Phrase(wert ?? "", tabellenTextFont)) { Padding = 5f };
                                    tabelle.AddCell(zelle);
                                }
                            }
                            doc.Add(tabelle);
                            doc.Add(new Paragraph(" "));
                            break;
                    }
                }

                if (mitUnterschrift)
                {
                    doc.Add(new Paragraph(" "));
                    var linksP = new Paragraph();
                    linksP.Add(new Chunk("__________________________", textFont));
                    linksP.Add(Chunk.NEWLINE);
                    linksP.Add(new Chunk(unterschriftLinks, textFont));

                    var rechtsP = new Paragraph();
                    rechtsP.Add(new Chunk("__________________________", textFont));
                    rechtsP.Add(Chunk.NEWLINE);
                    rechtsP.Add(new Chunk(unterschriftRechts, textFont));

                    var signaturTabelle = new PdfPTable(2) { WidthPercentage = 100f };
                    signaturTabelle.AddCell(new PdfPCell(linksP) { Border = Rectangle.NO_BORDER });
                    signaturTabelle.AddCell(new PdfPCell(rechtsP) { Border = Rectangle.NO_BORDER });
                    doc.Add(signaturTabelle);
                }

                doc.Close();
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[DokumentPdfGenerator] Fehler beim Erstellen von \"" + titel + "\": " + e.Message);
            return false;
        }
    }

    private static string PfadFuer(string dokumentTyp)
    {
        string username = "unbekannt";
        try { username = StateManager.Instance?.getCurrentUser()?.username ?? "unbekannt"; } catch { }
        foreach (char c in Path.GetInvalidFileNameChars()) username = username.Replace(c, '_');

        string ordner = Path.Combine(Application.persistentDataPath, "PDFs", username, "Dokumente");
        Directory.CreateDirectory(ordner);
        return Path.Combine(ordner, dokumentTyp + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");
    }

    // ─────────────────────────────────────────
    // GRÜNDUNGSURKUNDE
    // ─────────────────────────────────────────
    public static string ErstelleGruendungsurkunde(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string gruendernamen = Feld(f, "gruendernamen");
        string gruendungsdatum = Feld(f, "gruendungsdatum");
        string gruendungsvision = Feld(f, "gruendungsvision");
        string zusatztext = Feld(f, "zusatztext");

        var bloecke = new List<(bool, string)>
        {
            (false, $"In Anerkennung des Unternehmergeistes und der gemeinsamen Vision wird hiermit die offizielle Gründung des Unternehmens {firma.Name} {firma.Rechtsform} beurkundet. Mit diesem Dokument halten wir den Ursprung unserer gemeinsamen Reise fest."),
            (true,  "Die Gründer"),
            (false, "Als verantwortliche Gründungsmitglieder treten auf: " + (string.IsNullOrEmpty(gruendernamen) ? "[nicht angegeben]" : gruendernamen)),
            (true,  "Datum der Gründung"),
            (false, "Das Unternehmen wurde am " + (string.IsNullOrEmpty(gruendungsdatum) ? "[nicht angegeben]" : gruendungsdatum) + " offiziell ins Leben gerufen."),
            (true,  "Unsere Vision"),
            (false, "Wir verfolgen das Ziel: \u201e" + (string.IsNullOrEmpty(gruendungsvision) ? "[nicht angegeben]" : gruendungsvision) + "\u201c"),
        };

        if (!string.IsNullOrWhiteSpace(zusatztext))
        {
            bloecke.Add((true, "Abschluss"));
            bloecke.Add((false, zusatztext));
        }

        string pfad = PfadFuer("Gruendungsurkunde");
        bool ok = ErstellePdf(pfad, "Gründungsurkunde", bloecke, true, "(Ort, Datum)", "(Unterschriften der Gründer)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // HANDELSREGISTERAUSZUG
    // ─────────────────────────────────────────
    public static string ErstelleHandelsregisterauszug(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string registergericht = Feld(f, "registergericht");
        string registernummer = Feld(f, "registernummer");
        string tagDerEintragung = Feld(f, "tagDerEintragung");
        string stammkapital = Feld(f, "stammkapital");
        string geschaeftsfuehrung = Feld(f, "geschaeftsfuehrung");
        string zweck = HoleUnternehmenszweck();

        var bloecke = new List<(bool, string)>
        {
            (false, $"Hiermit werden die im System hinterlegten und verifizierten Daten zur Eintragung im Handelsregister für das Unternehmen {firma.Name} {firma.Rechtsform} zusammenfassend dargestellt."),
            (true,  "Sektion A: Registerdaten"),
            (false, "Zuständiges Gericht: " + (string.IsNullOrEmpty(registergericht) ? "[nicht angegeben]" : registergericht)),
            (false, "Nummer des Registers: " + (string.IsNullOrEmpty(registernummer) ? "[nicht angegeben]" : registernummer)),
            (false, "Datum der Ersteintragung: " + (string.IsNullOrEmpty(tagDerEintragung) ? "[nicht angegeben]" : tagDerEintragung)),
            (true,  "Sektion B: Kapital & Gegenstand"),
            (false, "Stammkapital: " + (string.IsNullOrEmpty(stammkapital) ? "[nicht angegeben]" : stammkapital) + " EUR"),
            (false, "Unternehmenszweck: " + zweck),
            (true,  "Sektion C: Vertretungsregelung"),
            (false, "Zur Vertretung des Unternehmens sind folgende Personen befugt: " + (string.IsNullOrEmpty(geschaeftsfuehrung) ? "[nicht angegeben]" : geschaeftsfuehrung)),
            (true,  "Bestätigungsbereich"),
            (false, "Die Richtigkeit der oben stehenden Angaben wird durch die Geschäftsführung bestätigt."),
        };

        string pfad = PfadFuer("Handelsregisterauszug");
        bool ok = ErstellePdf(pfad, "Handelsregisterauszug", bloecke, true, "(Ort, Datum)", "(Unterschrift Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // GESELLSCHAFTSVERTRAG
    // ─────────────────────────────────────────
    public static string ErstelleGesellschaftsvertrag(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string gesellschafter = Feld(f, "gesellschafter");
        string stammkapital = Feld(f, "stammkapital");
        string gewinnverteilung = Feld(f, "gewinnverteilung");
        string geschaeftsfuehrung = Feld(f, "geschaeftsfuehrung");
        string schlussbestimmungen = Feld(f, "schlussbestimmungen");
        string zweck = HoleUnternehmenszweck();

        var bloecke = new List<(bool, string)>
        {
            (false, $"Zwischen den nachfolgend aufgeführten Gesellschaftern wird für das Unternehmen {firma.Name} {firma.Rechtsform} der folgende Gesellschaftsvertrag zur Regelung der internen Verhältnisse und der Vertretung nach außen geschlossen."),
            (true,  "§ 1 Gesellschafter"),
            (false, "Am Gesellschaftsverhältnis sind folgende Personen beteiligt: " + (string.IsNullOrEmpty(gesellschafter) ? "[nicht angegeben]" : gesellschafter)),
            (true,  "§ 2 Stammkapital und Anteile"),
            (false, "Das Stammkapital der Gesellschaft beträgt " + (string.IsNullOrEmpty(stammkapital) ? "[nicht angegeben]" : stammkapital) + " EUR. Die Beteiligungsverhältnisse richten sich nach den jeweils erbrachten Einlagen der Gesellschafter."),
            (true,  "§ 3 Gegenstand des Unternehmens"),
            (false, "Der Zweck der Gesellschaft ist: " + zweck + "."),
            (true,  "§ 4 Geschäftsführung"),
            (false, "Zur Geschäftsführung und Vertretung der Gesellschaft sind berechtigt: " + (string.IsNullOrEmpty(geschaeftsfuehrung) ? "[nicht angegeben]" : geschaeftsfuehrung)),
            (true,  "§ 5 Gewinnverwendung"),
            (false, "Über die Verwendung des Jahresüberschusses entscheiden die Gesellschafter wie folgt: " + (string.IsNullOrEmpty(gewinnverteilung) ? "[nicht angegeben]" : gewinnverteilung)),
        };

        if (!string.IsNullOrWhiteSpace(schlussbestimmungen))
        {
            bloecke.Add((true, "§ 6 Sonstige Vereinbarungen"));
            bloecke.Add((false, schlussbestimmungen));
        }

        bloecke.Add((true, "Abschluss und Unterschriften"));
        bloecke.Add((false, "Die Gesellschafter bestätigen durch ihre Unterschrift die Kenntnisnahme und Anerkennung aller Vertragspunkte."));

        string pfad = PfadFuer("Gesellschaftsvertrag");
        bool ok = ErstellePdf(pfad, "Gesellschaftsvertrag", bloecke, true, "(Ort, Datum)", "(Unterschriften aller Gesellschafter)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // FRAGEBOGEN ZUR STEUERLICHEN ERFASSUNG
    // ─────────────────────────────────────────
    public static string ErstelleFragebogenSteuererfassung(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string finanzamt = Feld(f, "finanzamt");
        string beginn = Feld(f, "beginnTaetigkeit");
        string umsatz = Feld(f, "umsatzJahr1");
        string gewinn = Feld(f, "gewinnJahr1");
        string kleinunternehmer = Feld(f, "kleinunternehmer");
        string zweck = HoleUnternehmenszweck();

        var bloecke = new List<(bool, string)>
        {
            (false, "Nach der Gründung eines Unternehmens ist die steuerliche Erfassung beim zuständigen Finanzamt zwingend erforderlich. Dieser Leitfaden fasst die im System Ventoriq hinterlegten Daten zusammen, um die Übermittlung via ELSTER oder Papierformular zu erleichtern."),
            (true,  "Abschnitt 1: Allgemeine Angaben"),
            (false, "Zuständiges Finanzamt: " + (string.IsNullOrEmpty(finanzamt) ? "[nicht angegeben]" : finanzamt)),
            (false, "Beginn der gewerblichen Tätigkeit: " + (string.IsNullOrEmpty(beginn) ? "[nicht angegeben]" : beginn)),
            (false, "Gegenstand des Unternehmens: " + zweck),
            (true,  "Abschnitt 2: Schätzung der Einkünfte"),
            (false, "Für die Festsetzung etwaiger Vorauszahlungen wurden folgende Schätzwerte für das erste Rumpfgeschäftsjahr im System hinterlegt:"),
            (false, "Voraussichtlicher Gesamtumsatz: " + (string.IsNullOrEmpty(umsatz) ? "[nicht angegeben]" : umsatz) + " EUR"),
            (false, "Voraussichtlicher Gewinn: " + (string.IsNullOrEmpty(gewinn) ? "[nicht angegeben]" : gewinn) + " EUR"),
            (true,  "Abschnitt 3: Umsatzsteuerliche Einordnung"),
            (false, "Hinsichtlich der Umsatzsteuer wird folgende Regelung angestrebt:"),
            (false, "Gewählte Option: " + (string.IsNullOrEmpty(kleinunternehmer) ? "[nicht angegeben]" : kleinunternehmer) +
                    " (Hinweis: Bei Inanspruchnahme der Kleinunternehmerregelung nach § 19 UStG wird keine Umsatzsteuer erhoben und kein Vorsteuerabzug gewährt.)"),
            (true,  "Wichtiger Hinweis"),
            (false, "Dieses Dokument dient lediglich der Orientierung und ersetzt keine steuerliche Beratung. Die tatsächliche Meldung muss über das amtliche Portal ELSTER erfolgen."),
        };

        string pfad = PfadFuer("Fragebogen_Steuererfassung");
        bool ok = ErstellePdf(pfad, "Fragebogen zur Steuerlichen Erfassung", bloecke, true, "(Ort, Datum)", "(Unterschrift Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // GEWERBEANMELDUNG
    // ─────────────────────────────────────────
    public static string ErstelleGewerbeanmeldung(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string behoerde = Feld(f, "behoerde");
        string beginn = Feld(f, "beginnTaetigkeit");
        string anzahlMitarbeiter = Feld(f, "anzahlMitarbeiter");
        string nebenerwerb = Feld(f, "nebenerwerb");
        string anmeldungsgrund = Feld(f, "anmeldungsgrund");
        string zweck = HoleUnternehmenszweck();

        var bloecke = new List<(bool, string)>
        {
            (false, "An die zuständige Behörde: " + (string.IsNullOrEmpty(behoerde) ? "[nicht angegeben]" : behoerde)),
            (true,  "Abschnitt 1: Angaben zum Betriebsinhaber"),
            (false, "Name/Firma: " + firma.Name + " " + firma.Rechtsform),
            (false, "Anschrift: " + (string.IsNullOrEmpty(firma.Anschrift) ? "[nicht in den Einstellungen hinterlegt]" : firma.Anschrift)),
            (false, "Gegenstand der Tätigkeit: " + zweck),
            (true,  "Abschnitt 2: Angaben zum Betrieb"),
            (false, "Beginn der angemeldeten Tätigkeit: " + (string.IsNullOrEmpty(beginn) ? "[nicht angegeben]" : beginn)),
            (false, "Art des angemeldeten Betriebs: " + (string.IsNullOrEmpty(anmeldungsgrund) ? "[nicht angegeben]" : anmeldungsgrund)),
            (false, "Wird die Tätigkeit im Nebenerwerb betrieben? " + (string.IsNullOrEmpty(nebenerwerb) ? "[nicht angegeben]" : nebenerwerb)),
            (false, "Zahl der bei der Anmeldung tätigen Personen (ohne Inhaber): " + (string.IsNullOrEmpty(anzahlMitarbeiter) ? "[nicht angegeben]" : anzahlMitarbeiter)),
            (true,  "Rechtlicher Hinweis"),
            (false, "Die Anmeldung berechtigt nicht zum Beginn des Gewerbebetriebes, wenn noch eine erforderliche Erlaubnis fehlt. Die Vorlage dieses Dokuments dient der Systemdokumentation innerhalb von Ventoriq und als Vorlage für die Behördenmeldung."),
            (true,  "Bestätigung"),
            (false, "Ich bestätige die Richtigkeit der oben gemachten Angaben."),
        };

        string pfad = PfadFuer("Gewerbeanmeldung");
        bool ok = ErstellePdf(pfad, "Gewerbeanmeldung", bloecke, true, "(Ort, Datum)", "(Unterschrift Betriebsinhaber)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // ANMELDUNG BERUFSGENOSSENSCHAFT
    // ─────────────────────────────────────────
    public static string ErstelleBerufsgenossenschaft(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string zustaendigeBg = Feld(f, "zustaendigeBg");
        string tagDerEroeffnung = Feld(f, "tagDerEroeffnung");
        string anzahlVersicherte = Feld(f, "anzahlVersicherte");
        string artTaetigkeit = Feld(f, "artTaetigkeit");
        string lohnsumme = Feld(f, "lohnsumme");
        string zweck = HoleUnternehmenszweck();

        var bloecke = new List<(bool, string)>
        {
            (false, "An die zuständige Berufsgenossenschaft: " + (string.IsNullOrEmpty(zustaendigeBg) ? "[nicht angegeben]" : zustaendigeBg)),
            (false, "Hiermit zeigen wir die Eröffnung des unten genannten Unternehmens an, um die Aufnahme in das Mitgliederverzeichnis und den gesetzlichen Unfallversicherungsschutz sicherzustellen."),
            (true,  "Abschnitt 1: Angaben zum Unternehmen"),
            (false, "Name des Betriebs: " + firma.Name + " " + firma.Rechtsform),
            (false, "Anschrift: " + (string.IsNullOrEmpty(firma.Anschrift) ? "[nicht in den Einstellungen hinterlegt]" : firma.Anschrift)),
            (false, "Unternehmenszweck: " + zweck),
            (true,  "Abschnitt 2: Versicherungsrelevante Daten"),
            (false, "Datum der Betriebseröffnung: " + (string.IsNullOrEmpty(tagDerEroeffnung) ? "[nicht angegeben]" : tagDerEroeffnung)),
            (false, "Schwerpunkt der Tätigkeiten: " + (string.IsNullOrEmpty(artTaetigkeit) ? "[nicht angegeben]" : artTaetigkeit)),
            (false, "Anzahl der im Unternehmen tätigen Personen: " + (string.IsNullOrEmpty(anzahlVersicherte) ? "[nicht angegeben]" : anzahlVersicherte)),
            (false, "Voraussichtliche Lohnsumme (Jahr 1): " + (string.IsNullOrEmpty(lohnsumme) ? "[nicht angegeben]" : lohnsumme) + " EUR"),
            (true,  "Rechtlicher Hinweis"),
            (false, "Dieses Dokument dient der Systemdokumentation innerhalb von Ventoriq und als strukturierte Vorlage für die offizielle Meldung bei Ihrem Versicherungsträger."),
            (true,  "Bestätigung"),
            (false, "Ich versichere die Richtigkeit der gemachten Angaben."),
        };

        string pfad = PfadFuer("Berufsgenossenschaft");
        bool ok = ErstellePdf(pfad, "Anmeldung Berufsgenossenschaft", bloecke, true, "(Ort, Datum)", "(Unterschrift Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // ORGANIGRAMM
    // ─────────────────────────────────────────
    public static string ErstelleOrganigramm(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();

        var rollen = new (string Bezeichnung, string Key)[]
        {
            ("Geschäftsführung (CEO)",        "ceo"),
            ("Produktmanagement (PO)",        "po"),
            ("Technische Leitung (CTO)",      "cto"),
            ("Marketing & Vertrieb",          "marketing"),
            ("Finanzen (CFO)",                "cfo"),
            ("Design (Creative Director)",    "creative"),
            ("Projektorganisation (PM)",      "pm"),
            ("Qualitätssicherung (QA)",       "qa"),
        };

        var bloecke = new List<(bool, string)>
        {
            (false, $"Die folgende Übersicht dokumentiert die feste Rollenverteilung und die operativen Verantwortlichkeiten innerhalb des Unternehmens {firma.Name} {firma.Rechtsform}. Jede Position ist mit einer verantwortlichen Person besetzt, um klare Entscheidungswege sicherzustellen."),
            (true, "Rollenverteilung"),
        };

        foreach (var (bezeichnung, key) in rollen)
        {
            string wert = Feld(f, key);
            bloecke.Add((false, bezeichnung + ": " + (string.IsNullOrEmpty(wert) ? "[nicht besetzt]" : wert)));
        }

        string mitarbeiter = Feld(f, "mitarbeiter");
        string azubi = Feld(f, "azubi");
        string praktikant = Feld(f, "praktikant");
        if (!string.IsNullOrEmpty(mitarbeiter) || !string.IsNullOrEmpty(azubi) || !string.IsNullOrEmpty(praktikant))
        {
            bloecke.Add((true, "Weiteres Team"));
            if (!string.IsNullOrEmpty(mitarbeiter)) bloecke.Add((false, "Mitarbeiter: " + mitarbeiter));
            if (!string.IsNullOrEmpty(azubi)) bloecke.Add((false, "Azubi: " + azubi));
            if (!string.IsNullOrEmpty(praktikant)) bloecke.Add((false, "Praktikant: " + praktikant));
        }

        bloecke.Add((true, "Schlusswort"));
        bloecke.Add((false, "Die benannten Personen sind für die Einhaltung der fachlichen Standards und die Koordination ihrer jeweiligen Bereiche verantwortlich. Änderungen in der Struktur müssen durch die Geschäftsführung im System Ventoriq aktualisiert werden."));

        string pfad = PfadFuer("Organigramm");
        bool ok = ErstellePdf(pfad, "Organigramm", bloecke, true, "(Ort, Datum)", "(Unterschrift Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // ZAHLUNGSBEDINGUNGEN
    // ─────────────────────────────────────────
    public static string ErstelleZahlungsbedingungen(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string zahlungsfrist = Feld(f, "zahlungsfrist");
        string skontoSatz = Feld(f, "skontoSatz");
        string skontoZeitraum = Feld(f, "skontoZeitraum");
        string verzugszins = Feld(f, "verzugszins");
        string zusatzhinweise = Feld(f, "zusatzhinweise");

        var bloecke = new List<(bool, string)>
        {
            (false, $"Die folgenden Bedingungen regeln die Zahlungsabwicklung für alle zwischen der {firma.Name} {firma.Rechtsform} und dem Kunden geschlossenen Verträge. Soweit nicht anders vereinbart, gelten diese Fristen und Regelungen als verbindlich."),
            (true,  "Abschnitt 1: Fälligkeit und Zahlungsziel"),
            (false, "Rechnungsbeträge sind innerhalb von " + (string.IsNullOrEmpty(zahlungsfrist) ? "[nicht angegeben]" : zahlungsfrist) + " Tagen nach Erhalt der Rechnung ohne Abzug zur Zahlung auf das in den Stammdaten hinterlegte Geschäftskonto fällig."),
        };

        // Skonto-Absatz entfällt laut Spezifikation automatisch bei fehlender Eingabe
        if (!string.IsNullOrWhiteSpace(skontoSatz))
        {
            bloecke.Add((true, "Abschnitt 2: Skonto-Regelung"));
            bloecke.Add((false, "Bei Zahlung innerhalb von " + (string.IsNullOrEmpty(skontoZeitraum) ? "[nicht angegeben]" : skontoZeitraum) + " Tagen gewähren wir einen Skonto-Abzug in Höhe von " + skontoSatz + " % auf den Bruttorechnungsbetrag."));
        }

        bloecke.Add((true, "Abschnitt 3: Zahlungsverzug"));
        bloecke.Add((false, "Nach Ablauf des oben genannten Zahlungsziels gerät der Kunde automatisch in Verzug. Wir behalten uns vor, Verzugszinsen in Höhe von " + (string.IsNullOrEmpty(verzugszins) ? "[nicht angegeben]" : verzugszins) + " % über dem jeweiligen Basiszinssatz der EZB zu berechnen."));

        bloecke.Add((true, "Abschnitt 4: Sonstiges & Eigentumsvorbehalt"));
        if (!string.IsNullOrWhiteSpace(zusatzhinweise)) bloecke.Add((false, zusatzhinweise));
        bloecke.Add((false, $"Die gelieferte Ware bleibt bis zur vollständigen Bezahlung sämtlicher Forderungen Eigentum der {firma.Name} (Eigentumsvorbehalt)."));
        bloecke.Add((false, "Gültig ab dem Datum der Erstellung oder dem vertraglich vereinbarten Zeitpunkt."));
        bloecke.Add((false, $"Rechtlicher Hinweis: Dieses Dokument wurde im Rahmen des Systems Ventoriq erstellt. Es ist Bestandteil der kaufmännischen Unterlagen von {firma.Name} und auch ohne handschriftliche Unterschrift rechtsgültig."));

        string pfad = PfadFuer("Zahlungsbedingungen");
        bool ok = ErstellePdf(pfad, "Zahlungsbedingungen", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // AGB
    // ─────────────────────────────────────────
    public static string ErstelleAgb(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string leistungsbereich = Feld(f, "leistungsbereich");
        string widerrufsfrist = Feld(f, "widerrufsfrist");
        string zahlungsziel = Feld(f, "zahlungsziel");
        string verzugszinssatz = Feld(f, "verzugszinssatz");
        string mahngebuehr = Feld(f, "mahngebuehr");
        string abnahmefrist = Feld(f, "abnahmefrist");
        string lizenzmodell = Feld(f, "lizenzmodell");
        string schadensersatzfaktor = Feld(f, "schadensersatzfaktor");
        string ndaDauer = Feld(f, "ndaDauer");
        string gerichtsstand = Feld(f, "gerichtsstand");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "§ 1 Geltungsbereich & Disclaimer"),
            (false, $"(1) Diese Bedingungen gelten für alle Aufträge, Lieferungen und Leistungen der {firma.Name} im Bereich " + N(leistungsbereich) + "."),
            (false, "(2) Disclaimer: Die Inhalte unserer Software/Dienste wurden mit größter Sorgfalt erstellt. Für die Richtigkeit, Vollständigkeit und Aktualität können wir jedoch keine Gewähr übernehmen. (Hinweis: Weitere Angaben hierzu entnehmen Sie bitte dem beigefügten Dokument „Disclaimer“ oder werden auf Anfrage nachgereicht.)"),
            (true,  "§ 2 Widerrufsbelehrung für Verbraucher"),
            (false, "Sie haben das Recht, binnen " + N(widerrufsfrist) + " Tagen ohne Angabe von Gründen diesen Vertrag zu widerrufen. Die Frist beginnt mit dem Tag des Vertragsschlusses. (Hinweis: Die vollständige „Widerrufsbelehrung“ ist diesem Vertrag als Anhang beigefügt oder kann jederzeit angefordert werden.)"),
            (true,  "§ 3 Vergütung, Zahlungsbedingungen & Mahnverfahren"),
            (false, "(1) Rechnungen sind innerhalb von " + N(zahlungsziel) + " Tagen ohne Abzug fällig."),
            (false, "(2) Bei Überschreitung des Termins entstehen Verzugszinsen in Höhe von " + N(verzugszinssatz) + " %."),
            (false, "(3) Gerät der Kunde in Verzug, wird für jede Mahnstufe eine Pauschale von " + N(mahngebuehr) + " EUR erhoben."),
            (false, "(4) Nutzungsrechte gehen erst mit vollständiger Bezahlung aller Rechnungen auf den Kunden über. (Hinweis: Details zum Ablauf finden Sie im Dokument „Mahnverfahren“, welches auf Anfrage zur Verfügung gestellt wird.)"),
            (true,  "§ 4 Lieferung, Abnahme & Mitwirkung"),
            (false, "(1) Der Auftraggeber ist zur Abnahme verpflichtet. Die Abnahme gilt als erfolgt, wenn sie nicht innerhalb von " + N(abnahmefrist) + " Tagen nach Ablieferung schriftlich verweigert wird."),
            (false, "(2) Mit der Nutzung oder Zahlung des Werks gilt die Abnahme in jedem Fall als erfolgt."),
            (true,  "§ 5 Urheberrecht, Lizenzen & Copyright-Hinweis"),
            (false, $"(1) Alle Leistungen sind als persönliche geistige Schöpfungen urheberrechtlich geschützt. Das Eigentum und Copyright obliegen weiterhin {firma.Name}."),
            (false, "(2) Dem Kunden wird ein " + N(lizenzmodell) + " Nutzungsrecht eingeräumt."),
            (false, "(3) Copyright-Schutz: Jede Nachahmung – auch von Teilen – ist unzulässig. Bei Zuwiderhandlung steht uns ein Schadensersatz in mindestens der " + N(schadensersatzfaktor) + "-fachen Höhe des vereinbarten Honorars zu. (Hinweis: Detaillierte Bestimmungen hierzu entnehmen Sie bitte den Dokumenten „Copyright Hinweis“ sowie „Lizenzhinweis Einfach/Erweitert“ im Anhang.)"),
            (true,  "§ 6 Datenschutzerklärung (Systemstandard)"),
            (false, "Die Verarbeitung personenbezogener Daten erfolgt streng nach DSGVO. Die Datenspeicherung durch die Software Ventoriq erfolgt ausschließlich in einer lokalen SQLite-Instanz auf dem Endgerät des Nutzers. Eine Cloud-Übertragung findet nicht statt. (Hinweis: Weitere Angaben erfolgen in der beigefügten „Datenschutzerklärung“.)"),
            (true,  "§ 7 Vertraulichkeitserklärung (NDA)"),
            (false, "Die Parteien verpflichten sich, alle geschäftlichen Informationen und Unterlagen streng vertraulich zu behandeln. Diese Pflicht gilt über die Dauer des Vertrages hinaus für weitere " + N(ndaDauer) + " Jahre. (Hinweis: Weitere Angaben erfolgen in der beigefügten „Vertraulichkeitserklärung“.)"),
            (true,  "§ 8 Haftungsausschluss"),
            (false, "Wir haften nicht für Sachaussagen in Werbemaßnahmen des Kunden oder für Rechtsverstöße, die durch den Auftraggeber verursacht werden. Die Haftung für leichte Fahrlässigkeit wird ausgeschlossen."),
            (true,  "§ 9 Schlussbestimmungen & Gültigkeit"),
            (false, "(1) Gerichtsstand ist " + N(gerichtsstand) + "."),
            (false, "(2) Rechtsgültigkeit: Dieses Dokument wurde im Rahmen des Systems Ventoriq erstellt. Die Rechtswirksamkeit tritt mit der Einbeziehung in den jeweiligen Einzelvertrag in Kraft. Sollten Klauseln unwirksam sein, bleibt der Restvertrag unberührt. Diese Allgemeinen Geschäftsbedingungen sind auch ohne handschriftliche Unterschrift rechtsgültig."),
        };

        string pfad = PfadFuer("AGB");
        bool ok = ErstellePdf(pfad, "Allgemeine Geschäftsbedingungen", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // DISCLAIMER
    // ─────────────────────────────────────────
    public static string ErstelleDisclaimer(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string geltungsbereich = Feld(f, "geltungsbereich");
        string inhaltlichePruefung = Feld(f, "inhaltlichePruefung");
        string externeVerweise = Feld(f, "externeVerweise");
        string urheberrechtshinweis = Feld(f, "urheberrechtshinweis");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "§ 1 Haftung für Inhalte"),
            (false, $"Die Inhalte der Software/Dienste von {firma.Name} im Bereich " + N(geltungsbereich) + " wurden mit größter Sorgfalt erstellt. Für die Richtigkeit, Vollständigkeit und Aktualität der Inhalte können wir jedoch keine Gewähr übernehmen. Als Diensteanbieter sind wir gemäß § 7 Abs.1 TMG für eigene Inhalte auf diesen Seiten nach den allgemeinen Gesetzen verantwortlich. Wir behalten uns vor, Inhalte ohne gesonderte Ankündigung zu verändern oder zu löschen."),
            (false, "Inhaltliche Prüfung: " + N(inhaltlichePruefung) + "."),
            (true,  "§ 2 Haftung für Links"),
            (false, "Unser Angebot enthält Links zu externen Webseiten Dritter, auf deren Inhalte wir keinen Einfluss haben. Deshalb können wir für diese fremden Inhalte auch keine Gewähr übernehmen. Für die Inhalte der verlinkten Seiten ist stets der jeweilige Anbieter oder Betreiber der Seiten verantwortlich. " + N(externeVerweise)),
            (true,  "§ 3 Urheberrecht"),
            (false, "Die durch die Seitenbetreiber erstellten Inhalte und Werke auf diesen Seiten unterliegen dem deutschen Urheberrecht. Die Vervielfältigung, Bearbeitung, Verbreitung und jede Art der Verwertung außerhalb der Grenzen des Urheberrechtes bedürfen der schriftlichen Zustimmung des jeweiligen Autors bzw. Erstellers. Downloads und Kopien dieser Seite sind nur für den privaten, nicht kommerziellen Gebrauch gestattet. " + N(urheberrechtshinweis)),
            (true,  "§ 4 Hinweis zum MVP-Status"),
            (false, "Wir weisen ausdrücklich darauf hin, dass es sich bei der genutzten Software um einen Prototypen (Minimum Viable Product) handelt. Die Nutzung erfolgt auf eigenes Risiko des Anwenders. Eine Haftung für Schäden, die aus der Nutzung der bereitgestellten Kalkulationen oder Dokumentvorlagen resultieren, wird im gesetzlich zulässigen Rahmen ausgeschlossen."),
            (true,  "§ 5 Rechtswirksamkeit dieses Haftungsausschlusses"),
            (false, "Dieser Haftungsausschluss ist als Teil des Internetangebotes/der Software zu betrachten, von dem aus auf diese Seite verwiesen wurde. Sofern Teile oder einzelne Formulierungen dieses Textes der geltenden Rechtslage nicht, nicht mehr oder nicht vollständig entsprechen sollten, bleiben die übrigen Teile des Dokumentes in ihrem Inhalt und ihrer Gültigkeit davon unberührt."),
            (false, "Dieses Dokument wurde im Rahmen des Systems Ventoriq erstellt. Die Rechtswirksamkeit tritt mit der Einbeziehung in den jeweiligen Einzelvertrag oder die Nutzung der Dienste in Kraft. Dieser Disclaimer ist auch ohne handschriftliche Unterschrift rechtsgültig."),
        };

        string pfad = PfadFuer("Disclaimer");
        bool ok = ErstellePdf(pfad, "Disclaimer", bloecke, true, "(Ort, Datum)", "(Geschäftsführung / Maschinell erstellt)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // BARZAHLUNG (Quittungs-Vordruck)
    // ─────────────────────────────────────────
    public static string ErstelleBarzahlung(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string gueltigkeitsbereich = Feld(f, "gueltigkeitsbereich");
        string zusatzhinweis = Feld(f, "zusatzhinweis");
        string ausfuehrung = Feld(f, "ausfuehrung");
        bool zweifach = !string.IsNullOrWhiteSpace(ausfuehrung) && ausfuehrung.Trim().ToLower().Contains("zwei");

        List<(bool, string)> BaueQuittungsblock(string kennzeichnung)
        {
            var block = new List<(bool, string)>
            {
                (true,  "Barzahlung & Quittung" + (string.IsNullOrEmpty(kennzeichnung) ? "" : " (" + kennzeichnung + ")")),
                (false, "Aussteller: " + firma.Name + " " + firma.Rechtsform +
                        (string.IsNullOrEmpty(firma.Anschrift) ? "" : ", " + firma.Anschrift) +
                        (string.IsNullOrEmpty(firma.SteuerNr) ? "" : " \u2013 Steuer-Nr.: " + firma.SteuerNr)),
                (true,  "Organisatorische Angaben"),
                (false, "Gültigkeitsbereich: " + (string.IsNullOrEmpty(gueltigkeitsbereich) ? "[nicht angegeben]" : gueltigkeitsbereich)),
                (false, "Zusatzhinweis: " + (string.IsNullOrEmpty(zusatzhinweis) ? "\u2013" : zusatzhinweis)),
                (false, " "),
                (false, "Zahlung von: ________________________________________________"),
                (false, "(Name / Anschrift des Zahlers)"),
                (false, " "),
                (false, "Bruttobetrag: ____________________ €"),
                (false, "Betrag in Worten: ___________________________________________ Euro"),
                (false, "Verwendungszweck / Leistung: ________________________________"),
                (false, "Steuerliche Aufschlüsselung: [ ] 19% MwSt. | [ ] 7% MwSt. | [ ] ____ % MwSt."),
                (false, "MwSt.-Betrag: ________________ EUR      Nettobetrag: ________________ EUR"),
                (true,  "Rechtlicher Hinweis"),
                (false, "Dieser Vordruck wurde maschinell mit dem System Ventoriq erstellt. Er dient als offizieller Nachweis über den Erhalt eines Barbetrags."),
            };
            return block;
        }

        var bloecke = BaueQuittungsblock(zweifach ? "Original" : "");
        if (zweifach)
        {
            bloecke.Add((false, " "));
            bloecke.Add((false, "─────────────────────────────────────────"));
            bloecke.AddRange(BaueQuittungsblock("Kopie"));
        }

        string pfad = PfadFuer("Barzahlung");
        bool ok = ErstellePdf(pfad, "Barzahlung", bloecke, true, "Ort, Datum", "Unterschrift & Stempel des Empfängers");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // ÜBERWEISUNG (Informationsblatt)
    // ─────────────────────────────────────────
    public static string ErstelleUeberweisung(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string kontoinhaber = Feld(f, "kontoinhaber");
        string kreditinstitut = Feld(f, "kreditinstitut");
        string iban = Feld(f, "iban");
        string bic = Feld(f, "bic");
        string verwendungszweck = Feld(f, "verwendungszweck");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (false, "Um eine schnelle und fehlerfreie Zuordnung Ihrer Zahlung zu gewährleisten, bitten wir Sie, den ausstehenden Rechnungsbetrag unter Verwendung der folgenden Bankverbindung zu begleichen."),
            (true,  "Zahlungsdaten"),
            (false, "Empfänger: " + (string.IsNullOrEmpty(kontoinhaber) ? firma.Name : kontoinhaber)),
            (false, "Bankinstitut: " + N(kreditinstitut)),
            (false, "IBAN: " + N(iban)),
            (false, "BIC / SWIFT: " + N(bic)),
            (false, "Verwendungszweck: " + N(verwendungszweck)),
            (true,  "Wichtiger Hinweis"),
            (false, "Bitte beachten Sie die in den Zahlungsbedingungen vereinbarten Fristen. Bei Zahlungen aus dem Ausland können zusätzliche Gebühren anfallen, die vom Auftraggeber zu tragen sind."),
            (false, "Dieses Informationsblatt wurde systemseitig durch Ventoriq generiert und ist Bestandteil der kaufmännischen Unterlagen."),
        };

        string pfad = PfadFuer("Ueberweisung");
        bool ok = ErstellePdf(pfad, "Überweisung", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // SEPA-BASISLASTSCHRIFT-MANDAT
    // ─────────────────────────────────────────
    public static string ErstelleSepaMandat(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string glaeubigerId = Feld(f, "glaeubigerId");
        string artZahlung = Feld(f, "artZahlung");
        string zusatzangaben = Feld(f, "zusatzangaben");

        var bloecke = new List<(bool, string)>
        {
            (true,  "Mandatsinformationen"),
            (false, "Gläubiger-ID: " + (string.IsNullOrEmpty(glaeubigerId) ? "[nicht angegeben]" : glaeubigerId)),
            (false, "Mandatsreferenz: _______________"),
            (true,  "Ermächtigungstext"),
            (false, $"Ich ermächtige die {firma.Name}, Zahlungen von meinem Konto mittels Lastschrift einzuziehen. Zugleich weise ich mein Kreditinstitut an, die von der {firma.Name} auf mein Konto gezogenen Lastschriften einzulösen."),
            (false, "Hinweis: Ich kann innerhalb von acht Wochen, beginnend mit dem Belastungsdatum, die Erstattung des belasteten Betrages verlangen. Es gelten dabei die mit meinem Kreditinstitut vereinbarten Bedingungen."),
            (true,  "Zahlungspflichtiger"),
            (false, "Name: ________________________________________________________"),
            (false, "Anschrift: ___________________________________________________"),
            (false, "IBAN: ________________________________________________________"),
            (false, "BIC: _________________________________________________________"),
            (true,  "Zahlungsmodalitäten"),
            (false, "Dieses Mandat gilt für: " + (string.IsNullOrEmpty(artZahlung) ? "[nicht angegeben]" : artZahlung) + "."),
        };

        if (!string.IsNullOrWhiteSpace(zusatzangaben)) bloecke.Add((false, zusatzangaben));

        bloecke.Add((false, "Dieses Mandat wurde im Rahmen des Systems Ventoriq erstellt und dient zur kaufmännischen Dokumentation."));

        string pfad = PfadFuer("SEPA_Mandat");
        bool ok = ErstellePdf(pfad, "SEPA-Basislastschrift-Mandat", bloecke, true, "(Ort, Datum)", "(Unterschrift des Zahlungspflichtigen)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // WIDERRUFSBELEHRUNG
    // ─────────────────────────────────────────
    public static string ErstelleWiderrufsbelehrung(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string widerrufsfrist = Feld(f, "widerrufsfrist");
        string kontakt = Feld(f, "kontakt");
        string wertersatz = Feld(f, "wertersatzklausel");
        string vorzeitigesErloeschen = Feld(f, "vorzeitigesErloeschen");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;
        bool IstJa(string wert) => !wert.Trim().ToLower().Contains("nein");

        var bloecke = new List<(bool, string)>
        {
            (true,  "§ 1 Widerrufsrecht"),
            (false, "Sie haben das Recht, binnen " + N(widerrufsfrist) + " Tagen ohne Angabe von Gründen diesen Vertrag zu widerrufen. Die Widerrufsfrist beträgt " + N(widerrufsfrist) + " Tage ab dem Tag des Vertragsabschlusses."),
            (true,  "§ 2 Ausübung des Widerrufs"),
            (false, $"Um Ihr Widerrufsrecht auszuüben, müssen Sie uns ({firma.Name}, {(string.IsNullOrEmpty(firma.Anschrift) ? "[Anschrift nicht hinterlegt]" : firma.Anschrift)}, E-Mail: " + N(kontakt) + ") mittels einer eindeutigen Erklärung über Ihren Entschluss, diesen Vertrag zu widerrufen, informieren."),
            (true,  "§ 3 Folgen des Widerrufs & Wertersatz"),
            (false, "(1) Wenn Sie diesen Vertrag widerrufen, haben wir Ihnen alle Zahlungen, die wir von Ihnen erhalten haben, unverzüglich zurückzuzahlen."),
        };

        if (string.IsNullOrEmpty(wertersatz) || IstJa(wertersatz))
        {
            bloecke.Add((false, "(2) Verpflichtung zum Wertersatz: Haben Sie verlangt, dass die Dienstleistungen (z. B. Software-Programmierung, Consulting) während der Widerrufsfrist beginnen sollen, so haben Sie uns einen angemessenen Betrag zu zahlen. Dieser entspricht dem Anteil der bis zu dem Zeitpunkt, zu dem Sie uns von der Ausübung des Widerrufsrechts unterrichten, bereits erbrachten Leistungen im Vergleich zum Gesamtumfang der im Vertrag vorgesehenen Dienstleistungen. Damit sind bereits ausgeführte Tätigkeiten und der dafür aufgewendete Zeitaufwand in vollem Umfang zu erstatten."));
        }

        if (string.IsNullOrEmpty(vorzeitigesErloeschen) || IstJa(vorzeitigesErloeschen))
        {
            bloecke.Add((true, "§ 4 Vorzeitiges Erlöschen des Widerrufsrechts"));
            bloecke.Add((false, "Das Widerrufsrecht erlischt bei einem Vertrag zur Erbringung von Dienstleistungen auch dann, wenn der Auftragnehmer die Dienstleistung vollständig erbracht hat und mit der Ausführung der Dienstleistung erst begonnen hat, nachdem der Verbraucher dazu seine ausdrückliche Zustimmung gegeben hat und gleichzeitig seine Kenntnis davon bestätigt hat, dass er sein Widerrufsrecht bei vollständiger Vertragserfüllung verliert."));
        }

        bloecke.Add((true, "§ 5 Widerruf bei digitalen Inhalten"));
        bloecke.Add((false, "Bei Verträgen über die Bereitstellung von digitalen Inhalten (z. B. Software-Downloads, SaaS-Zugang), die nicht auf einem Datenträger geliefert werden, erlischt das Widerrufsrecht, wenn der Unternehmer mit der Ausführung des Vertrags begonnen hat, nachdem der Verbraucher ausdrücklich zugestimmt hat und seine Kenntnis vom Verlust des Widerrufsrechts bestätigt hat."));
        bloecke.Add((false, "Dieses Dokument wurde im Rahmen des Gründungssystems Ventoriq erstellt. Die Belehrung ist auch ohne handschriftliche Unterschrift rechtsgültig."));

        string pfad = PfadFuer("Widerrufsbelehrung");
        bool ok = ErstellePdf(pfad, "Widerrufsbelehrung", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // MAHNVERFAHREN
    // ─────────────────────────────────────────
    public static string ErstelleMahnverfahren(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string zahlungsziel = Feld(f, "zahlungsziel");
        string verzugszinssatz = Feld(f, "verzugszinssatz");
        string bearbeitungspauschale = Feld(f, "bearbeitungspauschale");
        string intervallMahnstufen = Feld(f, "intervallMahnstufen");
        string zusatzhinweis = Feld(f, "zusatzhinweis");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;
        string intervallText = string.IsNullOrWhiteSpace(intervallMahnstufen)
            ? "Sollte ein Zahlungseingang ausbleiben, folgen wir in der Regel diesem Prozess:"
            : "Sollte ein Zahlungseingang ausbleiben, folgen wir in der Regel diesem Prozess (Intervall zwischen den Stufen: " + intervallMahnstufen + " Tage):";

        var bloecke = new List<(bool, string)>
        {
            (false, "Sehr geehrte Damen und Herren, für eine vertrauensvolle Zusammenarbeit ist Transparenz die wichtigste Grundlage. Um Ihnen Planungssicherheit zu geben, erläutern wir Ihnen hier unser standardisiertes Verfahren im Falle eines Zahlungsverzugs. Dieses Dokument dient Ihrer Information und ist fester Bestandteil unserer Geschäftsbeziehung."),
            (true,  "§ 1 Fälligkeit & Zahlungsziel"),
            (false, "Sofern im Angebot nicht anders vereinbart, sind unsere Rechnungen innerhalb von " + N(zahlungsziel) + " Tagen nach Rechnungsdatum ohne Abzug fällig. Mit Ablauf dieser Frist gerät der Auftraggeber automatisch in Verzug, ohne dass es einer gesonderten Mahnung bedarf."),
            (true,  "§ 2 Verzugszinsen & Kosten"),
            (false, "(1) Bei Überschreitung des Zahlungstermins berechnen wir Verzugszinsen in Höhe von " + N(verzugszinssatz) + " %."),
            (false, "(2) Das Recht zur Geltendmachung eines darüber hinausgehenden Schadens bleibt hiervon unberührt."),
            (false, "(3) Ab der zweiten Mahnstufe wird eine Bearbeitungspauschale von " + N(bearbeitungspauschale) + " EUR erhoben."),
            (true,  "§ 3 Ablauf des Mahnverfahrens"),
            (false, intervallText),
            (false, "1. Zahlungserinnerung: Ein freundlicher Hinweis auf den offenen Posten (kurz nach Fristablauf)."),
            (false, "2. 1. Mahnung: Formelle Aufforderung zur Zahlung unter Setzung einer Nachfrist."),
            (false, "3. 2. Mahnung: Letzte außergerichtliche Aufforderung inkl. Mahngebühren."),
            (false, "4. Rechtliche Schritte: Übergabe an ein Inkassobüro oder Einleitung eines gerichtlichen Mahnbescheids."),
            (true,  "§ 4 Einbehalt von Rechten"),
            (false, "Wir weisen darauf hin, dass Nutzungsrechte an unseren Leistungen (Software, Designs, Lizenzen) erst mit der vollständigen Bezahlung aller den Auftrag betreffenden Rechnungen auf den Kunden übergehen."),
        };

        if (!string.IsNullOrWhiteSpace(zusatzhinweis)) bloecke.Add((false, "Hinweis: " + zusatzhinweis));

        bloecke.Add((false, $"Diese Informationen sind Teil des kaufmännischen Regelwerks von {firma.Name} und werden jedem Angebot als Anlage beigefügt. Sie sind auch ohne Unterschrift rechtsgültig."));

        string pfad = PfadFuer("Mahnverfahren");
        bool ok = ErstellePdf(pfad, "Mahnverfahren", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // RATENZAHLUNGSBESTIMMUNGEN
    // ─────────────────────────────────────────
    public static string ErstelleRatenzahlungsbestimmungen(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string mindestauftragswert = Feld(f, "mindestauftragswert");
        string maxLaufzeit = Feld(f, "maxLaufzeit");
        string bearbeitungsgebuehr = Feld(f, "bearbeitungsgebuehr");
        string zusatzhinweis = Feld(f, "zusatzhinweis");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (false, "Sehr geehrte Damen und Herren, wir wissen, dass große Visionen und innovative Softwareprojekte oft erhebliche Investitionen erfordern. Um Ihnen maximale finanzielle Flexibilität zu bieten, ermöglichen wir Ihnen bei größeren Projekten gerne die Begleichung unserer Leistungen in Teilbeträgen."),
            (true,  "§ 1 Allgemeine Möglichkeit zur Ratenzahlung"),
            (false, "(1) Für Aufträge mit einem Bruttovolumen ab " + N(mindestauftragswert) + " EUR bieten wir grundsätzlich die Möglichkeit einer Ratenzahlung an."),
            (false, "(2) Die maximale Laufzeit für solche Zahlungspläne beträgt in der Regel " + N(maxLaufzeit) + " Monate."),
            (true,  "§ 2 Notwendigkeit einer separaten Vereinbarung"),
            (false, "Dieses Informationsblatt stellt lediglich die grundsätzliche Bereitschaft zur Ratenzahlung dar. Die konkreten Konditionen (Ratenhöhe, Startdatum, Zinsen) sind nicht Bestandteil dieser Information, sondern müssen für jeden Auftrag in einer separaten Ratenzahlungsvereinbarung individuell und schriftlich zwischen den Parteien vereinbart werden. Erst mit der beidseitigen Bestätigung des spezifischen Zahlungsplans wird die Ratenzahlung wirksam."),
            (true,  "§ 3 Bearbeitung & Gebühren"),
            (false, "Für die Einrichtung und Verwaltung des individuellen Zahlungsplans erheben wir eine einmalige Bearbeitungsgebühr von " + N(bearbeitungsgebuehr) + " EUR, sofern im Einzelvertrag nichts anderes vereinbart wurde."),
            (true,  "§ 4 Eigentumsvorbehalt & Sicherung"),
            (false, "Wir weisen darauf hin, dass bei vereinbarter Ratenzahlung die Nutzungsrechte an erbrachten Leistungen (Software, Konzepte, Lizenzen) erst mit der vollständigen Bezahlung der letzten Rate auf den Kunden übergehen."),
        };

        if (!string.IsNullOrWhiteSpace(zusatzhinweis)) bloecke.Add((false, "Hinweis: " + zusatzhinweis));

        bloecke.Add((false, $"Diese Informationen sind Teil des kaufmännischen Regelwerks von {firma.Name} und dienen der transparenten Kundenkommunikation. Sie sind auch ohne Unterschrift rechtsgültig."));

        string pfad = PfadFuer("Ratenzahlungsbestimmungen");
        bool ok = ErstellePdf(pfad, "Ratenzahlungsbestimmungen", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // COPYRIGHT HINWEIS
    // ─────────────────────────────────────────
    public static string ErstelleCopyrightHinweis(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string schutzumfang = Feld(f, "schutzumfang");
        string referenzklausel = Feld(f, "referenzklausel");
        string schadensersatzfaktor = Feld(f, "schadensersatzfaktor");
        string zusatzangabe = Feld(f, "zusatzangabe");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;
        bool IstJa(string wert) => string.IsNullOrEmpty(wert) || !wert.Trim().ToLower().Contains("nein");

        var bloecke = new List<(bool, string)>
        {
            (true,  "§ 1 Urheberrechtsschutz"),
            (false, $"(1) Alle von {firma.Name} gelieferten Leistungen im Bereich " + N(schutzumfang) + " sind als persönliche geistige Schöpfungen urheberrechtlich geschützt."),
            (false, "(2) Das Eigentum und Copyright obliegen zu jedem Zeitpunkt dem Auftragnehmer. Eine Herausgabepflicht von Quelldaten oder offenen Dateien besteht nicht, sofern nicht ausdrücklich schriftlich vereinbart."),
            (true,  "§ 2 Nutzungsrechte & Nachahmungsverbot"),
            (false, "(1) Der Auftraggeber erhält mit der vollständigen Bezahlung ein eingeschränktes Nutzungsrecht im vereinbarten Umfang."),
            (false, "(2) Jede vollständige oder teilweise Nachahmung der Entwürfe, Werke oder Software-Module ist unzulässig. Bei Zuwiderhandlung steht uns ein Schadensersatz in mindestens der " + N(schadensersatzfaktor) + "-fachen Höhe des vereinbarten Honorars zu."),
        };

        if (IstJa(referenzklausel))
        {
            bloecke.Add((true, "§ 3 Referenznutzung & Eigenwerbung"));
            bloecke.Add((false, "(1) Referenzrecht: Der Auftragnehmer ist berechtigt, die konzipierten und gestalteten Produkte zeitlich und räumlich unbeschränkt zur Eigenwerbung zu nutzen."));
            bloecke.Add((false, "(2) Dies beinhaltet das Recht, den Auftraggeber als Referenzkunden namentlich zu benennen sowie die Entwürfe, Screenshots oder Beschreibungen der erbrachten Leistungen in Portfolios, auf Websites oder in Präsentationen zu verwenden."));
            bloecke.Add((false, "(3) Dieses Recht bleibt auch dann bestehen, wenn dem Auftraggeber ein ausschließliches Nutzungsrecht eingeräumt wurde, sofern keine abweichende Geheimhaltungsvereinbarung (NDA) für die optische Darstellung vorliegt."));
        }

        bloecke.Add((true, "§ 4 Mitwirkungspflicht & Freigabe"));
        bloecke.Add((false, "Mit der Freigabe der Arbeiten übernimmt der Auftraggeber die Verantwortung für die Richtigkeit von Text und Inhalt. Für Rechtsverstöße, die durch den Auftraggeber verursacht werden, wird keine Haftung übernommen."));

        if (!string.IsNullOrWhiteSpace(zusatzangabe)) bloecke.Add((false, zusatzangabe));

        bloecke.Add((false, $"Dieses Dokument wurde im Rahmen des Systems Ventoriq erstellt. Es ist Bestandteil der kaufmännischen Unterlagen von {firma.Name} und auch ohne handschriftliche Unterschrift rechtsgültig."));

        string pfad = PfadFuer("Copyright_Hinweis");
        bool ok = ErstellePdf(pfad, "Copyright Hinweis", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // LIZENZHINWEIS EINFACH
    // ─────────────────────────────────────────
    public static string ErstelleLizenzhinweisEinfach(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string zusatzangabe = Feld(f, "zusatzangabe");
        string kontakt = Feld(f, "kontakt");
        string preis = Feld(f, "preisErweiterteNutzung");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "§ 1 Grundsatz: Das einfache Nutzungsrecht (Auffang-Regelung)"),
            (false, "Sofern im schriftlichen Angebot oder dem jeweiligen Einzelvertrag nicht ausdrücklich und schriftlich die Einräumung eines erweiterten Nutzungsrechts vereinbart wurde, erhalten Sie an unseren Leistungen (Software, Designs, Konzepte) grundsätzlich ein einfaches (eingeschränktes) Nutzungsrecht."),
            (true,  "§ 2 Umfang und Beschränkungen"),
            (false, "(1) Das einfache Nutzungsrecht berechtigt Sie ausschließlich zur Nutzung der Arbeitsergebnisse für den vertraglich vorgesehenen Zweck."),
            (false, "(2) Veränderungsverbot: Ohne unsere ausdrückliche Einwilligung dürfen die Werke weder im Original noch bei einer Reproduktion verändert, bearbeitet oder dekompiliert werden."),
            (false, "(3) Keine Herausgabepflicht: Ein Anspruch auf die Aushändigung von Quelldaten (Source-Code) oder offenen Arbeitsdateien besteht standardmäßig nicht."),
            (false, "(4) Schutz vor Nachahmung: Jede – auch teilweise – Nachahmung ist unzulässig. Verstöße können Schadensersatzforderungen in mindestens der 3-fachen Höhe des vereinbarten Honorars nach sich ziehen."),
            (true,  "§ 3 Referenznutzung durch den Auftragnehmer"),
            (false, "Wir weisen darauf hin, dass wir berechtigt sind, die für Sie erbrachten Leistungen (z. B. in Form von Screenshots oder Projektbeschreibungen) zeitlich und räumlich unbeschränkt zur Eigenwerbung und als Referenz zu nutzen."),
            (true,  "§ 4 Bedingung der vollständigen Bezahlung"),
            (false, "Sämtliche einfache Nutzungsrechte, insofern nichts anderes vereinbart wurde, gehen erst mit der vollständigen Bezahlung aller den Auftrag betreffenden Rechnungen auf Sie über. Bis dahin verbleiben alle Rechte vollumfänglich bei uns."),
            (true,  "§ 5 Optionale Erweiterung der Rechte"),
            (false, "Sie haben jederzeit die Möglichkeit, Ihre Nutzungsrechte zu erweitern (z. B. für Weitervermarktung oder Bearbeitung). Dies erfordert:"),
            (false, "1. Einen formlosen schriftlichen Antrag an: " + N(kontakt) + "."),
            (false, "2. Die Entrichtung der Zusatzvergütung in Höhe von " + N(preis) + " Euro."),
        };

        if (!string.IsNullOrWhiteSpace(zusatzangabe)) bloecke.Add((false, zusatzangabe));

        bloecke.Add((true, "Rechtlicher Hinweis"));
        bloecke.Add((false, $"Dieses Dokument dient der Information über die standardmäßig geltenden Lizenzbestimmungen unserer AGB. Es ist Bestandteil der kaufmännischen Unterlagen von {firma.Name} und auch ohne handschriftliche Unterschrift rechtsgültig."));

        string pfad = PfadFuer("Lizenzhinweis_Einfach");
        bool ok = ErstellePdf(pfad, "Lizenzhinweis Einfach", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // ERÖFFNUNGSBILANZ
    // ─────────────────────────────────────────
    public static string ErstelleEroeffnungsbilanz(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string bilanzstichtag = Feld(f, "bilanzstichtag");
        string anmerkungen = Feld(f, "anmerkungen");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        DataBase db = null;
        try { db = UserDatabaseAccess.getCurrentUserDatabase(); } catch { }
        var k = db != null ? FinanzKennzahlenService.Berechne(db) : default;

        var bloecke = new List<(bool, string)>
        {
            (false, "Bilanzstichtag: " + N(bilanzstichtag)),
            (true,  "Aktiva"),
            (false, "Anlagevermögen: " + k.SummeInvestition.ToString("N2", DeKultur) + " €"),
            (false, "Umlaufvermögen: " + k.SummeUmlaufvermoegen.ToString("N2", DeKultur) + " €"),
            (true,  "Passiva"),
            (false, "Eigenkapital (Geldeinlagen + Sacheinlagen): " + (k.Geldeinlagen + k.Sacheinlagen).ToString("N2", DeKultur) + " €"),
            (false, "Fremdkapital (Kredite + Darlehen): " + (k.Kredite + k.Darlehen).ToString("N2", DeKultur) + " €"),
            (false, " "),
            (false, "Werte automatisch aus dem Kassenbuch berechnet (siehe Finanzierung -> Kapitalbedarf)."),
        };

        if (!string.IsNullOrWhiteSpace(anmerkungen))
        {
            bloecke.Add((true, "Anmerkungen"));
            bloecke.Add((false, anmerkungen));
        }

        string pfad = PfadFuer("Eroeffnungsbilanz");
        bool ok = ErstellePdf(pfad, "Eröffnungsbilanz", bloecke, true, "(Ort, Datum)", "(Unterschrift Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // STEUERNUMMER-BESCHEID / USt-IdNr
    // ─────────────────────────────────────────
    public static string ErstelleSteuernummerBescheid(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string finanzamt = Feld(f, "finanzamt");
        string steuernummer = Feld(f, "steuernummer");
        string ustIdNr = Feld(f, "ustIdNr");
        string ausstellungsdatum = Feld(f, "ausstellungsdatum");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (false, $"Diese Übersicht fasst die steuerliche Erfassung des Unternehmens {firma.Name} {firma.Rechtsform} zusammen."),
            (true,  "Angaben laut Bescheid"),
            (false, "Zuständiges Finanzamt: " + N(finanzamt)),
            (false, "Steuernummer: " + N(steuernummer)),
            (false, "USt-IdNr.: " + (string.IsNullOrEmpty(ustIdNr) ? "\u2013" : ustIdNr)),
            (false, "Ausstellungsdatum: " + N(ausstellungsdatum)),
        };

        string pfad = PfadFuer("Steuernummer_Bescheid");
        bool ok = ErstellePdf(pfad, "Steuernummer-Bescheid / USt-IdNr", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // IMPRESSUM
    // ─────────────────────────────────────────
    public static string ErstelleImpressum(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string vertretungsberechtigte = Feld(f, "vertretungsberechtigte");
        string email = Feld(f, "email");
        string telefon = Feld(f, "telefon");
        string handelsregisternummer = Feld(f, "handelsregisternummer");
        string zusatzangaben = Feld(f, "zusatzangaben");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "Angaben gemäß § 5 TMG"),
            (false, firma.Name + " " + firma.Rechtsform),
            (false, string.IsNullOrEmpty(firma.Anschrift) ? "[Anschrift nicht in den Einstellungen hinterlegt]" : firma.Anschrift),
            (true,  "Vertreten durch"),
            (false, N(vertretungsberechtigte)),
            (true,  "Kontakt"),
            (false, "E-Mail: " + N(email)),
            (false, "Telefon: " + N(telefon)),
        };

        if (!string.IsNullOrWhiteSpace(handelsregisternummer))
        {
            bloecke.Add((true, "Registereintrag"));
            bloecke.Add((false, "Handelsregisternummer: " + handelsregisternummer));
        }

        if (!string.IsNullOrEmpty(firma.UstIdNr))
        {
            bloecke.Add((true, "Umsatzsteuer-Identifikationsnummer"));
            bloecke.Add((false, firma.UstIdNr));
        }

        if (!string.IsNullOrWhiteSpace(zusatzangaben))
        {
            bloecke.Add((true, "Zusatzangaben"));
            bloecke.Add((false, zusatzangaben));
        }

        string pfad = PfadFuer("Impressum");
        bool ok = ErstellePdf(pfad, "Impressum", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // DIENSTLEISTUNGSKATALOG / PREISLISTE
    // ─────────────────────────────────────────
    public static string ErstelleDienstleistungskatalog(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string leistungsuebersicht = Feld(f, "leistungsuebersicht");
        string preismodell = Feld(f, "preismodell");
        string zusatzangaben = Feld(f, "zusatzangaben");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (false, $"Diese Übersicht listet die Leistungen der {firma.Name} {firma.Rechtsform} sowie das zugrunde liegende Preismodell auf."),
            (true,  "Leistungsübersicht"),
            (false, N(leistungsuebersicht)),
            (true,  "Preismodell"),
            (false, N(preismodell)),
        };

        if (!string.IsNullOrWhiteSpace(zusatzangaben))
        {
            bloecke.Add((true, "Zusatzangaben"));
            bloecke.Add((false, zusatzangaben));
        }

        string pfad = PfadFuer("Dienstleistungskatalog");
        bool ok = ErstellePdf(pfad, "Dienstleistungskatalog / Preisliste", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // GRÜNDUNGS-CHECKLISTE
    // ─────────────────────────────────────────
    public static string ErstelleGruendungsCheckliste(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string notizen = Feld(f, "notizen");

        string Haken(string key, string label)
        {
            bool erledigt = Feld(f, key) == "Ja";
            return (erledigt ? "[x] " : "[ ] ") + label;
        }

        var bloecke = new List<(bool, string)>
        {
            (true,  "Gründungs-Checkliste"),
            (false, Haken("geschaeftskontoEroeffnet", "Geschäftskonto eröffnet")),
            (false, Haken("gewerbeAngemeldet", "Gewerbe angemeldet")),
            (false, Haken("versicherungenAbgeschlossen", "Versicherungen abgeschlossen")),
            (false, Haken("buchhaltungEingerichtet", "Buchhaltung eingerichtet")),
            (false, Haken("websiteErstellt", "Website & Branding erstellt")),
        };

        if (!string.IsNullOrWhiteSpace(notizen))
        {
            bloecke.Add((true, "Notizen"));
            bloecke.Add((false, notizen));
        }

        string pfad = PfadFuer("Gruendungs_Checkliste");
        bool ok = ErstellePdf(pfad, "Gründungs-Checkliste", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // UNTERNEHMENSSTAMMDATEN
    // ─────────────────────────────────────────
    public static string ErstelleUnternehmensstammdaten(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string firmenname = Feld(f, "firma");
        string rechtsform = Feld(f, "rechtsform");
        string branche = Feld(f, "branche");
        string standort = Feld(f, "standort");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (false, "Diese Übersicht fasst die im System Ventoriq hinterlegten Stammdaten des Unternehmens zusammen."),
            (true,  "Unternehmensdaten"),
            (false, "Firmenname: " + N(firmenname)),
            (false, "Rechtsform: " + N(rechtsform)),
            (false, "Branche: " + N(branche)),
            (false, "Standort: " + N(standort)),
        };

        string pfad = PfadFuer("Unternehmensstammdaten");
        bool ok = ErstellePdf(pfad, "Unternehmensstammdaten", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // LIZENZHINWEIS ERWEITERT
    // ─────────────────────────────────────────
    public static string ErstelleLizenzhinweisErweitert(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string geltungsbereich = Feld(f, "geltungsbereich");
        string bedingung = Feld(f, "bedingungRechteuebergang");
        string nutzungsumfang = Feld(f, "nutzungsumfang");
        string raeumlich = Feld(f, "raeumlicheReichweite");
        string zeitlich = Feld(f, "zeitlicheReichweite");
        string zusatzvereinbarung = Feld(f, "zusatzvereinbarung");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "1. Grundsatz der Lizenzierung"),
            (false, $"Dieser Lizenzhinweis regelt die Einräumung von Nutzungsrechten für alle Leistungen, Entwürfe und Werke der {firma.Name}. Er ist integraler Bestandteil der vertraglichen Beziehung und gilt für: " + N(geltungsbereich) + "."),
            (true,  "2. Unbeschränktes Nutzungsrecht"),
            (false, "Dem Auftraggeber wird ein unbeschränktes Nutzungsrecht eingeräumt. Dies umfasst ausdrücklich:"),
            (false, "Art der Nutzung: " + N(nutzungsumfang)),
            (false, "Räumliche Reichweite: " + N(raeumlich)),
            (false, "Zeitliche Reichweite: " + N(zeitlich)),
            (true,  "3. Rechteübergang und Bedingungen"),
            (false, "Die Übertragung der genannten Rechte erfolgt unter der aufschiebenden Bedingung der: " + N(bedingung) + ". Bis zur Erfüllung dieser Bedingung verbleiben sämtliche Rechte sowie das Eigentum an Zwischenergebnissen beim Urheber."),
            (true,  "4. Bearbeitungs- und Weitergaberechte"),
            (false, "Der Lizenznehmer ist berechtigt, die Leistungen nach eigenem Ermessen zu verändern, zu erweitern oder mit anderen Werken zu verbinden. Die Weitergabe der Rechte an Dritte sowie die Erteilung von Unterlizenzen ist " + N(zusatzvereinbarung) + "."),
            (true,  "5. Urheberrecht"),
            (false, $"Das unveräußerliche Urheberrecht verbleibt bei der {firma.Name}. Der Urheber behält sich das Recht vor, die erbrachten Leistungen zum Zwecke der Eigenwerbung (Referenzen) zu nutzen, sofern nichts anderes schriftlich vereinbart wurde."),
            (false, $"Rechtlicher Hinweis: Dieses Dokument dient der Information über die standardmäßig geltenden Lizenzbestimmungen unserer AGB. Es ist Bestandteil der kaufmännischen Unterlagen von {firma.Name} und auch ohne handschriftliche Unterschrift rechtsgültig."),
        };

        string pfad = PfadFuer("Lizenzhinweis_Erweitert");
        bool ok = ErstellePdf(pfad, "Lizenzhinweis Erweitert", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // DATENSCHUTZERKLÄRUNG
    // ─────────────────────────────────────────
    public static string ErstelleDatenschutzerklaerung(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string versionsstand = Feld(f, "versionsstand");
        string verantwortlicheStelle = Feld(f, "verantwortlicheStelle");
        string kontaktDatenschutz = Feld(f, "kontaktDatenschutz");
        string zusatzangabe = Feld(f, "zusatzangabe");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>();
        if (!string.IsNullOrWhiteSpace(versionsstand)) bloecke.Add((false, versionsstand));

        bloecke.AddRange(new List<(bool, string)>
        {
            (true,  "§ 1 Grundsatz der Vertraulichkeit"),
            (false, $"(1) Gemäß § 5.1 unserer AGB werden alle Informationen und Unterlagen, die im Rahmen der Zusammenarbeit zwischen der {firma.Name} und dem Kunden bekannt werden oder entstehen, streng vertraulich behandelt."),
            (false, "(2) Dies gilt insbesondere für kundenspezifische Daten, Software-Architekturen, Geschäftsgeheimnisse und Kalkulationen, die während der gemeinsamen Tätigkeit ausgetauscht werden."),
            (true,  "§ 2 Lokale Datenverarbeitung (Systemstandard)"),
            (false, "(1) Der Kunde wird darüber informiert, dass zur Abwicklung von Angeboten und Rechnungen das System Ventoriq eingesetzt wird."),
            (false, "(2) Datensparsamkeit: Personenbezogene Daten werden ausschließlich lokal in einer verschlüsselten SQLite-Instanz auf unseren Endgeräten gespeichert."),
            (false, "(3) Es findet keine Übermittlung dieser Daten in eine Cloud oder an externe Server statt, sofern dies nicht ausdrücklich für die Projektdurchführung (z. B. Hosting-Setup) schriftlich vereinbart wurde."),
            (true,  "§ 3 Zweck der Datenerfassung"),
            (false, "Die Erfassung und Verarbeitung von Kundendaten (Name, Anschrift, Bankdaten, Projektdetails) erfolgt ausschließlich zum Zweck der: Erstellung von rechtskonformen Angeboten und Rechnungen; Dokumentation der erbrachten Dienstleistungen im Kassenbuch; Pflege der gemeinsamen Geschäftsbeziehung in der Kundendatenbank (KDB)."),
            (true,  "§ 4 Sicherheit & Löschung"),
            (false, "(1) Zum Schutz der Daten werden moderne Verschlüsselungsverfahren (SHA-256 Hashing für Zugänge) eingesetzt."),
            (false, "(2) Nach Abschluss der Tätigkeit und Ablauf der gesetzlichen Aufbewahrungsfristen werden alle projektspezifischen Daten auf Verlangen des Kunden gelöscht, sofern dem keine gesetzlichen Pflichten entgegenstehen."),
            (true,  "§ 5 Verantwortlichkeit & Kontakt"),
            (false, "Verantwortlich für die Einhaltung dieser Bestimmungen ist: " + N(verantwortlicheStelle) + "."),
            (false, "Bei Fragen wenden Sie sich bitte an: " + N(kontaktDatenschutz) + "."),
        });

        if (!string.IsNullOrWhiteSpace(zusatzangabe)) bloecke.Add((false, "Zusatzhinweis: " + zusatzangabe));

        bloecke.Add((false, "Rechtlicher Hinweis: Dieses Dokument wurde mit dem System Ventoriq erstellt und ist als Bestandteil des Angebots oder der Rechnung auch ohne handschriftliche Unterschrift rechtsgültig."));

        string pfad = PfadFuer("Datenschutzerklaerung");
        bool ok = ErstellePdf(pfad, "Datenschutzerklärung", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // VERTRAULICHKEITSERKLÄRUNG
    // ─────────────────────────────────────────
    public static string ErstelleVertraulichkeitserklaerung(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string versionsstand = Feld(f, "versionsstand");
        string dauerDerPflicht = Feld(f, "dauerDerPflicht");
        string kontaktNda = Feld(f, "kontaktNda");
        string zusatzangabe = Feld(f, "zusatzangabe");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "§ 1 Grundsatz der Verschwiegenheit"),
            (false, "(1) Vertrauen ist die Basis jeder Softwareentwicklung. Gemäß unserer Allgemeinen Geschäftsbedingungen verpflichten wir uns, alle Informationen und Unterlagen, die im Rahmen der Zusammenarbeit bekannt werden, streng vertraulich zu behandeln."),
            (false, "(2) Dies gilt für alle Geschäftsgeheimnisse, technischen Konzepte, Quellcodes und wirtschaftlichen Daten, die nicht ausdrücklich zur Weitergabe an Dritte bestimmt sind."),
            (true,  "§ 2 Schutz kundenspezifischer Daten (Ventoriq-Standard)"),
            (false, "(1) Zur Bearbeitung Ihres Projekts nutzen wir das System Ventoriq. Alle projektbezogenen Daten werden ausschließlich lokal in einer verschlüsselten Umgebung gespeichert."),
            (false, "(2) Es findet keine Übertragung Ihrer sensiblen Daten in eine Cloud statt. Damit stellen wir sicher, dass Ihre Informationen vor unbefugtem Zugriff durch externe Plattformbetreiber geschützt sind."),
            (true,  "§ 3 Umfang der Geheimhaltung"),
            (false, "Die Geheimhaltungspflicht umfasst: sämtliche im Rahmen von Beratungsgesprächen erlangten Kenntnisse; alle zur Verfügung gestellten Unterlagen (analog und digital); Ergebnisse von Zwischenphasen und Prototypen."),
            (true,  "§ 4 Dauer der Verpflichtung"),
            (false, "Die Pflicht zur Vertraulichkeit beginnt mit dem ersten Kontakt und bleibt auch nach Beendigung der aktiven Zusammenarbeit für einen Zeitraum von " + N(dauerDerPflicht) + " bestehen."),
            (true,  "§ 5 Ausnahmen"),
            (false, "Die Vertraulichkeit gilt nicht für Informationen, die nachweislich bereits öffentlich bekannt sind, ohne Zutun einer Vertragspartei öffentlich bekannt werden oder aufgrund gesetzlicher Vorschriften (z. B. gegenüber dem Finanzamt) offengelegt werden müssen."),
            (true,  "§ 6 Kontakt & Zusatzvereinbarungen"),
            (false, "Sollten für Ihr Projekt weiterführende, individuelle Geheimhaltungsvereinbarungen (NDAs) erforderlich sein, wenden Sie sich bitte an: " + N(kontaktNda) + "."),
        };

        if (!string.IsNullOrWhiteSpace(versionsstand)) bloecke.Insert(0, (false, versionsstand));
        if (!string.IsNullOrWhiteSpace(zusatzangabe)) bloecke.Add((false, "Zusatz: " + zusatzangabe));

        bloecke.Add((false, $"Rechtlicher Hinweis: Dieses Dokument dient der Information über unsere Standards zur Geheimhaltung. Es ist Bestandteil der kaufmännischen Unterlagen von {firma.Name} und auch ohne handschriftliche Unterschrift rechtsgültig."));

        string pfad = PfadFuer("Vertraulichkeitserklaerung");
        bool ok = ErstellePdf(pfad, "Vertraulichkeitserklärung", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // MUSTER-ARBEITSVERTRAG
    // ─────────────────────────────────────────
    public static string ErstelleArbeitsvertrag(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string vertragsart = Feld(f, "vertragsart");
        string stellenbezeichnung = Feld(f, "stellenbezeichnung");
        string vertragsbeginn = Feld(f, "vertragsbeginn");
        string wochenstunden = Feld(f, "wochenstunden");
        string bruttogehalt = Feld(f, "bruttogehalt");
        string urlaubstage = Feld(f, "urlaubstage");
        string probezeit = Feld(f, "probezeit");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;
        string titel = string.IsNullOrEmpty(vertragsart) ? "Arbeitsvertrag" : vertragsart + "-Vertrag";

        var bloecke = new List<(bool, string)>
        {
            (false, $"Zwischen der {firma.Name} (nachfolgend „Arbeitgeber“) und der/dem unten genannten Person (nachfolgend „Arbeitnehmer“) wird folgender Vertrag geschlossen."),
            (true,  "§ 1 Beginn & Tätigkeit"),
            (false, "(1) Der Arbeitnehmer wird ab dem " + N(vertragsbeginn) + " als " + N(stellenbezeichnung) + " eingestellt."),
            (false, "(2) Der Aufgabenbereich umfasst die Unterstützung in den betrieblichen Prozessen sowie spezifische Fachaufgaben gemäß den Weisungen der Geschäftsführung."),
            (true,  "§ 2 Probezeit"),
            (false, "Die ersten " + N(probezeit) + " Monate des Arbeitsverhältnisses gelten als Probezeit. Während dieser Zeit kann das Arbeitsverhältnis von beiden Seiten mit einer Frist von zwei Wochen gekündigt werden."),
            (true,  "§ 3 Vergütung & Arbeitszeit"),
            (false, "(1) Das monatliche Bruttogehalt beträgt " + N(bruttogehalt) + " EUR."),
            (false, "(2) Die regelmäßige wöchentliche Arbeitszeit beträgt " + N(wochenstunden) + " Stunden."),
            (true,  "§ 4 Urlaub"),
            (false, "Der Arbeitnehmer hat Anspruch auf einen bezahlten Erholungsurlaub von " + N(urlaubstage) + " Arbeitstagen pro Kalenderjahr."),
            (true,  "§ 5 Verschwiegenheit & Urheberrechte"),
            (false, "(1) Der Arbeitnehmer verpflichtet sich, über alle betrieblichen Belange und Geschäftsgeheimnisse Stillschweigen zu bewahren (gemäß der allgemeinen Vertraulichkeitserklärung von Ventoriq)."),
            (false, "(2) Alle im Rahmen der Tätigkeit erstellten Arbeitsergebnisse (insb. Software-Code, Designs, Konzepte) stehen urheberrechtlich dem Arbeitgeber zu."),
            (true,  "§ 6 Schlussbestimmungen"),
            (false, "Änderungen dieses Vertrages bedürfen der Schriftform. Sollten einzelne Bestimmungen unwirksam sein, bleibt die Wirksamkeit des restlichen Vertrages unberührt."),
        };

        string pfad = PfadFuer("Arbeitsvertrag");

        // Sonderfall: zwei Unterschriftenzeilen (Arbeitgeber + Arbeitnehmer)
        // statt der üblichen einen - deshalb hier direkt ohne den
        // Standard-Signaturblock von ErstellePdf gerendert.
        bool ok = ErstellePdfMitDoppelSignatur(pfad, titel, bloecke,
            "(Ort, Datum)", "(Unterschrift Arbeitgeber)",
            "(Ort, Datum)", "(Unterschrift Arbeitnehmer)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // VORLAGE KÜNDIGUNG
    // ─────────────────────────────────────────
    public static string ErstelleKuendigung(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string arbeitnehmer = Feld(f, "arbeitnehmer");
        string kuendigungsdatum = Feld(f, "kuendigungsdatum");
        string kuendigungstermin = Feld(f, "kuendigungstermin");
        string kuendigungsgrund = Feld(f, "kuendigungsgrund");
        string rueckgabeCheck = Feld(f, "rueckgabeCheck");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;
        bool zeigeRueckgabe(string wert) => string.IsNullOrEmpty(wert) || !wert.Trim().ToLower().Contains("nein");

        var bloecke = new List<(bool, string)>
        {
            (false, "An: " + (string.IsNullOrEmpty(arbeitnehmer) ? "[nicht angegeben]" : arbeitnehmer)),
            (false, " "),
            (false, "Sehr geehrte(r) Frau/Herr,"),
            (false, "hiermit kündigen wir das mit Ihnen bestehende Arbeitsverhältnis vom " + N(kuendigungsdatum) + " ordentlich und fristgerecht zum " + N(kuendigungstermin) + "."),
            (true,  "§ 1 Beendigungsgrund & Fristen"),
            (false, "Die Kündigung erfolgt " + N(kuendigungsgrund) + " unter Einhaltung der vertraglich vereinbarten Kündigungsfrist. Hilfsweise kündigen wir zum nächstmöglichen Termin."),
            (true,  "§ 2 Freistellung & Resturlaub"),
            (false, "Wir behalten uns vor, Sie bis zur Beendigung des Arbeitsverhältnisses unter Fortzahlung der Bezüge unwiderruflich von der Arbeit freizustellen. Etwaige noch bestehende Urlaubsansprüche sowie Guthaben auf dem Arbeitszeitkonto werden mit der Zeit der Freistellung verrechnet."),
        };

        if (zeigeRueckgabe(rueckgabeCheck))
        {
            bloecke.Add((true, "§ 3 Rückgabe von Firmeneigentum"));
            bloecke.Add((false, "Bitte übergeben Sie spätestens an Ihrem letzten Arbeitstag alle Ihnen überlassenen Arbeitsmittel. Dies umfasst insbesondere: Hardware (Laptop, Smartphone, Token); Zugangsdaten und Passkeys zu internen Systemen (gemäß Vertraulichkeitserklärung); Projektunterlagen und Datenträger."));
        }

        bloecke.Add((true, "§ 4 Arbeitszeugnis"));
        bloecke.Add((false, "Ein qualifiziertes Arbeitszeugnis wird Ihnen zeitnah erstellt und nach der Beendigung des Arbeitsverhältnisses zugesandt."));
        bloecke.Add((false, "Wir danken Ihnen für die bisherige Zusammenarbeit und wünschen Ihnen für Ihren weiteren Berufs- und Lebensweg alles Gute."));

        string pfad = PfadFuer("Kuendigung");
        bool ok = ErstellePdf(pfad, "Kündigung", bloecke, true, "(Ort, Datum)", "(Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // STELLENBESCHREIBUNG
    // ─────────────────────────────────────────
    public static string ErstelleStellenbeschreibung(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string stellentitel = Feld(f, "stellentitel");
        string abteilung = Feld(f, "abteilung");
        string berichtetAn = Feld(f, "berichtetAn");
        string hauptaufgaben = Feld(f, "hauptaufgaben");
        string anforderungsprofil = Feld(f, "anforderungsprofil");
        string benefits = Feld(f, "benefits");
        string starttermin = Feld(f, "starttermin");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "1. Allgemeine Informationen"),
            (false, "Position: " + N(stellentitel)),
            (false, "Abteilung: " + N(abteilung)),
            (false, "Hierarchische Einordnung: Berichtet direkt an " + N(berichtetAn)),
            (false, "Geplanter Beginn: " + N(starttermin)),
            (true,  "2. Zweck der Stelle"),
            (false, $"Die Position trägt maßgeblich dazu bei, die Vision von {firma.Name} umzusetzen."),
            (true,  "3. Ihre Hauptaufgaben"),
            (false, N(hauptaufgaben)),
            (true,  "4. Ihr Profil & Qualifikationen"),
            (false, N(anforderungsprofil)),
            (true,  "5. Besondere Rahmenbedingungen"),
            (false, "(1) IP-Schutz: Der Stelleninhaber ist zur strikten Geheimhaltung von Quellcodes und Geschäftsgeheimnissen verpflichtet."),
            (false, "(2) Methodik: Wir arbeiten nach agilen Prinzipien mit wöchentlichen Statuskontrollen und klaren Meilensteinen."),
            (true,  "6. Was wir bieten"),
            (false, N(benefits)),
        };

        string pfad = PfadFuer("Stellenbeschreibung");
        bool ok = ErstellePdf(pfad, "Stellenbeschreibung", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // URLAUBSANTRAG
    // ─────────────────────────────────────────
    public static string ErstelleUrlaubsantrag(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string kalenderjahr = Feld(f, "kalenderjahr");
        string einreichungsfrist = Feld(f, "einreichungsfrist");
        string zusatzhinweis = Feld(f, "zusatzhinweis");

        var bloecke = new List<(bool, string)>
        {
            (true,  "Persönliche Angaben des Mitarbeiters"),
            (false, "Name, Vorname: ________________________________________________"),
            (false, "Abteilung / Projekt: ___________________________________________"),
            (true,  "Zeitraum und Dauer" + (string.IsNullOrEmpty(kalenderjahr) ? "" : " (" + kalenderjahr + ")")),
            (false, "Hiermit beantrage ich Urlaub für den Zeitraum vom ____________ bis einschließlich ____________."),
            (false, "Dies entspricht einer Anzahl von ________________ Arbeitstagen."),
            (false, "Art des Urlaubs / Abwesenheit: [ ] Erholungsurlaub  [ ] Sonderurlaub  [ ] Zeitausgleich (Überstunden)"),
            (false, "Vertretungsregelung: Meine laufenden Aufgaben werden während meiner Abwesenheit übernommen von: ________________________________"),
        };

        var hinweise = new List<string>();
        if (!string.IsNullOrWhiteSpace(einreichungsfrist)) hinweise.Add(einreichungsfrist);
        if (!string.IsNullOrWhiteSpace(zusatzhinweis)) hinweise.Add(zusatzhinweis);
        if (hinweise.Count > 0)
        {
            bloecke.Add((true, "Hinweise des Arbeitgebers"));
            bloecke.Add((false, string.Join(" ", hinweise)));
        }

        bloecke.Add((true, "Bearbeitungsvermerk (durch die Geschäftsführung auszufüllen)"));
        bloecke.Add((false, "[ ] Der Urlaubsantrag wird genehmigt.   [ ] Der Urlaubsantrag wird abgelehnt."));
        bloecke.Add((false, "Begründung: ________________________________________________"));

        string pfad = PfadFuer("Urlaubsantrag");
        bool ok = ErstellePdf(pfad, "Urlaubsantrag", bloecke, true, "(Ort, Datum)", "(Unterschrift Mitarbeiter)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // CORPORATE IDENTITY MANUAL (Corporate Design Leitfaden)
    // ─────────────────────────────────────────
    public static string ErstelleCorporateIdentityManual(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string versionsstand = Feld(f, "versionsstand");
        string markenkern = Feld(f, "markenkern");
        string primaerfarbe = Feld(f, "primaerfarbe");
        string sekundaerfarbe = Feld(f, "sekundaerfarbe");
        string hausschrift = Feld(f, "hausschrift");
        string aufloesung = Feld(f, "aufloesung");
        string layoutRaster = Feld(f, "layoutRaster");
        string zusatzangabe = Feld(f, "zusatzangabe");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "§ 1 Markenidentität & Vision"),
            (false, "Unser Erscheinungsbild spiegelt unsere Professionalität und unseren technologischen Anspruch wider."),
            (false, "Markenkern: " + N(markenkern)),
            (false, "Ziel ist eine konsistente Wahrnehmung über alle digitalen und analogen Berührungspunkte hinweg."),
            (true,  "§ 2 Farbsystem"),
            (false, "Die Farbpalette ist verbindlich für alle Dokumente, Benutzeroberflächen und Marketingmaterialien anzuwenden."),
            (false, "Primärfarbe: " + N(primaerfarbe) + " (Einsatz für Interaktion und Highlights)."),
            (false, "Sekundärfarbe: " + N(sekundaerfarbe) + " (Einsatz für Hintergründe und Strukturen)."),
            (true,  "§ 3 Typografie"),
            (false, "Zur Sicherstellung der Lesbarkeit und Modernität wird ausschließlich folgende Typografie verwendet:"),
            (false, "Schriftart: " + N(hausschrift)),
            (false, "Hierarchie: Überschriften (H1) werden in Bold gesetzt, Fließtexte in Regular. Hilfetexte und Labels erscheinen in Italic."),
            (true,  "§ 4 Layout & Raster"),
            (false, "(1) Alle digitalen Anwendungen sind auf einen Desktop-Standard von " + N(aufloesung) + " optimiert."),
            (false, "(2) Abstände und Weißräume folgen konsequent dem " + N(layoutRaster) + "-System, um visuelle Ruhe und Stabilität zu gewährleisten."),
            (true,  "§ 5 Logo-Verwendung & Gestaltungselemente"),
            (false, "(1) Das Logo ist stets auf ausreichendem Kontrast zu platzieren."),
        };

        if (!string.IsNullOrWhiteSpace(versionsstand)) bloecke.Insert(0, (false, versionsstand));
        if (!string.IsNullOrWhiteSpace(zusatzangabe)) bloecke.Add((false, "(2) Zusatzrichtlinien: " + zusatzangabe));

        bloecke.Add((false, "(3) Ergänzende grafische Elemente (wie z. B. geometrische Formen) dürfen nur dezent und unterstützend eingesetzt werden."));
        bloecke.Add((false, $"Dieses Dokument wurde im Rahmen des Systems Ventoriq erstellt. Es ist die verbindliche Arbeitsgrundlage für alle gestalterischen Tätigkeiten von {firma.Name}."));

        string pfad = PfadFuer("Corporate_Identity_Manual");
        bool ok = ErstellePdf(pfad, "Corporate Identity Manual", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // UNTERNEHMENSRICHTLINIEN
    // ─────────────────────────────────────────
    public static string ErstelleUnternehmensrichtlinien(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string versionsstand = Feld(f, "versionsstand");
        string vision = Feld(f, "vision");
        string unternehmenskultur = Feld(f, "unternehmenskultur");
        string reaktionszeit = Feld(f, "reaktionszeit");
        string technologieStack = Feld(f, "technologieStack");
        string designStack = Feld(f, "designStack");
        string uxStack = Feld(f, "uxStack");
        string datenschutzabschnitt = Feld(f, "datenschutzabschnitt");
        string datensicherheitabschnitt = Feld(f, "datensicherheitabschnitt");
        string zusatzangaben = Feld(f, "zusatzangaben");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "§ 1 Vision & Unternehmenskultur"),
            (false, "(1) " + N(vision) + "."),
            (false, "(2) Unsere Werte: " + N(unternehmenskultur) + ". Wir arbeiten eigenverantwortlich, lösungsorientiert und proaktiv."),
            (true,  "§ 2 Kommunikation & Zusammenarbeit"),
            (false, "(1) Holschuld: Informationen werden aktiv im Team geteilt. Wir warten nicht, bis wir angesprochen werden."),
            (false, "(2) Reaktionsregel: Interne Anfragen (z. B. via Discord oder E-Mail) sind innerhalb von maximal " + N(reaktionszeit) + " Stunden zu beantworten. Ein kurzes Feedback („Gesehen, kümmere mich“) ist ausreichend."),
            (false, "(3) Ergebnisfokus: Jeder Arbeitsschritt muss ein lauffähiges Ergebnis liefern. Der Grundsatz „Integration schlägt Feature-Entwicklung“ ist bindend."),
            (true,  "§ 3 Technologische Standards"),
            (false, "(1) Zur Projektabwicklung und Verwaltung nutzen wir verbindlich: " + N(technologieStack) + "."),
            (false, "(2) Architektur & Design: " + N(designStack) + "."),
            (false, "(3) Qualitätssicherung: " + N(uxStack) + "."),
            (true,  "§ 4 Datenschutz & Sicherheit"),
            (false, "(1) Datenschutz: " + N(datenschutzabschnitt) + "."),
            (false, "(2) Datensicherheit: " + N(datensicherheitabschnitt) + "."),
        };

        if (!string.IsNullOrWhiteSpace(versionsstand)) bloecke.Insert(0, (false, versionsstand));

        if (!string.IsNullOrWhiteSpace(zusatzangaben))
        {
            bloecke.Add((true, "§ 5 Besondere Bestimmungen"));
            bloecke.Add((false, zusatzangaben));
        }

        bloecke.Add((false, $"Dieses Dokument wurde im Rahmen des Systems Ventoriq erstellt. Es bildet die verbindliche Arbeitsgrundlage für alle Mitarbeiter und Partner der {firma.Name}."));

        string pfad = PfadFuer("Unternehmensrichtlinien");
        bool ok = ErstellePdf(pfad, "Unternehmensrichtlinien", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // SOCIAL MEDIA STRATEGIE
    // ─────────────────────────────────────────
    public static string ErstelleSocialMediaStrategie(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string kernbotschaft = Feld(f, "kernbotschaft");
        string zielgruppe = Feld(f, "zielgruppe");
        string primaereKanaele = Feld(f, "primaereKanaele");
        string productInsights = Feld(f, "productInsights");
        string techTransparenz = Feld(f, "techTransparenz");
        string startupEducation = Feld(f, "startupEducation");
        string postFrequenz = Feld(f, "postFrequenz");
        string zusatzangabe = Feld(f, "zusatzangabe");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "§ 1 Vision & Markenkern"),
            (false, $"Die Präsenz in sozialen Netzwerken spiegelt die Vision von {firma.Name}."),
            (false, "Kernbotschaft: " + N(kernbotschaft)),
            (true,  "§ 2 Zielgruppe & Kanäle"),
            (false, "Wir konzentrieren unsere Ressourcen auf Kanäle mit maximaler Relevanz für unsere Zielgruppe: " + N(zielgruppe) + "."),
            (false, "Gewählte Plattformen: " + N(primaereKanaele)),
            (true,  "§ 3 Content-Säulen"),
            (false, "Unsere Inhalte folgen drei thematischen Schwerpunkten:"),
            (false, "1. Product-Insights: " + N(productInsights)),
            (false, "2. Tech-Transparency: " + N(techTransparenz)),
            (false, "3. Startup-Education: " + N(startupEducation)),
            (true,  "§ 4 Visual Styleguide"),
            (false, "Alle Postings müssen dem etablierten Designsystem folgen (siehe Corporate Identity Manual)."),
            (true,  "§ 5 Redaktionsplan & Frequenz"),
            (false, "Um eine kontinuierliche Sichtbarkeit zu gewährleisten, halten wir folgende Frequenz ein: " + N(postFrequenz) + "."),
        };

        if (!string.IsNullOrWhiteSpace(zusatzangabe)) bloecke.Add((false, "Aktueller Fokus: " + zusatzangabe));

        string pfad = PfadFuer("Social_Media_Strategie");
        bool ok = ErstellePdf(pfad, "Social Media Strategie", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // BUSINESSPLAN
    // ─────────────────────────────────────────
    public static string ErstelleBusinessplan(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string geschaeftsidee = Feld(f, "geschaeftsidee");
        string zielgruppeMarkt = Feld(f, "zielgruppeMarkt");
        string angebotPreise = Feld(f, "angebotPreise");
        string staerken = Feld(f, "staerken");
        string risiken = Feld(f, "risiken");
        string investitionsbedarf = Feld(f, "investitionsbedarf");
        string umsatzJ1 = Feld(f, "umsatzJ1");
        string umsatzJ2 = Feld(f, "umsatzJ2");
        string umsatzJ3 = Feld(f, "umsatzJ3");
        string teamMeilensteine = Feld(f, "teamMeilensteine");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "1. Zusammenfassung (Executive Summary)"),
            (false, "Das Vorhaben umfasst im Kern: " + N(geschaeftsidee) + "."),
            (false, "Unser Ziel ist es, eine nachhaltige Marktposition durch Qualität und Kundennutzen zu erreichen."),
            (true,  "2. Markt- und Zielgruppenanalyse"),
            (false, "Wir richten uns primär an: " + N(zielgruppeMarkt) + ". Der Markt wurde analysiert und bietet aufgrund aktueller Trends ein erhebliches Potenzial für unser Leistungsangebot."),
            (true,  "3. Leistungsangebot und Monetarisierung"),
            (false, "Unser Produkt-/Dienstleistungsportfolio besteht aus: " + N(angebotPreise) + ". Die Preisgestaltung orientiert sich an einem marktüblichen Standard bei gleichzeitigem Fokus auf hohe Effizienz."),
            (true,  "4. Strategische Analyse (SWOT)"),
            (false, "Wettbewerbsvorteile: " + N(staerken)),
            (false, "Risikomanagement: " + N(risiken)),
            (false, "Genaue SWOT-Analyse liegt bei (siehe Dokument „SWOT-Analyse“)."),
            (true,  "5. Finanzplanung und Kapitalbedarf"),
            (false, "Für die Umsetzung der Strategie wurde folgender Finanzrahmen kalkuliert:"),
            (false, "Einmaliger Kapitalbedarf: " + N(investitionsbedarf) + " EUR."),
            (false, "Ertragsplanung (geschätzt): Jahr 1: " + N(umsatzJ1) + " EUR, Jahr 2: " + N(umsatzJ2) + " EUR, Jahr 3: " + N(umsatzJ3) + " EUR."),
            (true,  "6. Organisation und Fahrplan"),
            (false, "Die operative Umsetzung erfolgt durch: " + N(teamMeilensteine) + "."),
            (false, "Wir planen die Erreichung der Gewinnschwelle (Break-Even) zeitnah nach dem Markteintritt."),
        };

        string pfad = PfadFuer("Businessplan");
        bool ok = ErstellePdf(pfad, "Businessplan", bloecke, true, "(Ort, Datum)", "(Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // MARKT- & WETTBEWERBSANALYSE
    // ─────────────────────────────────────────
    public static string ErstelleMarktWettbewerbsanalyse(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string marktbeschreibung = Feld(f, "marktbeschreibung");
        string zielgruppensegmente = Feld(f, "zielgruppensegmente");
        string direkteWettbewerber = Feld(f, "direkteWettbewerber");
        string indirekteWettbewerber = Feld(f, "indirekteWettbewerber");
        string wettbewerbsvorteile = Feld(f, "wettbewerbsvorteile");
        string marktpotenzial = Feld(f, "marktpotenzial");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "1. Marktumfeld und Branchentrends"),
            (false, "Die aktuelle Marktsituation lässt sich wie folgt zusammenfassen: " + N(marktbeschreibung) + "."),
            (true,  "2. Zielgruppenanalyse"),
            (false, "Unser Angebot richtet sich primär an: " + N(zielgruppensegmente) + "."),
            (true,  "3. Wettbewerbsbetrachtung"),
            (false, "Im Rahmen der Analyse wurden folgende Marktteilnehmer identifiziert:"),
            (false, "Direkte Konkurrenz: " + N(direkteWettbewerber)),
            (false, "Indirekte Konkurrenz / Alternativen: " + N(indirekteWettbewerber)),
            (true,  "4. Eigene Marktpositionierung (USP)"),
            (false, "Gegenüber dem Wettbewerb grenzen wir uns durch folgende Vorteile ab: " + N(wettbewerbsvorteile)),
            (true,  "5. Prognose und Chancen"),
            (false, "Basierend auf der aktuellen Entwicklung sehen wir folgendes Potenzial: " + N(marktpotenzial) + ". Ziel ist es, durch kontinuierliche Expansion und technologische Reife einen festen Marktanteil zu sichern."),
        };

        string pfad = PfadFuer("Markt_Wettbewerbsanalyse");
        bool ok = ErstellePdf(pfad, "Markt- & Wettbewerbsanalyse", bloecke, true, "(Ort, Datum)", "(Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // SWOT-ANALYSE
    // ─────────────────────────────────────────
    public static string ErstelleSwotAnalyse(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string interneStaerken = Feld(f, "interneStaerken");
        string interneSchwaechen = Feld(f, "interneSchwaechen");
        string externeChancen = Feld(f, "externeChancen");
        string externeRisiken = Feld(f, "externeRisiken");
        string strategischesFazit = Feld(f, "strategischesFazit");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "1. Interne Analyse: Stärken & Schwächen"),
            (false, "Stärken (Strengths): " + N(interneStaerken)),
            (false, "Schwächen (Weaknesses): " + N(interneSchwaechen)),
            (true,  "2. Externe Analyse: Chancen & Risiken"),
            (false, "Chancen (Opportunities): " + N(externeChancen)),
            (false, "Risiken (Threats): " + N(externeRisiken)),
            (true,  "3. Strategische Ableitung (Maßnahmenplan)"),
            (false, "Basierend auf der Gegenüberstellung der internen und externen Faktoren ergibt sich folgendes Fazit: " + N(strategischesFazit) + " Ziel ist es, die identifizierten Stärken gezielt einzusetzen, um Chancen zu nutzen, während gleichzeitig Maßnahmen zur Minimierung der Risiken und Schwächen ergriffen werden."),
        };

        string pfad = PfadFuer("SWOT_Analyse");
        bool ok = ErstellePdf(pfad, "SWOT-Analyse", bloecke, true, "(Ort, Datum)", "(Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // ZIELGRUPPENANALYSE
    // ─────────────────────────────────────────
    public static string ErstelleZielgruppenanalyse(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string kernZielgruppe = Feld(f, "kernZielgruppe");
        string demografischeMerkmale = Feld(f, "demografischeMerkmale");
        string psychografischeMerkmale = Feld(f, "psychografischeMerkmale");
        string painPoints = Feld(f, "painPoints");
        string kaufmotivation = Feld(f, "kaufmotivation");
        string kommunikationswege = Feld(f, "kommunikationswege");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "1. Definition der Primär-Zielgruppe"),
            (false, "Unsere Hauptzielgruppe lässt sich wie folgt charakterisieren: " + N(kernZielgruppe) + ". Diese Gruppe bildet das Fundament für unsere Marktaktivitäten."),
            (true,  "2. Merkmale der Zielgruppe"),
            (false, "Demografie: " + N(demografischeMerkmale)),
            (false, "Psychografie: " + N(psychografischeMerkmale)),
            (true,  "3. Bedürfnisse und Schmerzpunkte (Pain Points)"),
            (false, "Die Zielgruppe sieht sich derzeit mit folgenden Problemen konfrontiert: " + N(painPoints) + "."),
            (true,  "4. Kaufmotivation und Kundennutzen"),
            (false, "Der entscheidende Grund für die Inanspruchnahme unserer Leistungen ist: " + N(kaufmotivation) + "."),
            (true,  "5. Erreichbarkeit (Kanäle)"),
            (false, "Um die Zielgruppe effektiv anzusprechen, nutzen wir primär folgende Kanäle: " + N(kommunikationswege) + "."),
        };

        string pfad = PfadFuer("Zielgruppenanalyse");
        bool ok = ErstellePdf(pfad, "Zielgruppenanalyse", bloecke, true, "(Ort, Datum)", "(Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // FÖRDERMITTELÜBERSICHT
    // ─────────────────────────────────────────
    public static string ErstelleFoerdermittelUebersicht(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string planungszeitraum = Feld(f, "planungszeitraum");
        string zuschuss1 = Feld(f, "zuschussProgramm1");
        string zuschuss2 = Feld(f, "zuschussProgramm2");
        string kredit1 = Feld(f, "kreditProgramm1");
        string kredit2 = Feld(f, "kreditProgramm2");
        string sonstigeMittel = Feld(f, "sonstigeMittel");
        string strategischeNotizen = Feld(f, "strategischeNotizen");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<PdfBlock>
        {
            PdfBlock.Absatz("Planungszeitraum: " + N(planungszeitraum)),
            PdfBlock.Ueberschrift("1. Staatliche Zuschüsse & nicht rückzahlbare Mittel"),
            PdfBlock.Absatz("Diese Mittel belasten die Liquidität nicht durch Rückzahlungen und sind vorrangig zu prüfen."),
            PdfBlock.Tabelle(
                new[] { "Status", "Förderprogramm / Quelle", "Erwarteter Betrag", "Deadline" },
                new List<string[]>
                {
                    new[] { "[ ]", N(zuschuss1), "___________ €", "________" },
                    new[] { "[ ]", N(zuschuss2), "___________ €", "________" },
                    new[] { "[ ]", "Regionale Innovationsgutscheine", "___________ €", "________" },
                }),
            PdfBlock.Ueberschrift("2. Förderdarlehen & Kredite"),
            PdfBlock.Absatz("Zinsgünstige Darlehen zur Deckung des Investitionsbedarfs gemäß Businessplan."),
            PdfBlock.Tabelle(
                new[] { "Status", "Kreditprogramm / Institut", "Volumen", "Zins (gesch.)" },
                new List<string[]>
                {
                    new[] { "[ ]", N(kredit1), "___________ €", "________ %" },
                    new[] { "[ ]", N(kredit2), "___________ €", "________ %" },
                    new[] { "[ ]", "Mikromezzaninfonds", "___________ €", "________ %" },
                }),
            PdfBlock.Ueberschrift("3. Wettbewerbe & Alternative Finanzierung"),
            PdfBlock.Absatz("Zusätzliche Kapitalquellen durch Preisgelder oder Sponsoring."),
            PdfBlock.Tabelle(
                new[] { "Status", "Quelle / Wettbewerb", "Kapital", "Status" },
                new List<string[]>
                {
                    new[] { "[ ]", N(sonstigeMittel), "___________ €", "________" },
                    new[] { "[ ]", "Business Angel / Private Investoren", "___________ €", "________" },
                }),
            PdfBlock.Ueberschrift("4. Strategische Notizen & Nächste Schritte"),
            PdfBlock.Absatz(N(strategischeNotizen)),
        };

        string pfad = PfadFuer("Foerdermittelübersicht");
        bool ok = ErstellePdfErweitert(pfad, "Fördermittelübersicht", bloecke, true, "(Ort, Datum)", "(Unterschrift Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // DARLEHENSÜBERSICHT
    // ─────────────────────────────────────────
    public static string ErstelleDarlehensUebersicht(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string berichtsstand = Feld(f, "berichtsstand");
        string bezeichnung = Feld(f, "darlehensbezeichnung");
        string kreditgeber = Feld(f, "kreditgeber");
        string verwendungszweck = Feld(f, "verwendungszweck");
        string summe = Feld(f, "darlehenssumme");
        string zinssatz = Feld(f, "zinssatz");
        string laufzeit = Feld(f, "laufzeit");
        string rate = Feld(f, "monatlicheRate");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<PdfBlock>
        {
            PdfBlock.Absatz("Berichtsstand: " + N(berichtsstand)),
            PdfBlock.Ueberschrift("§ 1 Strategische Übersicht"),
            PdfBlock.Absatz($"Diese Übersicht dient zur Überwachung der langfristigen Finanzierungsstruktur der {firma.Name}. Sie ist die Grundlage für die monatliche Liquiditätsplanung und die Vorbereitung von Jahresabschlüssen."),
            PdfBlock.Ueberschrift("§ 2 Aktive Darlehen und Kredite"),
            PdfBlock.Tabelle(
                new[] { "Parameter", "Details zum Darlehen" },
                new List<string[]>
                {
                    new[] { "Bezeichnung", N(bezeichnung) },
                    new[] { "Kreditgeber", N(kreditgeber) },
                    new[] { "Verwendungszweck", N(verwendungszweck) },
                    new[] { "Nominalbetrag", N(summe) + " EUR" },
                    new[] { "Konditionen", N(zinssatz) + " Zinsen / " + N(laufzeit) + " Laufzeit" },
                    new[] { "Annuität", N(rate) + " EUR pro Monat" },
                }),
            PdfBlock.Ueberschrift("§ 3 Zahlungsplan & Liquiditätsrelevanz"),
            PdfBlock.Absatz("(1) Die monatlichen Belastungen aus Zins und Tilgung sind fest im Kassenbuch zu hinterlegen, um die Real-Liquidität abzubilden."),
            PdfBlock.Absatz("(2) Besondere Hinweise: Die Rückzahlung erfolgt gemäß dem vereinbarten Tilgungsplan. Etwaige Sondertilgungen sind separat zu dokumentieren."),
            PdfBlock.Ueberschrift("§ 4 Bestätigung der Vollständigkeit"),
            PdfBlock.Absatz("Hiermit wird bestätigt, dass alle zum Berichtszeitpunkt relevanten Darlehensverträge in dieser Übersicht erfasst wurden."),
        };

        string pfad = PfadFuer("Darlehensübersicht");
        bool ok = ErstellePdfErweitert(pfad, "Darlehensübersicht", bloecke, true, "(Ort, Datum)", "(Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // VERSICHERUNGSÜBERSICHT
    // ─────────────────────────────────────────
    public static string ErstelleVersicherungsUebersicht(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string berichtsstand = Feld(f, "berichtsstand");
        string typ = Feld(f, "versicherungstyp");
        string versicherer = Feld(f, "versicherer");
        string nr = Feld(f, "versicherungsnummer");
        string beitrag = Feld(f, "beitrag");
        string rhythmus = Feld(f, "zahlungsrhythmus");
        string zusatznotiz = Feld(f, "zusatznotiz");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;
        string beitragRhythmus = (string.IsNullOrEmpty(beitrag) ? "___" : beitrag) + " € / " + (string.IsNullOrEmpty(rhythmus) ? "___" : rhythmus);

        var bloecke = new List<PdfBlock>
        {
            PdfBlock.Absatz("Berichtsstand: " + N(berichtsstand)),
            PdfBlock.Ueberschrift("§ 1 Betriebliche Kernabsicherungen"),
            PdfBlock.Absatz($"Folgende Policen sind zur Sicherung des laufenden Geschäftsbetriebs der {firma.Name} aktiv:"),
            // Erste Zeile mit den erfassten Daten befüllt, zwei weitere
            // Zeilen leer für zusätzliche Policen (die Spezifikation zeigt
            // eine 3-Zeilen-Tabelle, das Feldset deckt aber nur EINE Police ab).
            PdfBlock.Tabelle(
                new[] { "Sparte", "Versicherer", "Police-Nr.", "Beitrag / Rhythmus" },
                new List<string[]>
                {
                    new[] { N(typ), N(versicherer), N(nr), beitragRhythmus },
                    new[] { "", "", "", "" },
                    new[] { "", "", "", "" },
                }),
            PdfBlock.Ueberschrift("§ 2 Notizen & Besondere Leistungen"),
            PdfBlock.Absatz("Wichtige Details: " + N(zusatznotiz)),
            PdfBlock.Absatz("Fristen: Alle Verträge sind monatlich auf Unter- oder Überversicherung zu prüfen, insbesondere bei Wachstum des Mitarbeiterstamms oder des Inventars."),
            PdfBlock.Ueberschrift("§ 3 Ergänzende Informationen"),
            PdfBlock.Absatz("(1) Im Falle eines Schadens ist der Versicherer unverzüglich zu benachrichtigen."),
            PdfBlock.Absatz("(2) Diese Übersicht dient als Kurzinformation für die Geschäftsführung und die Buchhaltung zur Liquiditätsplanung."),
            PdfBlock.Absatz($"Dieses Dokument wurde im Rahmen des Systems Ventoriq erstellt. Es unterstützt den Gründer bei der kaufmännischen Sorgfaltspflicht und der Risikokontrolle."),
        };

        string pfad = PfadFuer("Versicherungsübersicht");
        bool ok = ErstellePdfErweitert(pfad, "Versicherungsübersicht", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // KUNDENZUFRIEDENHEITSUMFRAGE
    // ─────────────────────────────────────────
    public static string ErstelleKundenzufriedenheitsumfrage(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string befragungszeitraum = Feld(f, "befragungszeitraum");
        string einleitungstext = Feld(f, "einleitungstext");
        string leistungsfokus = Feld(f, "leistungsfokus");
        string frage1 = Feld(f, "frage1");
        string frage2 = Feld(f, "frage2");
        string frage3 = Feld(f, "frage3");
        string schlusswort = Feld(f, "schlusswort");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<PdfBlock>();
        if (!string.IsNullOrWhiteSpace(befragungszeitraum)) bloecke.Add(PdfBlock.Absatz(befragungszeitraum));

        bloecke.AddRange(new List<PdfBlock>
        {
            PdfBlock.Absatz("Sehr geehrte Kundin, sehr geehrter Kunde, " + N(einleitungstext)),
            PdfBlock.Absatz("Bewertungsskala (Likert 5-Punkt): 1 = Trifft überhaupt nicht zu | 2 = Trifft eher nicht zu | 3 = Neutral | 4 = Trifft eher zu | 5 = Trifft voll zu."),
            PdfBlock.Ueberschrift("Ihre Bewertung für: " + N(leistungsfokus)),
            PdfBlock.Tabelle(
                new[] { "Qualitätsmerkmal", "1", "2", "3", "4", "5" },
                new List<string[]>
                {
                    new[] { N(frage1), "[ ]", "[ ]", "[ ]", "[ ]", "[ ]" },
                    new[] { N(frage2), "[ ]", "[ ]", "[ ]", "[ ]", "[ ]" },
                    new[] { N(frage3), "[ ]", "[ ]", "[ ]", "[ ]", "[ ]" },
                }),
            PdfBlock.Ueberschrift("Was können wir in Zukunft noch besser machen?"),
            PdfBlock.Absatz("________________________________________________________________\n________________________________________________________________\n________________________________________________________________"),
            PdfBlock.Absatz("Vielen Dank für Ihre Zeit! " + N(schlusswort)),
            PdfBlock.Absatz("Dieses Dokument wurde im Rahmen des Systems Ventoriq erstellt und dient der kontinuierlichen Qualitätssicherung gemäß ISO-nahen Standards."),
        });

        string pfad = PfadFuer("Kundenzufriedenheitsumfrage");
        bool ok = ErstellePdfErweitert(pfad, "Kundenzufriedenheitsumfrage", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // VOLLMACHTVORLAGE
    // ─────────────────────────────────────────
    public static string ErstelleVollmachtvorlage(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string betreff = Feld(f, "betreff");

        var bloecke = new List<(bool, string)>();
        if (!string.IsNullOrWhiteSpace(betreff)) bloecke.Add((true, betreff));

        bloecke.Add((true, "1. Vollmachtgeber"));
        bloecke.Add((false, $"Die {firma.Name} bevollmächtigt hiermit die unten genannte Person zur Vertretung des Unternehmens im Rahmen der nachfolgend definierten Befugnisse."));
        bloecke.Add((true, "2. Bevollmächtigte Person (Bitte in Druckbuchstaben ausfüllen)"));
        bloecke.Add((false, "Name, Vorname: __________________________________________________________"));
        bloecke.Add((false, "Anschrift: ________________________________________________________________"));
        bloecke.Add((false, "Identifikation (z. B. Ausweis-Nr.): ___________________________________________"));
        bloecke.Add((true, "3. Umfang und Zweck der Vollmacht"));
        bloecke.Add((false, "Die oben genannte Person ist berechtigt, folgende Handlungen vorzunehmen:"));
        bloecke.Add((false, "________________________________________________________________"));
        bloecke.Add((false, "________________________________________________________________"));
        bloecke.Add((true, "4. Geltungsdauer"));
        bloecke.Add((false, "Diese Vollmacht ist gültig bis: _______________________________________________"));
        bloecke.Add((true, "5. Rechtliche Hinweise"));
        bloecke.Add((false, "Diese Vollmacht erlischt durch Widerruf oder mit Erreichung des oben genannten Zwecks. Der Bevollmächtigte ist verpflichtet, das Dokument nach Beendigung der Vertretung an den Vollmachtgeber zurückzugeben."));

        string pfad = PfadFuer("Vollmachtvorlage");

        // Sonderfall: zwei Unterschriftenzeilen (Geschäftsführung +
        // Bevollmächtigter), wie beim Arbeitsvertrag.
        bool ok = ErstellePdfMitDoppelSignatur(pfad, "Vollmacht", bloecke,
            "(Ort, Datum)", "(Unterschrift Geschäftsführung)",
            "(Ort, Datum)", "(Unterschrift Bevollmächtigter)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // GESELLSCHAFTERLISTE
    // ─────────────────────────────────────────
    public static string ErstelleGesellschafterliste(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string erstellungsdatum = Feld(f, "erstellungsdatum");
        string gesellschafter1 = Feld(f, "gesellschafter1");
        string anteil1 = Feld(f, "anteil1");
        string gesellschafter2 = Feld(f, "gesellschafter2");
        string anteil2 = Feld(f, "anteil2");
        string stammkapitalGesamt = Feld(f, "stammkapitalGesamt");
        string zusatzangaben = Feld(f, "zusatzangaben");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<PdfBlock>
        {
            PdfBlock.Absatz("Unternehmensbezeichnung: " + firma.Name + (string.IsNullOrEmpty(firma.Rechtsform) ? "" : " " + firma.Rechtsform)),
            PdfBlock.Absatz("Stichtag: " + N(erstellungsdatum)),
            PdfBlock.Ueberschrift("§ 1 Beteiligungsverhältnisse"),
            PdfBlock.Absatz("Die Anteile am Stammkapital der Gesellschaft sind wie folgt verteilt:"),
            PdfBlock.Tabelle(
                new[] { "Gesellschafter (Name, Anschrift, Geburtsdatum)", "Nennbetrag des Geschäftsanteils" },
                new List<string[]>
                {
                    new[] { N(gesellschafter1), N(anteil1) + " EUR" },
                    new[] { N(gesellschafter2), N(anteil2) + " EUR" },
                    new[] { "GESAMTSUMME STAMMKAPITAL", N(stammkapitalGesamt) + " EUR" },
                }),
        };

        if (!string.IsNullOrWhiteSpace(zusatzangaben))
        {
            bloecke.Add(PdfBlock.Ueberschrift("§ 2 Besondere Bestimmungen & Nummern"));
            bloecke.Add(PdfBlock.Absatz(zusatzangaben));
        }

        bloecke.Add(PdfBlock.Ueberschrift("§ 3 Versicherung der Geschäftsführung"));
        bloecke.Add(PdfBlock.Absatz("Die Geschäftsführung versichert, dass die vorstehende Liste der Wahrheit entspricht und den aktuellen Stand der Beteiligungsverhältnisse zum oben genannten Stichtag wiedergibt. Jede Veränderung in den Personen der Gesellschafter oder des Umfangs ihrer Beteiligung ist unverzüglich beim Handelsregister einzureichen."));

        string pfad = PfadFuer("Gesellschafterliste");
        bool ok = ErstellePdfErweitert(pfad, "Gesellschafterliste", bloecke, true, "(Ort, Datum)", "(Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // GUTSCHRIFTVORLAGE
    // ─────────────────────────────────────────
    public static string ErstelleGutschriftvorlage(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string tabellenzeilen = Feld(f, "tabellenzeilen");
        string zusatzhinweis = Feld(f, "zusatzhinweis");
        int anzahlZeilen = LeseZeilenAnzahl(tabellenzeilen, 3, 20);

        var positionsZeilen = new List<string[]>();
        for (int i = 1; i <= anzahlZeilen; i++)
            positionsZeilen.Add(new[] { i.ToString(), "", "€" });

        var bloecke = new List<PdfBlock>
        {
            PdfBlock.Absatz("Empfänger (Bitte in Druckbuchstaben ausfüllen):"),
            PdfBlock.Absatz("Name / Firma: ___________________________________________________________"),
            PdfBlock.Absatz("Anschrift: _______________________________________________________________"),
            PdfBlock.Absatz("Gutschrift-Nr.: ____________________     Datum: ____________________"),
            PdfBlock.Absatz("Bezug auf Rechnung: ____________________     Kunden-Nr.: ____________________"),
            PdfBlock.Ueberschrift("§ 1 Korrekturpositionen"),
            PdfBlock.Absatz("Hiermit schreiben wir Ihnen für die folgenden Positionen den genannten Betrag gut:"),
            PdfBlock.Tabelle(
                new[] { "Pos.", "Beschreibung der Leistung / Ware", "Betrag (Netto)" },
                positionsZeilen),
            PdfBlock.Absatz("Gutschrift-Summe (Gesamt): ____________ €"),
            PdfBlock.Ueberschrift("§ 2 Grund der Gutschrift / Notizen"),
            PdfBlock.Absatz("[ ] Retoure   [ ] Preisnachlass   [ ] Falschlieferung   [ ] Sonstiges: " +
                (string.IsNullOrWhiteSpace(zusatzhinweis) ? "" : zusatzhinweis)),
            PdfBlock.Ueberschrift("§ 3 Steuerlicher Hinweis"),
            PdfBlock.Absatz("Der oben genannte Betrag wird mit bestehenden Forderungen verrechnet oder auf das uns bekannte Konto erstattet. Die Umsatzsteuer ist entsprechend der ursprünglichen Rechnung zu korrigieren."),
        };

        string pfad = PfadFuer("Gutschriftvorlage");
        bool ok = ErstellePdfErweitert(pfad, "Gutschrift", bloecke, true, "(Ort, Datum)", "(Unterschrift Geschäftsführung)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // INVENTARLISTE
    // ─────────────────────────────────────────
    public static string ErstelleInventarliste(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string inventurBereich = Feld(f, "inventurBereich");
        string tabellenzeilen = Feld(f, "tabellenzeilen");
        int anzahlZeilen = LeseZeilenAnzahl(tabellenzeilen, 10, 40);

        var zeilen = new List<string[]>();
        for (int i = 1; i <= anzahlZeilen; i++)
            zeilen.Add(new[] { i.ToString(), "", "", "________ €", "" });

        var bloecke = new List<PdfBlock>
        {
            PdfBlock.Absatz("Fokus / Abteilung: " + (string.IsNullOrEmpty(inventurBereich) ? "Gesamtes Unternehmen" : inventurBereich)),
            PdfBlock.Ueberschrift("§ 1 Erfassung der Wirtschaftsgüter"),
            PdfBlock.Absatz("Bitte tragen Sie alle im Unternehmen befindlichen Gegenstände leserlich in die Tabelle ein."),
            PdfBlock.Tabelle(
                new[] { "Pos.", "Gegenstand (Bezeichnung / Modell)", "Seriennr. / Interne ID", "Wert (Netto)", "Standort / Nutzer" },
                zeilen),
            PdfBlock.Ueberschrift("§ 2 Besondere Anmerkungen / Zustand"),
            PdfBlock.Absatz("(Hinweise zu Defekten, Leasing-Verträgen oder geplanten Neuanschaffungen)"),
            PdfBlock.Absatz("________________________________________________________________"),
            PdfBlock.Ueberschrift("§ 3 Bestätigung der Vollständigkeit"),
            PdfBlock.Absatz("Der Unterzeichnende bestätigt die Richtigkeit der oben aufgeführten Angaben zum Zeitpunkt der Aufnahme. Diese Liste dient als Grundlage für die buchhalterische Erfassung und die Versicherungswertermittlung."),
        };

        string pfad = PfadFuer("Inventarliste");
        bool ok = ErstellePdfErweitert(pfad, "Inventarliste", bloecke, true, "(Ort, Datum)", "(Unterschrift)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // BESPRECHUNGSPROTOKOLL
    // ─────────────────────────────────────────
    public static string ErstelleBesprechungsprotokoll(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        string themenFokus = Feld(f, "themenFokus");
        string anzahlAufgabenzeilen = Feld(f, "anzahlAufgabenzeilen");
        string zusatzhinweis = Feld(f, "zusatzhinweis");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;
        int anzahlZeilen = LeseZeilenAnzahl(anzahlAufgabenzeilen, 10, 25);

        var aufgabenZeilen = new List<string[]>();
        for (int i = 1; i <= anzahlZeilen; i++)
            aufgabenZeilen.Add(new[] { "", "", "", "" });

        // Mehrzeilige Freitext-Linien für Notizen (statt nur einer Zeile) -
        // Abschnitte 3+4 brauchten deutlich mehr Platz zum Ausfüllen.
        string LeereZeilen(int anzahl)
        {
            var zeilen = new List<string>();
            for (int i = 0; i < anzahl; i++)
                zeilen.Add("________________________________________________________________");
            return string.Join("\n", zeilen);
        }

        var bloecke = new List<PdfBlock>
        {
            PdfBlock.Ueberschrift("1. Rahmendaten"),
            PdfBlock.Absatz("Projekt / Bereich: " + N(themenFokus)),
            PdfBlock.Absatz("Datum: ____________________     Zeitraum: von __________ bis __________"),
            PdfBlock.Absatz("Ort / Medium: ____________________ (z. B. Discord, Präsenz)"),
            PdfBlock.Ueberschrift("2. Teilnehmer & Anwesenheit"),
            PdfBlock.Absatz("Anwesend: ________________________________________________"),
            PdfBlock.Absatz("Abwesend: ________________________________________________"),
            PdfBlock.Ueberschrift("3. Tagesordnung & Kerninhalte (Agenda)"),
            PdfBlock.Absatz(LeereZeilen(5)),
            PdfBlock.Ueberschrift("4. Besprochene Inhalte & Entscheidungen"),
            PdfBlock.Absatz(LeereZeilen(6)),
            PdfBlock.Ueberschrift("5. Aufgabenverteilung & Deadlines"),
            PdfBlock.Tabelle(
                new[] { "Nr.", "Aufgabe / Tätigkeit", "Verantwortlich", "Deadline" },
                aufgabenZeilen),
            PdfBlock.Ueberschrift("6. Nächste Schritte / Nächstes Meeting"),
            PdfBlock.Absatz("Datum: ____________________     Uhrzeit: ____________________"),
            PdfBlock.Absatz("Themen: ________________________________________________"),
        };

        if (!string.IsNullOrWhiteSpace(zusatzhinweis)) bloecke.Add(PdfBlock.Absatz("Hinweis: " + zusatzhinweis));

        string pfad = PfadFuer("Besprechungsprotokoll");
        bool ok = ErstellePdfErweitert(pfad, "Besprechungsprotokoll", bloecke, true, "(Ort, Datum)", "(Protokollführung / PL)");
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // KONTODATEN (IBAN/BIC)
    // ─────────────────────────────────────────
    public static string ErstelleKontodaten(DocumentDashboard.DocumentData doc)
    {
        var f = doc.strukturFelder;
        var firma = HoleFirmendaten();
        string iban = Feld(f, "iban");
        string bic = Feld(f, "bic");
        string bank = Feld(f, "bank");
        string kontoinhaber = Feld(f, "kontoinhaber");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (false, $"Übersicht der hinterlegten Geschäftskonto-Daten von {firma.Name}."),
            (true,  "Bankverbindung"),
            (false, "Kontoinhaber: " + (string.IsNullOrEmpty(kontoinhaber) ? firma.Name : kontoinhaber)),
            (false, "Bank: " + N(bank)),
            (false, "IBAN: " + N(iban)),
            (false, "BIC: " + N(bic)),
        };

        string pfad = PfadFuer("Kontodaten");
        bool ok = ErstellePdf(pfad, "Kontodaten (IBAN/BIC)", bloecke, false);
        return ok ? pfad : null;
    }

    // ─────────────────────────────────────────
    // ZENTRALER EINSTIEGSPUNKT
    // Wird vom Export-Screen aufgerufen - liefert null zurück, wenn für
    // diesen Dokument-Titel noch kein spezieller PDF-Generator existiert
    // (dann greift dort weiterhin der alte, generische Fallback).
    // ─────────────────────────────────────────
    public static string ErstellePdfFuerDokument(DocumentDashboard.DocumentData doc)
    {
        if (doc == null) return null;

        switch (doc.title)
        {
            case "Unternehmensstammdaten": return ErstelleUnternehmensstammdaten(doc);
            case "Eröffnungsbilanz":       return ErstelleEroeffnungsbilanz(doc);
            case "Steuernummer-Bescheid / USt-IdNr": return ErstelleSteuernummerBescheid(doc);
            case "Impressum":              return ErstelleImpressum(doc);
            case "Dienstleistungskatalog / Preisliste": return ErstelleDienstleistungskatalog(doc);
            case "Gründungs-Checkliste":   return ErstelleGruendungsCheckliste(doc);
            case "Kontodaten (IBAN/BIC)":  return ErstelleKontodaten(doc);
            case "Gründungsurkunde":       return ErstelleGruendungsurkunde(doc);
            case "Handelsregisterauszug":  return ErstelleHandelsregisterauszug(doc);
            case "Gesellschaftsvertrag":   return ErstelleGesellschaftsvertrag(doc);
            case "Fragebogen zur Steuerlichen Erfassung": return ErstelleFragebogenSteuererfassung(doc);
            case "Gewerbeanmeldung":       return ErstelleGewerbeanmeldung(doc);
            case "Anmeldung Berufsgenossenschaft": return ErstelleBerufsgenossenschaft(doc);
            case "Organigramm":            return ErstelleOrganigramm(doc);
            case "Zahlungsbedingungen":    return ErstelleZahlungsbedingungen(doc);
            case "AGB":                    return ErstelleAgb(doc);
            case "Disclaimer":             return ErstelleDisclaimer(doc);
            case "Barzahlung":             return ErstelleBarzahlung(doc);
            case "Überweisung":            return ErstelleUeberweisung(doc);
            case "SEPA-Basislastschrift-Mandat": return ErstelleSepaMandat(doc);
            case "Widerrufsbelehrung":     return ErstelleWiderrufsbelehrung(doc);
            case "Mahnverfahren":          return ErstelleMahnverfahren(doc);
            case "Ratenzahlungsbestimmungen": return ErstelleRatenzahlungsbestimmungen(doc);
            case "Copyright Hinweis":      return ErstelleCopyrightHinweis(doc);
            case "Lizenzhinweis Einfach":  return ErstelleLizenzhinweisEinfach(doc);
            case "Lizenzhinweis Erweitert": return ErstelleLizenzhinweisErweitert(doc);
            case "Datenschutzerklärung (DSGVO)": return ErstelleDatenschutzerklaerung(doc);
            case "Vertraulichkeitserklärung": return ErstelleVertraulichkeitserklaerung(doc);
            case "Muster-Arbeitsvertrag":   return ErstelleArbeitsvertrag(doc);
            case "Vorlage Kündigung":       return ErstelleKuendigung(doc);
            case "Stellenbeschreibung":     return ErstelleStellenbeschreibung(doc);
            case "Urlaubsantrag":           return ErstelleUrlaubsantrag(doc);
            case "Corporate Identity Manual": return ErstelleCorporateIdentityManual(doc);
            case "Unternehmensrichtlinien": return ErstelleUnternehmensrichtlinien(doc);
            case "Social Media Strategie":  return ErstelleSocialMediaStrategie(doc);
            case "Businessplan":            return ErstelleBusinessplan(doc);
            case "Markt- & Wettbewerbsanalyse": return ErstelleMarktWettbewerbsanalyse(doc);
            case "SWOT-Analyse":            return ErstelleSwotAnalyse(doc);
            case "Zielgruppenanalyse":      return ErstelleZielgruppenanalyse(doc);
            case "Fördermittelübersicht":   return ErstelleFoerdermittelUebersicht(doc);
            case "Darlehensübersicht":      return ErstelleDarlehensUebersicht(doc);
            case "Versicherungsübersicht":  return ErstelleVersicherungsUebersicht(doc);
            case "Kundenzufriedenheitsumfrage": return ErstelleKundenzufriedenheitsumfrage(doc);
            case "Vollmachtvorlage":        return ErstelleVollmachtvorlage(doc);
            case "Gesellschafterliste":     return ErstelleGesellschafterliste(doc);
            case "Gutschriftvorlage":       return ErstelleGutschriftvorlage(doc);
            case "Inventarliste":           return ErstelleInventarliste(doc);
            case "Besprechungsprotokoll":   return ErstelleBesprechungsprotokoll(doc);
            default: return null;
        }
    }
}
