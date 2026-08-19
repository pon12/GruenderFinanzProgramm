// DocumentDashboard.cs – Dokumente-Pool
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class DocumentDashboard : MonoBehaviour
{
    [Header("UI Document Asset")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Templates")]
    [SerializeField] private VisualTreeAsset categoryCardTemplate;
    [SerializeField] private Texture2D helpIconTexture;

    // UI-Elemente Hauptbildschirm
    private VisualElement root;
    private Button deleteButton;
    private VisualElement gridContainer;

    // Erstell-Popup (nur für flexible Kategorien)
    private VisualElement popupOverlay;
    private Button popupCancelButton;
    private Button popupSubmitButton;
    private DropdownField categoryDropdown;
    private TextField docNameInput;
    private Button btnTypeStandard;
    private Button btnTypeDiagramm;
    private Button btnTypeChecklist;

    // Kategorie-Listen-Popup
    private VisualElement detailPopupOverlay;
    private VisualElement detailListContainer;
    private VisualElement globalListContainer;
    private Button detailCloseButton;
    private Label detailPopupTitle;
    private Button listCreateNewButton;

    // Bearbeiten-Popup (flexible Dokumente: Titel + Freitext)
    private VisualElement editPopupOverlay;
    private TextField editDocNameInput;
    private TextField editInhaltInput;
    private TextField editStandardTitelInput, editStandardDatumInput;
    private VisualElement feldGruppeInhalt, feldGruppeCheckliste, feldGruppeDiagramm;
    private VisualElement editChecklisteBox, editDiagrammDatenBox, editDiagrammVorschau;
    private Button btnChecklisteHinzufuegen, btnDiagrammHinzufuegen;
    private EinfachesBalkendiagrammElement diagrammVorschauElement;
    private List<ChecklistPunkt> aktiveChecklistPunkte = new List<ChecklistPunkt>();
    private List<DiagrammPunkt> aktiveDiagrammPunkte = new List<DiagrammPunkt>();
    private Button editPopupSubmitButton;
    private Button editPopupCancelButton;
    private Button btnEditTypeStandard;
    private Button btnEditTypeDiagramm;
    private Button btnEditTypeChecklist;
    private Label editLockedHint;
    private VisualElement editTemplateGroup;
    private VisualElement editStrukturFelderBox;

    // Lösch-Bestätigungs-Popup
    private VisualElement deleteConfirmOverlay;
    private Button deleteConfirmYesButton;
    private Button deleteConfirmCancelButton;
    private Label deleteConfirmHint;

    // Systemzustände
    private string selectedType = "Standard";
    private string selectedEditType = "Standard";
    private string activeCategoryForList = "";
    private DocumentData activeDocForEditing;
    private List<VisualElement> aktiveStrukturFelder = new List<VisualElement>();

    // ============================================================
    // FELD-DEFINITION – ein einzelnes strukturiertes Eingabefeld
    // ============================================================
    // Text = normales Eingabefeld, Dropdown = feste Auswahl, JaNein = Checkbox.
    private enum FeldTyp { Text, Dropdown, JaNein }

    private class FeldDefinition
    {
        public string key;         // interner Schlüssel, z.B. "iban"
        public string label;       // Anzeigename, z.B. "IBAN"
        public string placeholder; // Platzhaltertext im Feld
        public FeldTyp typ = FeldTyp.Text;
        public string[] optionen;  // nur bei typ = Dropdown
    }

    // ============================================================
    // KATEGORIEN-DEFINITION
    //
    // istFest = true -> nicht löschbar, nicht verschiebbar.
    //                    Werte bleiben editierbar.
    // pflichtDocs    -> Pflichtdokumente dieser Kategorie, jeweils
    //                    mit eigener Feldstruktur (felderProDoc).
    // ============================================================
    private class KategorieDefinition
    {
        public string name;
        public bool istFest;
        public List<string> pflichtDocs;
    }

    private readonly List<KategorieDefinition> kategorien = new List<KategorieDefinition>
    {
        new KategorieDefinition { name = "Gründung",             istFest = true,  pflichtDocs = new List<string> { "Unternehmensstammdaten", "Gründungsurkunde", "Gesellschaftsvertrag", "Handelsregisterauszug", "Fragebogen zur Steuerlichen Erfassung", "Gewerbeanmeldung", "Anmeldung Berufsgenossenschaft", "Organigramm", "Gesellschafterliste" } },
        new KategorieDefinition { name = "Bezahlweise",            istFest = true,  pflichtDocs = new List<string> { "Kontodaten (IBAN/BIC)", "Zahlungsbedingungen", "AGB", "Disclaimer", "Barzahlung", "Überweisung", "SEPA-Basislastschrift-Mandat", "Widerrufsbelehrung", "Mahnverfahren", "Ratenzahlungsbestimmungen" } },
        new KategorieDefinition { name = "Finanzen",               istFest = false, pflichtDocs = new List<string> { "Eröffnungsbilanz" } },
        new KategorieDefinition { name = "Recht & Steuern",        istFest = false, pflichtDocs = new List<string> { "Datenschutzerklärung (DSGVO)", "Steuernummer-Bescheid / USt-IdNr", "Impressum", "Copyright Hinweis", "Lizenzhinweis Einfach", "Lizenzhinweis Erweitert", "Vertraulichkeitserklärung" } },
        new KategorieDefinition { name = "Marketing & Personal",   istFest = false, pflichtDocs = new List<string> { "Dienstleistungskatalog / Preisliste", "Corporate Identity Manual", "Muster-Arbeitsvertrag", "Vorlage Kündigung", "Stellenbeschreibung", "Urlaubsantrag", "Unternehmensrichtlinien", "Social Media Strategie" } },
        new KategorieDefinition { name = "Strategie & Planung",    istFest = true,  pflichtDocs = new List<string> { "Businessplan", "Markt- & Wettbewerbsanalyse", "SWOT-Analyse", "Zielgruppenanalyse" } },
        new KategorieDefinition { name = "Vorlagen & Checklisten", istFest = false, pflichtDocs = new List<string> { "Gründungs-Checkliste", "Inventarliste", "Inventur", "Fördermittelübersicht", "Darlehensübersicht", "Versicherungsübersicht", "Kundenzufriedenheitsumfrage", "Vollmachtvorlage", "Gutschriftvorlage" } },
        new KategorieDefinition { name = "Sonstiges",              istFest = false, pflichtDocs = new List<string> { "Besprechungsprotokoll" } },
    };

    // ============================================================
    // FELD-DEFINITIONEN PRO PFLICHTDOKUMENT
    //
    // Key muss exakt dem Dokumenttitel in pflichtDocs entsprechen.
    // ============================================================
    private readonly Dictionary<string, List<FeldDefinition>> felderProPflichtDoc =
        new Dictionary<string, List<FeldDefinition>>
        {
            ["Unternehmensstammdaten"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "firma",     label = "Firmenname", placeholder = "z.B. Mustermann GmbH" },
            new FeldDefinition { key = "rechtsform", label = "Rechtsform", placeholder = "z.B. GmbH" },
            new FeldDefinition { key = "branche",    label = "Branche",   placeholder = "z.B. IT & Software" },
            new FeldDefinition { key = "standort",   label = "Standort",  placeholder = "z.B. Berlin" },
        },
            ["Gründungsurkunde"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "gruendernamen",    label = "Gründernamen",    placeholder = "Vollständige Namen aller Beteiligten eintragen" },
            new FeldDefinition { key = "gruendungsdatum",  label = "Gründungsdatum",  placeholder = "Tag des offiziellen Starts auswählen (TT.MM.JJJJ)" },
            new FeldDefinition { key = "gruendungsvision", label = "Gründungsvision", placeholder = "Hauptziel deines Startups in 1-2 Sätzen" },
            new FeldDefinition { key = "zusatztext",       label = "Zusatztext",      placeholder = "Optionaler Satz über den Unterschriften" },
        },
            ["Handelsregisterauszug"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "registergericht",   label = "Registergericht",     placeholder = "Zuständiges Amtsgericht (z.B. Chemnitz)" },
            new FeldDefinition { key = "registernummer",    label = "Registernummer",      placeholder = "Offizielle Nummer (z.B. HRB 12345)" },
            new FeldDefinition { key = "tagDerEintragung",  label = "Tag der Eintragung",  placeholder = "Datum der offiziellen Registrierung (TT.MM.JJJJ)" },
            new FeldDefinition { key = "stammkapital",      label = "Stammkapital",        placeholder = "Höhe des gezeichneten Kapitals in Euro" },
            new FeldDefinition { key = "geschaeftsfuehrung",label = "Geschäftsführung",    placeholder = "Namen der vertretungsberechtigten Personen" },
        },
            ["Gesellschaftsvertrag"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "gesellschafter",        label = "Gesellschafter",        placeholder = "Namen und Anschriften aller Partner angeben" },
            new FeldDefinition { key = "stammkapital",          label = "Stammkapital",           placeholder = "Gesamtsumme der Einlagen in Euro" },
            new FeldDefinition { key = "gewinnverteilung",      label = "Gewinnverteilung",       placeholder = "Regelung zur Aufteilung (z.B. nach Anteilen)" },
            new FeldDefinition { key = "geschaeftsfuehrung",    label = "Geschäftsführung",       placeholder = "Namen der zur Leitung befugten Personen" },
            new FeldDefinition { key = "schlussbestimmungen",   label = "Schlussbestimmungen",    placeholder = "Optionale Klauseln oder Sonderregelungen" },
        },
            ["Fragebogen zur Steuerlichen Erfassung"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "finanzamt",          label = "Zuständiges Finanzamt",        placeholder = "Name deines Finanzamts am Unternehmenssitz" },
            new FeldDefinition { key = "beginnTaetigkeit",   label = "Beginn der Tätigkeit",         placeholder = "Datum der ersten Betriebseinnahme oder -ausgabe (TT.MM.JJJJ)" },
            new FeldDefinition { key = "umsatzJahr1",        label = "Geschätzter Umsatz (Jahr 1)",  placeholder = "Dein voraussichtlicher Bruttoumsatz für das erste Jahr" },
            new FeldDefinition { key = "gewinnJahr1",        label = "Geschätzter Gewinn (Jahr 1)",  placeholder = "Dein voraussichtlicher Reingewinn nach Abzug aller Kosten" },
            new FeldDefinition { key = "kleinunternehmer",   label = "Kleinunternehmer-Regelung (Umsatz < 22.000 € im ersten Jahr)", typ = FeldTyp.JaNein },
        },
            ["Gewerbeanmeldung"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "behoerde",         label = "Zuständige Behörde",   placeholder = "Name des örtlichen Gewerbeamts (z.B. Stadt Mittweida)" },
            new FeldDefinition { key = "beginnTaetigkeit", label = "Beginn der Tätigkeit", placeholder = "Datum des tatsächlichen Starts (auch rückwirkend möglich)" },
            new FeldDefinition { key = "anzahlMitarbeiter",label = "Zahl der Mitarbeiter", placeholder = "Anzahl der Angestellten zum Start (ohne Inhaber)" },
            new FeldDefinition { key = "nebenerwerb",      label = "Nebenerwerb (hauptberuflich noch angestellt)", typ = FeldTyp.JaNein },
            new FeldDefinition { key = "anmeldungsgrund",  label = "Anmeldungsgrund",      placeholder = "z.B. Neugründung oder Übernahme" },
        },
            ["Anmeldung Berufsgenossenschaft"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "zustaendigeBg",   label = "Zuständige BG",         placeholder = "Name der Berufsgenossenschaft (z.B. VBG oder BG ETEM)" },
            new FeldDefinition { key = "tagDerEroeffnung",label = "Tag der Eröffnung",     placeholder = "Datum, an dem der Betrieb tatsächlich startete" },
            new FeldDefinition { key = "anzahlVersicherte",label = "Zahl der Versicherten", placeholder = "Anzahl der Gründer und Mitarbeiter im Unternehmen" },
            new FeldDefinition { key = "artTaetigkeit",   label = "Art der Tätigkeit",     placeholder = "Detaillierte Beschreibung der ausgeübten Arbeiten" },
            new FeldDefinition { key = "lohnsumme",       label = "Lohnsumme (geschätzt)", placeholder = "Voraussichtliche Entgelte im ersten Jahr (optional)" },
        },
            ["Organigramm"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "ceo",          label = "Geschäftsführung (CEO)",         placeholder = "Name der Person für Vision und Strategie" },
            new FeldDefinition { key = "po",           label = "Produktmanagement (PO)",         placeholder = "Name des Verantwortlichen für die Roadmap" },
            new FeldDefinition { key = "cto",          label = "Technische Leitung (CTO)",       placeholder = "Name des Leiters für IT und Entwicklung" },
            new FeldDefinition { key = "marketing",    label = "Marketing & Vertrieb",           placeholder = "Name der Person für Kunden und Außenwirkung" },
            new FeldDefinition { key = "cfo",          label = "Finanzen (CFO)",                 placeholder = "Name des Verantwortlichen für Buchhaltung" },
            new FeldDefinition { key = "creative",     label = "Design (Creative Director)",     placeholder = "Name der Person für UI/UX und Markenidentität" },
            new FeldDefinition { key = "pm",           label = "Projektorganisation (PM)",       placeholder = "Name der Person für Zeitplan und Dokumentation" },
            new FeldDefinition { key = "qa",           label = "Qualitätssicherung (QA)",        placeholder = "Name des Verantwortlichen für Tests und Abnahme" },
            new FeldDefinition { key = "mitarbeiter",  label = "Mitarbeiter",                    placeholder = "Liste an normalen Mitarbeitern, Komma getrennt" },
            new FeldDefinition { key = "azubi",        label = "Azubi",                          placeholder = "Liste an Azubis, Komma getrennt" },
            new FeldDefinition { key = "praktikant",   label = "Praktikant",                     placeholder = "Liste an Praktikanten, Komma getrennt" },
        },
            ["Kontodaten (IBAN/BIC)"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "iban",         label = "IBAN",         placeholder = "DE00 0000 0000 0000 0000 00" },
            new FeldDefinition { key = "bic",          label = "BIC",          placeholder = "z.B. COBADEFFXXX" },
            new FeldDefinition { key = "bank",         label = "Bank",         placeholder = "z.B. Commerzbank" },
            new FeldDefinition { key = "kontoinhaber", label = "Kontoinhaber", placeholder = "Name laut Konto" },
        },
            ["Zahlungsbedingungen"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "zahlungsfrist",   label = "Zahlungsfrist",     placeholder = "Anzahl der Tage bis zur Fälligkeit (z.B. 14)" },
            new FeldDefinition { key = "skontoSatz",      label = "Skonto-Satz",       placeholder = "Rabatt bei Sofortzahlung in % (optional)" },
            new FeldDefinition { key = "skontoZeitraum",  label = "Skonto-Zeitraum",   placeholder = "Zeitraum für Skonto-Abzug in Tagen" },
            new FeldDefinition { key = "verzugszins",     label = "Verzugszins",       placeholder = "Prozentsatz über Basiszinssatz bei Verzug" },
            new FeldDefinition { key = "zusatzhinweise",  label = "Zusatzhinweise",    placeholder = "Optionale Klauseln (z.B. Eigentumsvorbehalt)" },
        },
            ["AGB"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "leistungsbereich",     label = "Leistungsbereich",       placeholder = "Genaue Art der Software/Dienste (z.B. SaaS-Lösungen)" },
            new FeldDefinition { key = "widerrufsfrist",       label = "Widerrufsfrist",         placeholder = "Anzahl der Tage für das Widerrufsrecht (Standard: 14)" },
            new FeldDefinition { key = "zahlungsziel",         label = "Zahlungsziel",           placeholder = "Tage bis zur Fälligkeit" },
            new FeldDefinition { key = "verzugszinssatz",      label = "Verzugszinssatz",        placeholder = "Prozentsatz bei Zahlungsverzug" },
            new FeldDefinition { key = "mahngebuehr",          label = "Mahngebühr",             placeholder = "Pauschalbetrag pro Mahnstufe in EUR (z.B. 5,00 €)" },
            new FeldDefinition { key = "abnahmefrist",         label = "Abnahmefrist",           placeholder = "Tage zur Prüfung durch den Kunden" },
            new FeldDefinition { key = "lizenzmodell",         label = "Lizenzmodell",           placeholder = "„Einfach“ (Nutzung) oder „Erweitert“ (Editierung)" },
            new FeldDefinition { key = "schadensersatzfaktor", label = "Schadensersatzfaktor",   placeholder = "Faktor bei Copyright-Verstoß" },
            new FeldDefinition { key = "ndaDauer",             label = "NDA-Dauer",              placeholder = "Jahre der Geheimhaltung nach Projektende (z.B. 3)" },
            new FeldDefinition { key = "gerichtsstand",        label = "Gerichtsstand",          placeholder = "Ort des zuständigen Gerichts (z.B. Mittweida)" },
        },
            ["Disclaimer"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "geltungsbereich",        label = "Geltungsbereich",         placeholder = "Name der Software oder Website (z.B. Ventoriq-Plattform)" },
            new FeldDefinition { key = "inhaltlichePruefung",    label = "Inhaltliche Prüfung",     placeholder = "Turnus der Aktualisierung (z.B. „regelmäßig“, „anlassbezogen“)" },
            new FeldDefinition { key = "externeVerweise",        label = "Externe Verweise",        placeholder = "Erlaubnis oder Ausschluss der Haftung für Links zu Dritten" },
            new FeldDefinition { key = "urheberrechtshinweis",   label = "Urheberrechtshinweis",    placeholder = "Besonderheiten zu genutzten Medien oder Programmcode" },
        },
            ["Barzahlung"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "gueltigkeitsbereich", label = "Gültigkeitsbereich", placeholder = "Wofür wird dieser Vordruck genutzt? (z.B. „Bürokasse“, „Projekt XY“)" },
            new FeldDefinition { key = "zusatzhinweis",       label = "Zusatzhinweis",      placeholder = "Interner Vermerk oder Anweisung (z.B. „Nur für Kleinbeträge bis 250 €“)" },
            new FeldDefinition { key = "ausfuehrung",         label = "Ausführung",         typ = FeldTyp.Dropdown, optionen = new[] { "Einfache Ausführung", "Zweifache Ausführung" } },
        },
            ["Überweisung"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "kontoinhaber",   label = "Kontoinhaber",              placeholder = "Name des Empfängers (Standard: Firmenname)" },
            new FeldDefinition { key = "kreditinstitut", label = "Kreditinstitut",            placeholder = "Name deiner Bank oder Sparkasse" },
            new FeldDefinition { key = "iban",           label = "IBAN",                      placeholder = "Deine 22-stellige IBAN (beginnend mit DE)" },
            new FeldDefinition { key = "bic",            label = "BIC / SWIFT",               placeholder = "Der 8- oder 11-stellige Code deiner Bank" },
            new FeldDefinition { key = "verwendungszweck", label = "Standard-Verwendungszweck", placeholder = "z.B. die im Angebot/Rechnung hinterlegte Nummer wie AN-0003" },
        },
            ["SEPA-Basislastschrift-Mandat"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "glaeubigerId",  label = "Gläubiger-Identifikationsnummer", placeholder = "Deine individuelle Kennung für das Lastschriftverfahren" },
            new FeldDefinition { key = "artZahlung",    label = "Art der Zahlung",                 placeholder = "Einmalige Zahlung / Wiederkehrende Zahlung" },
            new FeldDefinition { key = "zusatzangaben", label = "Zusatzangaben",                   placeholder = "Optionale Hinweise zum Einzugsrhythmus" },
        },
            ["Widerrufsbelehrung"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "widerrufsfrist",   label = "Widerrufsfrist",       placeholder = "Gesetzlich z.B. 14 Tage. Nur die Zahl eintragen" },
            new FeldDefinition { key = "wertersatzklausel",label = "Wertersatz-Klausel: Pflicht zur Zahlung bereits erbrachter Leistungen", typ = FeldTyp.JaNein },
            new FeldDefinition { key = "kontakt",          label = "Kontakt für Widerruf", placeholder = "E-Mail-Adresse oder Postanschrift deiner Firma" },
            new FeldDefinition { key = "vorzeitigesErloeschen", label = "Vorzeitiges Erlöschen: Hinweis auf Erlöschen bei vollständiger Ausführung", typ = FeldTyp.JaNein },
        },
            ["Mahnverfahren"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "zahlungsziel",       label = "Zahlungsziel (Standard)",  placeholder = "Tage bis zur Fälligkeit (nur Zahl)" },
            new FeldDefinition { key = "verzugszinssatz",    label = "Verzugszinssatz",           placeholder = "Zinssatz p.a. bei Überschreitung (Vorgabe: 10 %)" },
            new FeldDefinition { key = "bearbeitungspauschale", label = "Bearbeitungspauschale",  placeholder = "Gebühr pro Mahnstufe ab Stufe 2 (z.B. 5,00 €)" },
            new FeldDefinition { key = "intervallMahnstufen",label = "Intervall der Mahnstufen",  placeholder = "Zeitraum zwischen den Schritten in Tagen (nur Zahl)" },
            new FeldDefinition { key = "zusatzhinweis",      label = "Zusatzhinweis",             placeholder = "Optionaler Text (z.B. „Wir setzen auf faire Partnerschaft“)" },
        },
            ["Ratenzahlungsbestimmungen"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "mindestauftragswert", label = "Mindestauftragswert",  placeholder = "Ab welcher Summe bietest du Raten an? (z.B. 1.000 €)" },
            new FeldDefinition { key = "maxLaufzeit",          label = "Max. Laufzeit",        placeholder = "Maximale Anzahl an Monatsraten (z.B. 12 Monate)" },
            new FeldDefinition { key = "bearbeitungsgebuehr",  label = "Bearbeitungsgebühr",   placeholder = "Einmalige Gebühr oder „0,00 €“" },
            new FeldDefinition { key = "zusatzhinweis",        label = "Zusatzhinweis",        placeholder = "Optionaler Text (z.B. „Bonitätsprüfung vorbehalten“)" },
        },
            ["Copyright Hinweis"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "schutzumfang",          label = "Schutzumfang",           placeholder = "Beschreibung der geschützten Werke (z.B. „Programmcode, Designs und UI-Konzepte“)" },
            new FeldDefinition { key = "referenzklausel",       label = "Referenzklausel: Recht zur Nutzung als Referenz aktiv bewerben?", typ = FeldTyp.JaNein },
            new FeldDefinition { key = "schadensersatzfaktor",  label = "Schadensersatzfaktor",   placeholder = "Faktor bei Verstößen (Vorgabe: 3-fache Höhe)" },
            new FeldDefinition { key = "zusatzangabe",          label = "Zusatzangabe",           placeholder = "Optionale Einschränkungen (z.B. „Anonymisierte Referenz auf Wunsch möglich“)" },
        },
            ["Lizenzhinweis Einfach"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "zusatzangabe",              label = "Zusatzangabe",                  placeholder = "Beschreibung der erweiterten Rechte (z.B. „Inklusive Recht zur Bearbeitung und Dekompilierung des Quellcodes“)" },
            new FeldDefinition { key = "kontakt",                   label = "Kontakt",                       placeholder = "E-Mail oder Ansprechpartner für Rückfragen (z.B. support@deinefirma.de)" },
            new FeldDefinition { key = "preisErweiterteNutzung",    label = "Preis für erweiterte Nutzung",  placeholder = "Betrag in Euro" },
        },
            ["Lizenzhinweis Erweitert"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "geltungsbereich",         label = "Geltungsbereich",                placeholder = "Für welche Leistungen gilt dies? (Standard: „Alle erbrachten Werke & Leistungen“)" },
            new FeldDefinition { key = "bedingungRechteuebergang", label = "Bedingung für Rechteübergang",   placeholder = "Wann gehen die Rechte über? (z.B. „Vollständige Zahlung der Vergütung“)" },
            new FeldDefinition { key = "nutzungsumfang",          label = "Nutzungsumfang",                 placeholder = "Was darf der Kunde tun? (z.B. „Jegliche gewerbliche Nutzung, Vervielfältigung & Bearbeitung“)" },
            new FeldDefinition { key = "raeumlicheReichweite",    label = "Räumliche Reichweite",           placeholder = "Wo darf das Werk genutzt werden? (z.B. „Weltweit / Unbeschränkt“)" },
            new FeldDefinition { key = "zeitlicheReichweite",     label = "Zeitliche Reichweite",           placeholder = "Wie lange gilt das Recht? (Standard: „Zeitlich unbeschränkt / Unbefristet“)" },
            new FeldDefinition { key = "zusatzvereinbarung",      label = "Zusatzvereinbarung",             placeholder = "Besondere Hinweise (z.B. „Inklusive Recht zur Unterlizensierung“)" },
        },
            ["Eröffnungsbilanz"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "bilanzstichtag", label = "Bilanzstichtag",  placeholder = "Datum der Bilanzaufstellung (TT.MM.JJJJ)" },
            new FeldDefinition { key = "anmerkungen",    label = "Anmerkungen",     placeholder = "Optionale Erläuterungen zu den Bilanzpositionen" },
        },
            ["Steuernummer-Bescheid / USt-IdNr"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "finanzamt",         label = "Zuständiges Finanzamt", placeholder = "Name des Finanzamts, das den Bescheid ausgestellt hat" },
            new FeldDefinition { key = "steuernummer",      label = "Steuernummer",          placeholder = "z.B. 123/456/78910" },
            new FeldDefinition { key = "ustIdNr",           label = "USt-IdNr.",             placeholder = "z.B. DE123456789 (optional)" },
            new FeldDefinition { key = "ausstellungsdatum", label = "Ausstellungsdatum",     placeholder = "Datum des Bescheids (TT.MM.JJJJ)" },
        },
            ["Impressum"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "vertretungsberechtigte", label = "Vertretungsberechtigte Person", placeholder = "Name der laut § 5 TMG anzugebenden Person" },
            new FeldDefinition { key = "email",                  label = "E-Mail",                        placeholder = "Kontakt-E-Mail für rechtliche Anfragen" },
            new FeldDefinition { key = "telefon",                label = "Telefon",                       placeholder = "Erreichbare Telefonnummer" },
            new FeldDefinition { key = "handelsregisternummer",  label = "Handelsregisternummer",         placeholder = "Optional, falls eingetragen" },
            new FeldDefinition { key = "zusatzangaben",          label = "Zusatzangaben",                 placeholder = "z.B. Berufsbezeichnung, Aufsichtsbehörde" },
        },
            ["Dienstleistungskatalog / Preisliste"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "leistungsuebersicht", label = "Leistungsübersicht", placeholder = "Kurze Auflistung der angebotenen Leistungen" },
            new FeldDefinition { key = "preismodell",         label = "Preismodell",        typ = FeldTyp.Dropdown, optionen = new[] { "Stundensatz", "Festpreis", "Pauschale", "Individuell" } },
            new FeldDefinition { key = "zusatzangaben",       label = "Zusatzangaben",      placeholder = "z.B. Rabattstaffeln, Mindestbuchungsdauer" },
        },
            ["Gründungs-Checkliste"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "geschaeftskontoEroeffnet",     label = "Geschäftskonto eröffnet",       typ = FeldTyp.JaNein },
            new FeldDefinition { key = "gewerbeAngemeldet",            label = "Gewerbe angemeldet",            typ = FeldTyp.JaNein },
            new FeldDefinition { key = "versicherungenAbgeschlossen",  label = "Versicherungen abgeschlossen",  typ = FeldTyp.JaNein },
            new FeldDefinition { key = "buchhaltungEingerichtet",      label = "Buchhaltung eingerichtet",      typ = FeldTyp.JaNein },
            new FeldDefinition { key = "websiteErstellt",              label = "Website & Branding erstellt",   typ = FeldTyp.JaNein },
            new FeldDefinition { key = "notizen",                      label = "Notizen",                       placeholder = "Offene Punkte oder nächste Schritte" },
        },
            ["Datenschutzerklärung (DSGVO)"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "versionsstand",         label = "Versionsstand",         placeholder = "Datum oder Nummer (z.B. „Stand: August 2026“)" },
            new FeldDefinition { key = "verantwortlicheStelle", label = "Verantwortliche Stelle", placeholder = "Name der Person/Abteilung (Standard: Geschäftsführung)" },
            new FeldDefinition { key = "kontaktDatenschutz",    label = "Kontakt Datenschutz",    placeholder = "E-Mail für Rückfragen (z.B. datenschutz@firma.de)" },
            new FeldDefinition { key = "zusatzangabe",          label = "Zusatzangabe",           placeholder = "Optional: Hinweis auf spezifische NDAs oder Projekt-Besonderheiten" },
        },
            ["Vertraulichkeitserklärung"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "versionsstand",   label = "Versionsstand",     placeholder = "Datum oder Nummer (z.B. „Stand: August 2026“)" },
            new FeldDefinition { key = "dauerDerPflicht", label = "Dauer der Pflicht", placeholder = "Jahre nach Vertragsende (Standard: „unbefristet“ oder z.B. „3 Jahre“)" },
            new FeldDefinition { key = "kontaktNda",      label = "Kontakt für NDAs",  placeholder = "E-Mail-Adresse für spezifische Geheimhaltungsanfragen" },
            new FeldDefinition { key = "zusatzangabe",    label = "Zusatzangabe",      placeholder = "Optional: Hinweis auf ergänzende projektbezogene NDAs" },
        },
            ["Muster-Arbeitsvertrag"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "vertragsart",         label = "Vertragsart",         placeholder = "Welche Art es ist (Minijob, Senior Developer, CEO, ...)" },
            new FeldDefinition { key = "stellenbezeichnung",  label = "Stellenbezeichnung",  placeholder = "z.B. „Junior Developer“ oder „UI-Designer“" },
            new FeldDefinition { key = "vertragsbeginn",      label = "Vertragsbeginn",      placeholder = "Datum des ersten Arbeitstages (TT.MM.JJJJ)" },
            new FeldDefinition { key = "wochenstunden",       label = "Wochenstunden",       placeholder = "Anzahl der Stunden (z.B. „40 Stunden“)" },
            new FeldDefinition { key = "bruttogehalt",        label = "Bruttogehalt",        placeholder = "Monatliches Entgelt in EUR" },
            new FeldDefinition { key = "urlaubstage",         label = "Urlaubstage",         placeholder = "Jährlicher Anspruch (gesetzliches Minimum: 20 bei 5-Tage-Woche)" },
            new FeldDefinition { key = "probezeit",           label = "Probezeit",           placeholder = "Dauer in Monaten (Standard: 6 Monate)" },
        },
            ["Vorlage Kündigung"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "arbeitnehmer",       label = "Arbeitnehmer",       placeholder = "Name & Anschrift (aus KDB oder frei eintragen)" },
            new FeldDefinition { key = "kuendigungsdatum",   label = "Kündigungsdatum",    placeholder = "Datum, an dem das Schreiben übergeben wird" },
            new FeldDefinition { key = "kuendigungstermin",  label = "Kündigungstermin",   placeholder = "Datum, zu dem das Verhältnis endet (z.B. „30.09.2026“)" },
            new FeldDefinition { key = "kuendigungsgrund",   label = "Kündigungsgrund",    placeholder = "Fristgerecht / Fristlos / optionaler Freitext" },
            new FeldDefinition { key = "rueckgabeCheck",     label = "Aufforderung zur Rückgabe von Firmeneigentum einblenden", typ = FeldTyp.JaNein },
        },
            ["Stellenbeschreibung"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "stellentitel",        label = "Stellentitel",        placeholder = "Name der Position (z.B. „Backend-Developer“ oder „UI-Lead“)" },
            new FeldDefinition { key = "abteilung",           label = "Abteilung",           placeholder = "Fachbereich (z.B. „Software-Engineering“ oder „Design“)" },
            new FeldDefinition { key = "berichtetAn",         label = "Berichtet an",        placeholder = "Vorgesetzte Rolle (z.B. „CTO / Projektleitung“)" },
            new FeldDefinition { key = "hauptaufgaben",       label = "Hauptaufgaben",       placeholder = "Kernaktivitäten (z.B. „Entwicklung der ERP-Logik“, „Datenmodelle pflegen“)" },
            new FeldDefinition { key = "anforderungsprofil",  label = "Anforderungsprofil",  placeholder = "Erforderliche Skills (z.B. „C# Kenntnisse“, „Unity UI Toolkit“)" },
            new FeldDefinition { key = "benefits",            label = "Benefits",            placeholder = "Was die Firma bietet (z.B. „Flexible Arbeitszeiten“, „Home Office“)" },
            new FeldDefinition { key = "starttermin",         label = "Starttermin",         placeholder = "Datum oder „ab sofort“" },
        },
            ["Urlaubsantrag"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "kalenderjahr",       label = "Kalenderjahr",        placeholder = "Jahr, für das die Vorlage gedruckt wird (z.B. „2026“)" },
            new FeldDefinition { key = "einreichungsfrist",  label = "Einreichungsfrist",   placeholder = "z.B. „Antrag bitte 2 Wochen vor Urlaubsantritt einreichen“" },
            new FeldDefinition { key = "zusatzhinweis",      label = "Zusatzhinweis",       placeholder = "Optionale Anweisung (z.B. „Bitte in Blockschrift ausfüllen“)" },
        },
            ["Corporate Identity Manual"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "versionsstand",   label = "Versionsstand",         placeholder = "Datum oder Nummer (z.B. „Stand: August 2026“)" },
            new FeldDefinition { key = "markenkern",      label = "Markenkern",            placeholder = "Kurzbeschreibung der Vision (z.B. „Klarheit durch Design“)" },
            new FeldDefinition { key = "primaerfarbe",    label = "Primärfarbe (HEX)",     placeholder = "Hauptfarbcode (z.B. Grün: #80CF95)" },
            new FeldDefinition { key = "sekundaerfarbe",  label = "Sekundärfarbe (HEX)",   placeholder = "Akzentfarbcode (z.B. Grau: #646464)" },
            new FeldDefinition { key = "hausschrift",     label = "Hausschrift",           placeholder = "Name der primären Schriftart (z.B. Poppins)" },
            new FeldDefinition { key = "aufloesung",      label = "Auflösung",             placeholder = "Optimierte Auflösung bei digital erstellten Inhalten (z.B. 1080p)" },
            new FeldDefinition { key = "layoutRaster",    label = "Layout-Raster",         placeholder = "Standard-Abstände (z.B. 8px / 16px / 24px)" },
            new FeldDefinition { key = "zusatzangabe",    label = "Zusatzangabe",          placeholder = "Optional (z.B. „Nutzung von Kristall-Elementen erlaubt“)" },
        },
            ["Businessplan"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "geschaeftsidee",     label = "Geschäftsidee",           placeholder = "Was ist der Kern Ihres Unternehmens? Welches Problem lösen Sie?" },
            new FeldDefinition { key = "zielgruppeMarkt",    label = "Zielgruppe & Markt",      placeholder = "Wer sind Ihre Kunden und wie erreichen Sie diese?" },
            new FeldDefinition { key = "angebotPreise",      label = "Angebot & Preise",        placeholder = "Was verkaufen Sie konkret und zu welchen Konditionen?" },
            new FeldDefinition { key = "staerken",           label = "Stärken (SWOT)",          placeholder = "Warum sind Sie besser als der Wettbewerb?" },
            new FeldDefinition { key = "risiken",            label = "Risiken (SWOT)",          placeholder = "Welche Gefahren gibt es und wie sichern Sie sich ab?" },
            new FeldDefinition { key = "investitionsbedarf", label = "Investitionsbedarf",      placeholder = "Wie viel Startkapital wird benötigt (z.B. für Hardware, Miete)?" },
            new FeldDefinition { key = "umsatzJ1",           label = "Umsatzprognose Jahr 1",   placeholder = "Geschätzter Rohgewinn Jahr 1" },
            new FeldDefinition { key = "umsatzJ2",           label = "Umsatzprognose Jahr 2",   placeholder = "Geschätzter Rohgewinn Jahr 2" },
            new FeldDefinition { key = "umsatzJ3",           label = "Umsatzprognose Jahr 3",   placeholder = "Geschätzter Rohgewinn Jahr 3" },
            new FeldDefinition { key = "teamMeilensteine",   label = "Team & Meilensteine",     placeholder = "Wer setzt das Projekt um und was sind die nächsten Ziele?" },
        },
            ["Markt- & Wettbewerbsanalyse"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "marktbeschreibung",       label = "Marktbeschreibung",        placeholder = "In welcher Branche sind Sie tätig? Was sind die aktuellen Trends?" },
            new FeldDefinition { key = "zielgruppensegmente",     label = "Zielgruppensegmente",      placeholder = "Wer sind Ihre Kernkunden? (z.B. „Software-Startups“)" },
            new FeldDefinition { key = "direkteWettbewerber",     label = "Direkte Wettbewerber",     placeholder = "Welche Firmen bieten ein identisches Produkt an?" },
            new FeldDefinition { key = "indirekteWettbewerber",   label = "Indirekte Wettbewerber",   placeholder = "Welche alternativen Lösungen nutzen Kunden aktuell (z.B. Excel)?" },
            new FeldDefinition { key = "wettbewerbsvorteile",     label = "Wettbewerbsvorteile",      placeholder = "Was ist Ihr Alleinstellungsmerkmal (USP) (z.B. „Guided UI“)?" },
            new FeldDefinition { key = "marktpotenzial",          label = "Marktpotenzial",           placeholder = "Wie schätzen Sie das Wachstum in den nächsten 3 Jahren ein?" },
        },
            ["SWOT-Analyse"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "interneStaerken",   label = "Interne Stärken",     placeholder = "Was sind Ihre Wettbewerbsvorteile? (z.B. „Hocheffiziente Architektur“)" },
            new FeldDefinition { key = "interneSchwaechen", label = "Interne Schwächen",   placeholder = "Wo liegen interne Defizite? (z.B. „Begrenzte Personalressourcen“)" },
            new FeldDefinition { key = "externeChancen",    label = "Externe Chancen",     placeholder = "Welche Markttrends können Sie nutzen? (z.B. „Wachsender Digitalisierungsbedarf“)" },
            new FeldDefinition { key = "externeRisiken",    label = "Externe Risiken",     placeholder = "Welche Gefahren drohen von außen? (z.B. „Markteintritt großer Wettbewerber“)" },
            new FeldDefinition { key = "strategischesFazit",label = "Strategisches Fazit", placeholder = "Welche Kernmaßnahmen leiten Sie aus dieser Analyse ab?" },
        },
            ["Zielgruppenanalyse"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "kernZielgruppe",         label = "Kern-Zielgruppe",         placeholder = "Wer ist der ideale Kunde? (z.B. „Einzelgründer im IT-Sektor“)" },
            new FeldDefinition { key = "demografischeMerkmale",  label = "Demografische Merkmale",  placeholder = "Alter, Region, Branche oder Unternehmensgröße" },
            new FeldDefinition { key = "psychografischeMerkmale",label = "Psychografische Merkmale",placeholder = "Welche Werte und Interessen hat die Zielgruppe?" },
            new FeldDefinition { key = "painPoints",             label = "Probleme & Pain Points",  placeholder = "Vor welchen Herausforderungen stehen die Kunden aktuell?" },
            new FeldDefinition { key = "kaufmotivation",         label = "Kaufmotivation",          placeholder = "Warum entscheidet sich der Kunde für Ihr Produkt?" },
            new FeldDefinition { key = "kommunikationswege",     label = "Kommunikationswege",      placeholder = "Über welche Kanäle erreichen Sie diese Personen?" },
        },
            ["Fördermittelübersicht"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "planungszeitraum",   label = "Planungszeitraum",      placeholder = "Zeitraum oder Phase (z.B. „Gründungsphase 2026/27“)" },
            new FeldDefinition { key = "zuschussProgramm1",  label = "Zuschuss-Programm 1",   placeholder = "Name des ersten Zuschusses (z.B. „EXIST-Gründerstipendium“)" },
            new FeldDefinition { key = "zuschussProgramm2",  label = "Zuschuss-Programm 2",   placeholder = "Weiteres Programm (z.B. „Gründungszuschuss BfA“)" },
            new FeldDefinition { key = "kreditProgramm1",    label = "Kredit-Programm 1",     placeholder = "Name des Darlehens (z.B. „KfW Startgeld“)" },
            new FeldDefinition { key = "kreditProgramm2",    label = "Kredit-Programm 2",     placeholder = "Weiteres Darlehen (z.B. „ERP-Gründerkredit“)" },
            new FeldDefinition { key = "sonstigeMittel",     label = "Sonstige Mittel",       placeholder = "Wettbewerbe oder Sponsoring (z.B. „Businessplan-Wettbewerb Sachsen“)" },
            new FeldDefinition { key = "strategischeNotizen",label = "Strategische Notizen",  placeholder = "Freitext für Fristen oder nächste Schritte" },
        },
            ["Darlehensübersicht"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "berichtsstand",         label = "Berichtsstand",         placeholder = "Datum der Erfassung (z.B. „3. Quartal 2026“)" },
            new FeldDefinition { key = "darlehensbezeichnung",  label = "Darlehensbezeichnung",  placeholder = "Name des Kredits (z.B. „KfW Startgeld“)" },
            new FeldDefinition { key = "kreditgeber",           label = "Kreditgeber",           placeholder = "Name der Bank oder des Investors" },
            new FeldDefinition { key = "darlehenssumme",        label = "Darlehenssumme",        placeholder = "Gesamter Nominalbetrag in EUR" },
            new FeldDefinition { key = "zinssatz",              label = "Zinssatz (%)",          placeholder = "Jährlicher Zinssatz (z.B. „3,0 %“)" },
            new FeldDefinition { key = "laufzeit",              label = "Laufzeit (Jahre)",      placeholder = "Dauer der Rückzahlung (z.B. „3 Jahre“)" },
            new FeldDefinition { key = "monatlicheRate",        label = "Monatliche Rate",       placeholder = "Betrag für Zins und Tilgung in EUR" },
            new FeldDefinition { key = "verwendungszweck",      label = "Verwendungszweck",      placeholder = "Wofür wurde das Geld genutzt? (z.B. „Büroausstattung“)" },
        },
            ["Versicherungsübersicht"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "berichtsstand",        label = "Berichtsstand",        placeholder = "Datum der Erfassung (z.B. „August 2026“)" },
            new FeldDefinition { key = "versicherungstyp",     label = "Versicherungstyp",     placeholder = "Art der Absicherung (z.B. „IT-Haftpflicht“ oder „Rechtsschutz“)" },
            new FeldDefinition { key = "versicherer",          label = "Versicherer",          placeholder = "Name der Versicherungsgesellschaft" },
            new FeldDefinition { key = "versicherungsnummer",  label = "Versicherungsnummer",  placeholder = "Eindeutige Kennung der Police" },
            new FeldDefinition { key = "beitrag",              label = "Beitrag (EUR)",        placeholder = "Monatliche oder jährliche Prämie" },
            new FeldDefinition { key = "zahlungsrhythmus",     label = "Zahlungsrhythmus",     typ = FeldTyp.Dropdown, optionen = new[] { "monatlich", "vierteljährlich", "jährlich" } },
            new FeldDefinition { key = "zusatznotiz",          label = "Zusatznotiz",          placeholder = "Optionale Infos (z.B. „inkl. Cyber-Risk-Abdeckung“)" },
        },
            ["Kundenzufriedenheitsumfrage"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "befragungszeitraum", label = "Befragungszeitraum", placeholder = "Zeitraum oder Projektname (z.B. „Projekt XY – Q3 2026“)" },
            new FeldDefinition { key = "einleitungstext",    label = "Einleitungstext",     placeholder = "Persönliche Ansprache (z.B. „Helfen Sie uns, besser zu werden“)" },
            new FeldDefinition { key = "leistungsfokus",     label = "Leistungsfokus",      placeholder = "Was wurde bewertet? (z.B. „Software-Implementierung“)" },
            new FeldDefinition { key = "frage1",             label = "Frage 1",             placeholder = "Erste Qualitätsabfrage (z.B. „Wie zufrieden sind Sie mit der Usability?“)" },
            new FeldDefinition { key = "frage2",             label = "Frage 2",             placeholder = "Zweite Qualitätsabfrage (z.B. „Wie klar war die Kommunikation?“)" },
            new FeldDefinition { key = "frage3",             label = "Frage 3",             placeholder = "Dritte Qualitätsabfrage (z.B. „Entspricht das Ergebnis Ihren Erwartungen?“)" },
            new FeldDefinition { key = "schlusswort",        label = "Schlusswort",         placeholder = "Dankesformel an den Kunden" },
        },
            ["Vollmachtvorlage"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "betreff", label = "Betreff (optional)", placeholder = "Kurztitel für die Kopfzeile (z.B. „Postvollmacht“)" },
        },
            ["Gesellschafterliste"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "erstellungsdatum",  label = "Erstellungsdatum",       placeholder = "Datum der aktuellen Liste (Stichtag, TT.MM.JJJJ)" },
            new FeldDefinition { key = "gesellschafter1",   label = "Gesellschafter 1",       placeholder = "Name, Vorname, Geburtsdatum, Wohnort" },
            new FeldDefinition { key = "anteil1",           label = "Anteil 1 (Betrag)",      placeholder = "Nennbetrag des Anteils in EUR (z.B. „12.500 €“)" },
            new FeldDefinition { key = "gesellschafter2",   label = "Gesellschafter 2",       placeholder = "Name, Vorname, Geburtsdatum, Wohnort" },
            new FeldDefinition { key = "anteil2",           label = "Anteil 2 (Betrag)",      placeholder = "Nennbetrag des Anteils in EUR" },
            new FeldDefinition { key = "stammkapitalGesamt",label = "Stammkapital Gesamt",    placeholder = "Summe aller Anteile in EUR" },
            new FeldDefinition { key = "zusatzangaben",     label = "Zusatzangaben",          placeholder = "z.B. „Laufende Nummern der Anteile 1 bis X“" },
        },
            ["Gutschriftvorlage"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "tabellenzeilen", label = "Tabellenzeilen", placeholder = "Anzahl der Leerzeilen für Positionen (z.B. „5“)" },
            new FeldDefinition { key = "zusatzhinweis",  label = "Zusatzhinweis",  placeholder = "Optionaler Text (z.B. „Bitte Grund der Gutschrift angeben“)" },
        },
            ["Besprechungsprotokoll"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "themenFokus",         label = "Themen-Fokus",           placeholder = "Projekttitel oder Bereich (z.B. „Projekt XY – Meilenstein 1“)" },
            new FeldDefinition { key = "anzahlAufgabenzeilen",label = "Anzahl Aufgabenzeilen",  placeholder = "Auswahl: „Standard (8 Zeilen)“" },
            new FeldDefinition { key = "zusatzhinweis",       label = "Zusatzhinweis",          placeholder = "Optionaler Text (z.B. „Bitte zur Dokumentation in die Cloud hochladen“)" },
        },
            ["Inventarliste"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "tabellenzeilen",  label = "Tabellenzeilen",   placeholder = "Anzahl der Leerzeilen (z.B. „15“ oder „Ganze Seite“)" },
            new FeldDefinition { key = "inventurBereich", label = "Inventur-Bereich", placeholder = "Optionaler Fokus (z.B. „IT-Hardware“ oder „Büroausstattung“)" },
        },
            ["Unternehmensrichtlinien"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "versionsstand",        label = "Versionsstand",           placeholder = "Datum oder Nummer (z.B. „v1.0 – Stand August 2026“)" },
            new FeldDefinition { key = "vision",               label = "Vision",                  placeholder = "Was ist die Unternehmensvision in einem kurzen Text" },
            new FeldDefinition { key = "unternehmenskultur",   label = "Unternehmenskultur",      placeholder = "Kurzbeschreibung der Werte (z.B. „Transparenz & Eigenverantwortung“)" },
            new FeldDefinition { key = "reaktionszeit",        label = "Reaktionszeit (Std.)",    placeholder = "Zeitfenster für interne Rückmeldungen (Standard: „24 Stunden“)" },
            new FeldDefinition { key = "technologieStack",     label = "Technologie-Stack",       placeholder = "Genutzte Tools (z.B. „Unity, GitHub, SQLite, Ventoriq“)" },
            new FeldDefinition { key = "designStack",          label = "Design-Stack",            placeholder = "Genutzte Tools und Details (z.B. „Unity, GitHub, SQLite, Ventoriq, 1080p“)" },
            new FeldDefinition { key = "uxStack",              label = "UX-Stack",                placeholder = "Details zum Ablauf und zur Sicherstellung der Qualitätssicherung" },
            new FeldDefinition { key = "datenschutzabschnitt", label = "Datenschutzabschnitt",    placeholder = "Kurzbeschreibung wie der Datenschutz aussieht" },
            new FeldDefinition { key = "datensicherheitabschnitt", label = "Datensicherheitsabschnitt", placeholder = "Kurzbeschreibung wie es zum Thema Datensicherheit steht" },
            new FeldDefinition { key = "zusatzangaben",        label = "Zusatzangaben",           placeholder = "Optionale spezifische Regeln (z.B. „Home-Office-Vorgaben“)" },
        },
            ["Social Media Strategie"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "primaereKanaele",   label = "Primäre Kanäle",     placeholder = "Auswahl der Plattformen (z.B. „LinkedIn & Discord“)" },
            new FeldDefinition { key = "kernbotschaft",     label = "Kernbotschaft",      placeholder = "Die Hauptaussage (z.B. „Gründen ohne kognitive Last“)" },
            new FeldDefinition { key = "postFrequenz",      label = "Post-Frequenz",      placeholder = "Wie oft wird gepostet? (z.B. „3x wöchentlich“)" },
            new FeldDefinition { key = "zielgruppe",        label = "Zielgruppe",         placeholder = "Definition (z.B. „Software-Gründer & Startups“)" },
            new FeldDefinition { key = "zusatzangabe",      label = "Zusatzangabe",       placeholder = "Besondere Kampagnen (z.B. „Release-Countdown zum 01.07.“)" },
            new FeldDefinition { key = "productInsights",  label = "Product Insights",   placeholder = "Contentschwerpunkt - Inhalt für Produkte" },
            new FeldDefinition { key = "techTransparenz",  label = "Tech Transparenz",   placeholder = "Contentschwerpunkt - Inhalt für Technik & Transparenz" },
            new FeldDefinition { key = "startupEducation", label = "Startup Education",  placeholder = "Contentschwerpunkt - Inhalt für Weiteres" },
        },
        };


    private static readonly Dictionary<string, string> kategorieTooltips =
        new Dictionary<string, string>
        {
            ["Gründung"] = "Enthält Pflichtdokumente zur Unternehmensgründung. " +
                               "Fülle Stammdaten, Gründungsurkunde und Handelsregister aus. " +
                               "Diese Kategorie ist geschützt und kann nicht gelöscht werden.",
            ["Bezahlweise"] = "Enthält Pflichtdokumente für Zahlungsabwicklung und Rechnungsanhänge. " +
                               "AGB, Disclaimer, Barzahlung und Überweisung werden als PDF-Anhänge verwendet. " +
                               "Diese Kategorie ist geschützt und kann nicht gelöscht werden.",
            ["Finanzen"] = "Flexible Kategorie für finanzielle Dokumente wie Budgetpläne oder Kalkulationen. " +
                               "Du kannst hier eigene Dokumente anlegen und löschen.",
            ["Marketing"] = "Flexible Kategorie für Marketingmaterial wie Konzepte oder Kampagnenpläne. " +
                               "Du kannst hier eigene Dokumente anlegen und löschen.",
            ["Steuern"] = "Flexible Kategorie für steuerrelevante Dokumente wie Belege oder Bescheide. " +
                               "Du kannst hier eigene Dokumente anlegen und löschen.",
            ["Personal"] = "Flexible Kategorie für Personaldokumente wie Verträge oder Zeugnisse. " +
                               "Du kannst hier eigene Dokumente anlegen und löschen.",
            ["Recht"] = "Flexible Kategorie für rechtliche Dokumente wie Verträge oder Datenschutzerklärungen. " +
                               "Du kannst hier eigene Dokumente anlegen und löschen.",
        };

    private const string defaultFest = "Geschützte Pflichtdokument-Kategorie. " +
                                        "Dokumente können bearbeitet, aber nicht gelöscht werden.";
    private const string defaultFlex = "Flexible Kategorie \u2013 du kannst hier eigene Dokumente anlegen und löschen.";
    private bool IstKategorieFest(string kategorieName)
        => kategorien.Find(k => k.name == kategorieName)?.istFest ?? false;

    [System.Serializable]
    public class StrukturFeldWert
    {
        public string key;
        public string wert;
    }

    [System.Serializable]
    public class DocumentData
    {
        public string id;
        public string category;
        public string title;
        public string type;
        public bool istPflichtdokument;
        public string inhalt;
        public List<StrukturFeldWert> strukturFelder;
        // Nur bei type == "Standard" bzw. "Checklist"/"Diagramm" befüllt.
        public StandardDokumentDaten standardDaten;
        public List<ChecklistPunkt> checkliste;
        public List<DiagrammPunkt> diagrammDaten;
        // Zeitpunkt der letzten Änderung (yyyy-MM-dd) - für die Sortierung
        // im Export-Screen. Bei alten, bereits gespeicherten Dokumenten
        // leer, bis sie das nächste Mal bearbeitet werden.
        public string datum;
    }

    [System.Serializable]
    public class ChecklistPunkt
    {
        public string text;
        public bool erledigt;
    }

    [System.Serializable]
    public class DiagrammPunkt
    {
        public string label;
        public float wert;
    }

    [System.Serializable]
    public class StandardDokumentDaten
    {
        public string titel;
        public string datum;
        public string inhalt;
    }

    [System.Serializable]
    public class DocumentSaveData
    {
        public List<DocumentData> savedDocs = new List<DocumentData>();
    }

    private DocumentSaveData speicherDaten = new DocumentSaveData();
    private string saveFilePath;

    // ─────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────

    void OnEnable()
    {
        saveFilePath = GetSaveFilePath();

        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        // 1. Hauptbildschirm
        deleteButton = root.Q<Button>("Delete-Button");
        gridContainer = root.Q<VisualElement>("Grid-Container");

        var btnAddDocument = root.Q<Button>("Btn-Add-Document");
        if (btnAddDocument != null) btnAddDocument.clicked += () => OpenPopup();

        // 2. Erstell-Popup (nur flexible Kategorien)
        popupOverlay = root.Q<VisualElement>("Popup-Overlay");
        popupCancelButton = root.Q<Button>("Btn-Cancel");
        var btnAbbrechen = root.Q<Button>("Btn-Abbrechen");
        if (btnAbbrechen != null) btnAbbrechen.clicked += ClosePopup;
        popupSubmitButton = root.Q<Button>("Btn-Submit");
        categoryDropdown = root.Q<DropdownField>("dropKategorie");
        docNameInput = root.Q<TextField>("Doc-Name-Input");

        // 3. Zweispaltiges Listen-Popup
        detailPopupOverlay = root.Q<VisualElement>("Detail-Popup-Overlay");
        detailListContainer = root.Q<VisualElement>("Detail-List-Container");
        globalListContainer = root.Q<VisualElement>("Global-List-Container");
        detailCloseButton = root.Q<Button>("Btn-Detail-Close");
        detailPopupTitle = root.Q<Label>("Detail-Popup-Title");
        listCreateNewButton = root.Q<Button>("Btn-List-Create-New");

        // 4. Bearbeiten-Popup
        editPopupOverlay = root.Q<VisualElement>("Edit-Popup-Overlay");
        editDocNameInput = root.Q<TextField>("Edit-Doc-Name-Input");
        editInhaltInput = root.Q<TextField>("Edit-Inhalt-Input");
        editStandardTitelInput = root.Q<TextField>("Edit-Standard-Titel-Input");
        editStandardDatumInput = root.Q<TextField>("Edit-Standard-Datum-Input");

        // Platzhalter nur EINMAL registrieren - diese drei Felder sind
        // wiederverwendet (anders als die Checklisten-/Diagramm-Zeilen,
        // die bei jedem Öffnen frisch gebaut werden). Würde
        // SetzeFeldPlatzhalter hier bei jedem Popup-Öffnen erneut
        // aufgerufen, würden sich die FocusIn/FocusOut-Handler mit jedem
        // Öffnen aufsummieren.
        RegistrierePersistentenPlatzhalter(editStandardTitelInput, "z.B. Rechnungsvorlage, Angebot Website-Relaunch, ...");
        RegistrierePersistentenPlatzhalter(editStandardDatumInput, "TT.MM.JJJJ");
        RegistrierePersistentenPlatzhalter(editInhaltInput, "Haupttext des Dokuments...");
        feldGruppeInhalt = root.Q<VisualElement>("feld-gruppe-inhalt");
        feldGruppeCheckliste = root.Q<VisualElement>("feld-gruppe-checkliste");
        feldGruppeDiagramm = root.Q<VisualElement>("feld-gruppe-diagramm");
        editChecklisteBox = root.Q<VisualElement>("Edit-Checkliste-Box");
        editDiagrammDatenBox = root.Q<VisualElement>("Edit-Diagramm-Daten-Box");
        editDiagrammVorschau = root.Q<VisualElement>("Edit-Diagramm-Vorschau");
        btnChecklisteHinzufuegen = root.Q<Button>("Btn-Checkliste-Hinzufuegen");
        btnDiagrammHinzufuegen = root.Q<Button>("Btn-Diagramm-Hinzufuegen");

        if (editDiagrammVorschau != null)
        {
            diagrammVorschauElement = new EinfachesBalkendiagrammElement();
            diagrammVorschauElement.style.flexGrow = 1;
            editDiagrammVorschau.Add(diagrammVorschauElement);
        }

        if (btnChecklisteHinzufuegen != null)
            btnChecklisteHinzufuegen.clicked += () =>
            {
                aktiveChecklistPunkte.Add(new ChecklistPunkt { text = "", erledigt = false });
                BaueChecklistenUI();
            };

        if (btnDiagrammHinzufuegen != null)
            btnDiagrammHinzufuegen.clicked += () =>
            {
                aktiveDiagrammPunkte.Add(new DiagrammPunkt { label = "", wert = 0f });
                BaueDiagrammUI();
            };
        editPopupSubmitButton = root.Q<Button>("Btn-Edit-Submit");
        editPopupCancelButton = root.Q<Button>("Btn-Edit-Cancel");
        btnEditTypeStandard = root.Q<Button>("Btn-Edit-Type-Standard");
        btnEditTypeDiagramm = root.Q<Button>("Btn-Edit-Type-Diagramm");
        btnEditTypeChecklist = root.Q<Button>("Btn-Edit-Type-Checklist");
        editLockedHint = root.Q<Label>("Edit-Locked-Hint");
        editTemplateGroup = root.Q<VisualElement>("Edit-Buttons-Type-Group");
        editStrukturFelderBox = root.Q<VisualElement>("Edit-Struktur-Felder-Box");

        // 5. Lösch-Bestätigungs-Popup
        deleteConfirmOverlay = root.Q<VisualElement>("Delete-Confirm-Overlay");
        deleteConfirmYesButton = root.Q<Button>("Btn-Delete-Confirm-Yes");
        deleteConfirmCancelButton = root.Q<Button>("Btn-Delete-Confirm-Cancel");
        deleteConfirmHint = root.Q<Label>("Delete-Confirm-Hint");

        // Dropdown befüllen (nur flexible Kategorien)
        if (categoryDropdown != null)
        {
            var namen = kategorien.Select(k => k.name).ToList();
            categoryDropdown.choices = namen;
            if (namen.Count > 0)
                categoryDropdown.value = namen[0];
        }

        // Event-Verdrahtung Hauptmenü
        if (deleteButton != null) deleteButton.clicked += OpenDeleteConfirmPopup;

        // Event-Verdrahtung Erstell-Popup
        if (popupCancelButton != null) popupCancelButton.clicked += ClosePopup;
        if (popupSubmitButton != null) popupSubmitButton.clicked += CreateNewDocumentEntry;

        btnTypeStandard = root.Q<Button>("Btn-Type-Standard");
        btnTypeDiagramm = root.Q<Button>("Btn-Type-Diagramm");
        btnTypeChecklist = root.Q<Button>("Btn-Type-Checklist");

        if (btnTypeStandard != null) btnTypeStandard.clicked += () => ApplyTemplate("Standard");
        if (btnTypeDiagramm != null) btnTypeDiagramm.clicked += () => ApplyTemplate("Diagramm");
        if (btnTypeChecklist != null) btnTypeChecklist.clicked += () => ApplyTemplate("Checklist");

        // Event-Verdrahtung Listen-Popup
        if (detailCloseButton != null) detailCloseButton.clicked += CloseDetailPopup;
        if (listCreateNewButton != null)
            listCreateNewButton.clicked += () =>
            {
                CloseDetailPopup();
                OpenPopup(activeCategoryForList);
            };

        // Event-Verdrahtung Bearbeiten-Popup
        if (editPopupCancelButton != null) editPopupCancelButton.clicked += CloseEditPopup;
        if (editPopupSubmitButton != null) editPopupSubmitButton.clicked += SaveEditedDocumentEntry;
        if (btnEditTypeStandard != null) btnEditTypeStandard.clicked += () => SelectEditType("Standard");
        if (btnEditTypeDiagramm != null) btnEditTypeDiagramm.clicked += () => SelectEditType("Diagramm");
        if (btnEditTypeChecklist != null) btnEditTypeChecklist.clicked += () => SelectEditType("Checklist");

        // Event-Verdrahtung Lösch-Bestätigung
        if (deleteConfirmCancelButton != null) deleteConfirmCancelButton.clicked += CloseDeleteConfirmPopup;
        if (deleteConfirmYesButton != null) deleteConfirmYesButton.clicked += ConfirmDeleteAllDocuments;

        LoadDataLocally();
        SicherePflichtdokumente();
        SpawnAllCardsAtStart();
        RegistriereHelpTooltips();
        ButtonHoverController.RegistriereAlle(root);

        // Von einem anderen Screen aus (z.B. Einstellungen -> "Bearbeiten"
        // bei AGB/Disclaimer/Barzahlung/Überweisung) angefordert: direkt
        // das Bearbeiten-Popup für ein bestimmtes Dokument öffnen, statt
        // den Nutzer erst manuell die Karte suchen und anklicken zu lassen.
        if (!string.IsNullOrEmpty(OeffneDokumentBeimStart))
        {
            string gesuchterTitel = OeffneDokumentBeimStart;
            OeffneDokumentBeimStart = null;

            var zielDoc = speicherDaten.savedDocs.FirstOrDefault(d => d.title == gesuchterTitel);
            if (zielDoc != null) OpenEditPopup(zielDoc);
        }
    }

    // Von außen (z.B. EinstellungenController) setzbar: Titel des
    // Dokuments, dessen Bearbeiten-Popup beim Laden dieses Screens
    // automatisch geöffnet werden soll.
    public static string OeffneDokumentBeimStart = null;

    // ─────────────────────────────────────────
    // PFLICHTDOKUMENTE SICHERSTELLEN
    // ─────────────────────────────────────────
    private void SicherePflichtdokumente()
    {
        bool geaendert = false;

        foreach (var kategorie in kategorien)
        {
            foreach (string pflichtTitel in kategorie.pflichtDocs)
            {
                var bestehendesDoc = speicherDaten.savedDocs.FirstOrDefault(d =>
                    d.category == kategorie.name &&
                    d.istPflichtdokument &&
                    d.title == pflichtTitel);

                if (bestehendesDoc == null)
                {
                    var neuesDoc = new DocumentData
                    {
                        id = System.Guid.NewGuid().ToString(),
                        category = kategorie.name,
                        title = pflichtTitel,
                        type = "Standard",
                        istPflichtdokument = true,
                        inhalt = "",
                        strukturFelder = ErzeugeLeereStrukturFelder(pflichtTitel)
                    };
                    speicherDaten.savedDocs.Add(neuesDoc);
                    geaendert = true;
                }
                else if (bestehendesDoc.strukturFelder == null || bestehendesDoc.strukturFelder.Count == 0)
                {
                    bestehendesDoc.strukturFelder = ErzeugeLeereStrukturFelder(pflichtTitel);
                    geaendert = true;
                }
                else
                {
                    // Neue Felder ergänzen, falls die Definition erweitert wurde
                    if (felderProPflichtDoc.TryGetValue(pflichtTitel, out var definitionen))
                    {
                        foreach (var def in definitionen)
                        {
                            bool vorhanden = bestehendesDoc.strukturFelder.Any(f => f.key == def.key);
                            if (!vorhanden)
                            {
                                bestehendesDoc.strukturFelder.Add(new StrukturFeldWert { key = def.key, wert = "" });
                                geaendert = true;
                            }
                        }
                    }
                }
            }
        }

        // Aufräumen: verwaiste Pflichtdokument-Einträge entfernen, deren
        // (Kategorie, Titel)-Kombination zu KEINEM aktuellen Pflichtdokument
        // mehr passt - z.B. Reste vom Split "Gründungsurkunde /
        // Gesellschaftsvertrag" -> "Gründungsurkunde" + "Gesellschaftsvertrag".
        // Deren Feldstruktur ist ohnehin veraltet/inkompatibel, ein
        // Migrieren der alten Werte wäre nicht sauber möglich.
        var gueltigeKombinationen = new HashSet<(string, string)>(
            kategorien.SelectMany(k => k.pflichtDocs.Select(titel => (k.name, titel))));

        int entfernt = speicherDaten.savedDocs.RemoveAll(d =>
            d.istPflichtdokument && !gueltigeKombinationen.Contains((d.category, d.title)));
        if (entfernt > 0) geaendert = true;

        if (geaendert) SaveDataLocally();

        // Backfill: Dokumente, die schon Inhalt haben aber noch kein Datum
        // (weil sie vor der Einführung des Datum-Felds ausgefüllt wurden),
        // bekommen einmalig ein Datum gesetzt. Das exakte historische Datum
        // kennen wir nicht mehr - "heute" ist die ehrlichste Annahme, die wir
        // treffen können. Ab jetzt wird bei jeder Bearbeitung aktualisiert.
        bool datumBackfillGeaendert = false;
        foreach (var doc in speicherDaten.savedDocs)
        {
            if (!string.IsNullOrEmpty(doc.datum)) continue;

            bool hatInhalt   = !string.IsNullOrWhiteSpace(doc.inhalt);
            bool hatFeldwert = doc.strukturFelder != null &&
                               doc.strukturFelder.Any(f => !string.IsNullOrWhiteSpace(f.wert));
            if (!hatInhalt && !hatFeldwert) continue;

            doc.datum = System.DateTime.Now.ToString("yyyy-MM-dd");
            datumBackfillGeaendert = true;
        }
        if (datumBackfillGeaendert) SaveDataLocally();
    }

    private List<StrukturFeldWert> ErzeugeLeereStrukturFelder(string dokumentTitel)
    {
        var ergebnis = new List<StrukturFeldWert>();
        if (felderProPflichtDoc.TryGetValue(dokumentTitel, out var definitionen))
        {
            foreach (var def in definitionen)
                ergebnis.Add(new StrukturFeldWert { key = def.key, wert = "" });
        }
        return ergebnis;
    }

    // ─────────────────────────────────────────
    // ERSTELLLOGIK (nur für flexible Kategorien)
    // ─────────────────────────────────────────

    private void OpenPopup(string preselectedCategory = "")
    {
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.Flex;
        if (docNameInput != null) docNameInput.value = "";
        selectedType = "Standard";
        MarkiereAusgewaehlteVorlage(btnTypeStandard, btnTypeDiagramm, btnTypeChecklist, selectedType);

        if (!string.IsNullOrEmpty(preselectedCategory) && categoryDropdown != null)
            if (kategorien.Any(k => k.name == preselectedCategory && !k.istFest))
                categoryDropdown.value = preselectedCategory;
    }

    private void ClosePopup()
    {
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.None;
    }

    private void ApplyTemplate(string typeName)
    {
        selectedType = typeName;
        MarkiereAusgewaehlteVorlage(btnTypeStandard, btnTypeDiagramm, btnTypeChecklist, selectedType);
    }

    private void CreateNewDocumentEntry()
    {
        string selectedCategory = categoryDropdown != null ? categoryDropdown.value : "";

        string docText = docNameInput != null ? docNameInput.value.Trim() : "";
        if (string.IsNullOrEmpty(docText))
        {
            if (docNameInput != null)
            {
                docNameInput.AddToClassList("input-error");
                docNameInput.schedule.Execute(() => docNameInput.RemoveFromClassList("input-error")).ExecuteLater(1200);
            }
            return;
        }

        if (string.IsNullOrEmpty(selectedCategory)) return;

        DocumentData newDoc = new DocumentData
        {
            id = System.Guid.NewGuid().ToString(),
            category = selectedCategory,
            title = docText,
            type = selectedType,
            istPflichtdokument = false,
            inhalt = "",
            strukturFelder = new List<StrukturFeldWert>(),
            datum = System.DateTime.Now.ToString("yyyy-MM-dd")
        };

        speicherDaten.savedDocs.Add(newDoc);
        SaveDataLocally();
        SpawnAllCardsAtStart();
        ClosePopup();
    }

    // ─────────────────────────────────────────
    // DASHBOARD-KACHELN
    // ─────────────────────────────────────────

    private void SpawnAllCardsAtStart()
    {
        if (gridContainer == null)
        {
            Debug.LogError("[Dokumente] gridContainer ist leer - UXML-Struktur oder UIDocument-Zuweisung prüfen.");
            return;
        }
        if (categoryCardTemplate == null)
        {
            Debug.LogError("[Dokumente] categoryCardTemplate ist im Inspector nicht zugewiesen - " +
                "dadurch werden keine Kategorie-Karten angezeigt. Bitte am DocumentDashboard-" +
                "GameObject die CategoryCard.uxml-Datei ins Feld 'Category Card Template' ziehen.");
            return;
        }
        gridContainer.Clear();

        foreach (var kategorie in kategorien)
        {
            VisualElement cardInstance = categoryCardTemplate.Instantiate();

            Label titleLabel = cardInstance.Q<Label>("lblName");
            if (titleLabel != null) titleLabel.text = kategorie.name;

            VisualElement lockBadge = cardInstance.Q<VisualElement>("lock-badge");
            if (lockBadge != null)
                lockBadge.style.display = kategorie.istFest ? DisplayStyle.Flex : DisplayStyle.None;

            VisualElement imgShowList = cardInstance.Q("Btn-Show-List");
            if (imgShowList != null)
                imgShowList.RegisterCallback<ClickEvent>(evt => OpenDetailPopup(kategorie.name));

            Button alleAnzeigenBtn = cardInstance.Q<Button>("btnPlus");
            if (alleAnzeigenBtn != null) alleAnzeigenBtn.clicked += () => OpenDetailPopup(kategorie.name);

            List<DocumentData> kategorieDocs =
                speicherDaten.savedDocs.FindAll(d => d.category == kategorie.name);

            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                VisualElement contentBox = cardInstance.Q<VisualElement>($"Slot-{slotIndex}-Content");
                VisualElement iconBox = cardInstance.Q<VisualElement>($"Slot-{slotIndex}-Icon");
                Button plusBtn = cardInstance.Q<Button>($"Slot-{slotIndex}-Plus");

                if (contentBox == null) continue;
                contentBox.Clear();

                bool hatDokument = slotIndex < kategorieDocs.Count;

                if (hatDokument)
                {
                    var aktuellesDoc = kategorieDocs[slotIndex];

                    string iconGlyph = aktuellesDoc.istPflichtdokument ? "🔒" :
                                        aktuellesDoc.type == "Diagramm" ? "📊" :
                                        aktuellesDoc.type == "Checklist" ? "✅" : "📄";
                    if (iconBox != null)
                    {
                        iconBox.Clear();
                        var iconLabel = new Label(iconGlyph);
                        iconLabel.style.fontSize = 13;
                        iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                        iconLabel.style.flexGrow = 1;
                        iconBox.Add(iconLabel);
                    }

                    string titel = aktuellesDoc.title.Split('\n')[0];
                    if (titel.Length > 22) titel = titel.Substring(0, 19) + "...";
                    Label titelLabel = new Label(titel);
                    titelLabel.AddToClassList("doc-mini-title");
                    contentBox.Add(titelLabel);

                    string vorschauText = BildeInhaltVorschau(aktuellesDoc);
                    if (!string.IsNullOrEmpty(vorschauText))
                    {
                        Label inhaltLabel = new Label(vorschauText);
                        inhaltLabel.AddToClassList("doc-mini-inhalt");
                        contentBox.Add(inhaltLabel);
                    }
                    else
                    {
                        Label leerHinweis = new Label("Kein Inhalt hinterlegt");
                        leerHinweis.AddToClassList("doc-mini-inhalt-leer");
                        contentBox.Add(leerHinweis);
                    }

                    if (plusBtn != null)
                    {
                        plusBtn.text = "✎";
                        plusBtn.clicked += () => OpenEditPopup(aktuellesDoc);
                    }
                }
                else
                {
                    if (iconBox != null) { iconBox.Clear(); iconBox.style.opacity = 0.4f; }

                    Label leerLabel = new Label("Leer");
                    leerLabel.AddToClassList("doc-mini-empty-label");
                    contentBox.Add(leerLabel);

                    if (plusBtn != null)
                    {
                        plusBtn.text = "+";
                        if (!kategorie.istFest)
                            plusBtn.clicked += () => OpenPopup(kategorie.name);
                        else
                            plusBtn.SetEnabled(false);
                    }
                }
            }

            // Hilfe-Icon in die Kategorie-Karte einfügen
            var karteHelpIcon = new VisualElement();
            karteHelpIcon.name = "btn-help-karte";
            HelpTooltip.SetzeBasisStilOeffentlich(karteHelpIcon);
            // Icon-Textur setzen (helpIconTexture im Inspector zuweisen)
            if (helpIconTexture != null)
            {
                karteHelpIcon.style.backgroundImage = new StyleBackground(helpIconTexture);
                karteHelpIcon.style.unityBackgroundImageTintColor = new StyleColor(
                    new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f));
            }

            var headerRow = cardInstance.Q<VisualElement>(className: "category-header-row");
            if (headerRow != null)
                headerRow.Add(karteHelpIcon);
            else
                cardInstance.Add(karteHelpIcon);

            string karteTooltip;
            if (!kategorieTooltips.TryGetValue(kategorie.name, out karteTooltip))
                karteTooltip = kategorie.istFest ? defaultFest : defaultFlex;

            HelpTooltip.RegistriereInKarte(root, karteHelpIcon, karteTooltip);

            gridContainer.Add(cardInstance);
        }
    }

    // Baut eine kurze Vorschau aus den Strukturfeldern (Pflichtdokument)
    // oder dem Freitext (flexibles Dokument) für die Kachelansicht.
    private string BildeInhaltVorschau(DocumentData doc)
    {
        if (doc.istPflichtdokument && doc.strukturFelder != null && doc.strukturFelder.Count > 0)
        {
            var ersterAusgefuellterWert = doc.strukturFelder.FirstOrDefault(f => !string.IsNullOrEmpty(f.wert));
            if (ersterAusgefuellterWert == null) return "";

            string label = HoleFeldLabel(doc.title, ersterAusgefuellterWert.key);
            string wert = ersterAusgefuellterWert.wert;
            if (wert.Length > 18) wert = wert.Substring(0, 15) + "...";
            return $"{label}: {wert}";
        }

        if (doc.type == "Checklist" && doc.checkliste != null && doc.checkliste.Count > 0)
        {
            int erledigt = doc.checkliste.Count(p => p.erledigt);
            return $"{erledigt} von {doc.checkliste.Count} erledigt";
        }

        if (doc.type == "Diagramm" && doc.diagrammDaten != null && doc.diagrammDaten.Count > 0)
        {
            return $"{doc.diagrammDaten.Count} Werte im Diagramm";
        }

        if (!string.IsNullOrEmpty(doc.inhalt))
        {
            string vorschau = doc.inhalt.Split('\n')[0];
            if (vorschau.Length > 24) vorschau = vorschau.Substring(0, 21) + "...";
            return vorschau;
        }

        return "";
    }

    private string HoleFeldLabel(string dokumentTitel, string key)
    {
        if (felderProPflichtDoc.TryGetValue(dokumentTitel, out var definitionen))
        {
            var def = definitionen.FirstOrDefault(f => f.key == key);
            if (def != null) return def.label;
        }
        return key;
    }

    // ─────────────────────────────────────────
    // LISTEN-POPUP
    // ─────────────────────────────────────────

    private void OpenDetailPopup(string kategorie)
    {
        activeCategoryForList = kategorie;
        if (detailPopupOverlay != null) detailPopupOverlay.style.display = DisplayStyle.Flex;
        if (detailPopupTitle != null) detailPopupTitle.text = kategorie;

        bool istFest = IstKategorieFest(kategorie);
        if (listCreateNewButton != null)
            listCreateNewButton.style.display = istFest ? DisplayStyle.None : DisplayStyle.Flex;

        RefreshDetailList();
    }

    private void RefreshDetailList()
    {
        if (detailListContainer == null || globalListContainer == null) return;
        detailListContainer.Clear();
        globalListContainer.Clear();

        List<DocumentData> kategorieDocs =
            speicherDaten.savedDocs.FindAll(d => d.category == activeCategoryForList);
        BuildListColumn(kategorieDocs, detailListContainer, isGlobal: false);

        BuildListColumn(speicherDaten.savedDocs, globalListContainer, isGlobal: true);
    }

    private void BuildListColumn(List<DocumentData> docs, VisualElement container, bool isGlobal)
    {
        if (docs.Count == 0)
        {
            Label emptyLabel = new Label(isGlobal ? "Keine Dokumente vorhanden." : "Kategorie ist leer.");
            emptyLabel.AddToClassList("list-empty-hint");
            container.Add(emptyLabel);
            return;
        }

        foreach (var doc in docs)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("list-row-item");

            string icon = doc.istPflichtdokument ? "🔒" : doc.type == "Diagramm" ? "📊" : doc.type == "Checklist" ? "✅" : "📄";
            string displayTitle = doc.title.Split('\n')[0];
            if (displayTitle.Length > 30) displayTitle = displayTitle.Substring(0, 27) + "...";
            string textToShow = isGlobal
                ? $"{icon} [{doc.category}] {displayTitle}"
                : $"{icon} {displayTitle}";

            Label nameLabel = new Label(textToShow);
            nameLabel.AddToClassList("list-row-label");
            row.Add(nameLabel);

            if (!doc.istPflichtdokument)
            {
                string vorschau = BildeInhaltVorschau(doc);
                if (!string.IsNullOrEmpty(vorschau))
                {
                    Label inhaltLabel = new Label(vorschau);
                    inhaltLabel.AddToClassList("list-row-inhalt-preview");
                    row.Add(inhaltLabel);
                }
            }

            VisualElement btnGroup = new VisualElement();
            btnGroup.AddToClassList("row-btn-group");

            if (isGlobal)
            {
                if (doc.category != activeCategoryForList && !doc.istPflichtdokument)
                {
                    Button moveBtn = new Button { text = "Hinzufügen" };
                    moveBtn.AddToClassList("btn-add-global");
                    moveBtn.clicked += () => MoveDocumentToActiveCategory(doc);
                    btnGroup.Add(moveBtn);
                }
            }
            else
            {
                Button editBtn = new Button { text = "Bearbeiten" };
                editBtn.AddToClassList("btn-action-text");
                editBtn.AddToClassList("btn-edit-pen");
                editBtn.tooltip = "Dokument bearbeiten";
                editBtn.clicked += () => OpenEditPopup(doc);
                btnGroup.Add(editBtn);

                if (!doc.istPflichtdokument)
                {
                    Button deleteBtn = new Button { text = "Löschen" };
                    deleteBtn.AddToClassList("btn-action-text");
                    deleteBtn.AddToClassList("btn-minus-delete");
                    deleteBtn.tooltip = "Dokument löschen";
                    deleteBtn.clicked += () => DeleteSingleDocument(doc.id);
                    btnGroup.Add(deleteBtn);
                }
                else
                {
                    Label gesperrtLabel = new Label("Geschützt");
                    gesperrtLabel.AddToClassList("locked-badge-inline");
                    btnGroup.Add(gesperrtLabel);
                }
            }

            row.Add(btnGroup);
            container.Add(row);
        }
    }

    private void MoveDocumentToActiveCategory(DocumentData doc)
    {
        DocumentData docInStorage = speicherDaten.savedDocs.Find(d => d.id == doc.id);
        if (docInStorage == null) return;
        if (docInStorage.istPflichtdokument) return;

        docInStorage.category = activeCategoryForList;
        SaveDataLocally();
        RefreshDetailList();
        SpawnAllCardsAtStart();
    }

    private void DeleteSingleDocument(string docId)
    {
        var doc = speicherDaten.savedDocs.Find(d => d.id == docId);
        if (doc == null || doc.istPflichtdokument) return;

        speicherDaten.savedDocs.RemoveAll(d => d.id == docId);
        SaveDataLocally();
        RefreshDetailList();
        SpawnAllCardsAtStart();
    }

    private void CloseDetailPopup()
    {
        if (detailPopupOverlay != null)
            detailPopupOverlay.style.display = DisplayStyle.None;
    }

    // ─────────────────────────────────────────
    // BEARBEITEN-POPUP
    //
    // Zwei Modi:
    //  - Flexibles Dokument:  Titel + Freitext + Template-Picker
    //  - Pflichtdokument:     Titel (read-only Hinweis) + Strukturfelder,
    //                          kein Template-Picker
    // ─────────────────────────────────────────

    private void OpenEditPopup(DocumentData doc)
    {
        activeDocForEditing = doc;
        selectedEditType = string.IsNullOrEmpty(doc.type) ? "Standard" : doc.type;

        // Checklisten-/Diagramm-Daten des Dokuments in die aktiven Listen
        // laden (Kopie, damit ein Abbrechen nicht die gespeicherten Daten
        // anfasst).
        aktiveChecklistPunkte = doc.checkliste != null
            ? doc.checkliste.Select(p => new ChecklistPunkt { text = p.text, erledigt = p.erledigt }).ToList()
            : new List<ChecklistPunkt>();
        aktiveDiagrammPunkte = doc.diagrammDaten != null
            ? doc.diagrammDaten.Select(p => new DiagrammPunkt { label = p.label, wert = p.wert }).ToList()
            : new List<DiagrammPunkt>();

        if (editPopupOverlay != null)
            editPopupOverlay.style.display = DisplayStyle.Flex;

        if (editLockedHint != null)
            editLockedHint.style.display = doc.istPflichtdokument ? DisplayStyle.Flex : DisplayStyle.None;

        if (editDocNameInput != null)
        {
            editDocNameInput.value = doc.title;
            editDocNameInput.SetEnabled(!doc.istPflichtdokument);

            // Bei Pflichtdokumenten macht Fokus auf das (jetzt gesperrte)
            // Titelfeld keinen Sinn - stattdessen gleich das erste
            // Struktur-Feld fokussieren, wenn vorhanden. Bei freien
            // Dokumenten bleibt das Titelfeld wie gehabt fokussiert.
            if (!doc.istPflichtdokument)
                editDocNameInput.schedule.Execute(() => editDocNameInput.Focus()).ExecuteLater(50);
        }

        bool zeigeStrukturFelder = doc.istPflichtdokument && felderProPflichtDoc.ContainsKey(doc.title);

        if (editTemplateGroup != null)
            editTemplateGroup.style.display = doc.istPflichtdokument ? DisplayStyle.None : DisplayStyle.Flex;

        // Vorlagen-Hilfe-Icon nur bei nicht-Pflichtdokumenten anzeigen
        var vorlagenHelpIcon = editPopupOverlay?.Q<VisualElement>("btn-help-vorlagen-edit");
        if (vorlagenHelpIcon != null)
            vorlagenHelpIcon.style.display = doc.istPflichtdokument ? DisplayStyle.None : DisplayStyle.Flex;
        // Vorlagen-Label-Zeile ebenfalls ausblenden
        var vorlagenLabelZeile = vorlagenHelpIcon?.parent;
        if (vorlagenLabelZeile != null)
            vorlagenLabelZeile.style.display = doc.istPflichtdokument ? DisplayStyle.None : DisplayStyle.Flex;

        var inhaltGroup = editInhaltInput?.parent?.parent; // feld-gruppe-inhalt (äußere Gruppe)
        if (inhaltGroup != null)
            inhaltGroup.style.display = zeigeStrukturFelder ? DisplayStyle.None : DisplayStyle.Flex;

        // Standard-Felder laden: bei neueren Dokumenten aus standardDaten,
        // bei älteren (vor diesem Umbau gespeicherten) Dokumenten landet
        // der komplette alte Freitext ersatzweise im Inhalt-Feld, damit
        // nichts verloren geht.
        string standardTitel = doc.standardDaten?.titel ?? "";
        string standardDatum = doc.standardDaten?.datum ?? "";
        string standardInhalt = doc.standardDaten?.inhalt ?? (doc.standardDaten == null ? (doc.inhalt ?? "") : "");

        LadeFeldMitPlatzhalter(editStandardTitelInput, standardTitel, "z.B. Rechnungsvorlage, Angebot Website-Relaunch, ...");
        LadeFeldMitPlatzhalter(editStandardDatumInput, standardDatum, "TT.MM.JJJJ");
        LadeFeldMitPlatzhalter(editInhaltInput, standardInhalt, "Haupttext des Dokuments...");

        if (editStrukturFelderBox != null)
        {
            editStrukturFelderBox.Clear();
            editStrukturFelderBox.style.display = zeigeStrukturFelder ? DisplayStyle.Flex : DisplayStyle.None;
            aktiveStrukturFelder.Clear();

            if (zeigeStrukturFelder)
            {
                var definitionen = felderProPflichtDoc[doc.title];

                if (doc.strukturFelder == null) doc.strukturFelder = new List<StrukturFeldWert>();

                foreach (var def in definitionen)
                {
                    var bestehenderWert = doc.strukturFelder.FirstOrDefault(f => f.key == def.key);
                    string aktuellerWert = bestehenderWert?.wert ?? "";

                    var feldGroup = new VisualElement();
                    feldGroup.AddToClassList("form-group");

                    var feldLabel = new Label(def.label);
                    feldLabel.AddToClassList("field-label");
                    feldGroup.Add(feldLabel);

                    VisualElement feldInput;

                    switch (def.typ)
                    {
                        case FeldTyp.Dropdown:
                        {
                            var dropdown = new DropdownField();
                            dropdown.choices = def.optionen?.ToList() ?? new List<string>();
                            dropdown.value = !string.IsNullOrEmpty(aktuellerWert) && dropdown.choices.Contains(aktuellerWert)
                                ? aktuellerWert
                                : (dropdown.choices.Count > 0 ? dropdown.choices[0] : "");
                            feldInput = dropdown;
                            break;
                        }
                        case FeldTyp.JaNein:
                        {
                            var toggle = new Toggle { value = aktuellerWert == "Ja" };
                            toggle.AddToClassList("field-checkbox");
                            feldInput = toggle;
                            break;
                        }
                        default:
                        {
                            var textField = new TextField { value = aktuellerWert };
                            feldInput = textField;
                            break;
                        }
                    }

                    feldInput.name = $"struktur-feld-{def.key}";
                    feldGroup.Add(feldInput);

                    editStrukturFelderBox.Add(feldGroup);
                    aktiveStrukturFelder.Add(feldInput);

                    feldInput.userData = def.key;

                    // Platzhalter-Simulation gibt's nur bei normalen
                    // Textfeldern - Dropdown/Checkbox brauchen sowas nicht,
                    // die haben ja schon eine sichtbare Auswahl.
                    if (def.typ == FeldTyp.Text && feldInput is TextField tf && string.IsNullOrEmpty(aktuellerWert))
                        SetzeFeldPlatzhalter(tf, def.placeholder);
                }

                if (doc.istPflichtdokument && aktiveStrukturFelder.Count > 0)
                {
                    var erstesFeld = aktiveStrukturFelder[0];
                    erstesFeld.schedule.Execute(() => erstesFeld.Focus()).ExecuteLater(50);
                }
            }
        }

        MarkiereAusgewaehlteVorlage(btnEditTypeStandard, btnEditTypeDiagramm, btnEditTypeChecklist, selectedEditType);
        AktualisiereTypSichtbarkeit();
        BaueChecklistenUI();
        BaueDiagrammUI();
    }

    private void CloseEditPopup()
    {
        if (editPopupOverlay != null)
            editPopupOverlay.style.display = DisplayStyle.None;
        activeDocForEditing = null;
        aktiveStrukturFelder.Clear();
    }

    private void SelectEditType(string typeName)
    {
        selectedEditType = typeName;
        MarkiereAusgewaehlteVorlage(btnEditTypeStandard, btnEditTypeDiagramm, btnEditTypeChecklist, selectedEditType);
        AktualisiereTypSichtbarkeit();
    }

    // Blendet je nach gewähltem Dokumenttyp den richtigen Bereich ein
    // (Freitext / Checkliste / Diagramm) - vorher gab's dafür GAR KEINE
    // Umschaltung, alle drei Typen zeigten exakt dasselbe Textfeld.
    private void AktualisiereTypSichtbarkeit()
    {
        // FIX: Pflichtdokumente nutzen ausschließlich die Struktur-Felder,
        // niemals das Freitext-/Checklist-/Diagramm-System - unabhängig
        // vom (bei Pflichtdokumenten immer auf "Standard" stehenden)
        // type-Feld. Vorher wurde hier blind nach selectedEditType
        // umgeschaltet, wodurch bei JEDEM Pflichtdokument zusätzlich das
        // leere Freitextfeld neben den Struktur-Feldern auftauchte.
        bool istStrukturDokument = activeDocForEditing != null
            && activeDocForEditing.istPflichtdokument
            && felderProPflichtDoc.ContainsKey(activeDocForEditing.title);

        if (istStrukturDokument)
        {
            if (feldGruppeInhalt != null) feldGruppeInhalt.style.display = DisplayStyle.None;
            if (feldGruppeCheckliste != null) feldGruppeCheckliste.style.display = DisplayStyle.None;
            if (feldGruppeDiagramm != null) feldGruppeDiagramm.style.display = DisplayStyle.None;
            return;
        }

        if (feldGruppeInhalt != null) feldGruppeInhalt.style.display = selectedEditType == "Standard" ? DisplayStyle.Flex : DisplayStyle.None;
        if (feldGruppeCheckliste != null) feldGruppeCheckliste.style.display = selectedEditType == "Checklist" ? DisplayStyle.Flex : DisplayStyle.None;
        if (feldGruppeDiagramm != null) feldGruppeDiagramm.style.display = selectedEditType == "Diagramm" ? DisplayStyle.Flex : DisplayStyle.None;

        if (selectedEditType == "Checklist" && aktiveChecklistPunkte.Count == 0)
        {
            aktiveChecklistPunkte.Add(new ChecklistPunkt { text = "", erledigt = false });
            BaueChecklistenUI();
        }

        if (selectedEditType == "Diagramm" && aktiveDiagrammPunkte.Count == 0)
        {
            aktiveDiagrammPunkte.Add(new DiagrammPunkt { label = "", wert = 0f });
            BaueDiagrammUI();
        }
    }

    // Baut die Checklisten-Zeilen (Checkbox + Text + Löschen) neu auf.
    private void BaueChecklistenUI()
    {
        if (editChecklisteBox == null) return;
        editChecklisteBox.Clear();

        for (int i = 0; i < aktiveChecklistPunkte.Count; i++)
        {
            int index = i; // für die Closures unten
            var punkt = aktiveChecklistPunkte[i];

            var zeile = new VisualElement();
            zeile.style.flexDirection = FlexDirection.Row;
            zeile.style.alignItems = Align.Center;
            zeile.style.marginBottom = 6;

            var toggle = new Toggle { value = punkt.erledigt };
            toggle.RegisterValueChangedCallback(evt => punkt.erledigt = evt.newValue);
            zeile.Add(toggle);

            var textFeld = new TextField { value = punkt.text };
            textFeld.style.flexGrow = 1;
            textFeld.style.marginLeft = 6;
            textFeld.style.marginRight = 6;
            if (string.IsNullOrEmpty(punkt.text)) SetzeFeldPlatzhalter(textFeld, "Was soll erledigt werden?");
            textFeld.RegisterValueChangedCallback(evt => punkt.text = evt.newValue == "Was soll erledigt werden?" ? "" : evt.newValue);
            zeile.Add(textFeld);

            var loeschBtn = new Button(() =>
            {
                aktiveChecklistPunkte.RemoveAt(index);
                BaueChecklistenUI();
            }) { text = "✕" };
            loeschBtn.style.width = 28;
            zeile.Add(loeschBtn);

            editChecklisteBox.Add(zeile);
        }
    }

    // Baut die Diagramm-Datenzeilen (Label + Wert + Löschen) neu auf und
    // aktualisiert die Live-Vorschau darunter.
    private void BaueDiagrammUI()
    {
        if (editDiagrammDatenBox == null) return;
        editDiagrammDatenBox.Clear();

        for (int i = 0; i < aktiveDiagrammPunkte.Count; i++)
        {
            int index = i;
            var punkt = aktiveDiagrammPunkte[i];

            var zeile = new VisualElement();
            zeile.style.flexDirection = FlexDirection.Row;
            zeile.style.alignItems = Align.Center;
            zeile.style.marginBottom = 6;

            var labelFeld = new TextField { value = punkt.label };
            labelFeld.style.flexGrow = 1;
            labelFeld.style.marginRight = 6;
            if (string.IsNullOrEmpty(punkt.label)) SetzeFeldPlatzhalter(labelFeld, "z.B. Januar, Produkt A, ...");
            labelFeld.RegisterValueChangedCallback(evt => { punkt.label = evt.newValue == "z.B. Januar, Produkt A, ..." ? "" : evt.newValue; AktualisiereDiagrammVorschau(); });
            zeile.Add(labelFeld);

            var wertFeld = new TextField { value = punkt.wert.ToString("0.##") };
            wertFeld.style.width = 80;
            wertFeld.style.marginRight = 6;
            if (punkt.wert == 0f && string.IsNullOrEmpty(punkt.label)) SetzeFeldPlatzhalter(wertFeld, "z.B. 100");
            wertFeld.RegisterValueChangedCallback(evt =>
            {
                float.TryParse(evt.newValue.Replace(",", "."), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float wert);
                punkt.wert = wert;
                AktualisiereDiagrammVorschau();
            });
            zeile.Add(wertFeld);

            var loeschBtn = new Button(() =>
            {
                aktiveDiagrammPunkte.RemoveAt(index);
                BaueDiagrammUI();
                AktualisiereDiagrammVorschau();
            }) { text = "✕" };
            loeschBtn.style.width = 28;
            zeile.Add(loeschBtn);

            editDiagrammDatenBox.Add(zeile);
        }

        AktualisiereDiagrammVorschau();
    }

    private void AktualisiereDiagrammVorschau()
    {
        if (diagrammVorschauElement == null) return;
        var daten = new List<(string, float)>();
        foreach (var p in aktiveDiagrammPunkte) daten.Add((p.label, p.wert));
        diagrammVorschauElement.SetzeDaten(daten);
    }

    // Echte Platzhalter-Simulation für die Struktur-Felder (gleiches Muster
    // wie SetupPlaceholderSimulation im Kassenbuch) - UI Toolkit TextFields
    // haben in dieser Unity-Version kein natives Platzhalter-Verhalten.
    // Für wiederverwendete (persistente) Felder wie die drei Standard-
    // Felder: registriert die Platzhalter-Events nur EIN einziges Mal
    // (im Gegensatz zu SetzeFeldPlatzhalter, das für frisch erzeugte
    // Felder gedacht ist und bei jedem Aufruf neu registriert).
    // Lädt einen Wert in ein persistentes Feld: echten Wert wenn vorhanden,
    // sonst grauen Platzhalter (konsistent mit RegistrierePersistentenPlatzhalter).
    private void LadeFeldMitPlatzhalter(TextField field, string wert, string placeholder)
    {
        if (field == null) return;
        if (!string.IsNullOrEmpty(wert))
        {
            field.SetValueWithoutNotify(wert);
            field.style.color = new StyleColor(StyleKeyword.Null);
        }
        else
        {
            field.SetValueWithoutNotify(placeholder);
            field.style.color = new StyleColor(new Color(140f / 255f, 140f / 255f, 140f / 255f));
        }
    }

    private void RegistrierePersistentenPlatzhalter(TextField field, string placeholder)
    {
        if (field == null) return;

        var platzhalterFarbe = new StyleColor(new Color(140f / 255f, 140f / 255f, 140f / 255f));

        field.RegisterCallback<FocusInEvent>(_ =>
        {
            if (field.value == placeholder)
            {
                field.SetValueWithoutNotify("");
                field.style.color = new StyleColor(StyleKeyword.Null);
            }
        });

        field.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (string.IsNullOrEmpty(field.value))
            {
                field.SetValueWithoutNotify(placeholder);
                field.style.color = platzhalterFarbe;
            }
        });
    }

    private void SetzeFeldPlatzhalter(TextField field, string placeholder)
    {
        if (field == null || string.IsNullOrEmpty(placeholder)) return;

        var platzhalterFarbe = new StyleColor(new Color(140f / 255f, 140f / 255f, 140f / 255f));

        field.SetValueWithoutNotify(placeholder);
        field.style.color = platzhalterFarbe;

        field.RegisterCallback<FocusInEvent>(_ =>
        {
            if (field.value == placeholder)
            {
                field.SetValueWithoutNotify("");
                field.style.color = new StyleColor(StyleKeyword.Null);
            }
        });

        field.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (string.IsNullOrEmpty(field.value))
            {
                field.SetValueWithoutNotify(placeholder);
                field.style.color = platzhalterFarbe;
            }
        });
    }

    private void SaveEditedDocumentEntry()
    {
        if (activeDocForEditing == null) return;

        string updatedTitle = (editDocNameInput != null && !string.IsNullOrEmpty(editDocNameInput.value))
            ? editDocNameInput.value
            : "Unbenannt";

        DocumentData docInList = speicherDaten.savedDocs.Find(d => d.id == activeDocForEditing.id);
        if (docInList != null)
        {
            // Sicherheitsnetz: Titel von Pflichtdokumenten wird NIE
            // überschrieben, egal was im (eigentlich gesperrten) Feld
            // steht - alle Felder/Erfolge/Gründerpfad-Automatik hängen am
            // exakten Titel-String.
            if (!docInList.istPflichtdokument)
                docInList.title = updatedTitle;

            bool hatStrukturFelder = docInList.istPflichtdokument && felderProPflichtDoc.ContainsKey(docInList.title);

            if (hatStrukturFelder)
            {
                if (docInList.strukturFelder == null) docInList.strukturFelder = new List<StrukturFeldWert>();

                var definitionen = felderProPflichtDoc.ContainsKey(docInList.title)
                    ? felderProPflichtDoc[docInList.title]
                    : new List<FeldDefinition>();

                foreach (var feldInput in aktiveStrukturFelder)
                {
                    string key = feldInput.userData as string;
                    if (key == null) continue;

                    // Wert je nach Steuerelement-Typ auslesen - TextField,
                    // DropdownField und Toggle haben kein gemeinsames
                    // ".value" auf VisualElement-Ebene.
                    string wert;
                    if (feldInput is Toggle toggleFeld)
                    {
                        wert = toggleFeld.value ? "Ja" : "Nein";
                    }
                    else if (feldInput is DropdownField dropdownFeld)
                    {
                        wert = dropdownFeld.value ?? "";
                    }
                    else if (feldInput is TextField textFeld)
                    {
                        // Falls das Feld noch den grauen Platzhaltertext
                        // zeigt (nie angeklickt oder wieder leer verlassen),
                        // zählt das als "nichts eingegeben" - sonst würde
                        // der Hinweistext selbst als Wert gespeichert werden.
                        string platzhalter = definitionen.FirstOrDefault(d => d.key == key)?.placeholder;
                        wert = (platzhalter != null && textFeld.value == platzhalter) ? "" : textFeld.value;
                    }
                    else
                    {
                        wert = "";
                    }

                    var bestehenderEintrag = docInList.strukturFelder.FirstOrDefault(f => f.key == key);
                    if (bestehenderEintrag != null)
                        bestehenderEintrag.wert = wert;
                    else
                        docInList.strukturFelder.Add(new StrukturFeldWert { key = key, wert = wert });
                }
            }
            else
            {
                docInList.type = selectedEditType;

                // Nur die zum gewählten Typ passenden Daten speichern -
                // verhindert verwaiste alte Daten, falls jemand den Typ
                // eines Dokuments nachträglich wechselt.
                if (selectedEditType == "Checklist")
                {
                    docInList.checkliste = aktiveChecklistPunkte
                        .Where(p => !string.IsNullOrWhiteSpace(p.text))
                        .Select(p => new ChecklistPunkt { text = p.text, erledigt = p.erledigt })
                        .ToList();
                    docInList.diagrammDaten = null;
                    docInList.inhalt = "";
                }
                else if (selectedEditType == "Diagramm")
                {
                    docInList.diagrammDaten = aktiveDiagrammPunkte
                        .Where(p => !string.IsNullOrWhiteSpace(p.label))
                        .Select(p => new DiagrammPunkt { label = p.label, wert = p.wert })
                        .ToList();
                    docInList.checkliste = null;
                    docInList.inhalt = "";
                }
                else
                {
                    string HoleWert(TextField feld, string platzhalter) =>
                        feld != null && feld.value != platzhalter ? feld.value.Trim() : "";

                    string titel = HoleWert(editStandardTitelInput, "z.B. Rechnungsvorlage, Angebot Website-Relaunch, ...");
                    string datum = HoleWert(editStandardDatumInput, "TT.MM.JJJJ");
                    string inhalt = HoleWert(editInhaltInput, "Haupttext des Dokuments...");

                    docInList.standardDaten = new StandardDokumentDaten { titel = titel, datum = datum, inhalt = inhalt };

                    // Für Vorschau/Export weiterhin einen zusammengesetzten
                    // Text ablegen (Rückwärtskompatibilität mit allem, was
                    // bisher nur doc.inhalt kennt).
                    var teile = new List<string>();
                    if (!string.IsNullOrEmpty(titel)) teile.Add(titel);
                    if (!string.IsNullOrEmpty(datum)) teile.Add("Datum: " + datum);
                    if (!string.IsNullOrEmpty(inhalt)) teile.Add(inhalt);
                    docInList.inhalt = string.Join("\n\n", teile);

                    docInList.checkliste = null;
                    docInList.diagrammDaten = null;
                }
            }
        }

        docInList.datum = System.DateTime.Now.ToString("yyyy-MM-dd");
        SaveDataLocally();
        RefreshDetailList();
        SpawnAllCardsAtStart();
        CloseEditPopup();
    }

    // ─────────────────────────────────────────
    // LÖSCH-BESTÄTIGUNG ("Alle löschen")
    // ─────────────────────────────────────────

    private void OpenDeleteConfirmPopup()
    {
        int anzahlGeschuetzt = speicherDaten.savedDocs.Count(d => d.istPflichtdokument);
        int anzahlLoeschbar = speicherDaten.savedDocs.Count(d => !d.istPflichtdokument);

        if (deleteConfirmHint != null)
        {
            deleteConfirmHint.text = anzahlGeschuetzt > 0
                ? $"{anzahlLoeschbar} Dokument(e) werden gelöscht. {anzahlGeschuetzt} geschützte Pflichtdokument(e) (🔒) bleiben erhalten."
                : $"{anzahlLoeschbar} Dokument(e) werden unwiderruflich gelöscht.";
        }

        if (deleteConfirmOverlay != null)
            deleteConfirmOverlay.style.display = DisplayStyle.Flex;
    }

    private void CloseDeleteConfirmPopup()
    {
        if (deleteConfirmOverlay != null)
            deleteConfirmOverlay.style.display = DisplayStyle.None;
    }

    private void ConfirmDeleteAllDocuments()
    {
        speicherDaten.savedDocs.RemoveAll(d => !d.istPflichtdokument);
        SaveDataLocally();
        SpawnAllCardsAtStart();
        CloseDeleteConfirmPopup();
    }

    // ─────────────────────────────────────────
    // SPEICHERVERWALTUNG
    // ─────────────────────────────────────────

    private void SaveDataLocally()
    {
        try
        {
            string directoryPath = Path.GetDirectoryName(saveFilePath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string json = JsonUtility.ToJson(speicherDaten, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[DocumentDashboard] Fehler beim Speichern der Dokumentdaten: " + exception.Message);
        }
    }

    private void LoadDataLocally()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                DocumentSaveData loadedData = JsonUtility.FromJson<DocumentSaveData>(json);

                if (loadedData != null && loadedData.savedDocs != null)
                {
                    speicherDaten = loadedData;
                }
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[DocumentDashboard] Fehler beim Laden der Dokumentdaten: " + exception.Message);
            speicherDaten = new DocumentSaveData();
        }
    }

    // ─────────────────────────────────────────
    // STATISCHER ZUGRIFF FÜR EXPORT-SCREEN
    // ─────────────────────────────────────────

    // ============================================================
// BENUTZERBEZOGENER DOKUMENT-SPEICHER
// ============================================================

private static string GetCurrentUserFolder()
{
    try
    {
        // Der aktuell eingeloggte Benutzer wird über den StateManager
        // bestimmt. Das entspricht eurem bestehenden Login-System.
        PassKeyRecord currentUser = StateManager.Instance?.getCurrentUser();

        if (currentUser == null || string.IsNullOrWhiteSpace(currentUser.userId))
        {
            Debug.LogError(
                "[DocumentDashboard] Kein eingeloggter Benutzer gefunden."
            );

            return null;
        }

        // user_1 -> 1
        string rawUserId = currentUser.userId;

        if (rawUserId.StartsWith("user_"))
            rawUserId = rawUserId.Substring("user_".Length);

        // Sicherheitsprüfung:
        // Nur eine numerische User-ID darf als Ordnername verwendet werden.
        if (!int.TryParse(rawUserId, out int userId))
        {
            Debug.LogError(
                "[DocumentDashboard] Ungültige User-ID: " +
                currentUser.userId
            );

            return null;
        }

        string folderPath = Path.Combine(
            Application.persistentDataPath,
            "Dokumente",
            "User_" + userId
        );

        Directory.CreateDirectory(folderPath);

        return folderPath;
    }
    catch (System.Exception exception)
    {
        Debug.LogError(
            "[DocumentDashboard] Fehler beim Ermitteln des Benutzerordners: " +
            exception.Message
        );

        return null;
    }
}

public static string GetSaveFilePath()
{
    string userFolder = GetCurrentUserFolder();

    if (string.IsNullOrEmpty(userFolder))
        return null;

    return Path.Combine(
        userFolder,
        "MyDashboardSave.json"
    );
}

public static DocumentSaveData GetSavedDocuments()
{
    string path = GetSaveFilePath();

    if (string.IsNullOrEmpty(path))
        return new DocumentSaveData();

    try
    {
        if (!File.Exists(path))
            return new DocumentSaveData();

        string json = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(json))
            return new DocumentSaveData();

        DocumentSaveData data =
            JsonUtility.FromJson<DocumentSaveData>(json);

        if (data == null || data.savedDocs == null)
            return new DocumentSaveData();

        return data;
    }
    catch (System.Exception exception)
    {
        Debug.LogError(
            "[DocumentDashboard] Fehler beim Laden der Dokumentdaten: " +
            exception.Message
        );

        return new DocumentSaveData();
    }
}

    // Liefert die Unternehmensstammdaten als Key-Value-Dictionary.
    // Verwendung: var daten = DocumentDashboard.GetUnternehmenFelder();
    //             string name = daten.GetValueOrDefault("firmenname", "");
    public static Dictionary<string, string> GetUnternehmenFelder()
    {
        var ergebnis = new Dictionary<string, string>();
        var alle = GetSavedDocuments();

        var doc = alle.savedDocs.FirstOrDefault(d =>
            d.category == "Gründung" && d.title == "Unternehmensstammdaten");

        if (doc?.strukturFelder != null)
        {
            foreach (var feld in doc.strukturFelder)
                ergebnis[feld.key] = feld.wert;
        }

        return ergebnis;
    }

    // Liefert Kontodaten (IBAN/BIC) als Key-Value-Dictionary.
    // Verwendung: var konto = DocumentDashboard.GetKontodatenFelder();
    //             string iban = konto.GetValueOrDefault("iban", "");
    public static Dictionary<string, string> GetKontodatenFelder()
    {
        var ergebnis = new Dictionary<string, string>();
        var alle = GetSavedDocuments();

        var kontoDoc = alle.savedDocs.FirstOrDefault(d =>
            d.category == "Bezahlweise" && d.title == "Kontodaten (IBAN/BIC)");

        if (kontoDoc?.strukturFelder != null)
        {
            foreach (var feld in kontoDoc.strukturFelder)
                ergebnis[feld.key] = feld.wert;
        }

        return ergebnis;
    }

    // Bleibt für Rückwärtskompatibilität erhalten.
    public static List<DocumentData> GetBezahlweiseDaten()
    {
        var alle = GetSavedDocuments();
        return alle.savedDocs.FindAll(d => d.category == "Bezahlweise");
    }

    // Gibt den inhalt eines Bezahlweise-Dokuments anhand des Titels zurück.
    public static string GetBezahlweiseInhalt(string titel)
    {
        var alle = GetSavedDocuments();
        var doc = alle.savedDocs.FirstOrDefault(d =>
            d.category == "Bezahlweise" && d.title == titel);
        if (doc == null) return "";

        // FIX: Las bisher nur doc.inhalt - AGB/Disclaimer/Barzahlung/
        // Überweisung sind aber längst strukturierte Dokumente (siehe
        // felderProPflichtDoc), ihre Daten liegen in strukturFelder, nicht
        // in .inhalt. .inhalt ist bei denen praktisch immer leer -
        // die Vorschau in den Einstellungen zeigte deshalb nie etwas an,
        // egal was im Dok-Pool eingetragen wurde.
        if (!string.IsNullOrWhiteSpace(doc.inhalt)) return doc.inhalt;

        if (doc.strukturFelder != null && doc.strukturFelder.Count > 0)
        {
            var ausgefuellte = doc.strukturFelder.Where(f => !string.IsNullOrWhiteSpace(f.wert)).ToList();
            if (ausgefuellte.Count > 0)
                return string.Join(" · ", ausgefuellte.Select(f => f.wert));
        }

        return "";
    }

    // Prüft, ob für ein Bezahlweise-Dokument überhaupt Daten hinterlegt
    // sind (egal ob Freitext oder Struktur-Felder) - für den Status
    // "hinterlegt"/"nicht hinterlegt" in den Einstellungen.
    public static bool HatBezahlweiseInhalt(string titel)
    {
        return !string.IsNullOrWhiteSpace(GetBezahlweiseInhalt(titel));
    }

    // ─────────────────────────────────────────
    // VORLAGENAUSWAHL VISUELL MARKIEREN
    // ─────────────────────────────────────────
    private void MarkiereAusgewaehlteVorlage(Button standard, Button diagramm, Button checklist, string aktiverTyp)
    {
        standard?.RemoveFromClassList("selected-template");
        diagramm?.RemoveFromClassList("selected-template");
        checklist?.RemoveFromClassList("selected-template");

        switch (aktiverTyp)
        {
            case "Standard": standard?.AddToClassList("selected-template"); break;
            case "Diagramm": diagramm?.AddToClassList("selected-template"); break;
            case "Checklist": checklist?.AddToClassList("selected-template"); break;
        }
    }

    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Hier verwaltest du alle deine Dokumente. " +
            "Feste Kategorien (Gründung, Bezahlweise) sind geschützt. " +
            "Eigene Kategorien und Dokumente kannst du frei anlegen.");

        HelpTooltip.Registriere(root, "btn-help-hinzufuegen",
            "Legt ein neues Dokument an. Du wählst Titel, Kategorie und optional " +
            "eine Vorlage - das Dokument erscheint danach in der passenden Kategorie-Karte.");

        HelpTooltip.Registriere(root, "btn-help-alle-loeschen",
            "Löscht alle selbst erstellten Dokumente endgültig. " +
            "Pflichtdokumente bleiben erhalten. " +
            "Diese Aktion kann nicht rükgängig gemacht werden.");

        HelpTooltip.Registriere(root, "btn-help-popup-erstellen",
            "Lege ein neues Dokument an. " +
            "Gib einen Titel ein, wähle eine Kategorie und eine Vorlage. " +
            "Das Dokument erscheint danach in der gewählten Kategorie.");

        HelpTooltip.Registriere(root, "btn-help-vorlage",
            "Standard: Freitextdokument.\n" +
            "Diagramm: Strukturiertes Dokument.\n" +
            "Checklist: Abhakbare Liste.");

        HelpTooltip.Registriere(root, "btn-help-detail-liste",
            "Links: alle Dokumente dieser Kategorie. " +
            "Rechts: globale Liste aller Dokumente. " +
            "Nicht-Pflichtdokumente können per \"Hinzufügen\" in diese Kategorie verschoben werden.");

        HelpTooltip.Registriere(root, "btn-help-popup-bearbeiten",
            "Bearbeite Titel, Inhalt und Typ des Dokuments. " +
            "Pflichtdokumente haben strukturierte Felder " +
            "und können nicht gelöscht werden.");

        HelpTooltip.Registriere(root, "btn-help-strukturfelder",
            "Vordefinierte Felder für Pflichtdokumente (z.\u00a0B. IBAN, Firmenname). " +
            "Die Daten werden automatisch in Rechnungen und Angeboten verwendet.");

        HelpTooltip.Registriere(root, "btn-help-vorlagen-edit",
            "Standard: Freitext. Diagramm: Strukturiert. Checklist: Abhakbar. " +
            "Der Typ beeinflusst das Layout, nicht den Inhalt.");

        HelpTooltip.Registriere(root, "btn-help-popup-loeschen",
            "Löscht alle nicht geschützten Dokumente endgültig. " +
            "Pflichtdokumente bleiben erhalten. " +
            "Nicht rükgängig machbar.");
    }

}