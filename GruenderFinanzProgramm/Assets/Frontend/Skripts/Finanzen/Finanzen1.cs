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
        
        // 1. Tabelle im Bereich "Gruendung"
        VisualElement containerGruendung = uiDocument.rootVisualElement.Q<VisualElement>("Gruendung");
        if (containerGruendung != null)
        {
            containerGruendung.Clear();
            containerGruendung.Add(CreateHeader());
            string[] katGruendung = { "Anfangsbestand", "Umsatzerlöse", "Direkte Kosten", "Personalausgaben", "Betriebsausgaben", "Zinsen", "Tilgung", "MwSt-Zahlung an/von Finanzamt", "Privatentnahmen", "Überschuss/Fehlbetrag", "Endbestand" };

            foreach (var kat in katGruendung)
            {
                containerGruendung.Add(CreateRow(kat, 0,0));
            }
        }

        // 2. Tabelle im Bereich "Liquiditaet" (die vom Bild)
        VisualElement containerRentabilitaet = uiDocument.rootVisualElement.Q<VisualElement>("Rentabilitaet");
        if (containerRentabilitaet != null)
        {
            containerRentabilitaet.Clear();
            containerRentabilitaet.Add(CreateHeader());
            string[] katRentabilitaet = { 
                "Umsatzerlöse", "Direkte Kosten", "Personalausgaben", 
                "Betriebsausgaben", "Zinsen", "Tilgung", 
                "MwSt-Zahlung an/von Finanzamt", "Privatentnahmen" 
            };
            foreach (var kat in katRentabilitaet)
            {
                containerRentabilitaet.Add(CreateRow(kat, 0, 0));
            }
            containerRentabilitaet.Add(CreateRow("Überschuss/Fehlbetrag", 0, 0, true));
        }

        // 3. Tabelle im Bereich "Liquidität"
        VisualElement containerLiquiditaet = uiDocument.rootVisualElement.Q<VisualElement>("Liquiditaet");
        if (containerLiquiditaet != null)
        {
            containerLiquiditaet.Clear();
            containerLiquiditaet.Add(CreateHeader2());
            string[] katLiquid = { 
                "Geldeinlagen", "Kredite", "Investitionen", "Gründungskosten", "Rückerstattung MwSt", "Investitionen", "Rückerstattung MwSt", "Gründungskosten", "Anfangsbestand zu Geschäftsbeginn"
 
            };

            foreach (var kat in katLiquid)
            {
                containerLiquiditaet.Add(CreateRow2(kat, 0));
            }
        }

    }

    private VisualElement CreateHeader()
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
        row.Add(CreateCell("Kategorie", true, 40));
        row.Add(CreateCell("Jahr 1", true, 30));
        row.Add(CreateCell("Jahr 2", true, 30));
        return row;
    }

     private VisualElement CreateHeader2()
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
        row.Add(CreateCell("Kategorie", true, 40));
        row.Add(CreateCell("Gründung", true, 30));
        
        return row;
    }

    private VisualElement CreateRow(string name, float y1, float y2, bool isBold = false)
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
        row.Add(CreateCell(name, isBold, 40));
        row.Add(CreateCell(y1.ToString("N0") + " €", isBold, 30));
        row.Add(CreateCell(y2.ToString("N0") + " €", isBold, 30));
        return row;
    }

     private VisualElement CreateRow2(string name, float y1, bool isBold = false)
    {
        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
        row.Add(CreateCell(name, isBold, 40));
        row.Add(CreateCell(y1.ToString("N0") + " €", isBold, 30));
        return row;
    }

    private VisualElement CreateCell(string text, bool isBold, int widthPercent)
    {
        var label = new Label(text) { style = { color = Color.white, unityFontStyleAndWeight = isBold ? FontStyle.Bold : FontStyle.Normal, flexGrow = 1, width = Length.Percent(widthPercent) } };
        return label;
    }
}