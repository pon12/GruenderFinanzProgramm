// ================================================================
// WissensdatenbankController.cs
//
// Basiert auf dem Dokumente-Pool, aber stark vereinfacht:
//  - KEINE Pflicht/Fest-Unterscheidung – alle Inhalte sind
//    reine Lese-Inhalte
//  - KEIN Erstellen, KEIN Bearbeiten, KEIN Löschen durch den Nutzer
//  - Inhalte werden komplett im Inspector gepflegt (siehe unten)
//  - Klick auf eine Karte öffnet ein Lese-Popup mit dem Text
//
// EINRICHTUNG IN UNITY:
//  1. Dieses Script auf das GameObject mit dem UIDocument legen
//  2. Im Inspector unter "Wissens Einträge" die Liste befüllen:
//     - category: muss exakt einem Namen aus 'kategorien' entsprechen
//       (Gründung, Bezahlweise, Finanzen, Marketing, Steuern,
//        Personal, Recht)
//     - title: Anzeigename der Karte (z.B. "Wie lege ich ein
//       Angebot an?")
//     - inhalt: der eigentliche Erklärungstext, den der Nutzer
//       beim Klick zu lesen bekommt
//  3. categoryCardTemplate im Inspector zuweisen (gleiche
//     CategoryCard.uxml wie bei Dokumente, wiederverwendbar)
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

    // ============================================================
    // WISSENS-EINTRAG  – wird komplett im Inspector gepflegt
    // ============================================================
    [System.Serializable]
    public class WissensEintrag
    {
        public string category;                  // muss zu einer Kategorie unten passen
        public string title;                      // Kartentitel
        [TextArea(4, 12)]
        public string inhalt;                      // Erklärungstext, im Lese-Popup angezeigt
    }

    [Header("Wissens Einträge (im Inspector befüllen)")]
    [SerializeField] private List<WissensEintrag> wissensEintraege = new List<WissensEintrag>();

    // Feste Kategorienliste – identisch zu Dokumente, aber hier
    // ohne istFest/pflichtDocs, da alles gleich behandelt wird.
    private readonly string[] kategorien =
    {
        "Gründung", "Bezahlweise", "Finanzen", "Marketing", "Steuern", "Personal", "Recht"
    };

    // UI Elemente Hauptbildschirm
    private VisualElement root;
    private VisualElement gridContainer;

    // Listen-Popup (zeigt alle Einträge einer Kategorie)
    private VisualElement detailPopupOverlay;
    private VisualElement detailListContainer;
    private Button        detailCloseButton;
    private Label         detailPopupTitle;
    private string        activeCategoryForList = "";

    // Lese-Popup (zeigt den Inhalt eines einzelnen Eintrags)
    private VisualElement viewPopupOverlay;
    private Label          viewPopupTitle;
    private Label          viewPopupInhalt;
    private Button         viewPopupCloseButton;

    // ─────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        gridContainer = root.Q<VisualElement>("Grid-Container");

        // Listen-Popup
        detailPopupOverlay  = root.Q<VisualElement>("Detail-Popup-Overlay");
        detailListContainer = root.Q<VisualElement>("Detail-List-Container");
        detailCloseButton   = root.Q<Button>("Btn-Detail-Close");
        detailPopupTitle    = root.Q<Label>("Detail-Popup-Title");

        // Lese-Popup
        viewPopupOverlay     = root.Q<VisualElement>("View-Popup-Overlay");
        viewPopupTitle       = root.Q<Label>("View-Popup-Title");
        viewPopupInhalt       = root.Q<Label>("View-Popup-Inhalt");
        viewPopupCloseButton = root.Q<Button>("Btn-View-Close");

        if (detailCloseButton   != null) detailCloseButton.clicked   += CloseDetailPopup;
        if (viewPopupCloseButton != null) viewPopupCloseButton.clicked += CloseViewPopup;

        SpawnAllCardsAtStart();
    }

    // ─────────────────────────────────────────
    // KATEGORIE-KARTEN AUFBAUEN
    // ─────────────────────────────────────────

    private void SpawnAllCardsAtStart()
    {
        if (gridContainer == null || categoryCardTemplate == null) return;
        gridContainer.Clear();

        foreach (var kategorieName in kategorien)
        {
            VisualElement cardInstance = categoryCardTemplate.Instantiate();

            Label titleLabel = cardInstance.Q<Label>("lblName");
            if (titleLabel != null) titleLabel.text = kategorieName;

            // Schloss-Badge komplett ausblenden – bei der Wissensdatenbank
            // gibt es keine "geschützten" Karten, alles ist gleich
            VisualElement lockBadge = cardInstance.Q<VisualElement>("lock-badge");
            if (lockBadge != null) lockBadge.style.display = DisplayStyle.None;

            VisualElement imgShowList = cardInstance.Q("Btn-Show-List");
            if (imgShowList != null)
                imgShowList.RegisterCallback<ClickEvent>(evt => OpenDetailPopup(kategorieName));

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

                    // Statt "Bearbeiten" gibt's hier nur "Anzeigen"
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

                    // Leere Slots sind rein informativ, Nutzer kann hier nichts anlegen
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

    // ─────────────────────────────────────────
    // LISTEN-POPUP  (alle Einträge einer Kategorie)
    // ─────────────────────────────────────────

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

            // Nur ein "Anzeigen"-Button, kein Bearbeiten, kein Löschen
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

    // ─────────────────────────────────────────
    // LESE-POPUP  (zeigt einen einzelnen Eintrag, read-only)
    // ─────────────────────────────────────────

    private void OpenViewPopup(WissensEintrag eintrag)
    {
        if (viewPopupOverlay != null) viewPopupOverlay.style.display = DisplayStyle.Flex;
        if (viewPopupTitle   != null) viewPopupTitle.text            = eintrag.title;
        if (viewPopupInhalt   != null)
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
