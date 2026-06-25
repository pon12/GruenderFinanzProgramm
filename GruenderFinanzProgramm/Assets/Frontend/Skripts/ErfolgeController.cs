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
                    new Erfolg { id = "e_bilanz",        titel = "Eröffnungsbilanz erstellt",   beschreibung = "Eröffnungsbilanz hinterlegt",                     icon = "📈", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_umsatz_1k",     titel = "Erster Umsatz",               beschreibung = "Kassenbuch-Umsatz: €1k / €10k / €100k erreicht",  icon = "💵", typ = ErfolgTyp.Stackable, stufen = new[] { 1000, 10000, 100000 } },
                    new Erfolg { id = "e_kontostand",    titel = "Kontostand aktuell",          beschreibung = "Kassenbuch-Kontostand hinterlegt",                icon = "💳", typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Buchhaltung", icon = "📂",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_kunden",        titel = "Kunden hinterlegt",           beschreibung = "1 / 5 / 10 / 100 Kunden im System",             icon = "👥", typ = ErfolgTyp.Stackable, stufen = new[] { 1, 5, 10, 100 } },
                    new Erfolg { id = "e_angebote",      titel = "Angebote erstellt",           beschreibung = "1 / 10 / 50 Angebote erstellt",                  icon = "📄", typ = ErfolgTyp.Stackable, stufen = new[] { 1, 10, 50 } },
                    new Erfolg { id = "e_rechnungen",    titel = "Rechnungen gestellt",         beschreibung = "1 / 25 / 100 Rechnungen gestellt",               icon = "🧾", typ = ErfolgTyp.Stackable, stufen = new[] { 1, 25, 100 } },
                    new Erfolg { id = "e_export",        titel = "Erste Rechnung exportiert",   beschreibung = "Eine Rechnung als PDF exportiert",               icon = "📤", typ = ErfolgTyp.Einmalig },
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
                name = "Weitere", icon = "⭐",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_erster_login",  titel = "Erste Schritte",              beschreibung = "Ventoriq zum ersten Mal gestartet",              icon = "👋", typ = ErfolgTyp.Einmalig, erledigt = true },
                    new Erfolg { id = "e_ci",            titel = "Corporate Identity",          beschreibung = "CI Manual im Dok-Pool hinterlegt",               icon = "🎨", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_team",          titel = "Team aufgebaut",              beschreibung = "Muster-Arbeitsvertrag hinterlegt",               icon = "👨‍💼", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_dienstleistung",titel = "Dienstleistungen definiert",  beschreibung = "Dienstleistungskatalog hinterlegt",              icon = "📋", typ = ErfolgTyp.Einmalig },
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
    }

    // ============================================================
    // DATEN LADEN & AUSWERTEN
    // ============================================================

    private void LadeDaten()
    {
        LadeDokumenteDaten();
        LadeKundenbuchDaten();
        LadeGruendungspfadDaten();
        PruefeAlleEinmalig();
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
            { "Eröffnungsbilanz",                        "e_bilanz"        },
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

        // Kunden
        int kundenAnzahl = db.getAllUserDocuments()
            ?.Count(d => d.documentType == 1001) ?? 0; // Typ anpassen falls nötig
        SetzeStackable("e_kunden", kundenAnzahl);

        // Angebote & Rechnungen über AppEventManager-Counts
        // (werden beim nächsten Event aktualisiert – hier erstmal aus DB lesen)
        int angeboteAnzahl  = db.getAllUserDocuments()
            ?.Count(d => d.documentType == 2001) ?? 0;
        int rechnungAnzahl  = db.getAllUserDocuments()
            ?.Count(d => d.documentType == 2002) ?? 0;

        SetzeStackable("e_angebote",   angeboteAnzahl);
        SetzeStackable("e_rechnungen", rechnungAnzahl);

        if (rechnungAnzahl >= 1)
            SetzeErfolg("e_export", true); // vereinfacht – falls Rechnung existiert

        // Kassenbuch-Umsatz
        // Kontostand aus AppEventManager-Daten ist nicht persistent –
        // daher direkt aus DB lesen falls vorhanden
        SetzeErfolg("e_kontostand", rechnungAnzahl > 0 || angeboteAnzahl > 0);
    }

    private void LadeGruendungspfadDaten()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        var docs = db.getAllUserDocuments();
        var pfadSave = docs?.FirstOrDefault(d => d.documentType == 9001);
        if (pfadSave == null) return;

        try
        {
            var daten = JsonUtility.FromJson<PfadSpeicherDaten>(pfadSave.text);
            if (daten?.erledigteIds == null) return;

            var erledigt = new HashSet<string>(daten.erledigteIds);

            // Phasen prüfen
            bool vorbereitungFertig = new[] { "vorb_1","vorb_2","vorb_3","vorb_4","vorb_5" }.All(id => erledigt.Contains(id));
            bool anmeldungFertig    = new[] { "anm_1","anm_2","anm_3","anm_4","anm_5"     }.All(id => erledigt.Contains(id));
            bool finanzenFertig     = new[] { "fin_1","fin_2","fin_3","fin_4"              }.All(id => erledigt.Contains(id));
            bool betriebFertig      = new[] { "betr_1","betr_2","betr_3","betr_4"         }.All(id => erledigt.Contains(id));
            bool allesFertig        = vorbereitungFertig && anmeldungFertig && finanzenFertig && betriebFertig;

            SetzeErfolg("e_pfad_vorbereitung", vorbereitungFertig);
            SetzeErfolg("e_pfad_anmeldung",    anmeldungFertig);
            SetzeErfolg("e_pfad_finanzen",     finanzenFertig);
            SetzeErfolg("e_pfad_betrieb",      betriebFertig);
            SetzeErfolg("e_pfad_komplett",     allesFertig);
        }
        catch { /* JSON-Fehler ignorieren */ }
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
            var badge = new Label("Stufig");
            badge.AddToClassList("erfolg-stackable-badge");
            titelRow.Add(badge);
        }

        textBlock.Add(titelRow);

        var beschLabel = new Label(e.beschreibung);
        beschLabel.AddToClassList("erfolg-beschreibung");
        textBlock.Add(beschLabel);

        // Stackable Fortschritt
        if (e.typ == ErfolgTyp.Stackable && e.stufen != null)
        {
            int naechsteStufe = e.aktuellerStufenIndex < e.stufen.Length
                ? e.stufen[e.aktuellerStufenIndex]
                : e.stufen[e.stufen.Length - 1];

            float barProzent = naechsteStufe > 0
                ? Mathf.Clamp01((float)e.aktuellerWert / naechsteStufe)
                : 1f;

            var barBg = new VisualElement();
            barBg.AddToClassList("stackable-bar-bg");
            var barFill = new VisualElement();
            barFill.AddToClassList("stackable-bar-fill");
            barFill.style.width = new StyleLength(new Length(barProzent * 100f, LengthUnit.Percent));
            barBg.Add(barFill);
            textBlock.Add(barBg);

            string stufenText = string.Join(" / ", e.stufen.Select(s => s.ToString()));
            var countLabel = new Label($"{e.aktuellerWert} / {stufenText}");
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
