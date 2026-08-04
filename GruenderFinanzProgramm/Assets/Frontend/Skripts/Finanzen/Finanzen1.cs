using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Finanzen1 : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    // Identische Farben wie in Finanzen 2 (FinanceDashboardBinder), damit
    // beide Screens exakt gleich aussehen.
    private static readonly Color GreenColor = new Color(0.502f, 0.812f, 0.584f, 1f);
    private static readonly Color RedColor   = new Color(0.902f, 0.224f, 0.275f, 1f);
    private static readonly Color TextColor  = new Color(0.863f, 0.863f, 0.863f, 1f);

    private int _gewaehltesJahr = DateTime.Today.Year;

    private void Start()
    {
        StartCoroutine(InitUI());
    }

    private IEnumerator InitUI()
    {
        yield return new WaitForEndOfFrame();
        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            Debug.LogWarning("[Finanzen1] uiDocument/rootVisualElement war beim ersten Frame noch null - " +
                "Initialisierung (inkl. Hilfe-Tooltips) wird verschoben.");
            yield return null; // einen weiteren Frame warten und erneut versuchen
            if (uiDocument == null || uiDocument.rootVisualElement == null) yield break;
        }

        // WICHTIG: Reihenfolge bewusst geändert. Vorher lief AktualisiereTabellen()
        // (liest/rechnet Kassenbuch-Daten) VOR RegistriereHelpTooltips() - flog dort
        // eine Exception (z.B. durch einen unerwarteten Dateneintrag), brach die
        // GESAMTE Coroutine ab und die Tooltips wurden nie registriert, ohne dass
        // man das ohne Blick in die Console gemerkt hätte. Jetzt: Tooltips + Jahres-
        // Auswahl zuerst, UND jeder Schritt einzeln try/catch-abgesichert, damit ein
        // Fehler in einem Bereich nicht die anderen mit runterreißt.
        try { RegistriereJahresAuswahl(); }
        catch (Exception e) { Debug.LogError("[Finanzen1] RegistriereJahresAuswahl fehlgeschlagen: " + e); }

        try { RegistriereHelpTooltips(); }
        catch (Exception e) { Debug.LogError("[Finanzen1] RegistriereHelpTooltips fehlgeschlagen: " + e); }

        try { ButtonHoverController.RegistriereAlle(uiDocument.rootVisualElement); }
        catch (Exception e) { Debug.LogError("[Finanzen1] ButtonHoverController fehlgeschlagen: " + e); }

        try { AktualisiereTabellen(); }
        catch (Exception e) { Debug.LogError("[Finanzen1] AktualisiereTabellen fehlgeschlagen: " + e); }
    }

    // FIX: Vorher gab es keine Möglichkeit, sich vergangene Jahre anzusehen -
    // die Auswertung war immer fest auf das aktuelle Jahr beschränkt. Jetzt
    // wie im Kassenbuch ein Jahres-Dropdown von 2010 bis heute.
    private void RegistriereJahresAuswahl()
    {
        var dropJahr = uiDocument.rootVisualElement.Q<DropdownField>("dropJahr");
        if (dropJahr == null) return;

        var jahre = new List<string>();
        for (int j = DateTime.Today.Year; j >= 2010; j--) jahre.Add(j.ToString());

        dropJahr.choices = jahre;
        dropJahr.value   = _gewaehltesJahr.ToString();
        dropJahr.RegisterValueChangedCallback(evt =>
        {
            if (int.TryParse(evt.newValue, out int neuesJahr))
            {
                _gewaehltesJahr = neuesJahr;
                AktualisiereTabellen();
            }
        });
    }

    // ============================================================
    // HELP TOOLTIPS
    // ============================================================

    private void RegistriereHelpTooltips()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Diese Seite zeigt dir die wichtigsten Finanzkennzahlen deines Unternehmens. " +
            "Alle Werte werden direkt aus deinem Kassenbuch berechnet.");

        HelpTooltip.Registriere(root, "btn-help-rentabilitaet",
            "Die Rentabilitätsübersicht zeigt Umsatz, Kosten und den verbleibenden " +
            "Überschuss oder Fehlbetrag. Ein positiver Überschuss bedeutet Gewinn.");

        HelpTooltip.Registriere(root, "btn-help-liquiditaet",
            "Zeigt die verfügbaren Mittel zu Geschäftsbeginn: " +
            "Einlagen, Kredite und Sonstiges abzüglich Steuern und Tilgung.");

        HelpTooltip.Registriere(root, "btn-help-liquiditaetsplanung",
            "Planung der monatlichen Zahlungsströme: " +
            "Anfangsbestand, Einnahmen, Ausgaben und der resultierende Endbestand.");
    }

    // ============================================================
    // TABELLEN
    // ============================================================

    public void AktualisiereTabellen()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;

        int jahr = _gewaehltesJahr;

        var jahrLabel = uiDocument.rootVisualElement.Q<Label>("lbl-jahr");
        if (jahrLabel != null) jahrLabel.text = $"Auswertung {jahr}";

        float gesamtEinn = 0f, personal = 0f, betrieb = 0f;
        float steuern = 0f, tilgung = 0f, privatentnahme = 0f, zinsen = 0f;
        float geldeinlagen = 0f, kredite = 0f, sonstEinz = 0f;
        float[] einnahmenMonat = new float[12];
        float[] ausgabenMonat  = new float[12];

        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db != null)
        {
            var einkList = db.getAllEinkommenEntries();
            if (einkList != null)
                foreach (var e in einkList)
                    if (DateTime.TryParse(e.getDatum(), out DateTime d) && d.Year == jahr)
                    {
                        einnahmenMonat[d.Month - 1] += e.Amount;
                        gesamtEinn += e.Amount;
                        string art = e.getArt() ?? "";
                        if (art == "Privateinzahlung")        geldeinlagen += e.Amount;
                        else if (art == "Darlehen")           kredite      += e.Amount;
                        else if (art == "Sonstige Einzahlung") sonstEinz   += e.Amount;
                    }

            var ausgList = db.getAllAusgabenEntries();
            if (ausgList != null)
                foreach (var a in ausgList)
                    if (DateTime.TryParse(a.getDatum(), out DateTime d) && d.Year == jahr)
                    {
                        ausgabenMonat[d.Month - 1] += a.Amount;
                        string art = a.getArt() ?? "";
                        if (art == "Gehälter")                           personal       += a.Amount;
                        else if (art == "Tilgungsraten")                 tilgung        += a.Amount;
                        else if (art == "Steuern" || art == "Finanzamt") steuern        += a.Amount;
                        else if (art == "Barentnahme / Privatentnahme")  privatentnahme += a.Amount;
                        // FIX: "Zinsen" hatte vorher keine eigene Art-Kategorie
                        // im Kassenbuch und stand deshalb immer fest auf 0.
                        // Jetzt gibt es "Zinsen" als eigene Ausgaben-Kategorie
                        // im Kassenbuch-Dropdown, wird hier eingerechnet UND
                        // aus "Betriebsausgaben" rausgehalten (sonst doppelt).
                        else if (art == "Zinsen")                        zinsen         += a.Amount;
                        else                                             betrieb        += a.Amount;
                    }
        }

        float ueberschuss = gesamtEinn - personal - betrieb - steuern - tilgung - privatentnahme - zinsen;

        // Kumulierter Saldo aus ALLEN Buchungen vor dem 1.1. des gewählten Jahres.
        float anfangsbestand = BerechneKumuliertenSaldoVorJahr(db, jahr);
        float endbestand = anfangsbestand + ueberschuss;

        // 1. RENTABILITÄT (Farben/Trennlinien wie Finanzen 2: Einnahmen grün,
        // Kosten rot, Summenzeile grün/rot je nach Vorzeichen)
        var containerRentabilitaet = uiDocument.rootVisualElement.Q<VisualElement>("Rentabilitaet");
        if (containerRentabilitaet != null)
        {
            containerRentabilitaet.Clear();
            containerRentabilitaet.Add(MakeHeader("Kategorie", "Betrag"));
            containerRentabilitaet.Add(MakeRow("Umsatzerlöse",        gesamtEinn,     GreenColor));
            containerRentabilitaet.Add(MakeRow("Personalausgaben",    personal,       RedColor));
            containerRentabilitaet.Add(MakeRow("Betriebsausgaben",    betrieb,        RedColor));
            containerRentabilitaet.Add(MakeRow("Steuern / Finanzamt", steuern,        RedColor));
            containerRentabilitaet.Add(MakeRow("Tilgungsraten",       tilgung,        RedColor));
            containerRentabilitaet.Add(MakeRow("Privatentnahmen",     privatentnahme, RedColor));
            containerRentabilitaet.Add(MakeTrenn());
            containerRentabilitaet.Add(MakeRow("Überschuss/Fehlbetrag", ueberschuss,
                ueberschuss >= 0 ? GreenColor : RedColor, true));
        }

        // 2. LIQUIDITÄT ZUM GESCHÄFTSBEGINN (Einlagen/Kredite = Zufluss -> grün,
        // Steuern/Tilgung = Abfluss -> rot)
        var containerLiquiditaet = uiDocument.rootVisualElement.Q<VisualElement>("Liquiditaet");
        if (containerLiquiditaet != null)
        {
            containerLiquiditaet.Clear();
            containerLiquiditaet.Add(MakeHeader("Kategorie", "Gründung"));
            containerLiquiditaet.Add(MakeRow("Geldeinlagen",        geldeinlagen, GreenColor));
            containerLiquiditaet.Add(MakeRow("Kredite / Darlehen",  kredite,      GreenColor));
            containerLiquiditaet.Add(MakeRow("Sonstige Einzahlung", sonstEinz,    GreenColor));
            containerLiquiditaet.Add(MakeRow("Steuern",             steuern,      RedColor));
            containerLiquiditaet.Add(MakeRow("Tilgungsraten",       tilgung,      RedColor));
            containerLiquiditaet.Add(MakeTrenn());
            float liqGesamt = geldeinlagen + kredite + sonstEinz - steuern - tilgung;
            containerLiquiditaet.Add(MakeRow("Liquidität gesamt", liqGesamt,
                liqGesamt >= 0 ? GreenColor : RedColor, true));
        }

        // 3. LIQUIDITÄTSPLANUNG (Anfangsbestand neutral, Umsatz grün, alle
        // Ausgaben rot, Überschuss/Endbestand grün/rot je nach Vorzeichen)
        var containerGruendung = uiDocument.rootVisualElement.Q<VisualElement>("Gruendung");
        if (containerGruendung != null)
        {
            containerGruendung.Clear();
            containerGruendung.Add(MakeHeader("Kategorie", "Betrag"));
            // FIX: Vorher generische Labels ohne Jahresbezug ("Anfangsbestand",
            // "Überschuss/Fehlbetrag", "Endbestand") - dadurch war nicht klar
            // erkennbar, auf welches Jahr sich welche Zeile bezieht bzw. dass
            // der Anfangsbestand aus dem Vorjahr resultiert.
            containerGruendung.Add(MakeRow($"Anfangsbestand {jahr} aus Vorjahr", anfangsbestand, TextColor));
            containerGruendung.Add(MakeRow("Umsatzerlöse",     gesamtEinn,     GreenColor));
            containerGruendung.Add(MakeRow("Personalausgaben", personal,       RedColor));
            containerGruendung.Add(MakeRow("Betriebsausgaben", betrieb,        RedColor));
            containerGruendung.Add(MakeRow("Zinsen",           zinsen,         RedColor));
            containerGruendung.Add(MakeRow("Tilgung",          tilgung,        RedColor));
            containerGruendung.Add(MakeRow("MwSt / Finanzamt", steuern,        RedColor));
            containerGruendung.Add(MakeRow("Privatentnahmen",  privatentnahme, RedColor));
            containerGruendung.Add(MakeTrenn());
            containerGruendung.Add(MakeRow($"Überschuss {jahr}", ueberschuss,
                ueberschuss >= 0 ? GreenColor : RedColor, true));
            containerGruendung.Add(MakeRow($"Endbestand {jahr}", endbestand,
                endbestand >= 0 ? GreenColor : RedColor, true));
        }
    }

    // Kumulierter Kassenbuch-Saldo aus ALLEN Buchungen VOR dem 1.1. des
    // übergebenen Jahres - das ist der Anfangsbestand, mit dem das Jahr
    // rechnerisch startet (analog zum GuV-Export im Kassenbuch).
    private float BerechneKumuliertenSaldoVorJahr(DataBase db, int jahr)
    {
        if (db == null) return 0f;
        float summe = 0f;

        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
            foreach (var e in einkommen)
                if (DateTime.TryParse(e.getDatum(), out DateTime d) && d.Year < jahr)
                    summe += e.Amount;

        var ausgaben = db.getAllAusgabenEntries();
        if (ausgaben != null)
            foreach (var a in ausgaben)
                if (DateTime.TryParse(a.getDatum(), out DateTime d) && d.Year < jahr)
                    summe -= a.Amount;

        return summe;
    }

    // ============================================================
    // HILFSMETHODEN (1:1 identisch zum Aufbau in Finanzen 2 /
    // FinanceDashboardBinder - gleiche Trennlinien, gleiche Abstände)
    // ============================================================

    private VisualElement MakeHeader(string links, string rechts)
    {
        var row = new VisualElement();
        row.style.flexDirection     = FlexDirection.Row;
        row.style.justifyContent    = Justify.SpaceBetween;
        row.style.paddingBottom     = 8;
        row.style.marginBottom      = 4;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new Color(1, 1, 1, 0.2f);
        row.Add(Lbl(links,  new Color(1, 1, 1), 13, true));
        row.Add(Lbl(rechts, new Color(1, 1, 1), 13, true));
        return row;
    }

    private VisualElement MakeRow(string name, float wert, Color wertFarbe, bool fett = false, string suffix = " €")
    {
        var row = new VisualElement();
        row.style.flexDirection     = FlexDirection.Row;
        row.style.justifyContent    = Justify.SpaceBetween;
        row.style.paddingTop        = 5;
        row.style.paddingBottom     = 5;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new Color(1, 1, 1, 0.04f);
        string wertText = (wert < 0 ? "-" : "") + Mathf.Abs(wert).ToString("N0") + suffix;
        row.Add(Lbl(name,     fett ? new Color(1, 1, 1) : TextColor, 12, fett));
        row.Add(Lbl(wertText, wertFarbe, 12, fett));
        return row;
    }

    private VisualElement MakeTrenn()
    {
        var line = new VisualElement();
        line.style.height          = 1;
        line.style.backgroundColor = new Color(1, 1, 1, 0.1f);
        line.style.marginTop       = 4;
        line.style.marginBottom    = 4;
        return line;
    }

    private Label Lbl(string text, Color farbe, int size, bool fett)
    {
        var l = new Label(text);
        l.style.color = farbe;
        l.style.fontSize = size;
        l.style.unityFontStyleAndWeight = fett ? FontStyle.Bold : FontStyle.Normal;
        return l;
    }
}
