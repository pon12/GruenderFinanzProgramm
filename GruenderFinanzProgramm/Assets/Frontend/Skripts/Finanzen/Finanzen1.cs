using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Finanzen1 : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement _root;

    // Ventoriq-Farben
    private static readonly Color GreenColor  = new Color(0.502f, 0.812f, 0.584f, 1f);
    private static readonly Color RedColor    = new Color(0.902f, 0.224f, 0.275f, 1f);
    private static readonly Color GridColor   = new Color(0.25f,  0.25f,  0.25f,  1f);
    private static readonly Color TextColor   = new Color(0.863f, 0.863f, 0.863f, 1f);
    private static readonly Color HeaderColor = new Color(1f,     1f,     1f,     1f);
    private static readonly Color SubColor    = new Color(0.627f, 0.627f, 0.627f, 1f);

    private void Start()
    {
        StartCoroutine(InitUI());
    }

    private IEnumerator InitUI()
    {
        yield return new WaitForEndOfFrame();

        if (uiDocument == null || uiDocument.rootVisualElement == null) yield break;
        _root = uiDocument.rootVisualElement;

        int jahr = DateTime.Today.Year;

        var jahrLabel = _root.Q<Label>("lbl-jahr");
        if (jahrLabel != null) jahrLabel.text = $"Auswertung {jahr}";

        AktualisiereRentabilitaet(jahr);
        AktualisiereLiquiditaet(jahr);
        AktualisiereChart(jahr);
    }

    // ============================================================
    // RENTABILITÄT
    // Zeigt Einnahmen- und Ausgaben-Kategorien für das aktuelle Jahr
    // ============================================================
    private void AktualisiereRentabilitaet(int jahr)
    {
        var container = _root.Q<VisualElement>("Rentabilitaet");
        if (container == null) return;
        container.Clear();

        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        // Einnahmen nach Art summieren
        var einnahmenMap = new Dictionary<string, float>();
        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
            foreach (var e in einkommen)
                if (DateTime.TryParse(e.getDatum(), out DateTime d) && d.Year == jahr)
                {
                    string art = string.IsNullOrEmpty(e.getArt()) ? "Sonstige Einnahmen" : e.getArt();
                    if (!einnahmenMap.ContainsKey(art)) einnahmenMap[art] = 0;
                    einnahmenMap[art] += e.Amount;
                }

        // Ausgaben nach Art summieren
        var ausgabenMap = new Dictionary<string, float>
        {
            { "Gehälter",                   0 },
            { "Marketing",                  0 },
            { "Reisekosten",                0 },
            { "Steuern",                    0 },
            { "Finanzamt",                  0 },
            { "Tilgungsraten",              0 },
            { "Barentnahme / Privatentnahme", 0 },
            { "Sonstige Kosten",            0 },
        };
        var ausgaben = db.getAllAusgabenEntries();
        if (ausgaben != null)
            foreach (var a in ausgaben)
                if (DateTime.TryParse(a.getDatum(), out DateTime d) && d.Year == jahr)
                {
                    string art = string.IsNullOrEmpty(a.getArt()) ? "Sonstige Kosten" : a.getArt();
                    if (!ausgabenMap.ContainsKey(art)) ausgabenMap[art] = 0;
                    ausgabenMap[art] += a.Amount;
                }

        float gesamtEinnahmen = einnahmenMap.Values.Sum();
        float gesamtAusgaben  = ausgabenMap.Values.Sum();
        float ueberschuss     = gesamtEinnahmen - gesamtAusgaben;

        // Header
        container.Add(CreateTabelleHeader("Kategorie", "Betrag"));

        // Einnahmen-Sektion
        container.Add(CreateSektionLabel("── Einnahmen"));
        foreach (var kv in einnahmenMap.Where(k => k.Value > 0))
            container.Add(CreateTabelleRow(kv.Key, kv.Value, GreenColor));
        container.Add(CreateTabelleRow("Gesamt Einnahmen", gesamtEinnahmen, GreenColor, true));

        // Trennlinie
        container.Add(CreateTrennlinie());

        // Ausgaben-Sektion
        container.Add(CreateSektionLabel("── Ausgaben"));
        foreach (var kv in ausgabenMap.Where(k => k.Value > 0))
            container.Add(CreateTabelleRow(kv.Key, kv.Value, RedColor));
        container.Add(CreateTabelleRow("Gesamt Ausgaben", gesamtAusgaben, RedColor, true));

        // Trennlinie
        container.Add(CreateTrennlinie());

        // Überschuss/Fehlbetrag
        Color ueberschussColor = ueberschuss >= 0 ? GreenColor : RedColor;
        container.Add(CreateTabelleRow("Überschuss / Fehlbetrag", ueberschuss, ueberschussColor, true));
    }

    // ============================================================
    // LIQUIDITÄT ZUM GESCHÄFTSBEGINN
    // Zeigt Startkapital-relevante Einnahmen (Einlagen, Kredite etc.)
    // ============================================================
    private void AktualisiereLiquiditaet(int jahr)
    {
        var container = _root.Q<VisualElement>("Liquiditaet");
        if (container == null) return;
        container.Clear();

        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        var liquidMap = new Dictionary<string, float>
        {
            { "Privateinzahlung",  0 },
            { "Darlehen",          0 },
            { "Sonstige Einzahlung", 0 },
        };

        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
            foreach (var e in einkommen)
                if (DateTime.TryParse(e.getDatum(), out DateTime d) && d.Year == jahr)
                {
                    string art = e.getArt();
                    if (liquidMap.ContainsKey(art))
                        liquidMap[art] += e.Amount;
                }

        var ausgaben = db.getAllAusgabenEntries();
        var gruendungskostenMap = new Dictionary<string, float>
        {
            { "Tilgungsraten", 0 },
            { "Finanzamt",     0 },
        };
        if (ausgaben != null)
            foreach (var a in ausgaben)
                if (DateTime.TryParse(a.getDatum(), out DateTime d) && d.Year == jahr)
                {
                    string art = a.getArt();
                    if (gruendungskostenMap.ContainsKey(art))
                        gruendungskostenMap[art] += a.Amount;
                }

        float gesamtZufluss  = liquidMap.Values.Sum();
        float gesamtAbfluss  = gruendungskostenMap.Values.Sum();
        float liquiditaet    = gesamtZufluss - gesamtAbfluss;

        container.Add(CreateTabelleHeader("Kategorie", "Betrag"));

        container.Add(CreateSektionLabel("── Zufluss"));
        foreach (var kv in liquidMap)
            container.Add(CreateTabelleRow(kv.Key, kv.Value, GreenColor));

        container.Add(CreateTrennlinie());

        container.Add(CreateSektionLabel("── Abfluss"));
        foreach (var kv in gruendungskostenMap)
            container.Add(CreateTabelleRow(kv.Key, kv.Value, RedColor));

        container.Add(CreateTrennlinie());

        Color liqColor = liquiditaet >= 0 ? GreenColor : RedColor;
        container.Add(CreateTabelleRow("Liquidität gesamt", liquiditaet, liqColor, true));
    }

    // ============================================================
    // CHART: Einnahmen vs Ausgaben monatlich
    // ============================================================
    private void AktualisiereChart(int jahr)
    {
        var container = _root.Q<VisualElement>("chart-container");
        if (container == null) return;

        var db = UserDatabaseAccess.getCurrentUserDatabase();
        float[] einnahmen = new float[12];
        float[] ausgaben  = new float[12];

        if (db != null)
        {
            var einkommen = db.getAllEinkommenEntries();
            if (einkommen != null)
                foreach (var e in einkommen)
                    if (DateTime.TryParse(e.getDatum(), out DateTime d) && d.Year == jahr)
                        einnahmen[d.Month - 1] += e.Amount;

            var ausgabenList = db.getAllAusgabenEntries();
            if (ausgabenList != null)
                foreach (var a in ausgabenList)
                    if (DateTime.TryParse(a.getDatum(), out DateTime d) && d.Year == jahr)
                        ausgaben[d.Month - 1] += a.Amount;
        }

        var chart = new FinanzChartElement();
        chart.style.position = Position.Absolute;
        chart.style.left = 0; chart.style.right  = 0;
        chart.style.top  = 0; chart.style.bottom = 0;
        container.Add(chart);
        chart.SetData(einnahmen, ausgaben);
    }

    // ============================================================
    // UI HELPERS
    // ============================================================
    private VisualElement CreateTabelleHeader(string links, string rechts)
    {
        var row = new VisualElement();
        row.style.flexDirection   = FlexDirection.Row;
        row.style.justifyContent  = Justify.SpaceBetween;
        row.style.paddingBottom   = 6;
        row.style.marginBottom    = 4;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new Color(1, 1, 1, 0.2f);

        row.Add(MakeLabel(links,  HeaderColor, 14, true));
        row.Add(MakeLabel(rechts, HeaderColor, 14, true));
        return row;
    }

    private VisualElement CreateTabelleRow(string name, float wert, Color wertFarbe, bool fett = false)
    {
        var row = new VisualElement();
        row.style.flexDirection  = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.paddingTop     = 5;
        row.style.paddingBottom  = 5;

        string wertText = (wert < 0 ? "-" : "") + Mathf.Abs(wert).ToString("N0") + " €";

        row.Add(MakeLabel(name,     fett ? HeaderColor : TextColor, 13, fett));
        row.Add(MakeLabel(wertText, wertFarbe, 13, fett));
        return row;
    }

    private VisualElement CreateSektionLabel(string text)
    {
        var row = new VisualElement();
        row.style.paddingTop    = 8;
        row.style.paddingBottom = 4;
        row.Add(MakeLabel(text, SubColor, 11, false));
        return row;
    }

    private VisualElement CreateTrennlinie()
    {
        var line = new VisualElement();
        line.style.height           = 1;
        line.style.backgroundColor  = new Color(1, 1, 1, 0.1f);
        line.style.marginTop        = 6;
        line.style.marginBottom     = 6;
        return line;
    }

    private Label MakeLabel(string text, Color farbe, int size, bool fett)
    {
        var l = new Label(text);
        l.style.color = farbe;
        l.style.fontSize = size;
        l.style.unityFontStyleAndWeight = fett ? FontStyle.Bold : FontStyle.Normal;
        return l;
    }
}
