using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DienstleistungenScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset popupAsset;

    private VisualElement root;
    private VisualElement popupRoot;
    private VisualElement cardsContainer;

    // Datenspeicher fuer alle Karten
    private List<DienstleistungData> dienstleistungen = new List<DienstleistungData>();

    // Index der Karte die gerade bearbeitet wird (-1 = neue Karte)
    private int aktuellerEditIndex = -1;

    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;
        popupRoot = root.Q("popup-root");
        cardsContainer = root.Q("cards-container");

        if (popupRoot == null)  { Debug.LogError("popup-root nicht gefunden!"); return; }
        if (cardsContainer == null) { Debug.LogError("cards-container nicht gefunden!"); return; }
        if (popupAsset == null) { Debug.LogError("popupAsset nicht zugewiesen!"); return; }

        // Popup ans Root haengen damit es ueber allem liegt
        popupRoot.RemoveFromHierarchy();
        root.Add(popupRoot);

        // SCHRITT 1: Sofort unsichtbar machen, damit die UI darunter klickbar ist!
        popupRoot.style.display = DisplayStyle.None;

        // SCHRITT 2: Callback nur EINMAL registrieren, statt bei jedem Oeffnen
        popupRoot.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == popupRoot) SchliessPopup();
        });

        // Beispieldaten laden
        LadeBeispieldaten();

        // Grid neu aufbauen
        AktualisiereGrid();
    }

    private void LadeBeispieldaten()
    {
        if (dienstleistungen.Count == 0) // Verhindert doppelte Daten bei Re-Enable
        {
            dienstleistungen.Add(new DienstleistungData("Beratung",        "Professionelle Unternehmensberatung.",              "", "Festpreis",    "676767.67€", ""));
            dienstleistungen.Add(new DienstleistungData("Webdesign",       "Modernes und responsives Webdesign.",               "", "Stundensatz",  "676767.67€", ""));
            dienstleistungen.Add(new DienstleistungData("Programmierung",  "Individuelle Softwareentwicklung.",                 "", "Festpreis",    "676767.68€", ""));
            dienstleistungen.Add(new DienstleistungData("Logo",            "Kreative Logogestaltung fuer Ihr Unternehmen.",     "", "Pauschal",     "676767.69€", ""));
            dienstleistungen.Add(new DienstleistungData("Lizenskey",       "Softwarelizenzen und Aktivierungskeys.",            "", "Festpreis",    "676767.70€", ""));
        }
    }

    private void AktualisiereGrid()
    {
        cardsContainer.Clear();

        for (int i = 0; i < dienstleistungen.Count; i++)
        {
            int index = i; // Closure-fix
            var karte = ErstelleKarte(dienstleistungen[i], index);
            cardsContainer.Add(karte);
        }

        var plusButton = ErstellePlusButton();
        cardsContainer.Add(plusButton);
    }

    private VisualElement ErstelleKarte(DienstleistungData data, int index)
    {
        var karte = new VisualElement();
        karte.AddToClassList("cards");
        karte.name = $"card-{index}";

        var titel = new Label(data.Titel);
        titel.AddToClassList("Label1");
        titel.name = $"title-{index}";
        karte.Add(titel);

        var desc = new Label(data.Beschreibung);
        desc.name = $"desc-{index}";
        desc.style.fontSize = 15;
        desc.style.color = new StyleColor(new Color(0.73f, 0.73f, 0.73f));
        desc.style.whiteSpace = WhiteSpace.Normal;
        desc.style.overflow = Overflow.Hidden;
        desc.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
        desc.AddToClassList("mb-3");
        karte.Add(desc);

        var preisZeile = new VisualElement();
        preisZeile.AddToClassList("preis");
        preisZeile.name = "Preis";

        var preisLabel = new Label("Preis:");
        preisLabel.style.fontSize = 15;
        preisLabel.style.color = Color.white;
        preisLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        preisLabel.style.flexGrow = 1;
        preisZeile.Add(preisLabel);

        var preisWert = new Label(data.Betrag);
        preisWert.name = $"price-{index}";
        preisWert.style.fontSize = 15;
        preisWert.style.color = Color.white;
        preisWert.style.unityFontStyleAndWeight = FontStyle.Bold;
        preisZeile.Add(preisWert);

        karte.Add(preisZeile);

        var buttons = new VisualElement();
        buttons.AddToClassList("buttons");
        buttons.name = "Buttons";
        buttons.style.flexDirection = FlexDirection.Row;

        var editBtn = new Button(() => OeffnePopupBearbeiten(index));
        editBtn.text = "Bearbeiten";
        editBtn.name = $"btn-edit-{index}";
        editBtn.style.backgroundColor = new StyleColor(new Color(0.502f, 0.812f, 0.584f));
        editBtn.style.color = Color.black;
        editBtn.style.fontSize = 14;
        editBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        editBtn.style.height = 38;
        editBtn.style.flexGrow = 1;
        editBtn.style.borderTopLeftRadius = editBtn.style.borderTopRightRadius =
        editBtn.style.borderBottomLeftRadius = editBtn.style.borderBottomRightRadius = 8;
        editBtn.style.borderTopWidth = editBtn.style.borderRightWidth =
        editBtn.style.borderBottomWidth = editBtn.style.borderLeftWidth = 0;
        editBtn.style.marginRight = 4;
        buttons.Add(editBtn);

        var delBtn = new Button(() => LoescheKarte(index));
        delBtn.text = "Loeschen";
        delBtn.name = $"btn-del-{index}";
        delBtn.style.backgroundColor = new StyleColor(new Color(0.502f, 0.812f, 0.584f));
        delBtn.style.color = Color.black;
        delBtn.style.fontSize = 14;
        delBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        delBtn.style.height = 38;
        delBtn.style.flexGrow = 1;
        delBtn.style.borderTopLeftRadius = delBtn.style.borderTopRightRadius =
        delBtn.style.borderBottomLeftRadius = delBtn.style.borderBottomRightRadius = 8;
        delBtn.style.borderTopWidth = delBtn.style.borderRightWidth =
        delBtn.style.borderBottomWidth = delBtn.style.borderLeftWidth = 0;
        buttons.Add(delBtn);

        karte.Add(buttons);
        return karte;
    }

    private VisualElement ErstellePlusButton()
    {
        var plusBtn = new Button(() => OeffnePopupNeu());
        plusBtn.name = "btn-neue-dienstleistung";
        plusBtn.AddToClassList("cards");
        plusBtn.AddToClassList("add-card");
        plusBtn.style.justifyContent = Justify.Center;
        plusBtn.style.alignItems = Align.Center;
        plusBtn.style.borderLeftColor  = new StyleColor(new Color(0.502f, 0.812f, 0.584f));
        plusBtn.style.borderRightColor = new StyleColor(new Color(0.502f, 0.812f, 0.584f));
        plusBtn.style.borderTopColor   = new StyleColor(new Color(0.502f, 0.812f, 0.584f));
        plusBtn.style.borderBottomColor= new StyleColor(new Color(0.502f, 0.812f, 0.584f));
        plusBtn.style.borderTopWidth = plusBtn.style.borderRightWidth =
        plusBtn.style.borderBottomWidth = plusBtn.style.borderLeftWidth = 2;
        plusBtn.style.backgroundColor = StyleKeyword.None;

        var plusLabel = new Label("+");
        plusLabel.style.fontSize = 80;
        plusLabel.style.color = Color.white;
        plusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        plusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        plusBtn.Add(plusLabel);

        return plusBtn;
    }

    private void OeffnePopupNeu()
    {
        aktuellerEditIndex = -1;
        OeffnePopup("Neue Dienstleistung", "", "", "Festpreis", "", "");
    }

    private void OeffnePopupBearbeiten(int index)
    {
        if (index < 0 || index >= dienstleistungen.Count) return;
        aktuellerEditIndex = index;
        var d = dienstleistungen[index];
        OeffnePopup(d.Titel, d.Beschreibung, d.Detail, d.Preismodell, d.Betrag, d.Anzahl);
    }

    private void OeffnePopup(string titel, string beschreibung,
                              string detail, string preismodell,
                              string betrag, string anzahl)
    {
        popupRoot.Clear();

        popupRoot.style.position        = Position.Absolute;
        popupRoot.style.left            = 0;
        popupRoot.style.right           = 0;
        popupRoot.style.top             = 0;
        popupRoot.style.bottom          = 0;
        popupRoot.style.alignItems      = Align.Center;
        popupRoot.style.justifyContent  = Justify.Center;

        var popup = popupAsset.Instantiate();
        popup.style.flexShrink = 0;

        // Namen-Sicherheitscheck via Unity Konsole
        try 
        {
            popup.Q<Label>("popup-titel").text                  = titel;
            popup.Q<TextField>("feld-beschreibung").value       = beschreibung;
            popup.Q<TextField>("feld-detailbeschreibung").value = detail;
            popup.Q<TextField>("feld-betrag").value             = betrag;
            popup.Q<TextField>("feld-anzahl").value             = anzahl;
            popup.Q<DropdownField>("feld-preismodell").value    = preismodell;
            popup.Q<Button>("btn-fertig").clicked += () => SpeichereUndSchliesse(popup);
        }
        catch (System.NullReferenceException)
        {
            Debug.LogError("Ein Element im Popup-UXML wurde nicht gefunden! Pruefe die Namen im UI Builder.");
        }

        popupRoot.Add(popup);
        popupRoot.style.display = DisplayStyle.Flex;
    }

    private void SpeichereUndSchliesse(VisualElement popup)
    {
        var neuerTitel       = popup.Q<Label>("popup-titel").text;
        var neueBeschreibung = popup.Q<TextField>("feld-beschreibung").value;
        var neuesDetail      = popup.Q<TextField>("feld-detailbeschreibung").value;
        var neuesPreismodell = popup.Q<DropdownField>("feld-preismodell").value;
        var neuerBetrag      = popup.Q<TextField>("feld-betrag").value;
        var neueAnzahl       = popup.Q<TextField>("feld-anzahl").value;

        var data = new DienstleistungData(neuerTitel, neueBeschreibung, neuesDetail,
                                          neuesPreismodell, neuerBetrag, neueAnzahl);

        if (aktuellerEditIndex >= 0 && aktuellerEditIndex < dienstleistungen.Count)
        {
            dienstleistungen[aktuellerEditIndex] = data;
        }
        else
        {
            dienstleistungen.Add(data);
        }

        SchliessPopup();
        AktualisiereGrid();
    }

    private void LoescheKarte(int index)
    {
        if (index < 0 || index >= dienstleistungen.Count) return;
        dienstleistungen.RemoveAt(index);
        AktualisiereGrid();
    }

    private void SchliessPopup()
    {
        popupRoot.style.display = DisplayStyle.None;
        popupRoot.Clear();
        aktuellerEditIndex = -1;
    }
}

[System.Serializable]
public class DienstleistungData
{
    public string Titel;
    public string Beschreibung;
    public string Detail;
    public string Preismodell;
    public string Betrag;
    public string Anzahl;

    public DienstleistungData(string titel, string beschreibung, string detail,
                               string preismodell, string betrag, string anzahl)
    {
        Titel        = titel;
        Beschreibung = beschreibung;
        Detail       = detail;
        Preismodell  = preismodell;
        Betrag       = betrag;
        Anzahl       = anzahl;
    }
}