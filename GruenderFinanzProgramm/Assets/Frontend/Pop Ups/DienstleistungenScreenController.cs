using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DienstleistungenScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset cardAsset;
    [SerializeField] private VisualTreeAsset popupAsset;

    private VisualElement root;
    private VisualElement popupRoot;
    private VisualElement gridContainer;

    private List<DienstleistungData> dienstleistungen = new List<DienstleistungData>();
    private int editIndex = -1;

    private void OnEnable()
    {
        root          = uiDocument.rootVisualElement;
        popupRoot     = root.Q("popup-root");
        gridContainer = root.Q("grid-container");

        popupRoot.RemoveFromHierarchy();
        root.Add(popupRoot);
        popupRoot.style.display = DisplayStyle.None;
        popupRoot.RegisterCallback<ClickEvent>(evt => { if (evt.target == popupRoot) SchliessPopup(); });

        LadeBeispieldaten();
        AktualisiereGrid();
    }

    private void LadeBeispieldaten()
    {
        if (dienstleistungen.Count > 0) return;
        dienstleistungen.Add(new DienstleistungData("Beratung",       "Professionelle Unternehmensberatung.",          "", "Festpreis",   "500€", "1"));
        dienstleistungen.Add(new DienstleistungData("Webdesign",      "Modernes und responsives Webdesign.",           "", "Stundensatz", "80€",  "10"));
        dienstleistungen.Add(new DienstleistungData("Programmierung", "Individuelle Softwareentwicklung.",             "", "Festpreis",   "120€", "5"));
        dienstleistungen.Add(new DienstleistungData("Logo",           "Kreative Logogestaltung fuer Ihr Unternehmen.", "", "Pauschal",    "300€", "1"));
        dienstleistungen.Add(new DienstleistungData("Lizenskey",      "Softwarelizenzen und Aktivierungskeys.",        "", "Festpreis",   "50€",  "3"));
    }

    private void AktualisiereGrid()
    {
        gridContainer.Clear();

        for (int i = 0; i < dienstleistungen.Count; i++)
        {
            int index = i;
            var karte = cardAsset.Instantiate().Q("card-root");
            karte.Q<Label>("card-titel").text        = dienstleistungen[i].Titel;
            karte.Q<Label>("card-beschreibung").text = dienstleistungen[i].Beschreibung;
            karte.Q<Label>("card-preis-wert").text   = dienstleistungen[i].Betrag;
            karte.Q<Button>("card-btn-edit").clicked += () => OeffnePopup(index);
            karte.Q<Button>("card-btn-del").clicked  += () => { dienstleistungen.RemoveAt(index); AktualisiereGrid(); };
            gridContainer.Add(karte);
        }

       // Plus-Button
        var plus = new Button(() => OeffnePopup(-1));
        plus.AddToClassList("cards");
        plus.AddToClassList("add-card");
        var plusLabel = new Label("+");
        plusLabel.AddToClassList("add-card-label");
        plus.Add(plusLabel);
        gridContainer.Add(plus);
    }

    private void OeffnePopup(int index)
    {
        editIndex = index;
        popupRoot.Clear();

        var popup = popupAsset.Instantiate();
        popup.style.flexShrink = 0;

        var d = index >= 0 ? dienstleistungen[index] : new DienstleistungData("", "", "", "Festpreis", "", "");

        popup.Q<TextField>("feld-titel").value             = d.Titel;
        popup.Q<TextField>("feld-beschreibung").value      = d.Beschreibung;
        popup.Q<TextField>("feld-detailbeschreibung").value = d.Detail;
        popup.Q<DropdownField>("feld-preismodell").value   = d.Preismodell;
        popup.Q<TextField>("feld-betrag").value            = d.Betrag;
        popup.Q<TextField>("feld-anzahl").value            = d.Anzahl;

        popup.Q<Button>("btn-fertig").clicked += () =>
        {
            var neu = new DienstleistungData(
                popup.Q<TextField>("feld-titel").value,
                popup.Q<TextField>("feld-beschreibung").value,
                popup.Q<TextField>("feld-detailbeschreibung").value,
                popup.Q<DropdownField>("feld-preismodell").value,
                popup.Q<TextField>("feld-betrag").value,
                popup.Q<TextField>("feld-anzahl").value
            );

            if (editIndex >= 0) dienstleistungen[editIndex] = neu;
            else                dienstleistungen.Add(neu);

            SchliessPopup();
            AktualisiereGrid();
        };

        popupRoot.Add(popup);
        popupRoot.style.display = DisplayStyle.Flex;
    }

    private void SchliessPopup()
    {
        popupRoot.style.display = DisplayStyle.None;
        popupRoot.Clear();
        editIndex = -1;
    }
}

[System.Serializable]
public class DienstleistungData
{
    public string Titel, Beschreibung, Detail, Preismodell, Betrag, Anzahl;

    public DienstleistungData(string titel, string beschreibung, string detail,
                               string preismodell, string betrag, string anzahl)
    {
        Titel = titel; Beschreibung = beschreibung; Detail = detail;
        Preismodell = preismodell; Betrag = betrag; Anzahl = anzahl;
    }
}