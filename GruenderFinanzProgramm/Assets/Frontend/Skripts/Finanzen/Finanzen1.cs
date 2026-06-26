using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Finanzen1 : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    
    private void Start()
    {
        StartCoroutine(InitUI());
    }

    private IEnumerator InitUI()
    {
        yield return new WaitForEndOfFrame(); 
        AktualisiereTabellen();
    }

    public void AktualisiereTabellen()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;

        // DB laden
        float gesamtEinn = 0f, personal = 0f, betrieb = 0f;
        float steuern = 0f, tilgung = 0f, privatentnahme = 0f;
        float geldeinlagen = 0f, kredite = 0f, sonstEinz = 0f;
        float[] einnahmenMonat = new float[12];
        float[] ausgabenMonat  = new float[12];

        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db != null)
        {
            int jahr = DateTime.Today.Year;

            var einkList = db.getAllEinkommenEntries();
            if (einkList != null)
                foreach (var e in einkList)
                    if (DateTime.TryParse(e.getDatum(), out DateTime d) && d.Year == jahr)
                    {
                        einnahmenMonat[d.Month - 1] += e.Amount;
                        gesamtEinn += e.Amount;
                        string art = e.getArt() ?? "";
                        if (art == "Privateinzahlung")    geldeinlagen += e.Amount;
                        else if (art == "Darlehen")       kredite      += e.Amount;
                        else if (art == "Sonstige Einzahlung") sonstEinz += e.Amount;
                    }

            var ausgList = db.getAllAusgabenEntries();
            if (ausgList != null)
                foreach (var a in ausgList)
                    if (DateTime.TryParse(a.getDatum(), out DateTime d) && d.Year == jahr)
                    {
                        ausgabenMonat[d.Month - 1] += a.Amount;
                        string art = a.getArt() ?? "";
                        if (art == "Gehälter")                          personal       += a.Amount;
                        else if (art == "Tilgungsraten")                tilgung        += a.Amount;
                        else if (art == "Steuern" || art == "Finanzamt") steuern       += a.Amount;
                        else if (art == "Barentnahme / Privatentnahme") privatentnahme += a.Amount;
                        else                                             betrieb        += a.Amount;
                    }
        }

        float ueberschuss = gesamtEinn - personal - betrieb - steuern - tilgung - privatentnahme;

        // 1. RENTABILITÄT
        VisualElement containerRentabilitaet = uiDocument.rootVisualElement.Q<VisualElement>("Rentabilitaet");
        if (containerRentabilitaet != null)
        {
            containerRentabilitaet.Clear();
            containerRentabilitaet.Add(CreateHeader());
            containerRentabilitaet.Add(CreateRow("Umsatzerlöse",           gesamtEinn,     0));
            containerRentabilitaet.Add(CreateRow("Personalausgaben",       personal,       0));
            containerRentabilitaet.Add(CreateRow("Betriebsausgaben",       betrieb,        0));
            containerRentabilitaet.Add(CreateRow("Steuern / Finanzamt",    steuern,        0));
            containerRentabilitaet.Add(CreateRow("Tilgungsraten",          tilgung,        0));
            containerRentabilitaet.Add(CreateRow("Privatentnahmen",        privatentnahme, 0));
            containerRentabilitaet.Add(CreateRow("Überschuss/Fehlbetrag",  ueberschuss,    0, true));
        }

        // 2. LIQUIDITÄT ZUM GESCHÄFTSBEGINN
        VisualElement containerLiquiditaet = uiDocument.rootVisualElement.Q<VisualElement>("Liquiditaet");
        if (containerLiquiditaet != null)
        {
            containerLiquiditaet.Clear();
            containerLiquiditaet.Add(CreateHeader2());
            containerLiquiditaet.Add(CreateRow2("Geldeinlagen",        geldeinlagen));
            containerLiquiditaet.Add(CreateRow2("Kredite / Darlehen",  kredite));
            containerLiquiditaet.Add(CreateRow2("Sonstige Einzahlung", sonstEinz));
            containerLiquiditaet.Add(CreateRow2("Steuern",             steuern));
            containerLiquiditaet.Add(CreateRow2("Tilgungsraten",       tilgung));
            float liqGesamt = geldeinlagen + kredite + sonstEinz - steuern - tilgung;
            containerLiquiditaet.Add(CreateRow2("Liquidität gesamt",   liqGesamt, true));
        }

        // 3. LIQUIDITÄTSPLANUNG
        VisualElement containerGruendung = uiDocument.rootVisualElement.Q<VisualElement>("Gruendung");
        if (containerGruendung != null)
        {
            containerGruendung.Clear();
            containerGruendung.Add(CreateHeader());
            containerGruendung.Add(CreateRow("Anfangsbestand",         0f,             0));
            containerGruendung.Add(CreateRow("Umsatzerlöse",           gesamtEinn,     0));
            containerGruendung.Add(CreateRow("Personalausgaben",       personal,       0));
            containerGruendung.Add(CreateRow("Betriebsausgaben",       betrieb,        0));
            containerGruendung.Add(CreateRow("Zinsen",                 0f,             0));
            containerGruendung.Add(CreateRow("Tilgung",                tilgung,        0));
            containerGruendung.Add(CreateRow("MwSt / Finanzamt",       steuern,        0));
            containerGruendung.Add(CreateRow("Privatentnahmen",        privatentnahme, 0));
            containerGruendung.Add(CreateRow("Überschuss/Fehlbetrag",  ueberschuss,    0, true));
            containerGruendung.Add(CreateRow("Endbestand",             ueberschuss,    0, true));
        }


    }

    // Header mit 2 Spalten (Kategorie | Betrag)
    private VisualElement CreateHeader()
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
        row.Add(CreateCell("Kategorie", true, 60));
        row.Add(CreateCell("Betrag",    true, 40));
        return row;
    }

    // Header mit 2 Spalten (Kategorie | Gründung)
    private VisualElement CreateHeader2()
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
        row.Add(CreateCell("Kategorie", true, 60));
        row.Add(CreateCell("Gründung",  true, 40));
        return row;
    }

    private VisualElement CreateRow(string name, float wert, int unused, bool isBold = false)
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
        row.Add(CreateCell(name, isBold, 60));
        row.Add(CreateCell(wert.ToString("N0") + " €", isBold, 40));
        return row;
    }

    private VisualElement CreateRow2(string name, float wert, bool isBold = false)
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
        row.Add(CreateCell(name, isBold, 60));
        row.Add(CreateCell(wert.ToString("N0") + " €", isBold, 40));
        return row;
    }

    private VisualElement CreateCell(string text, bool isBold, int widthPercent)
    {
        var label = new Label(text);
        label.style.color = Color.white;
        label.style.unityFontStyleAndWeight = isBold ? FontStyle.Bold : FontStyle.Normal;
        label.style.flexGrow = 1;
        label.style.width = Length.Percent(widthPercent);
        return label;
    }
}
