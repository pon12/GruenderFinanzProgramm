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
    // ─────────────────────────────────────────
    // HILFSFUNKTIONEN
    // ─────────────────────────────────────────

    // Holt den Wert eines Struktur-Felds anhand des Keys, oder "" falls nicht gesetzt.
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
            string zweck = Feld(stammdaten?.strukturFelder, "zweck");
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
                    var p = new Paragraph(text, istUeberschrift ? ueberschriftFont : textFont);
                    p.SpacingAfter = istUeberschrift ? 6f : 10f;
                    doc.Add(p);
                }

                if (mitUnterschrift)
                {
                    doc.Add(new Paragraph(" "));
                    doc.Add(new Paragraph(" "));

                    PdfPTable unterschriftTable = new PdfPTable(2);
                    unterschriftTable.WidthPercentage = 100f;
                    unterschriftTable.AddCell(new PdfPCell(new Phrase("__________________________\n" + unterschriftLinks, textFont)) { Border = Rectangle.NO_BORDER });
                    unterschriftTable.AddCell(new PdfPCell(new Phrase("__________________________\n" + unterschriftRechts, textFont)) { Border = Rectangle.NO_BORDER });
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

        string pfad = PfadFuer("Zahlungsbedingungen");
        bool ok = ErstellePdf(pfad, "Zahlungsbedingungen", bloecke, true, "(Ort, Datum)", "(Unterschrift Geschäftsführung)");
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
        string externeVerweise = Feld(f, "externeVerweise");
        string urheberrechtshinweis = Feld(f, "urheberrechtshinweis");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

        var bloecke = new List<(bool, string)>
        {
            (true,  "§ 1 Haftung für Inhalte"),
            (false, $"Die Inhalte der Software/Dienste von {firma.Name} im Bereich " + N(geltungsbereich) + " wurden mit größter Sorgfalt erstellt. Für die Richtigkeit, Vollständigkeit und Aktualität der Inhalte können wir jedoch keine Gewähr übernehmen. Als Diensteanbieter sind wir gemäß § 7 Abs.1 TMG für eigene Inhalte auf diesen Seiten nach den allgemeinen Gesetzen verantwortlich. Wir behalten uns vor, Inhalte ohne gesonderte Ankündigung zu verändern oder zu löschen."),
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

        var bloecke = new List<(bool, string)>
        {
            (true,  "Barzahlung & Quittung"),
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
            (false, "Bruttobetrag (in Zahlen): € ____________________"),
            (false, "Betrag in Worten: ___________________________________________"),
            (false, "Verwendungszweck / Leistung: ________________________________"),
            (false, "Steuerliche Aufschlüsselung: [ ] 19% MwSt. | [ ] 7% MwSt. | [ ] ____ % MwSt."),
            (false, "MwSt.-Betrag: ________________ EUR      Nettobetrag: ________________ EUR"),
            (true,  "Rechtlicher Hinweis"),
            (false, "Dieser Vordruck wurde maschinell mit dem System Ventoriq erstellt. Er dient als offizieller Nachweis über den Erhalt eines Barbetrags."),
        };

        string pfad = PfadFuer("Barzahlung");
        bool ok = ErstellePdf(pfad, "Barzahlung", bloecke, true, "Ort, Datum", "(Handzeichen / Stempel des Empfängers)");
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
            (false, "Name: _______________"),
            (false, "Anschrift: _______________"),
            (false, "IBAN: _______________"),
            (false, "BIC: _______________"),
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
        string zusatzhinweis = Feld(f, "zusatzhinweis");
        string N(string wert) => string.IsNullOrEmpty(wert) ? "[nicht angegeben]" : wert;

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
            (false, "Sollte ein Zahlungseingang ausbleiben, folgen wir in der Regel diesem Prozess:"),
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
            default: return null;
        }
    }
}
