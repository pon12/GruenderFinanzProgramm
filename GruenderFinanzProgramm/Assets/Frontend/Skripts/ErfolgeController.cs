// ================================================================
// ErfolgeController.cs
//
// Zeigt Errungenschaften & Meilensteine in Kategorien an.
// Daten kommen aus:
//   - DocumentDashboard.GetSavedDocuments()  (Dokumente)
//   - UserDatabaseAccess (Kunden, Angebote, Rechnungen)
//   - GruendungspfadController-Saves (Schritte erledigt)
//
// EINRICHTUNG IN UNITY:
//   1. Neues GameObject in der Erfolge-Scene
//   2. Dieses Script + UIDocument drauf
//   3. UIDocument im Inspector zuweisen
// ================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class ErfolgeController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    // ============================================================
    // DATENMODELL
    // ============================================================

    public enum ErfolgTyp { Einmalig, Stackable }

    // Stufen-System für Stackable-Erfolge: Bronze/Silber/Gold/Platin.
    // Index 0 = 1. Stufe (Bronze) usw. - jeder Stackable-Erfolg soll genau
    // 4 Stufen haben, damit das Schema überall gleich aussieht.
    private static readonly (string name, string icon, Color farbe)[] Stufennamen =
    {
        ("Bronze", "\U0001f949", new Color(205f / 255f, 127f / 255f,  50f / 255f)), // 🥉
        ("Silber", "\U0001f948", new Color(192f / 255f, 192f / 255f, 192f / 255f)), // 🥈
        ("Gold",   "\U0001f947", new Color(255f / 255f, 215f / 255f,   0f / 255f)), // 🥇
        ("Platin", "\U0001f48e", new Color(180f / 255f, 220f / 255f, 255f / 255f)), // 💎
    };

    public class Erfolg
    {
        public string  id;
        public string  titel;
        public string  beschreibung;
        public string  icon;          // Emoji
        public ErfolgTyp typ;

        // Einmalig: wird per Check-Funktion gesetzt
        public bool    erledigt;

        // Stackable
        public int     aktuellerWert;
        public int[]   stufen;        // z.B. {1, 10, 100}
        public int     aktuellerStufenIndex; // welche Stufe gerade erreicht
    }

    public class ErfolgsKategorie
    {
        public string       name;
        public string       icon;
        public List<Erfolg> erfolge;
    }

    // ============================================================
    // KATEGORIEN & ERFOLGE DEFINITION
    // ============================================================

    private List<ErfolgsKategorie> kategorien;

    private void InitKategorien()
    {
        kategorien = new List<ErfolgsKategorie>
        {
            new ErfolgsKategorie
            {
                name = "Gründung", icon = "🏢",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_stammdaten",    titel = "Stammdaten hinterlegt",      beschreibung = "Unternehmensstammdaten ausgefüllt",               icon = "📋", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_gewerbe",       titel = "Gewerbe angemeldet",          beschreibung = "Gewerbeanmeldung im Dok-Pool hinterlegt",         icon = "🏛", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_handelsreg",    titel = "Handelsregistereintrag",      beschreibung = "Handelsregisterauszug hochgeladen",               icon = "📜", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_businessplan",  titel = "Businessplan erstellt",       beschreibung = "Businessplan im Dok-Pool hinterlegt",             icon = "📊", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_marktanalyse",  titel = "Marktanalyse durchgeführt",   beschreibung = "Markt- & Wettbewerbsanalyse hinterlegt",          icon = "🔍", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_gesellschaft",  titel = "Gesellschaft gegründet",      beschreibung = "Gründungsurkunde oder Gesellschaftsvertrag hinterlegt", icon = "🤝", typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Finanzen", icon = "💰",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_konto",         titel = "Geschäftskonto eröffnet",     beschreibung = "Kontodaten im Dok-Pool hinterlegt",               icon = "🏦", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_bilanz",        titel = "Eröffnungsbilanz erstellt",   beschreibung = "Echte Kapitalstruktur im Kapitalbedarf-Panel aufgebaut",  icon = "📈", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_umsatz_1k",     titel = "Erster Umsatz",               beschreibung = "Kassenbuch-Umsatz: €1k / €10k / €100k / €1 Mio.",  icon = "💵", typ = ErfolgTyp.Stackable, stufen = new[] { 1000, 10000, 100000, 1000000 } },
                    new Erfolg { id = "e_kontostand",    titel = "Kontostand aktuell",          beschreibung = "Kassenbuch-Kontostand hinterlegt",                icon = "💳", typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Buchhaltung", icon = "📂",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_kunden",        titel = "Kunden hinterlegt",           beschreibung = "1 / 5 / 10 / 100 Kunden im System",             icon = "👥", typ = ErfolgTyp.Stackable, stufen = new[] { 1, 5, 10, 100 } },
                    new Erfolg { id = "e_angebote",      titel = "Angebote erstellt",           beschreibung = "1 / 10 / 50 / 200 Angebote erstellt",            icon = "📄", typ = ErfolgTyp.Stackable, stufen = new[] { 1, 10, 50, 200 } },
                    new Erfolg { id = "e_rechnungen",    titel = "Rechnungen gestellt",         beschreibung = "1 / 25 / 100 / 500 Rechnungen gestellt",         icon = "🧾", typ = ErfolgTyp.Stackable, stufen = new[] { 1, 25, 100, 500 } },
                    new Erfolg { id = "e_export",        titel = "Erste Rechnung exportiert",   beschreibung = "Eine Rechnung als PDF exportiert",               icon = "📤", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_angebot_angenommen", titel = "Erstes angenommenes Angebot", beschreibung = "Ein Angebot mit Status \"Angenommen\"",      icon = "🤝", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_rechnung_bezahlt",   titel = "Erste bezahlte Rechnung",     beschreibung = "Eine Rechnung mit Status \"Bezahlt\"",       icon = "💶", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_perfekte_quote",     titel = "Perfekte Quote",              beschreibung = "Mind. 5 Angebote erstellt, alle angenommen", icon = "🎯", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_kassenbuch_fleiss",  titel = "Buchhaltungs-Fleiß",          beschreibung = "10 / 50 / 100 / 500 Kassenbuch-Einträge",    icon = "📚", typ = ErfolgTyp.Stackable, stufen = new[] { 10, 50, 100, 500 } },
                }
            },
            new ErfolgsKategorie
            {
                name = "Dokumente", icon = "🗂",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_agb",           titel = "AGBs erstellt",               beschreibung = "AGB im Dok-Pool hinterlegt",                     icon = "📑", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_dsgvo",         titel = "DSGVO-konform",               beschreibung = "Datenschutzerklärung hinterlegt",                icon = "🔒", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_impressum",     titel = "Impressum erstellt",          beschreibung = "Impressum im Dok-Pool hinterlegt",               icon = "📋", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_alle_dok",      titel = "Alle Dokumente vollständig",  beschreibung = "Alle Pflichtdokumente ausgefüllt",               icon = "✅", typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Gründerpfad", icon = "🗺",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_pfad_vorbereitung", titel = "Vorbereitung abgeschlossen", beschreibung = "Alle Schritte der Vorbereitung erledigt",     icon = "📋", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_pfad_anmeldung",    titel = "Anmeldung abgeschlossen",    beschreibung = "Alle Anmeldeschritte erledigt",               icon = "📝", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_pfad_finanzen",     titel = "Finanzen abgeschlossen",     beschreibung = "Alle Finanzschritte erledigt",                icon = "💰", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_pfad_betrieb",      titel = "Betrieb gestartet",          beschreibung = "Alle Betriebsschritte erledigt",              icon = "🚀", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_pfad_komplett",     titel = "Gründerpfad komplett",       beschreibung = "Alle 21 Schritte des Gründerpfads erledigt", icon = "🏆", typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Finanzplanung", icon = "📊",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "fp_rohgewinn",        titel = "Erster Rohgewinn erzielt",     beschreibung = "Rohgewinn (Einnahmen - direkte Kosten) ist positiv", icon = "📈", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "fp_rohgewinn_stufen", titel = "Rohgewinn-Meilensteine",       beschreibung = "€1k / €10k / €50k / €100k Rohgewinn erreicht",       icon = "💹", typ = ErfolgTyp.Stackable, stufen = new[] { 1000, 10000, 50000, 100000 } },
                    new Erfolg { id = "fp_investition",      titel = "Erste Investition getätigt",   beschreibung = "Büroausstattung, Fuhrpark, Maschinen oder Software gebucht", icon = "🏗", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "fp_gruenderkosten",   titel = "Erste Gründerkosten gebucht",  beschreibung = "Corporate Design, Homepage oder Grundausstattung gebucht", icon = "🧾", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "fp_kapitalbedarf_gedeckt", titel = "Kapitalbedarf gedeckt",   beschreibung = "Verfügbares Kapital deckt den berechneten Kapitalbedarf", icon = "✅", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "fp_liq_reserve",      titel = "Liquiditätsreserve aufgebaut", beschreibung = "€1k / €10k / €50k / €100k Liquiditätsreserve",       icon = "🛟", typ = ErfolgTyp.Stackable, stufen = new[] { 1000, 10000, 50000, 100000 } },
                    new Erfolg { id = "fp_fremdkapital",     titel = "Fremdkapital gesichert",       beschreibung = "Kredit oder Darlehen im Kassenbuch gebucht",         icon = "🏦", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "fp_eigenkapital",     titel = "Eigenkapital eingebracht",     beschreibung = "Sacheinlage oder Privateinzahlung gebucht",          icon = "💼", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "fp_krypto",           titel = "Krypto-Investor",              beschreibung = "Kategorie \"Börse / Krypto\" im Kassenbuch genutzt",  icon = "🪙", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "fp_kostenbewusst",    titel = "Kostenbewusst",                beschreibung = "Betriebsausgaben unter 20 % des Gesamtumsatzes",     icon = "🧠", typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Weitere", icon = "⭐",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_erster_login",  titel = "Erste Schritte",              beschreibung = "Ventoriq zum ersten Mal gestartet",              icon = "👋", typ = ErfolgTyp.Einmalig, erledigt = true },
                    new Erfolg { id = "e_ci",            titel = "Corporate Identity",          beschreibung = "CI Manual im Dok-Pool hinterlegt",               icon = "🎨", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_team",          titel = "Team aufgebaut",              beschreibung = "Muster-Arbeitsvertrag hinterlegt",               icon = "👨‍💼", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_dienstleistung",titel = "Dienstleistungen definiert",  beschreibung = "Dienstleistungskatalog hinterlegt",              icon = "📋", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_profil",        titel = "Profil komplett",             beschreibung = "Firmendaten und Bankverbindung vollständig ausgefüllt", icon = "🪪", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_meta_sammler",  titel = "Erfolgs-Sammler",             beschreibung = "Bronze/Silber/Gold/Platin je nach Anteil aller freigeschalteten Erfolge", icon = "🏅", typ = ErfolgTyp.Stackable, stufen = new[] { 1, 2, 3, 4 } },
                }
            },
        };
    }

    // ============================================================
    // LIFECYCLE
    // ============================================================

    private VisualElement root;
    private VisualElement erfolgeGrid;
    private Label         lblGesamtProzent;
    private VisualElement gesamtBalkenFill;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        erfolgeGrid      = root.Q<VisualElement>("erfolge-grid");
        lblGesamtProzent = root.Q<Label>("lbl-gesamt-prozent");
        gesamtBalkenFill = root.Q<VisualElement>("gesamt-balken-fill");

        InitKategorien();
        LadeDaten();
        BaueUI();
        RegistriereHelpTooltips();
        ButtonHoverController.RegistriereAlle(root);
    }

    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Hier siehst du alle Erfolge und Meilensteine, die du im Programm freischalten kannst. " +
            "Erfolge werden automatisch freigeschaltet wenn du bestimmte Aktionen abschließt.");

        HelpTooltip.Registriere(root, "btn-help-fortschritt",
            "Zeigt wie viele Erfolge du bereits freigeschaltet hast. " +
            "Je mehr Aufgaben du erfüllst, desto höher steigt dein Gesamtfortschritt.");
    }

    // ============================================================
    // DATEN LADEN & AUSWERTEN
    // ============================================================

    private void LadeDaten()
    {
        LadeDokumenteDaten();
        LadeKundenbuchDaten();
        LadeGruendungspfadDaten();
        LadeFinanzplanungDaten();
        LadeProfilDaten();
        PruefeAlleEinmalig();

        // Muss als Letztes laufen: braucht den finalen erledigt-Stand aller
        // anderen Erfolge, um die eigenen Stufen-Schwellen (25/50/75/100%)
        // und den eigenen Fortschritt zu berechnen.
        AktualisiereMetaErfolg();
    }

    // ============================================================
    // META-ERFOLG: "Erfolgs-Sammler" - Bronze/Silber/Gold/Platin je
    // nachdem wie viel Prozent ALLER ANDEREN Erfolge freigeschaltet sind.
    // Die Stufen-Schwellen werden dynamisch berechnet, weil sich die
    // Gesamtzahl der Erfolge im Programm noch ändern kann.
    // ============================================================
    private void AktualisiereMetaErfolg()
    {
        var alleAusserMeta = kategorien.SelectMany(k => k.erfolge)
            .Where(e => e.id != "e_meta_sammler").ToList();

        int gesamt = alleAusserMeta.Count;
        int erledigt = alleAusserMeta.Count(e => e.erledigt);
        if (gesamt == 0) return;

        var meta = kategorien.SelectMany(k => k.erfolge).FirstOrDefault(e => e.id == "e_meta_sammler");
        if (meta == null) return;

        int s25  = Mathf.Max(1, Mathf.RoundToInt(gesamt * 0.25f));
        int s50  = Mathf.Max(s25 + 1, Mathf.RoundToInt(gesamt * 0.5f));
        int s75  = Mathf.Max(s50 + 1, Mathf.RoundToInt(gesamt * 0.75f));
        int s100 = Mathf.Max(s75 + 1, gesamt);
        meta.stufen = new[] { s25, s50, s75, s100 };
        meta.beschreibung = $"{s25} / {s50} / {s75} / {s100} von {gesamt} Erfolgen freigeschaltet";

        SetzeStackable("e_meta_sammler", erledigt);
    }

    private void LadeDokumenteDaten()
    {
        var gespeichert = DocumentDashboard.GetSavedDocuments();
        if (gespeichert?.savedDocs == null) return;

        // Welche Pflichtdoks sind ausgefüllt?
        var ausgefuellte = new HashSet<string>();
        foreach (var doc in gespeichert.savedDocs)
        {
            if (!doc.istPflichtdokument) continue;
            bool hatInhalt   = !string.IsNullOrWhiteSpace(doc.inhalt);
            bool hatFeldwert = doc.strukturFelder != null &&
                               doc.strukturFelder.Any(f => !string.IsNullOrWhiteSpace(f.wert));
            if (hatInhalt || hatFeldwert)
                ausgefuellte.Add(doc.title);
        }

        // Mapping Dokument → Erfolg-ID
        var dokZuErfolg = new Dictionary<string, string>
        {
            { "Unternehmensstammdaten",                  "e_stammdaten"    },
            { "Gewerbeanmeldung",                        "e_gewerbe"       },
            { "Handelsregisterauszug",                   "e_handelsreg"    },
            { "Businessplan",                            "e_businessplan"  },
            { "Markt- & Wettbewerbsanalyse",             "e_marktanalyse"  },
            { "Gründungsurkunde / Gesellschaftsvertrag", "e_gesellschaft"  },
            { "Gesellschafterliste",                     "e_gesellschaft"  },
            { "Kontodaten (IBAN/BIC)",                   "e_konto"         },
            { "AGB",                                     "e_agb"           },
            { "Datenschutzerklärung (DSGVO)",            "e_dsgvo"         },
            { "Impressum",                               "e_impressum"     },
            { "Corporate Identity Manual",               "e_ci"            },
            { "Muster-Arbeitsvertrag",                   "e_team"          },
            { "Dienstleistungskatalog / Preisliste",     "e_dienstleistung"},
        };

        foreach (var titel in ausgefuellte)
        {
            if (dokZuErfolg.TryGetValue(titel, out string erfolgId))
                SetzeErfolg(erfolgId, true);
        }

        // Alle Pflichtdoks vollständig?
        int pflichtGesamt    = gespeichert.savedDocs.Count(d => d.istPflichtdokument);
        int pflichtAusgefuellt = ausgefuellte.Count;
        if (pflichtGesamt > 0 && pflichtAusgefuellt >= pflichtGesamt)
            SetzeErfolg("e_alle_dok", true);
    }

    private void LadeKundenbuchDaten()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        try
        {
            // getAllCustomers direkt – ohne Musterkunden-Fallback aus KundendatenbankController
            var kundenRoh = db.getAllCustomers();
            int kundenAnzahl = kundenRoh?.Count ?? 0;
            var angebote = db.getAllOffers();
            var rechnungen = db.getAllInvoices();
            int angeboteAnzahl = angebote?.Count   ?? 0;
            int rechnungAnzahl = rechnungen?.Count ?? 0;

            SetzeStackable("e_kunden",     kundenAnzahl);
            SetzeStackable("e_angebote",   angeboteAnzahl);
            SetzeStackable("e_rechnungen", rechnungAnzahl);

            if (rechnungAnzahl >= 1) SetzeErfolg("e_export",     true);
            if (rechnungAnzahl >= 1 || angeboteAnzahl >= 1) SetzeErfolg("e_kontostand", true);

            // Vertrieb: Status-basierte Erfolge
            bool hatAngenommenes = angebote != null && angebote.Any(a => a.status == "Angenommen");
            bool hatBezahlte     = rechnungen != null && rechnungen.Any(r => r.status == "Bezahlt");
            bool perfekteQuote   = angebote != null && angebote.Count >= 5
                && angebote.All(a => a.status == "Angenommen");

            SetzeErfolg("e_angebot_angenommen", hatAngenommenes);
            SetzeErfolg("e_rechnung_bezahlt",   hatBezahlte);
            SetzeErfolg("e_perfekte_quote",     perfekteQuote);

            // Kassenbuch-Fleiß: reine Anzahl an Buchungen, unabhängig vom Betrag
            var kennzahlen = FinanzKennzahlenService.Berechne(db);
            SetzeStackable("e_kassenbuch_fleiss", kennzahlen.AnzahlKassenbuchEintraege);

            // BUG-FIX: "Erster Umsatz" (e_umsatz_1k) wurde nirgends gesetzt -
            // war ein kompletter Blindgänger, unabhängig vom tatsächlichen
            // Kassenbuch-Umsatz. Nutzt die Gesamteinnahmen (lebenszeit).
            SetzeStackable("e_umsatz_1k", Mathf.RoundToInt(kennzahlen.GesamtEinnahmen));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Erfolge] LadeKundenbuchDaten: " + e.Message);
        }
    }

    // ============================================================
    // FINANZPLANUNG (Finanzen 1 & 2) - basiert auf FinanzKennzahlenService
    // ============================================================
    private void LadeFinanzplanungDaten()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        try
        {
            var k = FinanzKennzahlenService.Berechne(db);

            SetzeErfolg("fp_rohgewinn", k.Rohgewinn > 0);
            SetzeStackable("fp_rohgewinn_stufen", Mathf.Max(0, Mathf.RoundToInt(k.Rohgewinn)));

            SetzeErfolg("fp_investition", k.SummeInvestition > 0);
            SetzeErfolg("fp_gruenderkosten", k.SummeGruenderkosten > 0);

            // GEÄNDERT: "Eröffnungsbilanz erstellt" hing vorher an einem
            // reinen Freitext-Dokument (jedes Zeichen reichte, egal ob
            // sinnvoll). Jetzt gekoppelt an echte Kapitalstruktur-Daten aus
            // dem Kassenbuch - genau das, was eine Eröffnungsbilanz
            // eigentlich zeigen soll.
            SetzeErfolg("e_bilanz", k.Kapitalbedarf > 0);

            bool kapitalbedarfGedeckt = k.Kapitalbedarf > 0 && k.GesamtKapital >= k.Kapitalbedarf;
            SetzeErfolg("fp_kapitalbedarf_gedeckt", kapitalbedarfGedeckt);

            SetzeStackable("fp_liq_reserve", Mathf.Max(0, Mathf.RoundToInt(k.Liquiditaetsreserve)));

            SetzeErfolg("fp_fremdkapital", k.Kredite > 0 || k.Darlehen > 0);
            SetzeErfolg("fp_eigenkapital", k.Sacheinlagen > 0 || k.Geldeinlagen > 0);
            SetzeErfolg("fp_krypto", k.BoerseKrypto > 0);

            bool kostenbewusst = k.GesamtEinnahmen > 0 && (k.Betriebsausgaben / k.GesamtEinnahmen) < 0.2f;
            SetzeErfolg("fp_kostenbewusst", kostenbewusst);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Erfolge] LadeFinanzplanungDaten: " + e.Message);
        }
    }

    // ============================================================
    // PROFIL (Einstellungen: Firmendaten + Bankverbindung)
    // ============================================================
    private void LadeProfilDaten()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        try
        {
            var company = db.getAllCompanies()?.FirstOrDefault();
            bool firmendatenVollstaendig = company != null
                && !string.IsNullOrWhiteSpace(company.name)
                && !string.IsNullOrWhiteSpace(company.steuerNr)
                && !string.IsNullOrWhiteSpace(company.strasseuHausNr)
                && !string.IsNullOrWhiteSpace(company.email);

            var settings = db.getOrCreateSettingsForUser(0);
            bool bankdatenVollstaendig = settings != null && !string.IsNullOrWhiteSpace(settings.iban);

            SetzeErfolg("e_profil", firmendatenVollstaendig && bankdatenVollstaendig);

            // BUG-FIX: "Geschäftskonto eröffnet" (e_konto) prüfte bisher NUR
            // das alte Dok-Pool-Dokument "Kontodaten (IBAN/BIC)". Seit es die
            // echte Bankverbindung in Einstellungen gibt (Settings.iban),
            // tragen viele Nutzer ihre IBAN nur noch DORT ein - der Erfolg
            // blieb für die dann für immer gesperrt. Zählt jetzt auch.
            if (bankdatenVollstaendig) SetzeErfolg("e_konto", true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Erfolge] LadeProfilDaten: " + e.Message);
        }
    }

    private void LadeGruendungspfadDaten()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        var erledigt = new HashSet<string>();
        try
        {
            var docs = db.getAllUserDocuments();
            var pfadSave = docs?.FirstOrDefault(d => d.documentType == 9001);
            if (pfadSave != null)
            {
                var daten = JsonUtility.FromJson<PfadSpeicherDaten>(pfadSave.text);
                if (daten?.erledigteIds != null) erledigt.UnionWith(daten.erledigteIds);
            }
        }
        catch { /* JSON-Fehler ignorieren - unten kommt trotzdem die Auto-Erkennung */ }

        // Zusätzlich: Schritte, die sich aus echten Daten ableiten lassen
        // (Dokumente, Kassenbuch, Kunden) - unabhängig davon, ob der
        // Gründerpfad-Screen schon besucht/gespeichert wurde.
        erledigt.UnionWith(GruenderpfadAutoErkennung.ErmittleAlle(db));

        // Phasen prüfen
        bool vorbereitungFertig = new[] { "vorb_1","vorb_2","vorb_3","vorb_4","vorb_5" }.All(id => erledigt.Contains(id));
        bool anmeldungFertig    = new[] { "anm_1","anm_2","anm_3","anm_4","anm_5"     }.All(id => erledigt.Contains(id));
        bool finanzenFertig     = new[] { "fin_1","fin_2","fin_3","fin_4"              }.All(id => erledigt.Contains(id));
        bool betriebFertig      = new[] { "betr_1","betr_2","betr_3","betr_4"         }.All(id => erledigt.Contains(id));
        // BUG-FIX: "Gründerpfad komplett" prüfte vorher nur 18 der 21
        // Schritte - die "Sonstiges"-Phase (sonst_1-3: Team, Fördermittel,
        // Skalierung) fehlte komplett in der Prüfung, obwohl der Erfolg
        // wörtlich "Alle 21 Schritte" verspricht. Konnte also als
        // "komplett" erledigt gelten, obwohl noch 3 Schritte offen waren.
        bool sonstigesFertig    = new[] { "sonst_1","sonst_2","sonst_3" }.All(id => erledigt.Contains(id));
        bool allesFertig        = vorbereitungFertig && anmeldungFertig && finanzenFertig && betriebFertig && sonstigesFertig;

        SetzeErfolg("e_pfad_vorbereitung", vorbereitungFertig);
        SetzeErfolg("e_pfad_anmeldung",    anmeldungFertig);
        SetzeErfolg("e_pfad_finanzen",     finanzenFertig);
        SetzeErfolg("e_pfad_betrieb",      betriebFertig);
        SetzeErfolg("e_pfad_komplett",     allesFertig);
    }

    // Hilfsdatenklasse zum Deserialisieren des Gründerpfad-Saves
    [System.Serializable]
    private class PfadSpeicherDaten
    {
        public List<string> erledigteIds;
    }

    private void PruefeAlleEinmalig()
    {
        // "Erste Schritte" ist immer erledigt (App wurde gestartet)
        SetzeErfolg("e_erster_login", true);
    }

    // ============================================================
    // HELFER
    // ============================================================

    private void SetzeErfolg(string id, bool erledigt)
    {
        foreach (var kat in kategorien)
        {
            var e = kat.erfolge.FirstOrDefault(x => x.id == id);
            if (e != null) { e.erledigt = erledigt; return; }
        }
    }

    private void SetzeStackable(string id, int wert)
    {
        foreach (var kat in kategorien)
        {
            var e = kat.erfolge.FirstOrDefault(x => x.id == id);
            if (e == null || e.typ != ErfolgTyp.Stackable) continue;

            e.aktuellerWert = wert;
            e.aktuellerStufenIndex = 0;
            for (int i = 0; i < e.stufen.Length; i++)
                if (wert >= e.stufen[i]) e.aktuellerStufenIndex = i + 1;

            e.erledigt = e.aktuellerStufenIndex > 0;
            return;
        }
    }

    // ============================================================
    // UI AUFBAUEN
    // ============================================================

    private void BaueUI()
    {
        if (erfolgeGrid == null) return;
        erfolgeGrid.Clear();

        int gesamtErfolge  = 0;
        int erledigtGesamt = 0;

        foreach (var kat in kategorien)
        {
            int kat_erledigt = kat.erfolge.Count(e => e.erledigt);
            int kat_gesamt   = kat.erfolge.Count;
            gesamtErfolge  += kat_gesamt;
            erledigtGesamt += kat_erledigt;

            var card = BaueKategorieKarte(kat, kat_erledigt, kat_gesamt);
            erfolgeGrid.Add(card);
        }

        // Gesamtfortschritt
        float prozent = gesamtErfolge > 0 ? (float)erledigtGesamt / gesamtErfolge : 0f;
        if (lblGesamtProzent != null)
            lblGesamtProzent.text = $"{Mathf.RoundToInt(prozent * 100)}%";
        if (gesamtBalkenFill != null)
            gesamtBalkenFill.style.width = new StyleLength(new Length(prozent * 100f, LengthUnit.Percent));
        // Gesamttext aktualisieren
        var lblGesamt = root.Q<Label>("lbl-gesamt-text");
        if (lblGesamt != null)
            lblGesamt.text = $"{erledigtGesamt} von {gesamtErfolge} Erfolgen";
    }

    private VisualElement BaueKategorieKarte(ErfolgsKategorie kat, int erledigt, int gesamt)
    {
        var card = new VisualElement();
        card.AddToClassList("kategorie-card");

        // Header
        var header = new VisualElement();
        header.AddToClassList("kategorie-header");

        var iconLabel = new Label(kat.icon);
        iconLabel.AddToClassList("kategorie-icon");
        header.Add(iconLabel);

        var nameLabel = new Label(kat.name);
        nameLabel.AddToClassList("kategorie-name");
        header.Add(nameLabel);

        float katProzent = gesamt > 0 ? (float)erledigt / gesamt : 0f;
        var prozentLabel = new Label($"{Mathf.RoundToInt(katProzent * 100)}%");
        prozentLabel.AddToClassList("kategorie-prozent");
        header.Add(prozentLabel);

        card.Add(header);

        // Mini-Fortschrittsbalken
        var miniBarBg = new VisualElement();
        miniBarBg.AddToClassList("kategorie-mini-bar-bg");
        var miniBarFill = new VisualElement();
        miniBarFill.AddToClassList("kategorie-mini-bar-fill");
        miniBarFill.style.width = new StyleLength(new Length(katProzent * 100f, LengthUnit.Percent));
        miniBarBg.Add(miniBarFill);
        card.Add(miniBarBg);

        // Kacheln-Grid
        var kachelnGrid = new VisualElement();
        kachelnGrid.AddToClassList("erfolge-kacheln-grid");

        foreach (var erfolg in kat.erfolge)
            kachelnGrid.Add(BaueKachel(erfolg));

        card.Add(kachelnGrid);
        return card;
    }

    private VisualElement BaueKachel(Erfolg e)
    {
        var kachel = new VisualElement();
        kachel.AddToClassList("erfolg-kachel");
        if (e.erledigt) kachel.AddToClassList("erfolg-kachel--erledigt");
        else            kachel.AddToClassList("erfolg-kachel--gesperrt");

        // Icon
        var iconBox = new VisualElement();
        iconBox.AddToClassList("erfolg-icon-box");
        if (e.erledigt) iconBox.AddToClassList("erfolg-icon-box--erledigt");
        var iconLabel = new Label(e.icon);
        iconLabel.style.fontSize = 18;
        iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        iconBox.Add(iconLabel);
        kachel.Add(iconBox);

        // Text
        var textBlock = new VisualElement();
        textBlock.AddToClassList("erfolg-text-block");

        // Titel + ggf. Stackable Badge
        var titelRow = new VisualElement();
        titelRow.style.flexDirection = FlexDirection.Row;
        titelRow.style.alignItems = Align.Center;

        var titelLabel = new Label(e.titel);
        titelLabel.AddToClassList("erfolg-titel");
        if (e.erledigt) titelLabel.AddToClassList("erfolg-titel--erledigt");
        titelRow.Add(titelLabel);

        if (e.typ == ErfolgTyp.Stackable)
        {
            bool hatStufe = e.aktuellerStufenIndex > 0;
            string stufenName = "Noch keine Stufe";
            string stufenIcon = "";
            Color stufenFarbe = new Color(150f / 255f, 150f / 255f, 150f / 255f);
            if (hatStufe)
            {
                var stufe = Stufennamen[Mathf.Clamp(e.aktuellerStufenIndex - 1, 0, Stufennamen.Length - 1)];
                stufenName = stufe.name;
                stufenIcon = stufe.icon;
                stufenFarbe = stufe.farbe;
            }

            var badge = new Label(hatStufe ? $"{stufenIcon} {stufenName}" : stufenName);
            badge.AddToClassList("erfolg-stackable-badge");
            badge.style.color = stufenFarbe;
            badge.style.borderTopColor = stufenFarbe;
            badge.style.borderBottomColor = stufenFarbe;
            badge.style.borderLeftColor = stufenFarbe;
            badge.style.borderRightColor = stufenFarbe;
            badge.style.backgroundColor = new Color(stufenFarbe.r, stufenFarbe.g, stufenFarbe.b, 0.16f);
            titelRow.Add(badge);
        }

        textBlock.Add(titelRow);

        var beschLabel = new Label(e.beschreibung);
        beschLabel.AddToClassList("erfolg-beschreibung");
        textBlock.Add(beschLabel);

        // Stackable Fortschritt
        if (e.typ == ErfolgTyp.Stackable && e.stufen != null)
        {
            int gesamtStufen = e.stufen.Length;
            bool alleStufenErreicht = e.aktuellerStufenIndex >= gesamtStufen;

            int naechsteStufe = alleStufenErreicht
                ? e.stufen[gesamtStufen - 1]
                : e.stufen[e.aktuellerStufenIndex];

            float barProzent = naechsteStufe > 0
                ? Mathf.Clamp01((float)e.aktuellerWert / naechsteStufe)
                : 1f;

            Color balkenFarbe = new Color(128f / 255f, 207f / 255f, 149f / 255f);
            if (e.aktuellerStufenIndex > 0)
                balkenFarbe = Stufennamen[Mathf.Clamp(e.aktuellerStufenIndex - 1, 0, Stufennamen.Length - 1)].farbe;

            var barBg = new VisualElement();
            barBg.AddToClassList("stackable-bar-bg");
            var barFill = new VisualElement();
            barFill.AddToClassList("stackable-bar-fill");
            barFill.style.width = new StyleLength(new Length(barProzent * 100f, LengthUnit.Percent));
            barFill.style.backgroundColor = balkenFarbe;
            barBg.Add(barFill);
            textBlock.Add(barBg);

            // Zeige z.B. "5 von 10 – nächste Stufe: Silber" oder bei Maximalstufe "100 – Platin erreicht!"
            string stufenLabel;
            if (alleStufenErreicht)
            {
                var (topName, topIcon, _) = Stufennamen[Stufennamen.Length - 1];
                stufenLabel = $"{e.aktuellerWert} \u2013 {topIcon} {topName} erreicht!";
            }
            else
            {
                var (naechsterName, _, _) = Stufennamen[Mathf.Clamp(e.aktuellerStufenIndex, 0, Stufennamen.Length - 1)];
                stufenLabel = $"{e.aktuellerWert} von {naechsteStufe} \u2013 n\u00e4chste Stufe: {naechsterName}";
            }
            var countLabel = new Label(stufenLabel);
            countLabel.AddToClassList("stackable-count-label");
            textBlock.Add(countLabel);
        }

        kachel.Add(textBlock);

        // Haken wenn erledigt
        if (e.erledigt)
        {
            var check = new Label("✓");
            check.AddToClassList("erfolg-check");
            kachel.Add(check);
        }

        return kachel;
    }
}
