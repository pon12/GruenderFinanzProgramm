// ================================================================
// ErfolgeController.cs
// Zeigt Errungenschaften & Meilensteine in Kategorien an.
// ================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class ErfolgeController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Help Icon (Help circle.png zuweisen)")]
    [SerializeField] private Texture2D helpIconTexture;

    // ============================================================
    // DATENMODELL
    // ============================================================

    public enum ErfolgTyp { Einmalig, Stackable }

    public class Erfolg
    {
        public string    id;
        public string    titel;
        public string    beschreibung;
        public string    icon;
        public ErfolgTyp typ;
        public bool      erledigt;
        public int       aktuellerWert;
        public int[]     stufen;
        public int       aktuellerStufenIndex;
    }

    public class ErfolgsKategorie
    {
        public string       name;
        public string       icon;
        public List<Erfolg> erfolge;
    }

    // ============================================================
    // KATEGORIEN
    // ============================================================

    private List<ErfolgsKategorie> kategorien;

    private void InitKategorien()
    {
        kategorien = new List<ErfolgsKategorie>
        {
            new ErfolgsKategorie
            {
                name = "Gründung", icon = "\U0001f3e2",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_stammdaten",   titel = "Stammdaten hinterlegt",       beschreibung = "Unternehmensstammdaten ausgefüllt",                          icon = "\U0001f4cb", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_gewerbe",      titel = "Gewerbe angemeldet",           beschreibung = "Gewerbeanmeldung im Dok-Pool hinterlegt",                    icon = "\U0001f3db", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_handelsreg",   titel = "Handelsregistereintrag",       beschreibung = "Handelsregisterauszug hochgeladen",                          icon = "\U0001f4dc", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_businessplan", titel = "Businessplan erstellt",        beschreibung = "Businessplan im Dok-Pool hinterlegt",                        icon = "\U0001f4ca", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_marktanalyse", titel = "Marktanalyse durchgeführt",    beschreibung = "Markt- & Wettbewerbsanalyse hinterlegt",                     icon = "\U0001f50d", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_gesellschaft", titel = "Gesellschaft gegründet",       beschreibung = "Gründungsurkunde oder Gesellschaftsvertrag hinterlegt",      icon = "\U0001f91d", typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Finanzen", icon = "\U0001f4b0",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_konto",      titel = "Geschäftskonto eröffnet",    beschreibung = "Kontodaten im Dok-Pool hinterlegt",                 icon = "\U0001f3e6", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_bilanz",     titel = "Eröffnungsbilanz erstellt",  beschreibung = "Eröffnungsbilanz hinterlegt",                       icon = "\U0001f4c8", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_umsatz_1k",  titel = "Erster Umsatz",              beschreibung = "Kassenbuch-Umsatz: €1k / €10k / €100k erreicht",   icon = "\U0001f4b5", typ = ErfolgTyp.Stackable, stufen = new[] { 1000, 10000, 100000 } },
                    new Erfolg { id = "e_kontostand", titel = "Kontostand aktuell",          beschreibung = "Kassenbuch-Kontostand hinterlegt",                  icon = "\U0001f4b3", typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Buchhaltung", icon = "\U0001f4c2",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_kunden",     titel = "Kunden hinterlegt",          beschreibung = "1 / 5 / 10 / 100 Kunden im System",   icon = "\U0001f465", typ = ErfolgTyp.Stackable, stufen = new[] { 1, 5, 10, 100 } },
                    new Erfolg { id = "e_angebote",   titel = "Angebote erstellt",           beschreibung = "1 / 10 / 50 Angebote erstellt",        icon = "\U0001f4c4", typ = ErfolgTyp.Stackable, stufen = new[] { 1, 10, 50 } },
                    new Erfolg { id = "e_rechnungen", titel = "Rechnungen gestellt",         beschreibung = "1 / 25 / 100 Rechnungen gestellt",     icon = "\U0001f9fe", typ = ErfolgTyp.Stackable, stufen = new[] { 1, 25, 100 } },
                    new Erfolg { id = "e_export",     titel = "Erste Rechnung exportiert",   beschreibung = "Eine Rechnung als PDF exportiert",     icon = "\U0001f4e4", typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Dokumente", icon = "\U0001f5c2",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_agb",      titel = "AGBs erstellt",                 beschreibung = "AGB im Dok-Pool hinterlegt",                icon = "\U0001f4d1", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_dsgvo",    titel = "DSGVO-konform",                 beschreibung = "Datenschutzerklärung hinterlegt",          icon = "\U0001f512", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_impressum",titel = "Impressum erstellt",             beschreibung = "Impressum im Dok-Pool hinterlegt",         icon = "\U0001f4cb", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_alle_dok", titel = "Alle Dokumente vollständig",     beschreibung = "Alle Pflichtdokumente ausgefüllt",         icon = "\u2705",     typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Gründerpfad", icon = "\U0001f5fa",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_pfad_vorbereitung", titel = "Vorbereitung abgeschlossen", beschreibung = "Alle Schritte der Vorbereitung erledigt",         icon = "\U0001f4cb", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_pfad_anmeldung",    titel = "Anmeldung abgeschlossen",    beschreibung = "Alle Anmeldeschritte erledigt",                   icon = "\U0001f4dd", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_pfad_finanzen",     titel = "Finanzen abgeschlossen",     beschreibung = "Alle Finanzschritte erledigt",                    icon = "\U0001f4b0", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_pfad_betrieb",      titel = "Betrieb gestartet",          beschreibung = "Alle Betriebsschritte erledigt",                  icon = "\U0001f680", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_pfad_komplett",     titel = "Gründerpfad komplett",       beschreibung = "Alle 21 Schritte des Gründerpfads erledigt",      icon = "\U0001f3c6", typ = ErfolgTyp.Einmalig },
                }
            },
            new ErfolgsKategorie
            {
                name = "Weitere", icon = "\u2b50",
                erfolge = new List<Erfolg>
                {
                    new Erfolg { id = "e_erster_login",   titel = "Erste Schritte",              beschreibung = "Ventoriq zum ersten Mal gestartet",      icon = "\U0001f44b", typ = ErfolgTyp.Einmalig, erledigt = true },
                    new Erfolg { id = "e_ci",             titel = "Corporate Identity",           beschreibung = "CI Manual im Dok-Pool hinterlegt",       icon = "\U0001f3a8", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_team",           titel = "Team aufgebaut",               beschreibung = "Muster-Arbeitsvertrag hinterlegt",       icon = "\U0001f468\u200d\U0001f4bc", typ = ErfolgTyp.Einmalig },
                    new Erfolg { id = "e_dienstleistung", titel = "Dienstleistungen definiert",   beschreibung = "Dienstleistungskatalog hinterlegt",      icon = "\U0001f4cb", typ = ErfolgTyp.Einmalig },
                }
            },
        };
    }

    // ============================================================
    // KATEGORIE-TOOLTIPS
    // ============================================================

    private static readonly Dictionary<string, string> KategorieTooltips =
        new Dictionary<string, string>
    {
        ["Gründung"]    = "Erfolge rund um die offizielle Gründung deines Unternehmens. Hinterlege Stammdaten, Gewerbeanmeldung und Businessplan um diese Erfolge freizuschalten.",
        ["Finanzen"]    = "Finanzielle Meilensteine: Geschäftskonto, Bilanz und Umsätze. Stufige Erfolge steigen mit deinem Kassenbuch-Umsatz.",
        ["Buchhaltung"] = "Erfolge für deine tägliche Geschäftstätigkeit: Kunden anlegen, Angebote und Rechnungen erstellen. Stufige Erfolge wachsen mit der Anzahl.",
        ["Dokumente"]   = "Erfolge für vollständig ausgefüllte Dokumente im Dokumenten-Pool. Fülle Pflichtfelder im Dokumente-Screen aus um sie freizuschalten.",
        ["Gründerpfad"] = "Erfolge basierend auf deinem Fortschritt im Gründerpfad. Hake Schritte im Gründerpfad ab um diese Erfolge zu erhalten.",
        ["Weitere"]     = "Besondere Errungenschaften außerhalb der Hauptkategorien. Einige werden automatisch freigeschaltet, andere erfordern spezifische Aktionen.",
    };

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

        root             = uiDocument.rootVisualElement;
        erfolgeGrid      = root.Q<VisualElement>("erfolge-grid");
        lblGesamtProzent = root.Q<Label>("lbl-gesamt-prozent");
        gesamtBalkenFill = root.Q<VisualElement>("gesamt-balken-fill");

        InitKategorien();
        LadeDaten();
        BaueUI();
        RegistriereHelpTooltips();
    }

    // ============================================================
    // DATEN LADEN
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

        var dokZuErfolg = new Dictionary<string, string>
        {
            { "Unternehmensstammdaten",                  "e_stammdaten"     },
            { "Gewerbeanmeldung",                        "e_gewerbe"        },
            { "Handelsregisterauszug",                   "e_handelsreg"     },
            { "Businessplan",                            "e_businessplan"   },
            { "Markt- & Wettbewerbsanalyse",             "e_marktanalyse"   },
            { "Gründungsurkunde / Gesellschaftsvertrag", "e_gesellschaft"   },
            { "Gesellschafterliste",                     "e_gesellschaft"   },
            { "Kontodaten (IBAN/BIC)",                   "e_konto"          },
            { "Eröffnungsbilanz",                        "e_bilanz"         },
            { "AGB",                                     "e_agb"            },
            { "Datenschutzerklärung (DSGVO)",            "e_dsgvo"          },
            { "Impressum",                               "e_impressum"      },
            { "Corporate Identity Manual",               "e_ci"             },
            { "Muster-Arbeitsvertrag",                   "e_team"           },
            { "Dienstleistungskatalog / Preisliste",     "e_dienstleistung" },
        };

        foreach (var titel in ausgefuellte)
            if (dokZuErfolg.TryGetValue(titel, out string erfolgId))
                SetzeErfolg(erfolgId, true);

        int pflichtGesamt      = gespeichert.savedDocs.Count(d => d.istPflichtdokument);
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
            int kundenAnzahl   = db.getAllCustomers()?.Count ?? 0;
            int angeboteAnzahl = db.getAllOffers()?.Count    ?? 0;
            int rechnungAnzahl = db.getAllInvoices()?.Count  ?? 0;

            SetzeStackable("e_kunden",     kundenAnzahl);
            SetzeStackable("e_angebote",   angeboteAnzahl);
            SetzeStackable("e_rechnungen", rechnungAnzahl);

            if (rechnungAnzahl >= 1) SetzeErfolg("e_export",     true);
            if (rechnungAnzahl >= 1 || angeboteAnzahl >= 1) SetzeErfolg("e_kontostand", true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Erfolge] LadeKundenbuchDaten: " + e.Message);
        }
    }

    private void LadeGruendungspfadDaten()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        var docs     = db.getAllUserDocuments();
        var pfadSave = docs?.FirstOrDefault(d => d.documentType == 9001);
        if (pfadSave == null) return;

        try
        {
            var daten = JsonUtility.FromJson<PfadSpeicherDaten>(pfadSave.text);
            if (daten?.erledigteIds == null) return;

            var erledigt = new HashSet<string>(daten.erledigteIds);

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
        catch { }
    }

    [System.Serializable]
    private class PfadSpeicherDaten
    {
        public List<string> erledigteIds;
    }

    private void PruefeAlleEinmalig()
    {
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

            e.aktuellerWert        = wert;
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

            erfolgeGrid.Add(BaueKategorieKarte(kat, kat_erledigt, kat_gesamt));
        }

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

        // Hilfe-Icon rechts im Header
        var helpIcon = new VisualElement();
        helpIcon.name = "btn-help-kat-" + kat.name.ToLower();
        HelpTooltip.SetzeBasisStilOeffentlich(helpIcon);
        var tex = helpIconTexture != null
            ? helpIconTexture
            : Resources.Load<Texture2D>("Icons/Help circle");
        if (tex != null)
        {
            helpIcon.style.backgroundImage               = new StyleBackground(tex);
            helpIcon.style.unityBackgroundImageTintColor  = new StyleColor(
                new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f));
        }
        header.Add(helpIcon);
        RegistriereKarteTooltip(helpIcon, kat.name);

        card.Add(header);

        var miniBarBg = new VisualElement();
        miniBarBg.AddToClassList("kategorie-mini-bar-bg");
        var miniBarFill = new VisualElement();
        miniBarFill.AddToClassList("kategorie-mini-bar-fill");
        miniBarFill.style.width = new StyleLength(new Length(katProzent * 100f, LengthUnit.Percent));
        miniBarBg.Add(miniBarFill);
        card.Add(miniBarBg);

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
        kachel.AddToClassList(e.erledigt ? "erfolg-kachel--erledigt" : "erfolg-kachel--gesperrt");
        kachel.style.width = new StyleLength(new Length(100f, LengthUnit.Percent));

        var iconBox = new VisualElement();
        iconBox.AddToClassList("erfolg-icon-box");
        if (e.erledigt) iconBox.AddToClassList("erfolg-icon-box--erledigt");
        var iconLabel = new Label(e.icon);
        iconLabel.style.fontSize       = 18;
        iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        iconBox.Add(iconLabel);
        kachel.Add(iconBox);

        var textBlock = new VisualElement();
        textBlock.AddToClassList("erfolg-text-block");

        var titelRow = new VisualElement();
        titelRow.style.flexDirection = FlexDirection.Row;
        titelRow.style.alignItems    = Align.Center;

        var titelLabel = new Label(e.titel);
        titelLabel.AddToClassList("erfolg-titel");
        if (e.erledigt) titelLabel.AddToClassList("erfolg-titel--erledigt");
        titelRow.Add(titelLabel);

        if (e.typ == ErfolgTyp.Stackable)
        {
            var badge = new Label("Stufig");
            badge.AddToClassList("erfolg-stackable-badge");
            badge.style.color = new StyleColor(new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f));
            titelRow.Add(badge);
        }

        textBlock.Add(titelRow);

        var beschLabel = new Label(e.beschreibung);
        beschLabel.AddToClassList("erfolg-beschreibung");
        beschLabel.style.color = new StyleColor(new UnityEngine.Color(0.7f, 0.7f, 0.7f));
        textBlock.Add(beschLabel);

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

            int gesamtStufen = e.stufen.Length;
            int naechsteZiel = e.aktuellerStufenIndex < gesamtStufen
                ? e.stufen[e.aktuellerStufenIndex]
                : e.stufen[gesamtStufen - 1];
            string stufenText = e.aktuellerStufenIndex >= gesamtStufen
                ? $"{e.aktuellerWert} – Alle Stufen erreicht!"
                : $"{e.aktuellerWert} von {naechsteZiel} – Stufe {e.aktuellerStufenIndex + 1} von {gesamtStufen}";
            var countLabel = new Label(stufenText);
            countLabel.AddToClassList("stackable-count-label");
            countLabel.style.color = new StyleColor(new UnityEngine.Color(0.6f, 0.6f, 0.6f));
            textBlock.Add(countLabel);
        }

        kachel.Add(textBlock);

        if (e.erledigt)
        {
            var check = new Label("✓");
            check.AddToClassList("erfolg-check");
            kachel.Add(check);
        }

        return kachel;
    }

    // ============================================================
    // HELP TOOLTIPS
    // ============================================================

    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Hier siehst du alle deine Errungenschaften. " +
            "Erfolge werden automatisch freigeschaltet wenn du " +
            "Dokumente hinterlegst, Kunden anlegst oder Umsätze erzielst.");

        HelpTooltip.Registriere(root, "btn-help-fortschritt",
            "Zeigt wie viele Erfolge du insgesamt bereits freigeschaltet hast. " +
            "Der Balken füllt sich mit jedem neuen Erfolg.");
    }

    private void RegistriereKarteTooltip(VisualElement helpIcon, string kategorieName)
    {
        if (!KategorieTooltips.TryGetValue(kategorieName, out string text))
            text = "Schalte Erfolge in dieser Kategorie frei.";
        HelpTooltip.RegistriereInKarte(root, helpIcon, text);
    }
}
