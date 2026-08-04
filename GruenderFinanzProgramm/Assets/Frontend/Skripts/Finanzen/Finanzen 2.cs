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

    private int _gewaehltesJahr = DateTime.Today.Year;

    // Merkt sich die aktuelle DB-Referenz, damit das Eingabefeld für die
    // Liquiditätsreserve auch außerhalb von BefuelleAlles() speichern kann.
    private DataBase _db;
    private const int SETTINGS_USER_ID = 0; // Settings sind je Nutzer-DB-Datei eindeutig

    private void Start()
    {
        StartCoroutine(InitUI());
    }

    private IEnumerator InitUI()
    {
        yield return new WaitForEndOfFrame();
        if (uiDocument == null || uiDocument.rootVisualElement == null) yield break;

        _db = UserDatabaseAccess.getCurrentUserDatabase();

        try { RegistriereJahresAuswahl(); }
        catch (Exception e) { Debug.LogError("[Finanzierung] RegistriereJahresAuswahl fehlgeschlagen: " + e); }

        try { RegistriereHelpTooltips(); }
        catch (Exception e) { Debug.LogError("[Finanzierung] RegistriereHelpTooltips fehlgeschlagen: " + e); }

        try { RegistriereLiquiditaetsreserveEingabe(); }
        catch (Exception e) { Debug.LogError("[Finanzierung] RegistriereLiquiditaetsreserveEingabe fehlgeschlagen: " + e); }

        try { BefuelleAlles(); }
        catch (Exception e) { Debug.LogError("[Finanzierung] BefuelleAlles fehlgeschlagen: " + e); }
    }

    // FIX: Die 7 Hilfe-Icons im UXML (btn-help-seitentitel, -ertragsquellen,
    // -direkte-kosten, -betriebsausgaben, -investition, -gruenderkosten,
    // -kapitalbedarf) waren nie mit HelpTooltip.Registriere(...) verknüpft -
    // deshalb passierte beim Hovern buchstäblich gar nichts. War schlicht nie
    // eingebaut worden (kein Regressions-Bug, hat hier einfach noch gefehlt).
    private void RegistriereHelpTooltips()
    {
        var root = uiDocument.rootVisualElement;

        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Diese Seite zeigt dir die Finanzierungs-Übersicht deines Unternehmens: " +
            "Erträge, Kosten und den Kapitalbedarf für die Gründung.");

        HelpTooltip.Registriere(root, "btn-help-ertragsquellen",
            "Zeigt, aus welchen Quellen dein Umsatz kommt, sowie Summe Umsätze, " +
            "Summe Kosten und den daraus resultierenden Rohgewinn.");

        HelpTooltip.Registriere(root, "btn-help-direkte-kosten",
            "Kosten, die direkt mit deiner Leistungserbringung zusammenhängen, " +
            "z. B. Gehälter/Honorare und sonstige direkte Kosten.");

        HelpTooltip.Registriere(root, "btn-help-betriebsausgaben",
            "Laufende Ausgaben für den Geschäftsbetrieb: Marketing, Reisekosten, " +
            "Steuern, Tilgungsraten und Privatentnahmen.");

        HelpTooltip.Registriere(root, "btn-help-investition",
            "Anschaffungen zum Start deines Unternehmens, z. B. Büroausstattung, " +
            "Fuhrpark, Maschinen/Anlagen und Software/Lizenzen.");

        HelpTooltip.Registriere(root, "btn-help-gruenderkosten",
            "Einmalige Kosten rund um die Gründung: Corporate Design, Homepage, " +
            "Grundausstattung und sonstige Kosten aus dem Kassenbuch.");

        HelpTooltip.Registriere(root, "btn-help-kapitalbedarf",
            "Wie viel Kapital du insgesamt für die Gründung brauchst: Investition + " +
            "Gründerkosten + deine Ziel-Liquiditätsreserve (siehe Eingabefeld oben).");
    }

    // Ziel-Liquiditätsreserve ist eine reine Planungsgröße (Puffer-Betrag),
    // kein Kassenbuch-Wert - wird deshalb manuell erfasst und in den
    // Settings der Nutzer-DB gespeichert (siehe DataBase.getLiquiditaetsreserveZiel).
    private void RegistriereLiquiditaetsreserveEingabe()
    {
        var input = uiDocument.rootVisualElement.Q<TextField>("input-liquiditaetsreserve");
        if (input == null || _db == null) return;

        float aktuelleReserve = _db.getLiquiditaetsreserveZiel(SETTINGS_USER_ID);
        input.SetValueWithoutNotify(aktuelleReserve.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("de-DE")));

        input.RegisterCallback<FocusOutEvent>(_ => SpeichereLiquiditaetsreserve(input));
        input.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                SpeichereLiquiditaetsreserve(input);
        });
    }

    private void SpeichereLiquiditaetsreserve(TextField input)
    {
        if (_db == null) return;

        string text = (input.value ?? "").Replace(".", "").Replace(",", ".");
        if (!float.TryParse(text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float wert))
        {
            wert = 0f;
        }
        if (wert < 0) wert = 0f;

        _db.setLiquiditaetsreserveZiel(SETTINGS_USER_ID, wert);
        input.SetValueWithoutNotify(wert.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("de-DE")));

        // Kapitalbedarf-Block sofort mit dem neuen Wert neu berechnen
        BefuelleAlles();
    }

    // FIX: Vorher fest auf das aktuelle Jahr beschränkt - keine Möglichkeit,
    // sich vergangene Jahre (2010 bis heute) anzusehen. Gleiches
    // Dropdown-Muster wie im Kassenbuch und bei Finanzen 1.
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
                BefuelleAlles();
            }
        });
    }

    private void BefuelleAlles()
    {
        var root = uiDocument.rootVisualElement;
        int jahr = _gewaehltesJahr;

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

        // FIX: Neue Investitions- und Gründerkosten-Kategorien im Kassenbuch
        // (waren vorher gar nicht vorhanden -> "wieso keine Ausgabe mit
        // Investition?"). Jeweils mind. 3 gängige Kategorien nach
        // Businessplan-Konvention ergänzt.
        float invBueroausstattung = 0f, invFuhrpark = 0f, invMaschinen = 0f, invSoftware = 0f;
        float gkCorporateDesign   = 0f, gkHomepage   = 0f, gkGrundausstattung = 0f;

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
                            case "Gehälter":                          gehaelter          += a.Amount; break;
                            case "Marketing":                         marketing          += a.Amount; break;
                            case "Reisekosten":                       reisekosten        += a.Amount; break;
                            case "Steuern":                           steuern            += a.Amount; break;
                            case "Finanzamt":                         finanzamt          += a.Amount; break;
                            case "Tilgungsraten":                     tilgungsraten      += a.Amount; break;
                            case "Barentnahme / Privatentnahme":      privatentnahme     += a.Amount; break;
                            case "Sonstige Kosten":                   sonstigeKosten     += a.Amount; break;
                            case "Büroausstattung":                   invBueroausstattung += a.Amount; break;
                            case "Fuhrpark":                          invFuhrpark        += a.Amount; break;
                            case "Maschinen / Anlagen":               invMaschinen       += a.Amount; break;
                            case "Software / Lizenzen":               invSoftware        += a.Amount; break;
                            case "Corporate Design":                  gkCorporateDesign  += a.Amount; break;
                            case "Homepage":                          gkHomepage         += a.Amount; break;
                            case "Grundausstattung":                  gkGrundausstattung += a.Amount; break;
                            default:                                  sonstigeAusg       += a.Amount; break;
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
        // FIX: Vorher fest "Mustermodell"/"Büroausstattung" mit 0f, ohne
        // Bezug zu echten Kassenbuch-Kategorien. Jetzt 4 echte
        // Investitionsarten (Businessplan-Standardkategorien), die auch im
        // Kassenbuch-Dropdown als Art auswählbar sind.
        float summeInvestition = invBueroausstattung + invFuhrpark + invMaschinen + invSoftware;
        var cInvest = root.Q<VisualElement>("Investition");
        if (cInvest != null)
        {
            cInvest.Clear();
            cInvest.Add(MakeHeader("Kategorie", "Betrag"));
            cInvest.Add(MakeRow("Büroausstattung",    invBueroausstattung, RedColor));
            cInvest.Add(MakeRow("Fuhrpark",           invFuhrpark,         RedColor));
            cInvest.Add(MakeRow("Maschinen / Anlagen",invMaschinen,        RedColor));
            cInvest.Add(MakeRow("Software / Lizenzen",invSoftware,         RedColor));
            cInvest.Add(MakeTrenn());
            cInvest.Add(MakeRow("Summe Investition",  summeInvestition,    RedColor, true));
            // Sacheinlagen (Geräte etc. die eingebracht statt gekauft werden)
            // sind keine Kassenbuch-Buchung, sondern eine reine
            // Businessplan-Angabe - dafür gibt es hier bewusst keine
            // automatische Herleitung.
            cInvest.Add(MakeRow("Summe Sacheinlagen", 0f, TextColor, true));
        }

        // GRÜNDERKOSTEN
        // FIX: Vorher fest "Corporate Design"/"Grundausstattung"/"Homepage"
        // mit 0f. Jetzt echte Kassenbuch-Kategorien, plus weiterhin der
        // Sammel-Posten "Sonstige Kosten (Kassenbuch)" für alles, was in
        // keine der 3 spezifischen Kategorien passt.
        float sonstigeKostenGruend = sonstigeKosten + sonstigeAusg;
        float summeGruenderkosten  = gkCorporateDesign + gkHomepage + gkGrundausstattung + sonstigeKostenGruend;
        var cGruend = root.Q<VisualElement>("Grunderkosten");
        if (cGruend != null)
        {
            cGruend.Clear();
            cGruend.Add(MakeHeader("Kategorie", "Betrag"));
            cGruend.Add(MakeRow("Corporate Design",     gkCorporateDesign,   RedColor));
            cGruend.Add(MakeRow("Grundausstattung",     gkGrundausstattung,  RedColor));
            cGruend.Add(MakeRow("Homepage",             gkHomepage,          RedColor));
            cGruend.Add(MakeRow("Sonstige Kosten (Kassenbuch)", sonstigeKostenGruend, sonstigeKostenGruend > 0 ? RedColor : TextColor));
            cGruend.Add(MakeTrenn());
            cGruend.Add(MakeRow("Summe Gründungskosten", summeGruenderkosten, summeGruenderkosten > 0 ? RedColor : TextColor, true));
        }

        // KAPITALBEDARF
        var cKapital = root.Q<VisualElement>("Kapitalbedarf");
        if (cKapital != null)
        {
            float liquiditaetsreserve = db != null ? db.getLiquiditaetsreserveZiel(SETTINGS_USER_ID) : 0f;
            // Kapitalbedarf = Investition + Gründerkosten + Ziel-Liquiditätsreserve
            // (Standard-Formel im Kapitalbedarfsplan). Vorher floss die Reserve
            // gar nicht in die Summe ein, weil sie fest 0 war.
            float kapitalbedarf = summeInvestition + summeGruenderkosten + liquiditaetsreserve;
            float gesamtKapital = geldeinlagen + kredite;

            cKapital.Clear();
            cKapital.Add(MakeHeader("Kategorie", "Betrag"));
            cKapital.Add(MakeRow("Investition",        summeInvestition,   RedColor));
            // Sacheinlagen: siehe Hinweis oben bei "Investition" - keine
            // Kassenbuch-Buchung, daher bewusst ohne automatische Herleitung.
            cKapital.Add(MakeRow("Sacheinlagen",       0f,                  TextColor));
            cKapital.Add(MakeRow("Gründerkosten",      summeGruenderkosten, RedColor));
            cKapital.Add(MakeRow("Liquiditätsreserve", liquiditaetsreserve, liquiditaetsreserve > 0 ? RedColor : TextColor));
            cKapital.Add(MakeTrenn());
            cKapital.Add(MakeRow("Kapitalbedarf",      kapitalbedarf,       RedColor, true));
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
