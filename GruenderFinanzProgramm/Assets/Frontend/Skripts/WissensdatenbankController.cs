// ================================================================
// WissensdatenbankController.cs
//
//  - Lädt Einträge aus einer JSON-Datei (Resources/wissensdatenbank.json)
//  - Klick auf eine Karte öffnet ein Lese-Popup mit dem Text
//
// EINRICHTUNG IN UNITY:
//  1. Script auf das GameObject mit dem UIDocument legen
//  2. wissensdatenbank.json in Assets/Resources/ legen
//  3. categoryCardTemplate im Inspector zuweisen
//  4. helpIconTexture (Help circle.png) im Inspector zuweisen
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

    [Header("Help Icon (Help circle.png zuweisen)")]
    [SerializeField] private Texture2D helpIconTexture;

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

    // Kategorie-Tooltips für die Karten-Icons
    private static readonly Dictionary<string, string> KategorieTooltips =
        new Dictionary<string, string>
    {
        ["Gründung"]     = "Grundlegendes Wissen rund um die Unternehmensgründung: Rechtsformen, Anmeldeprozesse und erste Schritte.",
        ["Strategie"]    = "Strategische Grundlagen: Businessplan, Marktanalyse, Wettbewerbspositionierung und Wachstumsplanung.",
        ["Rechtliches"]  = "Rechtliche Themen: Vertragsrecht, DSGVO, Impressumspflicht und weitere gesetzliche Anforderungen.",
        ["Finanzen"]     = "Finanzwissen für Gründer: Buchhaltung, Liquiditätsplanung, Finanzierung und Fördermittel.",
        ["Steuern"]      = "Steuerrelevante Themen: Umsatzsteuer, Einkommensteuer, Steuernummer und Zusammenarbeit mit dem Finanzamt.",
        ["Vertrieb"]     = "Vertriebsstrategien: Kundengewinnung, Angebotserstellung und Preisgestaltung.",
        ["Marketing"]    = "Marketinggrundlagen: Zielgruppenanalyse, Online-Marketing, Social Media und Corporate Identity.",
        ["Organisation"] = "Organisatorische Themen: Prozesse, Tools, Zeitmanagement und interne Strukturen.",
        ["Personal"]     = "Personalthemen: Einstellung, Arbeitsverträge, Führung und Mitarbeitermotivation.",
        ["System-Hilfe"] = "Hilfe zur Nutzung dieser Anwendung: Funktionen, Tipps und häufige Fragen.",
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

        gridContainer        = root.Q<VisualElement>("Grid-Container");
        detailPopupOverlay   = root.Q<VisualElement>("Detail-Popup-Overlay");
        detailListContainer  = root.Q<VisualElement>("Detail-List-Container");
        detailCloseButton    = root.Q<Button>("Btn-Detail-Close");
        detailPopupTitle     = root.Q<Label>("Detail-Popup-Title");
        viewPopupOverlay     = root.Q<VisualElement>("View-Popup-Overlay");
        viewPopupTitle       = root.Q<Label>("View-Popup-Title");
        viewPopupInhalt      = root.Q<Label>("View-Popup-Inhalt");
        viewPopupCloseButton = root.Q<Button>("Btn-View-Close");

        if (detailCloseButton    != null) detailCloseButton.clicked    += CloseDetailPopup;
        if (viewPopupCloseButton != null) viewPopupCloseButton.clicked += CloseViewPopup;

        SpawnAllCardsAtStart();
        RegistriereHelpTooltips();
    }

    // ============================================================
    // DATEN LADEN
    // ============================================================

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

    // ============================================================
    // KARTEN AUFBAUEN
    // ============================================================

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

            // Karten-Hilfe-Icon mit Textur und Tooltip registrieren
            var karteHelpIcon = cardInstance.Q<VisualElement>("btn-help-karte");
            if (karteHelpIcon != null)
            {
                var tex = helpIconTexture != null
                    ? helpIconTexture
                    : Resources.Load<Texture2D>("Icons/Help circle");
                if (tex != null)
                {
                    karteHelpIcon.style.backgroundImage               = new StyleBackground(tex);
                    karteHelpIcon.style.unityBackgroundImageTintColor  = new StyleColor(
                        new UnityEngine.Color(128f / 255f, 207f / 255f, 149f / 255f));
                }

                if (KategorieTooltips.TryGetValue(kategorieName, out string tooltipText))
                    HelpTooltip.RegistriereInKarte(root, karteHelpIcon, tooltipText);
            }

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
                        var iconLabel = new Label("\U0001f4d6");
                        iconLabel.style.fontSize       = 13;
                        iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                        iconLabel.style.flexGrow       = 1;
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
                        plusBtn.text    = "\U0001f441";
                        plusBtn.tooltip = "Anzeigen";
                        plusBtn.clicked += () => OpenViewPopup(eintrag);
                        RegistriereEyeButtonHover(plusBtn);
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

    // ============================================================
    // POPUPS
    // ============================================================

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

        var kategorieEintraege = wissensEintraege
            .Where(w => w.category == activeCategoryForList).ToList();

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

            Label nameLabel = new Label($"\U0001f4d6 {displayTitle}");
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

    // ============================================================
    // EYE-BUTTON HOVER (manuell statt :hover-Pseudoklasse)
    // ------------------------------------------------------------
    // BUG-FIX: Wenn man auf das Augen-Icon klickt, öffnet sich das
    // View-Popup-Overlay direkt über dem Button, während der Mauszeiger
    // noch drüber steht. Dadurch bekommt Unity nie ein PointerLeave für
    // den Button und die grüne :hover-Farbe aus dem Stylesheet bleibt
    // hängen, auch nachdem die Maus längst woanders ist. Deshalb hier
    // eigene, garantiert korrekte Hover-Steuerung per Inline-Style
    // (überschreibt immer die evtl. hängengebliebene CSS-Pseudoklasse)
    // plus ein Zwangs-Reset beim Öffnen/Schließen des Popups.
    // ============================================================
    private static readonly Color EyeBtnNormalBg     = new Color(40f / 255f, 40f / 255f, 40f / 255f);
    private static readonly Color EyeBtnNormalBorder  = new Color(70f / 255f, 70f / 255f, 70f / 255f);
    private static readonly Color EyeBtnNormalText    = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color EyeBtnHoverBg       = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color EyeBtnHoverText     = new Color(20f / 255f, 20f / 255f, 20f / 255f);

    private void RegistriereEyeButtonHover(Button btn)
    {
        SetzeEyeButtonNormal(btn);
        btn.RegisterCallback<PointerEnterEvent>(_ =>
        {
            // WICHTIG: Bei schnellem Mausbewegen zwischen mehreren Augen-Icons
            // feuert PointerLeave beim vorherigen Button in Unity manchmal
            // nicht zuverlässig (bekannte UI-Toolkit-Eigenheit). Deshalb hier
            // NICHT auf das eigene Leave-Event verlassen, sondern bei jedem
            // Enter zwangsweise ALLE anderen Augen-Icons zurücksetzen -
            // Enter feuert zuverlässig, Leave nicht.
            gridContainer?.Query<Button>(className: "doc-mini-plus").ForEach(other =>
            {
                if (other != btn) SetzeEyeButtonNormal(other);
            });
            SetzeEyeButtonHover(btn);
        });
        btn.RegisterCallback<PointerLeaveEvent>(_ => SetzeEyeButtonNormal(btn));
    }

    private void SetzeEyeButtonHover(Button btn)
    {
        btn.style.backgroundColor = new StyleColor(EyeBtnHoverBg);
        btn.style.borderTopColor = btn.style.borderBottomColor =
            btn.style.borderLeftColor = btn.style.borderRightColor = new StyleColor(EyeBtnHoverBg);
        btn.style.color = new StyleColor(EyeBtnHoverText);
    }

    private void SetzeEyeButtonNormal(Button btn)
    {
        btn.style.backgroundColor = new StyleColor(EyeBtnNormalBg);
        btn.style.borderTopColor = btn.style.borderBottomColor =
            btn.style.borderLeftColor = btn.style.borderRightColor = new StyleColor(EyeBtnNormalBorder);
        btn.style.color = new StyleColor(EyeBtnNormalText);
    }

    private void CloseDetailPopup()
    {
        if (detailPopupOverlay != null)
            detailPopupOverlay.style.display = DisplayStyle.None;
    }

    private void OpenViewPopup(WissensEintrag eintrag)
    {
        // Sicherheits-Reset: alle Augen-Icons zwingend auf "normal" setzen,
        // bevor das Overlay sie verdeckt (siehe Kommentar oben).
        gridContainer?.Query<Button>(className: "doc-mini-plus").ForEach(SetzeEyeButtonNormal);

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

        // Nochmal zur Sicherheit zurücksetzen (falls die Maus beim
        // Schließen zufällig noch über einem Augen-Icon steht).
        gridContainer?.Query<Button>(className: "doc-mini-plus").ForEach(SetzeEyeButtonNormal);
    }

    // ============================================================
    // HELP TOOLTIPS
    // ============================================================

    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Die Wissensdatenbank enthält Anleitungen und Erklärungen rund um deine Gründung. " +
            "Klicke auf eine Kategorie-Karte um alle Einträge zu sehen. " +
            "Mit dem Auge-Symbol öffnest du einen Eintrag zum Lesen.");

        HelpTooltip.Registriere(root, "btn-help-detail-popup",
            "Alle Einträge dieser Kategorie auf einen Blick. " +
            "Klicke auf Anzeigen um einen Eintrag vollständig zu lesen.");

        HelpTooltip.Registriere(root, "btn-help-view-popup",
            "Vollansicht des gewählten Wissenseintrags. " +
            "Scrolle nach unten um den gesamten Inhalt zu lesen.");
    }
}
