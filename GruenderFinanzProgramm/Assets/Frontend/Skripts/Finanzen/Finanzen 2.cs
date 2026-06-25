using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Finanzen2 : MonoBehaviour
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
        
        // 1. Tabelle im Bereich "Kosten"
        VisualElement containerKosten = uiDocument.rootVisualElement.Q<VisualElement>("Kosten");
        if (containerKosten != null)
        {
            containerKosten.Clear();
            containerKosten.Add(CreateHeader());
            string[] katKosten = { "Honorar", "Material", "Honorar", "Honorar", "Honorar"};

            foreach (var kat in katKosten)
            {
                containerKosten.Add(CreateRow(kat, 0,0));
            }
        }

        // 2. Tabelle im Bereich "Liquiditaet" (die vom Bild)
        VisualElement containerBetriebsausgaben = uiDocument.rootVisualElement.Q<VisualElement>("Betriebsausgaben");
        if (containerBetriebsausgaben != null)
        {
            containerBetriebsausgaben.Clear();
            containerBetriebsausgaben.Add(CreateHeader());
            string[] katBetriebsausgaben = { 
                "Auto", "Marketing", "Sonstige Reisekosten", "Summe Betriebsausgaben",
            };
            foreach (var kat in katBetriebsausgaben)
            {
                containerBetriebsausgaben.Add(CreateRow(kat, 0, 0));
            }
            
        }

        // 3. Tabelle im Bereich "Liquidität"
        VisualElement containerDienstleistungen = uiDocument.rootVisualElement.Q<VisualElement>("Dienstleistungen");
        if (containerDienstleistungen != null)
        {
            containerDienstleistungen.Clear();
            containerDienstleistungen.Add(CreateHeader());
            string[] katLiquid = { 
                "Dienstleistung 1", "Dienstleistung 2", "Dienstleistung 3", "Dienstleistung 4", "Summe Umsätze", "Summe direkte Kosten", "Rohgewinn", "Rohgewinn %"
 
            };

            foreach (var kat in katLiquid)
            {
                containerDienstleistungen.Add(CreateRow(kat, 0, 0));
            }
        }

         // 3. Tabelle im Bereich "Liquidität"
        VisualElement containerInvestitionen = uiDocument.rootVisualElement.Q<VisualElement>("Investition");
        if (containerInvestitionen != null)
        {
            containerInvestitionen.Clear();
            containerInvestitionen.Add(CreateHeader2());
            string[] katLiquid = { 
                "Mustermodelle", "Summe Investitionen"
 
            };

            foreach (var kat in katLiquid)
            {
                containerInvestitionen.Add(CreateRow2(kat, 0));
            }
        }

         // 3. Tabelle im Bereich "Liquidität"
        VisualElement containerGründerkosten = uiDocument.rootVisualElement.Q<VisualElement>("Grunderkosten");
        if (containerGründerkosten != null)
        {
            containerGründerkosten.Clear();
            containerGründerkosten.Add(CreateHeader2());
            string[] katLiquid = { "Beratung CorporateDesign", "Grundausstattung mit Marketingmaterialien", "Homepage-Erstellung","Summe Gründungskosten"
                
 
            };

            foreach (var kat in katLiquid)
            {
                containerGründerkosten.Add(CreateRow2(kat, 0));
            }
        }

         // 3. Tabelle im Bereich "Liquidität"
        VisualElement containerKapitalbedarf = uiDocument.rootVisualElement.Q<VisualElement>("Kapitalbedarf");
        if (containerKapitalbedarf != null)
        {
            containerKapitalbedarf.Clear();
            containerKapitalbedarf.Add(CreateHeader2());
            string[] katLiquid = { "Investitionen", "Sacheinlagen", "Gründungskosten", "Kapitalbedarf für Anlaufphase", "Liquiditätsreserve", "Gesamtkapitalbedarf" };

    

            foreach (var kat in katLiquid)
            {
                containerKapitalbedarf.Add(CreateRow2(kat, 0));
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
        row.Add(CreateCell("Wert", true, 30));
        
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