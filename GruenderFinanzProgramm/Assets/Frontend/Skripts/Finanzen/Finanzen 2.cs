using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;

public class FinanceDashboardBinder : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private static readonly Color GreenColor  = new Color(0.502f, 0.812f, 0.584f, 1f);
    private static readonly Color RedColor    = new Color(0.902f, 0.224f, 0.275f, 1f);
    private static readonly Color TextColor   = new Color(0.863f, 0.863f, 0.863f, 1f);
    private static readonly Color HeaderColor = new Color(1f, 1f, 1f, 1f);

    private void Start()
    {
        StartCoroutine(InitUI());
    }

    private IEnumerator InitUI()
    {
        yield return new WaitForEndOfFrame();
        if (uiDocument == null || uiDocument.rootVisualElement == null) yield break;

        var root = uiDocument.rootVisualElement;
        int jahr = DateTime.Today.Year;

        var jahrLabel = root.Q<Label>("lbl-jahr");
        if (jahrLabel != null) jahrLabel.text = $"Auswertung {jahr}";

        // DB laden
        float gesamtEinn      = 0f;
        float reisekosten     = 0f;
        float tilgungsraten   = 0f;
        float steuern         = 0f;
        float finanzamt       = 0f;
        float gehaelter       = 0f;
        float marketing       = 0f;
        float sonstigeKosten  = 0f;
        float privatentnahme  = 0f;
        float sonstigeAusg    = 0f;
        float geldeinlagen    = 0f;
        float kredite         = 0f;
        var einnahmenMap = new Dictionary<string, float>();

        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db != null)
        {
            var einkList = db.getAllEinkommenEntries();
            if (einkList != null)
                foreach (var e in einkList)
                    if (DateTime.TryParse(e.getDatum(), out DateTime d) && d.Year == jahr)
                    {
                        gesamtEinn += e.Amount;
                        string art = string.IsNullOrEmpty(e.getArt()) ? "Sonstige Einzahlung" : e.getArt();
                        if (!einnahmenMap.ContainsKey(art)) einnahmenMap[art] = 0;
                        einnahmenMap[art] += e.Amount;
                        if (art == "Privateinzahlung")    geldeinlagen += e.Amount;
                        else if (art == "Darlehen")       kredite      += e.Amount;
                    }

            var ausgList = db.getAllAusgabenEntries();
            if (ausgList != null)
                foreach (var a in ausgList)
                    if (DateTime.TryParse(a.getDatum(), out DateTime d) && d.Year == jahr)
                    {
                        string art = string.IsNullOrEmpty(a.getArt()) ? "Sonstige Kosten" : a.getArt();
                        switch (art)
                        {
                            case "Gehälter":                          gehaelter      += a.Amount; break;
                            case "Marketing":                         marketing      += a.Amount; break;
                            case "Reisekosten":                       reisekosten    += a.Amount; break;
                            case "Steuern":                           steuern        += a.Amount; break;
                            case "Finanzamt":                         finanzamt      += a.Amount; break;
                            case "Tilgungsraten":                     tilgungsraten  += a.Amount; break;
                            case "Barentnahme / Privatentnahme":      privatentnahme += a.Amount; break;
                            case "Sonstige Kosten":                   sonstigeKosten += a.Amount; break;
                            default:                                  sonstigeAusg   += a.Amount; break;
                        }
                    }
        }

        float direkteKosten  = gehaelter + sonstigeKosten + sonstigeAusg;
        float betriebGesamt  = marketing + reisekosten + steuern + finanzamt + tilgungsraten + privatentnahme;
        float gesamtAusgaben = direkteKosten + betriebGesamt;
        float rohgewinn      = gesamtEinn - direkteKosten;
        float rohProzent     = gesamtEinn > 0 ? (rohgewinn / gesamtEinn) * 100f : 0f;

        // ERTRAGSQUELLEN
        var cErtrag = root.Q<VisualElement>("Ertragsquellen");
        if (cErtrag != null)
        {
            cErtrag.Clear();
            cErtrag.Add(MakeHeader("Kategorie", "Betrag"));
            foreach (var kv in einnahmenMap)
                cErtrag.Add(MakeRow(kv.Key, kv.Value, GreenColor));
            cErtrag.Add(MakeTrenn());
            cErtrag.Add(MakeRow("Summe Umsätze",   gesamtEinn,  GreenColor, true));
            cErtrag.Add(MakeRow("Summe Kosten",    gesamtAusgaben, RedColor, true));
            cErtrag.Add(MakeRow("Rohgewinn",       rohgewinn,   rohgewinn >= 0 ? GreenColor : RedColor, true));
            cErtrag.Add(MakeRow("Rohgewinn %",     rohProzent,  rohProzent >= 0 ? GreenColor : RedColor, true, " %"));
        }

        // DIREKTE KOSTEN
        var cDirekt = root.Q<VisualElement>("DirekteKosten");
        if (cDirekt != null)
        {
            cDirekt.Clear();
            cDirekt.Add(MakeHeader("Kategorie", "Betrag"));
            cDirekt.Add(MakeRow("Gehälter / Honorar", gehaelter,     RedColor));
            cDirekt.Add(MakeRow("Sonstige Kosten",    sonstigeKosten + sonstigeAusg, RedColor));
            cDirekt.Add(MakeTrenn());
            cDirekt.Add(MakeRow("Gesamt direkte Kosten", direkteKosten, RedColor, true));
        }

        // BETRIEBSAUSGABEN
        var cBetrieb = root.Q<VisualElement>("Betriebsausgaben");
        if (cBetrieb != null)
        {
            cBetrieb.Clear();
            cBetrieb.Add(MakeHeader("Kategorie", "Betrag"));
            cBetrieb.Add(MakeRow("Marketing",      marketing,     RedColor));
            cBetrieb.Add(MakeRow("Reisekosten",    reisekosten,   RedColor));
            cBetrieb.Add(MakeRow("Steuern",        steuern,       RedColor));
            cBetrieb.Add(MakeRow("Finanzamt",      finanzamt,     RedColor));
            cBetrieb.Add(MakeRow("Tilgungsraten",  tilgungsraten, RedColor));
            cBetrieb.Add(MakeRow("Privatentnahmen",privatentnahme,RedColor));
            cBetrieb.Add(MakeTrenn());
            cBetrieb.Add(MakeRow("Gesamt Betrieb", betriebGesamt, RedColor, true));
        }

        // INVESTITION
        var cInvest = root.Q<VisualElement>("Investition");
        if (cInvest != null)
        {
            cInvest.Clear();
            cInvest.Add(MakeHeader("Kategorie", "Betrag"));
            cInvest.Add(MakeRow("Mustermodell",      0f, TextColor));
            cInvest.Add(MakeRow("Büroausstattung",   0f, TextColor));
            cInvest.Add(MakeTrenn());
            cInvest.Add(MakeRow("Summe Investition", 0f, TextColor, true));
            cInvest.Add(MakeRow("Summe Sacheinlagen",0f, TextColor, true));
        }

        // GRÜNDERKOSTEN
        // Diese Kategorien kommen aus "Sonstige Kosten" im Kassenbuch.
        // Für detaillierte Aufschlüsselung (Homepage, Corporate Design etc.)
        // bitte entsprechende Buchungen im Kassenbuch unter "Sonstige Kosten" erfassen.
        float sonstigeKostenGruend = sonstigeKosten + sonstigeAusg;
        var cGruend = root.Q<VisualElement>("Grunderkosten");
        if (cGruend != null)
        {
            cGruend.Clear();
            cGruend.Add(MakeHeader("Kategorie", "Betrag"));
            cGruend.Add(MakeRow("Corporate Design",     0f, TextColor));
            cGruend.Add(MakeRow("Grundausstattung",     0f, TextColor));
            cGruend.Add(MakeRow("Homepage",             0f, TextColor));
            cGruend.Add(MakeTrenn());
            // Summe aus "Sonstige Kosten" im Kassenbuch
            cGruend.Add(MakeRow("Sonstige Kosten (Kassenbuch)", sonstigeKostenGruend, sonstigeKostenGruend > 0 ? RedColor : TextColor));
            cGruend.Add(MakeTrenn());
            cGruend.Add(MakeRow("Summe Gründungskosten", sonstigeKostenGruend, sonstigeKostenGruend > 0 ? RedColor : TextColor, true));
        }

        // KAPITALBEDARF
        var cKapital = root.Q<VisualElement>("Kapitalbedarf");
        if (cKapital != null)
        {
            float gesamtKapital = geldeinlagen + kredite;
            cKapital.Clear();
            cKapital.Add(MakeHeader("Kategorie", "Betrag"));
            cKapital.Add(MakeRow("Investition",        0f,           TextColor));
            cKapital.Add(MakeRow("Sacheinlagen",       0f,           TextColor));
            cKapital.Add(MakeRow("Gründerkosten",      0f,           TextColor));
            cKapital.Add(MakeTrenn());
            cKapital.Add(MakeRow("Kapitalbedarf",      0f,           TextColor, true));
            cKapital.Add(MakeRow("Liquiditätsreserve", 0f,           TextColor));
            cKapital.Add(MakeTrenn());
            cKapital.Add(MakeRow("Gesamtkapitalbedarf",gesamtKapital,
                gesamtKapital >= 0 ? GreenColor : RedColor, true));
        }
    }

    private VisualElement MakeHeader(string links, string rechts)
    {
        var row = new VisualElement();
        row.style.flexDirection     = FlexDirection.Row;
        row.style.justifyContent    = Justify.SpaceBetween;
        row.style.paddingBottom     = 8;
        row.style.marginBottom      = 4;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new Color(1, 1, 1, 0.2f);
        row.Add(Lbl(links,  new Color(1,1,1), 13, true));
        row.Add(Lbl(rechts, new Color(1,1,1), 13, true));
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
        row.Add(Lbl(name,     fett ? new Color(1,1,1) : TextColor, 12, fett));
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
