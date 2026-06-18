// ================================================================
// DocumentDashboard.cs  – Dokumente-Pool, überarbeitet
//
// NEU:
//  - Feste Kategorien (Gründung, Bezahlweise) mit vordefinierten
//    Pflicht-Dokumenten. Diese sind NICHT löschbar, aber Titel und
//    Inhalt bleiben frei bearbeitbar. Sie können nicht aus der
//    "Globale Liste" in andere Kategorien verschoben werden.
//  - Flexible Kategorien (Finanzen, Marketing, Steuern, Personal,
//    Recht) funktionieren weiterhin wie bisher.
//  - Bezahlweise-Daten sind über GetBezahlweiseDaten() für den
//    BelegScreenController / PDF-Export zugänglich.
//  - Design an Angebot-Screen angepasst (siehe Doc-Screen.uss).
// ================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class DocumentDashboard : MonoBehaviour
{
    [Header("UI Document Asset")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Templates")]
    [SerializeField] private VisualTreeAsset categoryCardTemplate;

    // UI Elemente Hauptbildschirm
    private VisualElement root;
    private Button        deleteButton;
    private VisualElement gridContainer;

    // Erstell-Popup Elemente
    private VisualElement popupOverlay;
    private Button        popupCancelButton;
    private Button        popupSubmitButton;
    private DropdownField categoryDropdown;
    private TextField     docNameInput;
    private Button        btnTypeStandard;
    private Button        btnTypeDiagramm;
    private Button        btnTypeChecklist;

    // Kategorie-Listen-Popup Elemente
    private VisualElement detailPopupOverlay;
    private VisualElement detailListContainer;
    private VisualElement globalListContainer;
    private Button        detailCloseButton;
    private Label         detailPopupTitle;
    private Button        listCreateNewButton;

    // Bearbeiten-Popup Elemente
    private VisualElement editPopupOverlay;
    private TextField     editDocNameInput;
    private TextField     editInhaltInput;
    private Button        editPopupSubmitButton;
    private Button        editPopupCancelButton;
    private Button        btnEditTypeStandard;
    private Button        btnEditTypeDiagramm;
    private Button        btnEditTypeChecklist;
    private Label         editLockedHint;

    // System-Zustaende
    private string       selectedType           = "Standard";
    private string       selectedEditType       = "Standard";
    private string       activeCategoryForList  = "";
    private DocumentData activeDocForEditing;

    // ============================================================
    // KATEGORIEN-DEFINITION
    //
    // istFest = true  -> nicht löschbar, nicht in andere Kategorie
    //                     verschiebbar. Titel/Inhalt bleiben editierbar.
    // pflichtDocs      -> werden beim ersten Start automatisch angelegt
    //                     falls noch nicht vorhanden.
    // ============================================================
    private class KategorieDefinition
    {
        public string       name;
        public bool         istFest;
        public List<string> pflichtDocs;
    }

    private readonly List<KategorieDefinition> kategorien = new List<KategorieDefinition>
    {
        new KategorieDefinition { name = "Gründung",    istFest = true,  pflichtDocs = new List<string> { "Unternehmensstammdaten", "Gründungsurkunde", "Handelsregisterauszug" } },
        new KategorieDefinition { name = "Bezahlweise",  istFest = true,  pflichtDocs = new List<string> { "Kontodaten (IBAN/BIC)", "Zahlungsbedingungen" } },
        new KategorieDefinition { name = "Finanzen",     istFest = false, pflichtDocs = new List<string>() },
        new KategorieDefinition { name = "Marketing",    istFest = false, pflichtDocs = new List<string>() },
        new KategorieDefinition { name = "Steuern",      istFest = false, pflichtDocs = new List<string>() },
        new KategorieDefinition { name = "Personal",     istFest = false, pflichtDocs = new List<string>() },
        new KategorieDefinition { name = "Recht",        istFest = false, pflichtDocs = new List<string>() },
    };

    private bool IstKategorieFest(string kategorieName)
        => kategorien.Find(k => k.name == kategorieName)?.istFest ?? false;

    [System.Serializable]
    public class DocumentData
    {
        public string id;
        public string category;
        public string title;
        public string type;
        public bool   istPflichtdokument; // true = Teil der festen Kategorie, nicht löschbar
        public string inhalt;             // freier Text/Daten (z.B. IBAN bei Bezahlweise)
    }

    [System.Serializable]
    public class DocumentSaveData
    {
        public List<DocumentData> savedDocs = new List<DocumentData>();
    }

    private DocumentSaveData speicherDaten = new DocumentSaveData();
    private string           saveFilePath;

    // ─────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────

    void OnEnable()
    {
        saveFilePath = Application.persistentDataPath + "/MyDashboardSave.json";

        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        // 1. Hauptbildschirm
        deleteButton  = root.Q<Button>("Delete-Button");
        gridContainer = root.Q<VisualElement>("Grid-Container");

        // 2. Erstell-Popup
        popupOverlay      = root.Q<VisualElement>("Popup-Overlay");
        popupCancelButton = root.Q<Button>("Btn-Cancel");
        var btnAbbrechen  = root.Q<Button>("Btn-Abbrechen");
        if (btnAbbrechen != null) btnAbbrechen.clicked += ClosePopup;
        popupSubmitButton = root.Q<Button>("Btn-Submit");
        categoryDropdown  = root.Q<DropdownField>("dropKategorie");
        docNameInput      = root.Q<TextField>("Doc-Name-Input");
        ErzwingeCursorSichtbarkeit(docNameInput);

        // 3. Zweispaltiges Listen-Popup
        detailPopupOverlay  = root.Q<VisualElement>("Detail-Popup-Overlay");
        detailListContainer = root.Q<VisualElement>("Detail-List-Container");
        globalListContainer = root.Q<VisualElement>("Global-List-Container");
        detailCloseButton   = root.Q<Button>("Btn-Detail-Close");
        detailPopupTitle    = root.Q<Label>("Detail-Popup-Title");
        listCreateNewButton = root.Q<Button>("Btn-List-Create-New");

        // 4. Bearbeiten-Popup
        editPopupOverlay      = root.Q<VisualElement>("Edit-Popup-Overlay");
        editDocNameInput      = root.Q<TextField>("Edit-Doc-Name-Input");
        editInhaltInput       = root.Q<TextField>("Edit-Inhalt-Input");
        ErzwingeCursorSichtbarkeit(editDocNameInput);
        ErzwingeCursorSichtbarkeit(editInhaltInput);
        editPopupSubmitButton = root.Q<Button>("Btn-Edit-Submit");
        editPopupCancelButton = root.Q<Button>("Btn-Edit-Cancel");
        btnEditTypeStandard   = root.Q<Button>("Btn-Edit-Type-Standard");
        btnEditTypeDiagramm   = root.Q<Button>("Btn-Edit-Type-Diagramm");
        btnEditTypeChecklist  = root.Q<Button>("Btn-Edit-Type-Checklist");
        editLockedHint        = root.Q<Label>("Edit-Locked-Hint");

        // Dropdown befuellen (nur Kategorienamen)
        if (categoryDropdown != null)
        {
            var namen = kategorien.Select(k => k.name).ToList();
            categoryDropdown.choices = namen;
            if (namen.Count > 0)
                categoryDropdown.value = namen[0];
        }

        // Event-Verdrahtung Hauptmenu
        if (deleteButton != null) deleteButton.clicked += deleteAllDocuments;

        // Event-Verdrahtung Erstell-Popup
        if (popupCancelButton != null) popupCancelButton.clicked += ClosePopup;
        if (popupSubmitButton != null) popupSubmitButton.clicked += CreateNewDocumentEntry;

        btnTypeStandard  = root.Q<Button>("Btn-Type-Standard");
        btnTypeDiagramm  = root.Q<Button>("Btn-Type-Diagramm");
        btnTypeChecklist = root.Q<Button>("Btn-Type-Checklist");

        if (btnTypeStandard  != null) btnTypeStandard.clicked  += () => ApplyTemplate("Standard");
        if (btnTypeDiagramm  != null) btnTypeDiagramm.clicked  += () => ApplyTemplate("Diagramm");
        if (btnTypeChecklist != null) btnTypeChecklist.clicked += () => ApplyTemplate("Checklist");

        // Event-Verdrahtung Listen-Popup
        if (detailCloseButton   != null) detailCloseButton.clicked += CloseDetailPopup;
        if (listCreateNewButton != null)
            listCreateNewButton.clicked += () =>
            {
                CloseDetailPopup();
                OpenPopup(activeCategoryForList);
            };

        // Event-Verdrahtung Bearbeiten-Popup
        if (editPopupCancelButton != null) editPopupCancelButton.clicked += CloseEditPopup;
        if (editPopupSubmitButton != null) editPopupSubmitButton.clicked += SaveEditedDocumentEntry;
        if (btnEditTypeStandard  != null) btnEditTypeStandard.clicked   += () => SelectEditType("Standard");
        if (btnEditTypeDiagramm  != null) btnEditTypeDiagramm.clicked   += () => SelectEditType("Diagramm");
        if (btnEditTypeChecklist != null) btnEditTypeChecklist.clicked  += () => SelectEditType("Checklist");

        // Enter schluckt nur Zeilenumbruch im Edit-Textfeld
        if (editDocNameInput != null)
            editDocNameInput.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    evt.StopPropagation();
            }, TrickleDown.TrickleDown);

        LoadDataLocally();
        SicherePflichtdokumente();   // legt fehlende Pflicht-Docs an
        SpawnAllCardsAtStart();
    }

    // ─────────────────────────────────────────
    // PFLICHTDOKUMENTE SICHERSTELLEN
    // (läuft bei jedem Start, ergänzt nur was fehlt)
    // ─────────────────────────────────────────
    private void SicherePflichtdokumente()
    {
        bool geaendert = false;

        foreach (var kategorie in kategorien.Where(k => k.istFest))
        {
            foreach (string pflichtTitel in kategorie.pflichtDocs)
            {
                bool existiertSchon = speicherDaten.savedDocs.Any(d =>
                    d.category == kategorie.name &&
                    d.istPflichtdokument &&
                    d.title == pflichtTitel);

                if (!existiertSchon)
                {
                    speicherDaten.savedDocs.Add(new DocumentData
                    {
                        id                 = System.Guid.NewGuid().ToString(),
                        category           = kategorie.name,
                        title              = pflichtTitel,
                        type               = "Standard",
                        istPflichtdokument = true,
                        inhalt             = ""
                    });
                    geaendert = true;
                }
            }
        }

        if (geaendert) SaveDataLocally();
    }

    // ─────────────────────────────────────────
    // ERSTELLLOGIK
    // ─────────────────────────────────────────

    private void OpenPopup(string preselectedCategory = "")
    {
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.Flex;
        if (docNameInput != null) docNameInput.value         = "";
        selectedType = "Standard";
        MarkiereAusgewaehlteVorlage(btnTypeStandard, btnTypeDiagramm, btnTypeChecklist, selectedType);

        if (!string.IsNullOrEmpty(preselectedCategory) && categoryDropdown != null)
            if (kategorien.Any(k => k.name == preselectedCategory))
                categoryDropdown.value = preselectedCategory;
    }

    private void ClosePopup()
    {
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.None;
    }

    private void ApplyTemplate(string typeName)
    {
        selectedType = typeName;
        MarkiereAusgewaehlteVorlage(btnTypeStandard, btnTypeDiagramm, btnTypeChecklist, selectedType);
    }

    private void CreateNewDocumentEntry()
    {
        string selectedCategory = categoryDropdown != null ? categoryDropdown.value : "";

        // Pflicht: Dokumenttitel muss vorhanden sein
        string docText = docNameInput != null ? docNameInput.value.Trim() : "";
        if (string.IsNullOrEmpty(docText))
        {
            if (docNameInput != null)
            {
                docNameInput.AddToClassList("input-error");
                docNameInput.schedule.Execute(() => docNameInput.RemoveFromClassList("input-error")).ExecuteLater(1200);
            }
            return;
        }

        if (string.IsNullOrEmpty(selectedCategory)) return;

        DocumentData newDoc = new DocumentData
        {
            id                 = System.Guid.NewGuid().ToString(),
            category           = selectedCategory,
            title              = docText,
            type               = selectedType,
            istPflichtdokument = false,
            inhalt             = ""
        };

        speicherDaten.savedDocs.Add(newDoc);
        SaveDataLocally();
        SpawnAllCardsAtStart();
        ClosePopup();
    }

    // ─────────────────────────────────────────
    // DASHBOARD KACHELN
    // ─────────────────────────────────────────

    private void SpawnAllCardsAtStart()
    {
        if (gridContainer == null || categoryCardTemplate == null) return;
        gridContainer.Clear();

        foreach (var kategorie in kategorien)
        {
            VisualElement cardInstance = categoryCardTemplate.Instantiate();

            Label titleLabel = cardInstance.Q<Label>("lblName");
            if (titleLabel != null) titleLabel.text = kategorie.name;

            // Feste Kategorien bekommen ein Schloss-Badge
            VisualElement lockBadge = cardInstance.Q<VisualElement>("lock-badge");
            if (lockBadge != null)
                lockBadge.style.display = kategorie.istFest ? DisplayStyle.Flex : DisplayStyle.None;

            VisualElement imgShowList = cardInstance.Q("Btn-Show-List");
            if (imgShowList != null)
                imgShowList.RegisterCallback<ClickEvent>(evt => OpenDetailPopup(kategorie.name));

            // "Alle anzeigen" öffnet ebenfalls die Detail-Liste dieser Kategorie
            Button alleAnzeigenBtn = cardInstance.Q<Button>("btnPlus");
            if (alleAnzeigenBtn != null) alleAnzeigenBtn.clicked += () => OpenDetailPopup(kategorie.name);

            List<DocumentData> kategorieDocs =
                speicherDaten.savedDocs.FindAll(d => d.category == kategorie.name);

            // 2x2 Grid: 4 Slots befüllen, jeder mit eigenem Icon, Inhalt und Plus-Button
            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                VisualElement slot       = cardInstance.Q<VisualElement>($"Slot-{slotIndex}");
                VisualElement iconBox    = cardInstance.Q<VisualElement>($"Slot-{slotIndex}-Icon");
                VisualElement contentBox = cardInstance.Q<VisualElement>($"Slot-{slotIndex}-Content");
                Button        plusBtn    = cardInstance.Q<Button>($"Slot-{slotIndex}-Plus");

                if (contentBox == null) continue;
                contentBox.Clear();

                bool hatDokument = slotIndex < kategorieDocs.Count;

                if (hatDokument)
                {
                    var aktuellesDoc = kategorieDocs[slotIndex];

                    string iconGlyph = aktuellesDoc.type == "Diagramm" ? "📊" :
                                        aktuellesDoc.type == "Checklist" ? "✅" : "📄";
                    if (iconBox != null)
                    {
                        iconBox.Clear();
                        var iconLabel = new Label(iconGlyph);
                        iconLabel.style.fontSize = 13;
                        iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                        iconLabel.style.flexGrow = 1;
                        iconBox.Add(iconLabel);
                    }

                    string titel = aktuellesDoc.title.Split('\n')[0];
                    if (titel.Length > 22) titel = titel.Substring(0, 19) + "...";
                    Label titelLabel = new Label(titel);
                    titelLabel.AddToClassList("doc-mini-title");
                    contentBox.Add(titelLabel);

                    if (!string.IsNullOrEmpty(aktuellesDoc.inhalt))
                    {
                        string inhaltVorschau = aktuellesDoc.inhalt.Split('\n')[0];
                        if (inhaltVorschau.Length > 24) inhaltVorschau = inhaltVorschau.Substring(0, 21) + "...";
                        Label inhaltLabel = new Label(inhaltVorschau);
                        inhaltLabel.AddToClassList("doc-mini-inhalt");
                        contentBox.Add(inhaltLabel);
                    }
                    else
                    {
                        Label leerHinweis = new Label("Kein Inhalt hinterlegt");
                        leerHinweis.AddToClassList("doc-mini-inhalt-leer");
                        contentBox.Add(leerHinweis);
                    }

                    // Plus-Button im befüllten Slot öffnet die Bearbeitung dieses Dokuments
                    if (plusBtn != null)
                    {
                        plusBtn.text = "✎";
                        plusBtn.clicked += () => OpenEditPopup(aktuellesDoc);
                    }
                }
                else
                {
                    // Leerer Slot: dezenter Hinweis + Plus-Button öffnet Erstell-Popup
                    if (iconBox != null)
                    {
                        iconBox.Clear();
                        iconBox.style.opacity = 0.4f;
                    }

                    Label leerLabel = new Label("Leer");
                    leerLabel.AddToClassList("doc-mini-empty-label");
                    contentBox.Add(leerLabel);

                    if (plusBtn != null)
                    {
                        plusBtn.text = "+";
                        plusBtn.clicked += () => OpenPopup(kategorie.name);
                    }
                }

                if (slot != null)
                    slot.style.opacity = hatDokument ? 1f : 0.75f;
            }

            gridContainer.Add(cardInstance);
        }
    }

    // ─────────────────────────────────────────
    // LISTEN-POPUP
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
        if (detailListContainer == null || globalListContainer == null) return;
        detailListContainer.Clear();
        globalListContainer.Clear();

        List<DocumentData> kategorieDocs =
            speicherDaten.savedDocs.FindAll(d => d.category == activeCategoryForList);
        BuildListColumn(kategorieDocs, detailListContainer, isGlobal: false);

        BuildListColumn(speicherDaten.savedDocs, globalListContainer, isGlobal: true);
    }

    private void BuildListColumn(List<DocumentData> docs, VisualElement container, bool isGlobal)
    {
        if (docs.Count == 0)
        {
            Label emptyLabel = new Label(isGlobal ? "Keine Dokumente vorhanden." : "Kategorie ist leer.");
            emptyLabel.AddToClassList("list-empty-hint");
            container.Add(emptyLabel);
            return;
        }

        foreach (var doc in docs)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("list-row-item");

            string icon         = doc.type == "Diagramm" ? "D" : doc.type == "Checklist" ? "C" : "S";
            string displayTitle = doc.title.Split('\n')[0];
            if (displayTitle.Length > 30) displayTitle = displayTitle.Substring(0, 27) + "...";
            string lockIcon      = doc.istPflichtdokument ? "🔒 " : "";
            string textToShow    = isGlobal
                ? $"{lockIcon}[{doc.category}] [{icon}] {displayTitle}"
                : $"{lockIcon}[{icon}] {displayTitle}";

            Label nameLabel = new Label(textToShow);
            nameLabel.AddToClassList("list-row-label");
            row.Add(nameLabel);

            // Inhalt-Vorschau direkt in der Zeile (falls vorhanden)
            if (!string.IsNullOrEmpty(doc.inhalt))
            {
                string vorschau = doc.inhalt.Length > 40 ? doc.inhalt.Substring(0, 37) + "..." : doc.inhalt;
                Label inhaltLabel = new Label(vorschau);
                inhaltLabel.AddToClassList("list-row-inhalt-preview");
                row.Add(inhaltLabel);
            }

            VisualElement btnGroup = new VisualElement();
            btnGroup.AddToClassList("row-btn-group");

            if (isGlobal)
            {
                // Pflichtdokumente können NICHT in andere Kategorien verschoben werden
                if (doc.category != activeCategoryForList && !doc.istPflichtdokument)
                {
                    Button moveBtn = new Button { text = "Hinzufügen" };
                    moveBtn.AddToClassList("btn-add-global");
                    moveBtn.clicked += () => MoveDocumentToActiveCategory(doc);
                    btnGroup.Add(moveBtn);
                }
            }
            else
            {
                Button editBtn = new Button { text = "Bearbeiten" };
                editBtn.AddToClassList("btn-action-text");
                editBtn.AddToClassList("btn-edit-pen");
                editBtn.tooltip  = "Dokument bearbeiten";
                editBtn.clicked += () => OpenEditPopup(doc);
                btnGroup.Add(editBtn);

                // Pflichtdokumente sind nicht löschbar
                if (!doc.istPflichtdokument)
                {
                    Button deleteBtn = new Button { text = "Löschen" };
                    deleteBtn.AddToClassList("btn-action-text");
                    deleteBtn.AddToClassList("btn-minus-delete");
                    deleteBtn.tooltip  = "Dokument löschen";
                    deleteBtn.clicked += () => DeleteSingleDocument(doc.id);
                    btnGroup.Add(deleteBtn);
                }
                else
                {
                    Label gesperrtLabel = new Label("Geschützt");
                    gesperrtLabel.AddToClassList("locked-badge-inline");
                    btnGroup.Add(gesperrtLabel);
                }
            }

            row.Add(btnGroup);
            container.Add(row);
        }
    }

    private void MoveDocumentToActiveCategory(DocumentData doc)
    {
        DocumentData docInStorage = speicherDaten.savedDocs.Find(d => d.id == doc.id);
        if (docInStorage == null) return;
        if (docInStorage.istPflichtdokument) return; // Sicherheitsnetz

        docInStorage.category = activeCategoryForList;
        SaveDataLocally();
        RefreshDetailList();
        SpawnAllCardsAtStart();
    }

    private void DeleteSingleDocument(string docId)
    {
        var doc = speicherDaten.savedDocs.Find(d => d.id == docId);
        if (doc == null || doc.istPflichtdokument) return; // Pflichtdoks nicht löschbar

        speicherDaten.savedDocs.RemoveAll(d => d.id == docId);
        SaveDataLocally();
        RefreshDetailList();
        SpawnAllCardsAtStart();
    }

    private void CloseDetailPopup()
    {
        if (detailPopupOverlay != null)
            detailPopupOverlay.style.display = DisplayStyle.None;
    }

    // ─────────────────────────────────────────
    // BEARBEITEN-POPUP
    // ─────────────────────────────────────────

    private void OpenEditPopup(DocumentData doc)
    {
        activeDocForEditing = doc;
        selectedEditType    = doc.type;

        if (editPopupOverlay != null)
            editPopupOverlay.style.display = DisplayStyle.Flex;

        if (editLockedHint != null)
            editLockedHint.style.display = doc.istPflichtdokument ? DisplayStyle.Flex : DisplayStyle.None;

        if (editDocNameInput != null)
        {
            editDocNameInput.value = doc.title;
            editDocNameInput.schedule.Execute(() => editDocNameInput.Focus()).ExecuteLater(50);
        }

        if (editInhaltInput != null)
            editInhaltInput.value = doc.inhalt ?? "";

        MarkiereAusgewaehlteVorlage(btnEditTypeStandard, btnEditTypeDiagramm, btnEditTypeChecklist, selectedEditType);
    }

    private void CloseEditPopup()
    {
        if (editPopupOverlay != null)
            editPopupOverlay.style.display = DisplayStyle.None;
        activeDocForEditing = null;
    }

    private void SelectEditType(string typeName)
    {
        selectedEditType = typeName;
        MarkiereAusgewaehlteVorlage(btnEditTypeStandard, btnEditTypeDiagramm, btnEditTypeChecklist, selectedEditType);
    }

    private void SaveEditedDocumentEntry()
    {
        if (activeDocForEditing == null) return;

        string updatedTitle = (editDocNameInput != null && !string.IsNullOrEmpty(editDocNameInput.value))
            ? editDocNameInput.value
            : "Unbenannt";

        string updatedInhalt = editInhaltInput != null ? editInhaltInput.value : "";

        DocumentData docInList = speicherDaten.savedDocs.Find(d => d.id == activeDocForEditing.id);
        if (docInList != null)
        {
            // Titel UND Inhalt bleiben editierbar, auch bei Pflichtdokumenten
            docInList.title  = updatedTitle;
            docInList.type   = selectedEditType;
            docInList.inhalt = updatedInhalt;
        }

        SaveDataLocally();
        RefreshDetailList();
        SpawnAllCardsAtStart();
        CloseEditPopup();
    }

    // ─────────────────────────────────────────
    // SPEICHERVERWALTUNG
    // ─────────────────────────────────────────

    private void SaveDataLocally()
    {
        string json = JsonUtility.ToJson(speicherDaten, true);
        File.WriteAllText(saveFilePath, json);
    }

    private void deleteAllDocuments()
    {
        // Löscht nur NICHT-Pflichtdokumente. Feste Kategorien bleiben erhalten.
        speicherDaten.savedDocs.RemoveAll(d => !d.istPflichtdokument);
        SaveDataLocally();
        SpawnAllCardsAtStart();
    }

    private void LoadDataLocally()
    {
        if (File.Exists(saveFilePath))
            speicherDaten = JsonUtility.FromJson<DocumentSaveData>(File.ReadAllText(saveFilePath));
    }

    // ─────────────────────────────────────────
    // STATISCHER ZUGRIFF FUER EXPORT SCREEN
    // ─────────────────────────────────────────

    public static string GetSaveFilePath()
    {
        return Application.persistentDataPath + "/MyDashboardSave.json";
    }

    public static DocumentSaveData GetSavedDocuments()
    {
        string path = GetSaveFilePath();
        if (!File.Exists(path)) return new DocumentSaveData();
        return JsonUtility.FromJson<DocumentSaveData>(File.ReadAllText(path));
    }

    // ─────────────────────────────────────────
    // BEZAHLWEISE-ZUGRIFF
    // Für BelegScreenController / PDF-Export:
    // Liefert alle Dokumente der Kategorie "Bezahlweise" als
    // Liste von (Titel, Inhalt)-Paaren, z.B. für IBAN/BIC-Einfügung
    // in Angebot/Rechnung PDFs.
    // ─────────────────────────────────────────
    public static List<DocumentData> GetBezahlweiseDaten()
    {
        var alle = GetSavedDocuments();
        return alle.savedDocs.FindAll(d => d.category == "Bezahlweise");
    }

    // ─────────────────────────────────────────
    // VORLAGEN-AUSWAHL VISUELL MARKIEREN
    //
    // Setzt die Klasse "selected-template" auf den Button der
    // aktuell aktiven Vorlage und entfernt sie von den anderen.
    // ─────────────────────────────────────────
    private void MarkiereAusgewaehlteVorlage(Button standard, Button diagramm, Button checklist, string aktiverTyp)
    {
        standard?.RemoveFromClassList("selected-template");
        diagramm?.RemoveFromClassList("selected-template");
        checklist?.RemoveFromClassList("selected-template");

        switch (aktiverTyp)
        {
            case "Standard":  standard?.AddToClassList("selected-template");  break;
            case "Diagramm":  diagramm?.AddToClassList("selected-template");  break;
            case "Checklist": checklist?.AddToClassList("selected-template"); break;
        }
    }

    // ─────────────────────────────────────────
    // CURSOR-SICHTBARKEIT
    //
    // Wird vollständig über die USS-Eigenschaft --unity-cursor-color
    // in Doc-Screen.uss gesteuert. Die C#-Properties cursorColor /
    // selectionColor sind in dieser Unity-Version deprecated und
    // wurden deshalb entfernt.
    // ─────────────────────────────────────────
    private void ErzwingeCursorSichtbarkeit(TextField feld)
    {
        // Bewusst leer – Cursor-Farbe kommt rein aus USS (siehe .unity-base-field__input)
    }
}
