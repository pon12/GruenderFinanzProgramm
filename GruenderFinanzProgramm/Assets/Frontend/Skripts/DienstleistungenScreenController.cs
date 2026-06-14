using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DienstleistungenScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset popupAsset;

    // ── Daten ──────────────────────────────────────────────
    private List<DienstleistungData> daten = new List<DienstleistungData>();

    // ── UI Referenzen ──────────────────────────────────────
    private ScrollView tabelleBody;
    private VisualElement popupOverlay;
    private int editIndex = -1;

    // ──────────────────────────────────────────────────────
    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        tabelleBody  = root.Query<ScrollView>("tabelle-body").First();
        popupOverlay = root.Query<VisualElement>("popup-overlay").First();

        var btnNeu = root.Q<Button>("btn-neu");
        if (btnNeu == null) { Debug.LogError("btn-neu nicht gefunden!"); return; }

        btnNeu.clicked += () => OeffnePopup(-1);
        popupOverlay.RegisterCallback<ClickEvent>(evt => { if (evt.target == popupOverlay) SchliessPopup(); });

        BeispieldatenLaden();
        TabelleAktualisieren();
    }

    // ── Beispieldaten ──────────────────────────────────────
    private void BeispieldatenLaden()
    {
        if (daten.Count > 0) return;
        daten.Add(new DienstleistungData("Beratung",       "Professionelle Unternehmensberatung", "Festpreis",   "500€"));
        daten.Add(new DienstleistungData("Webdesign",      "Modernes und responsives Webdesign",  "Stundensatz", "80€"));
        daten.Add(new DienstleistungData("Programmierung", "Individuelle Softwareentwicklung",    "Festpreis",   "120€"));
        daten.Add(new DienstleistungData("Logo",           "Kreative Logogestaltung",             "Pauschal",    "300€"));
        daten.Add(new DienstleistungData("Lizenskey",      "Softwarelizenzen & Aktivierungskeys", "Festpreis",   "50€"));
    }

    // ── Tabelle aufbauen ───────────────────────────────────
    private void TabelleAktualisieren()
    {
        tabelleBody.Clear();

        for (int i = 0; i < daten.Count; i++)
        {
            int index = i; // Closure-Fix
            var d = daten[i];

            var zeile = new VisualElement();
            zeile.AddToClassList("tabelle-zeile");

            zeile.Add(Zelle(d.Titel,       "col-titel"));
            zeile.Add(Zelle(d.Beschreibung,"col-beschreibung"));
            zeile.Add(Zelle(d.Preismodell, "col-preismodell"));
            zeile.Add(Zelle(d.Betrag,      "col-betrag"));

            // Aktionen
            var aktionen = new VisualElement();
            aktionen.AddToClassList("col-aktionen");

            var btnEdit = new Button(() => OeffnePopup(index));
            btnEdit.text = "Bearbeiten";
            btnEdit.AddToClassList("zeile-btn");

            var btnDel = new Button(() => { daten.RemoveAt(index); TabelleAktualisieren(); });
            btnDel.text = "Löschen";
            btnDel.AddToClassList("zeile-btn");
            btnDel.AddToClassList("zeile-btn-loeschen");

            aktionen.Add(btnEdit);
            aktionen.Add(btnDel);
            zeile.Add(aktionen);

            tabelleBody.Add(zeile);
        }
    }

    // Hilfsmethode: eine Tabellenzelle erstellen
    private VisualElement Zelle(string text, string spaltenKlasse)
    {
        var label = new Label(text);
        label.AddToClassList("tabelle-zelle");
        label.AddToClassList(spaltenKlasse);
        return label;
    }

    // ── Popup ──────────────────────────────────────────────
    private void OeffnePopup(int index)
    {
        editIndex = index;
        popupOverlay.Clear();

        var popup = popupAsset.Instantiate();

        var d = index >= 0 ? daten[index] : new DienstleistungData("", "", "Festpreis", "");

        popup.Q<TextField>("feld-titel").value          = d.Titel;
        popup.Q<TextField>("feld-beschreibung").value   = d.Beschreibung;
        popup.Q<DropdownField>("feld-preismodell").value = d.Preismodell;
        popup.Q<TextField>("feld-betrag").value         = d.Betrag;

        popup.Q<Button>("btn-fertig").clicked += () =>
        {
            var neu = new DienstleistungData(
                popup.Q<TextField>("feld-titel").value,
                popup.Q<TextField>("feld-beschreibung").value,
                popup.Q<DropdownField>("feld-preismodell").value,
                popup.Q<TextField>("feld-betrag").value
            );

            if (editIndex >= 0) daten[editIndex] = neu;
            else                daten.Add(neu);

            SchliessPopup();
            TabelleAktualisieren();
        };

        popupOverlay.Add(popup);
        popupOverlay.style.display = DisplayStyle.Flex;
    }

    private void SchliessPopup()
    {
        popupOverlay.style.display = DisplayStyle.None;
        popupOverlay.Clear();
        editIndex = -1;
    }
}

// ── Datenklasse ────────────────────────────────────────────
[System.Serializable]
public class DienstleistungData
{
    public string Titel;
    public string Beschreibung;
    public string Preismodell;
    public string Betrag;

    public DienstleistungData(string titel, string beschreibung, string preismodell, string betrag)
    {
        Titel        = titel;
        Beschreibung = beschreibung;
        Preismodell  = preismodell;
        Betrag       = betrag;
    }
}