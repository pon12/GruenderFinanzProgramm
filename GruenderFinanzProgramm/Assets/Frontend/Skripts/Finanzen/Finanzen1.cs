using UnityEngine;
using UnityEngine.UIElements;
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
        AktualisiereGruendungTabelle();
    }

    public void AktualisiereGruendungTabelle()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            Debug.LogError("[Finanzen1] UIDocument ist nicht zugewiesen!");
            return;
        }

        VisualElement container = uiDocument.rootVisualElement.Q<VisualElement>("Gruendung");
        
        if (container == null) 
        {
            Debug.LogError("[Finanzen1] Element 'Gruendung' nicht gefunden!");
            return;
        }
        
        // Datenbank abrufen
        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        var alleEinnahmen = db.getAllEinkommenEntries();
        var alleAusgaben = db.getAllAusgabenEntries();

        container.Clear();

        string[] kategorien = { 
            "Geldeinlagen", "Kredite", "Investitionen", 
            "Gründungskosten", "Rückerstattung MwSt Investitionen", 
            "Rückerstattung MwSt Gründungskosten" 
        };

        container.Add(CreateRentHeader());

        foreach (string kat in kategorien)
        {
            float summe = 0;

            // Einnahmen verrechnen
            if (alleEinnahmen != null)
                summe += alleEinnahmen.Where(e => e.Kategorie == kat).Sum(e => e.getAmountAsFloat());

            // Ausgaben verrechnen
            if (alleAusgaben != null)
                summe -= alleAusgaben.Where(a => a.Kategorie == kat).Sum(a => a.getAmountAsFloat());

            container.Add(CreateRentRow(kat, summe));
        }
        
        Debug.Log("[Finanzen1] Tabelle wurde mit dynamischen Daten gerendert.");
    }

    private VisualElement CreateRentHeader()
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 6;

        row.Add(CreateCell("Kategorie", true));
        row.Add(CreateCell("Betrag", true));

        return row;
    }

    private VisualElement CreateRentRow(string name, float wert)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 4;

        row.Add(CreateCell(name, false));
        row.Add(CreateCell(wert.ToString("N2") + " €", false)); 

        return row;
    }

    private VisualElement CreateCell(string text, bool isHeader)
    {
        var label = new Label(text);
        label.style.color = Color.white;
        label.style.unityTextAlign = TextAnchor.MiddleLeft;
        label.style.flexGrow = 1;
        label.style.width = Length.Percent(50);
        label.style.fontSize = isHeader ? 16 : 14;

        if (isHeader)
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

        return label;
    }
}