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
    private List<TextField> aktiveStrukturFelder = new List<TextField>();

    // ============================================================
    // FELD-DEFINITION – ein einzelnes strukturiertes Eingabefeld
    // ============================================================
    private class FeldDefinition
    {
        public string key;         // interner Schlüssel, z.B. "iban"
        public string label;       // Anzeigename, z.B. "IBAN"
        public string placeholder; // Platzhaltertext im Feld
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
        new KategorieDefinition { name = "Recht & Steuern",        istFest = false, pflichtDocs = new List<string> { "Datenschutzerklärung (DSGVO)", "Steuernummer-Bescheid / USt-IdNr", "Impressum", "Copyright Hinweis", "Lizenzhinweis Einfach" } },
        new KategorieDefinition { name = "Marketing & Personal",   istFest = false, pflichtDocs = new List<string> { "Dienstleistungskatalog / Preisliste", "Corporate Identity Manual", "Muster-Arbeitsvertrag" } },
        new KategorieDefinition { name = "Strategie & Planung",    istFest = true,  pflichtDocs = new List<string> { "Businessplan", "Markt- & Wettbewerbsanalyse" } },
        new KategorieDefinition { name = "Vorlagen & Checklisten", istFest = false, pflichtDocs = new List<string> { "Gründungs-Checkliste", "Inventarliste", "Inventur" } },
        new KategorieDefinition { name = "Sonstiges",              istFest = false, pflichtDocs = new List<string>() },
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
            new FeldDefinition { key = "kleinunternehmer",   label = "Kleinunternehmer-Regelung",    placeholder = "Ja/Nein (Umsatz < 22.000 € im ersten Jahr)" },
        },
            ["Gewerbeanmeldung"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "behoerde",         label = "Zuständige Behörde",   placeholder = "Name des örtlichen Gewerbeamts (z.B. Stadt Mittweida)" },
            new FeldDefinition { key = "beginnTaetigkeit", label = "Beginn der Tätigkeit", placeholder = "Datum des tatsächlichen Starts (auch rückwirkend möglich)" },
            new FeldDefinition { key = "anzahlMitarbeiter",label = "Zahl der Mitarbeiter", placeholder = "Anzahl der Angestellten zum Start (ohne Inhaber)" },
            new FeldDefinition { key = "nebenerwerb",      label = "Nebenerwerb",          placeholder = "„Ja“, wenn du noch hauptberuflich angestellt bist" },
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
            new FeldDefinition { key = "ausfuehrung",         label = "Ausführung",         placeholder = "Einfache Ausführung / Zweifache Ausführung" },
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
            new FeldDefinition { key = "wertersatzklausel",label = "Wertersatz-Klausel",   placeholder = "Pflicht zur Zahlung bereits erbrachter Leistungen (Ja/Nein)" },
            new FeldDefinition { key = "kontakt",          label = "Kontakt für Widerruf", placeholder = "E-Mail-Adresse oder Postanschrift deiner Firma" },
            new FeldDefinition { key = "vorzeitigesErloeschen", label = "Vorzeitiges Erlöschen", placeholder = "Hinweis auf Erlöschen bei vollständiger Ausführung (Ja/Nein)" },
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
            new FeldDefinition { key = "referenzklausel",       label = "Referenzklausel",        placeholder = "Recht zur Nutzung als Referenz aktiv bewerben? (Ja/Nein)" },
            new FeldDefinition { key = "schadensersatzfaktor",  label = "Schadensersatzfaktor",   placeholder = "Faktor bei Verstößen (Vorgabe: 3-fache Höhe)" },
            new FeldDefinition { key = "zusatzangabe",          label = "Zusatzangabe",           placeholder = "Optionale Einschränkungen (z.B. „Anonymisierte Referenz auf Wunsch möglich“)" },
        },
            ["Lizenzhinweis Einfach"] = new List<FeldDefinition>
        {
            new FeldDefinition { key = "zusatzangabe",              label = "Zusatzangabe",                  placeholder = "Beschreibung der erweiterten Rechte (z.B. „Inklusive Recht zur Bearbeitung und Dekompilierung des Quellcodes“)" },
            new FeldDefinition { key = "kontakt",                   label = "Kontakt",                       placeholder = "E-Mail oder Ansprechpartner für Rückfragen (z.B. support@deinefirma.de)" },
            new FeldDefinition { key = "preisErweiterteNutzung",    label = "Preis für erweiterte Nutzung",  placeholder = "Betrag in Euro" },
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
        // Zeitpunkt der letzten Änderung (yyyy-MM-dd) - für die Sortierung
        // im Export-Screen. Bei alten, bereits gespeicherten Dokumenten
        // leer, bis sie das nächste Mal bearbeitet werden.
        public string datum;
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
    }

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
        if (gridContainer == null || categoryCardTemplate == null) return;
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
        selectedEditType = doc.type;

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

        var inhaltGroup = editInhaltInput?.parent;
        if (inhaltGroup != null)
            inhaltGroup.style.display = zeigeStrukturFelder ? DisplayStyle.None : DisplayStyle.Flex;
        if (editInhaltInput != null)
            editInhaltInput.value = doc.inhalt ?? "";

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

                    var feldInput = new TextField { value = aktuellerWert };
                    feldInput.name = $"struktur-feld-{def.key}";
                    feldGroup.Add(feldInput);

                    editStrukturFelderBox.Add(feldGroup);
                    aktiveStrukturFelder.Add(feldInput);

                    feldInput.userData = def.key;

                    // FIX: Hinweistext war bisher nur als natives .tooltip
                    // gesetzt - das erfordert Hovern und rendert im Build
                    // ohnehin oft nicht zuverlässig (gleiches Problem wie
                    // beim Kalender-Tooltip). Jetzt echte Platzhalter-
                    // Simulation: grauer Hinweistext direkt im leeren Feld,
                    // verschwindet beim Fokussieren, kommt beim Verlassen
                    // zurück falls das Feld leer bleibt.
                    if (string.IsNullOrEmpty(aktuellerWert))
                        SetzeFeldPlatzhalter(feldInput, def.placeholder);
                }

                if (doc.istPflichtdokument && aktiveStrukturFelder.Count > 0)
                {
                    var erstesFeld = aktiveStrukturFelder[0];
                    erstesFeld.schedule.Execute(() => erstesFeld.Focus()).ExecuteLater(50);
                }
            }
        }

        MarkiereAusgewaehlteVorlage(btnEditTypeStandard, btnEditTypeDiagramm, btnEditTypeChecklist, selectedEditType);
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
    }

    // Echte Platzhalter-Simulation für die Struktur-Felder (gleiches Muster
    // wie SetupPlaceholderSimulation im Kassenbuch) - UI Toolkit TextFields
    // haben in dieser Unity-Version kein natives Platzhalter-Verhalten.
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

                    // Falls das Feld noch den grauen Platzhaltertext zeigt
                    // (nie angeklickt oder wieder leer verlassen), zählt das
                    // als "nichts eingegeben" - sonst würde der Hinweistext
                    // selbst als Wert gespeichert werden.
                    string platzhalter = definitionen.FirstOrDefault(d => d.key == key)?.placeholder;
                    string wert = (platzhalter != null && feldInput.value == platzhalter) ? "" : feldInput.value;

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
                docInList.inhalt = editInhaltInput != null ? editInhaltInput.value : "";
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

    public static string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "MyDashboardSave.json");
    }

    public static DocumentSaveData GetSavedDocuments()
    {
        string path = GetSaveFilePath();
        if (!File.Exists(path)) return new DocumentSaveData();
        return JsonUtility.FromJson<DocumentSaveData>(File.ReadAllText(path));
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
        return doc?.inhalt ?? "";
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