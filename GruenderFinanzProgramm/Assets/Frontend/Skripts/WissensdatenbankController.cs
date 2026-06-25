// ================================================================
// WissensdatenbankController.cs
//
//  - Lädt Einträge aus einer JSON-Datei (Resources/wissensdatenbank.json)
//  - KEINE manuelle Pflege im Inspector nötig
//  - Klick auf eine Karte öffnet ein Lese-Popup mit dem Text
//
// EINRICHTUNG IN UNITY:
//  1. Dieses Script auf das GameObject mit dem UIDocument legen
//  2. wissensdatenbank.json in den Ordner Assets/Resources/ legen
//  3. categoryCardTemplate im Inspector zuweisen
// ================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class WissensdatenbankController : MonoBehaviour
{
    [Header("UI Document Asset")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Templates")]
    [SerializeField] private VisualTreeAsset categoryCardTemplate;

    [System.Serializable]
    private class WissensEintrag
    {
        public string category;
        public string title;
        public string inhalt;
    }

    [System.Serializable]
    private class WissensDaten
    {
        public List<WissensEintrag> eintraege;
    }

    private List<WissensEintrag> wissensEintraege = new List<WissensEintrag>();

    private readonly string[] kategorien =
    {
        "Gründung", "Strategie", "Rechtliches", "Finanzen", "Steuern",
        "Vertrieb", "Marketing", "Organisation", "Personal", "System-Hilfe"
    };

    private VisualElement root;
    private VisualElement gridContainer;

    private VisualElement detailPopupOverlay;
    private VisualElement detailListContainer;
    private Button        detailCloseButton;
    private Label         detailPopupTitle;
    private string        activeCategoryForList = "";

    private VisualElement viewPopupOverlay;
    private Label         viewPopupTitle;
    private Label         viewPopupInhalt;
    private Button        viewPopupCloseButton;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        LoadJSON();

        root = uiDocument.rootVisualElement;

        gridContainer       = root.Q<VisualElement>("Grid-Container");
        detailPopupOverlay  = root.Q<VisualElement>("Detail-Popup-Overlay");
        detailListContainer = root.Q<VisualElement>("Detail-List-Container");
        detailCloseButton   = root.Q<Button>("Btn-Detail-Close");
        detailPopupTitle    = root.Q<Label>("Detail-Popup-Title");
        viewPopupOverlay    = root.Q<VisualElement>("View-Popup-Overlay");
        viewPopupTitle      = root.Q<Label>("View-Popup-Title");
        viewPopupInhalt     = root.Q<Label>("View-Popup-Inhalt");
        viewPopupCloseButton = root.Q<Button>("Btn-View-Close");

        if (detailCloseButton    != null) detailCloseButton.clicked    += CloseDetailPopup;
        if (viewPopupCloseButton != null) viewPopupCloseButton.clicked += CloseViewPopup;

        SpawnAllCardsAtStart();
    }

    private void LoadJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("wissensdatenbank");
        if (jsonFile == null)
        {
            Debug.LogWarning("[Wissensdatenbank] wissensdatenbank.json nicht gefunden in Assets/Resources/");
            return;
        }
        WissensDaten daten = JsonUtility.FromJson<WissensDaten>(jsonFile.text);
        if (daten != null && daten.eintraege != null)
            wissensEintraege = daten.eintraege;
        else
            Debug.LogWarning("[Wissensdatenbank] JSON konnte nicht gelesen werden.");
    }

    private void SpawnAllCardsAtStart()
    {
        if (gridContainer == null || categoryCardTemplate == null) return;
        gridContainer.Clear();

        foreach (var kategorieName in kategorien)
        {
            VisualElement cardInstance = categoryCardTemplate.Instantiate();

            Label titleLabel = cardInstance.Q<Label>("lblName");
            if (titleLabel != null) titleLabel.text = kategorieName;

            VisualElement lockBadge = cardInstance.Q<VisualElement>("lock-badge");
            if (lockBadge != null) lockBadge.style.display = DisplayStyle.None;

            VisualElement imgShowList = cardInstance.Q("Btn-Show-List");
            if (imgShowList != null)
                imgShowList.RegisterCallback<ClickEvent>(evt => OpenDetailPopup(kategorieName));

            Button btnAlleAnzeigen = cardInstance.Q<Button>("btnPlus");
            if (btnAlleAnzeigen != null)
                btnAlleAnzeigen.clicked += () => OpenDetailPopup(kategorieName);

            List<WissensEintrag> kategorieEintraege =
                wissensEintraege.Where(w => w.category == kategorieName).ToList();

            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                VisualElement contentBox = cardInstance.Q<VisualElement>($"Slot-{slotIndex}-Content");
                VisualElement iconBox    = cardInstance.Q<VisualElement>($"Slot-{slotIndex}-Icon");
                Button        plusBtn    = cardInstance.Q<Button>($"Slot-{slotIndex}-Plus");

                if (contentBox == null) continue;
                contentBox.Clear();

                bool hatEintrag = slotIndex < kategorieEintraege.Count;

                if (hatEintrag)
                {
                    var eintrag = kategorieEintraege[slotIndex];

                    if (iconBox != null)
                    {
                        iconBox.Clear();
                        var iconLabel = new Label("📖");
                        iconLabel.style.fontSize = 13;
                        iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                        iconLabel.style.flexGrow = 1;
                        iconBox.Add(iconLabel);
                    }

                    string titel = eintrag.title.Split('\n')[0];
                    if (titel.Length > 22) titel = titel.Substring(0, 19) + "...";
                    Label titelLabel = new Label(titel);
                    titelLabel.AddToClassList("doc-mini-title");
                    contentBox.Add(titelLabel);

                    if (!string.IsNullOrEmpty(eintrag.inhalt))
                    {
                        string vorschau = eintrag.inhalt.Split('\n')[0];
                        if (vorschau.Length > 24) vorschau = vorschau.Substring(0, 21) + "...";
                        Label inhaltLabel = new Label(vorschau);
                        inhaltLabel.AddToClassList("doc-mini-inhalt");
                        contentBox.Add(inhaltLabel);
                    }
                    else
                    {
                        Label leerHinweis = new Label("Inhalt folgt");
                        leerHinweis.AddToClassList("doc-mini-inhalt-leer");
                        contentBox.Add(leerHinweis);
                    }

                    if (plusBtn != null)
                    {
                        plusBtn.text = "👁";
                        plusBtn.tooltip = "Anzeigen";
                        plusBtn.clicked += () => OpenViewPopup(eintrag);
                    }
                }
                else
                {
                    if (iconBox != null) { iconBox.Clear(); iconBox.style.opacity = 0.4f; }

                    Label leerLabel = new Label("Leer");
                    leerLabel.AddToClassList("doc-mini-empty-label");
                    contentBox.Add(leerLabel);

                    if (plusBtn != null)
                    {
                        plusBtn.text = "";
                        plusBtn.SetEnabled(false);
                        plusBtn.style.display = DisplayStyle.None;
                    }
                }
            }

            gridContainer.Add(cardInstance);
        }
    }

    private void OpenDetailPopup(string kategorie)
    {
        activeCategoryForList = kategorie;
        if (detailPopupOverlay != null) detailPopupOverlay.style.display = DisplayStyle.Flex;
        if (detailPopupTitle   != null) detailPopupTitle.text            = kategorie;
        RefreshDetailList();
    }

    private void RefreshDetailList()
    {
        if (detailListContainer == null) return;
        detailListContainer.Clear();

        var kategorieEintraege = wissensEintraege.Where(w => w.category == activeCategoryForList).ToList();

        if (kategorieEintraege.Count == 0)
        {
            Label emptyLabel = new Label("Noch keine Einträge in dieser Kategorie.");
            emptyLabel.AddToClassList("list-empty-hint");
            detailListContainer.Add(emptyLabel);
            return;
        }

        foreach (var eintrag in kategorieEintraege)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("list-row-item");

            string displayTitle = eintrag.title.Split('\n')[0];
            if (displayTitle.Length > 30) displayTitle = displayTitle.Substring(0, 27) + "...";

            Label nameLabel = new Label($"📖 {displayTitle}");
            nameLabel.AddToClassList("list-row-label");
            row.Add(nameLabel);

            VisualElement btnGroup = new VisualElement();
            btnGroup.AddToClassList("row-btn-group");

            Button anzeigenBtn = new Button { text = "Anzeigen" };
            anzeigenBtn.AddToClassList("btn-action-text");
            anzeigenBtn.AddToClassList("btn-edit-pen");
            anzeigenBtn.clicked += () => OpenViewPopup(eintrag);
            btnGroup.Add(anzeigenBtn);

            row.Add(btnGroup);
            detailListContainer.Add(row);
        }
    }

    private void CloseDetailPopup()
    {
        if (detailPopupOverlay != null)
            detailPopupOverlay.style.display = DisplayStyle.None;
    }

    private void OpenViewPopup(WissensEintrag eintrag)
    {
        if (viewPopupOverlay != null) viewPopupOverlay.style.display = DisplayStyle.Flex;
        if (viewPopupTitle   != null) viewPopupTitle.text            = eintrag.title;
        if (viewPopupInhalt  != null)
        {
            viewPopupInhalt.text = string.IsNullOrEmpty(eintrag.inhalt)
                ? "Inhalt folgt in Kürze."
                : eintrag.inhalt;
        }
    }

    private void CloseViewPopup()
    {
        if (viewPopupOverlay != null)
            viewPopupOverlay.style.display = DisplayStyle.None;
    }
}
