using System.Collections.Generic;
using System.IO; 
using UnityEngine;
using UnityEngine.UIElements;

public class DocumentDashboard : MonoBehaviour
{
    [Header("UI Document Asset")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Templates")]
    [SerializeField] private VisualTreeAsset categoryCardTemplate;

    [Header("Font Settings")]
    [Tooltip("Zieh hier dein Poppins-Bold Font Asset aus dem Projektordner rein!")]
    [SerializeField] private Font poppinsBoldFont;

    // UI Elemente Hauptbildschirm
    private VisualElement root;
    private Button createButton;
    private Button deleteButton;
    private VisualElement gridContainer;

    // Erstell-Pop-up Elemente
    private VisualElement popupOverlay;
    private Button popupCancelButton;
    private Button popupSubmitButton; 
    private DropdownField categoryDropdown; 
    private TextField docNameInput; 

    // Kategorie-Listen-Pop-up Elemente (Zweispaltig, extra breit)
    private VisualElement detailPopupOverlay;
    private VisualElement detailListContainer;  // Linke Liste (Kategorie-Docs)
    private VisualElement globalListContainer;  // Rechte Liste (Globale Docs)
    private Button detailCloseButton;
    private Label detailPopupTitle;
    private Button listCreateNewButton; 

    // Bearbeiten-Pop-up Elemente (Groß & Breit für Textbearbeitung)
    private VisualElement editPopupOverlay;
    private TextField editDocNameInput;         // Große Multiline-Textbox
    private Button editPopupSubmitButton;
    private Button editPopupCancelButton;
    private Button btnEditTypeStandard;
    private Button btnEditTypeDiagramm;
    private Button btnEditTypeChecklist;

    // System-Zustände
    private string selectedType = "Standard";
    private string selectedEditType = "Standard"; 
    private string activeCategoryForList = ""; 
    private DocumentData activeDocForEditing;    
    
    private List<string> dropdownKategorien = new List<string> { 
        "Gründung", "Finanzen", "Marketing", "Steuern", "Personal", "Recht" 
    };

    [System.Serializable]
    public class DocumentData
    {
        public string id; 
        public string category;
        public string title;
        public string type;
    }

    [System.Serializable]
    public class DocumentSaveData
    {
        public List<DocumentData> savedDocs = new List<DocumentData>();
    }

    private DocumentSaveData speicherDaten = new DocumentSaveData();
    private string saveFilePath;

    void OnEnable()
    {
        saveFilePath = Application.persistentDataPath + "/MyDashboardSave.json";

        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        // 1. Hauptbildschirm
        createButton = root.Q<Button>("Create-Button");
        deleteButton = root.Q<Button>("Delete-Button");
        gridContainer = root.Q<VisualElement>("Grid-Container");

        // 2. Erstell-Popup
        popupOverlay = root.Q<VisualElement>("Popup-Overlay");
        popupCancelButton = root.Q<Button>("Btn-Cancel"); 
        popupSubmitButton = root.Q<Button>("Btn-Submit"); 
        categoryDropdown = root.Q<DropdownField>("dropKategorie"); 
        docNameInput = root.Q<TextField>("Doc-Name-Input"); 

        // 3. Zweispaltiges Listen-Popup
        detailPopupOverlay = root.Q<VisualElement>("Detail-Popup-Overlay");
        detailListContainer = root.Q<VisualElement>("Detail-List-Container"); // Links
        globalListContainer = root.Q<VisualElement>("Global-List-Container"); // Rechts
        detailCloseButton = root.Q<Button>("Btn-Detail-Close");
        detailPopupTitle = root.Q<Label>("Detail-Popup-Title");
        listCreateNewButton = root.Q<Button>("Btn-List-Create-New"); 

        // 4. Überarbeitetes Bearbeiten-Popup
        editPopupOverlay = root.Q<VisualElement>("Edit-Popup-Overlay");
        editDocNameInput = root.Q<TextField>("Edit-Doc-Name-Input"); // Große Textbox
        editPopupSubmitButton = root.Q<Button>("Btn-Edit-Submit");
        editPopupCancelButton = root.Q<Button>("Btn-Edit-Cancel");
        btnEditTypeStandard = root.Q<Button>("Btn-Edit-Type-Standard");
        btnEditTypeDiagramm = root.Q<Button>("Btn-Edit-Type-Diagramm");
        btnEditTypeChecklist = root.Q<Button>("Btn-Edit-Type-Checklist");

        // Dropdown Befüllung
        if (categoryDropdown != null)
        {
            categoryDropdown.choices = dropdownKategorien;
            if (dropdownKategorien.Count > 0) categoryDropdown.value = dropdownKategorien[0]; 
        }

        // Event-Verdrahtung Erstellung & Hauptmenü
        if (createButton != null) createButton.clicked += () => OpenPopup();
        if (deleteButton != null) deleteButton.clicked += deleteAllDocuments;
        if (popupCancelButton != null) popupCancelButton.clicked += ClosePopup;
        if (popupSubmitButton != null) popupSubmitButton.clicked += CreateNewDocumentEntry;

        // Die 3 Vorlagen-Buttons rufen jetzt sauber die korrigierte ApplyTemplate Methode auf
        Button btnTypeStandard = root.Q<Button>("Btn-Type-Standard");
        Button btnTypeDiagramm = root.Q<Button>("Btn-Type-Diagramm");
        Button btnTypeChecklist = root.Q<Button>("Btn-Type-Checklist");

        if (btnTypeStandard != null) btnTypeStandard.clicked += () => ApplyTemplate("Standard");
        if (btnTypeDiagramm != null) btnTypeDiagramm.clicked += () => ApplyTemplate("Diagramm");
        if (btnTypeChecklist != null) btnTypeChecklist.clicked += () => ApplyTemplate("Checklist");

        // Event-Verdrahtung Listen-Popup
        if (detailCloseButton != null) detailCloseButton.clicked += CloseDetailPopup;
        if (listCreateNewButton != null)
        {
            listCreateNewButton.clicked += () => {
                CloseDetailPopup();
                OpenPopup(activeCategoryForList); 
            };
        }

        // Event-Verdrahtung Bearbeiten-Popup
        if (editPopupCancelButton != null) editPopupCancelButton.clicked += CloseEditPopup;
        if (editPopupSubmitButton != null) editPopupSubmitButton.clicked += SaveEditedDocumentEntry;
        if (btnEditTypeStandard != null) btnEditTypeStandard.clicked += () => SelectEditType("Standard");
        if (btnEditTypeDiagramm != null) btnEditTypeDiagramm.clicked += () => SelectEditType("Diagramm");
        if (btnEditTypeChecklist != null) btnEditTypeChecklist.clicked += () => SelectEditType("Checklist");

        LoadDataLocally();
        SpawnAllCardsAtStart();
    }

    // --- ERSTELLLOGIK ---
    private void OpenPopup(string preselectedCategory = "") 
    { 
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.Flex; 
        if (docNameInput != null) docNameInput.value = ""; 
        selectedType = "Standard"; 
    }

    private void ClosePopup() => popupOverlay.style.display = DisplayStyle.None;

    private void ApplyTemplate(string typeName)
    {
        selectedType = typeName;
    }

    private void CreateNewDocumentEntry()
    {
        string selectedCategory = categoryDropdown != null ? categoryDropdown.value : "";
        string docText = (docNameInput != null && !string.IsNullOrEmpty(docNameInput.value)) ? docNameInput.value : "Unbenannt";
        if (string.IsNullOrEmpty(selectedCategory)) return;

        DocumentData newDoc = new DocumentData { 
            id = System.Guid.NewGuid().ToString(), 
            category = selectedCategory, 
            title = docText, 
            type = selectedType 
        };
        speicherDaten.savedDocs.Add(newDoc);
        
        SaveDataLocally();
        SpawnAllCardsAtStart();
        ClosePopup();
    }

    // --- RENDERING DASHBOARD KACHELN ---
    private void SpawnAllCardsAtStart()
    {
        if (gridContainer == null || categoryCardTemplate == null) return;
        gridContainer.Clear(); 

        foreach (string kategorieName in dropdownKategorien)
        {
            VisualElement cardInstance = categoryCardTemplate.Instantiate();
            cardInstance.style.width = Length.Percent(30f);
            cardInstance.style.marginBottom = 20;

            Label titleLabel = cardInstance.Q<Label>("lblName");
            if (titleLabel != null) titleLabel.text = kategorieName;

            VisualElement imgShowList = cardInstance.Q("Btn-Show-List");
            if (imgShowList != null) imgShowList.RegisterCallback<ClickEvent>(evt => OpenDetailPopup(kategorieName));

            Button plusBtn = cardInstance.Q<Button>("btnPlus");
            if (plusBtn != null) plusBtn.clicked += () => OpenPopup(kategorieName);

            VisualElement feldOben = cardInstance.Q<VisualElement>("Datenfeld-Oben");
            VisualElement feldUnten = cardInstance.Q<VisualElement>("Datenfeld-Unten");
            if (feldOben != null) feldOben.Clear();
            if (feldUnten != null) feldUnten.Clear();

            List<DocumentData> kategorieDocs = speicherDaten.savedDocs.FindAll(d => d.category == kategorieName);

            for (int i = 0; i < kategorieDocs.Count; i++)
            {
                if (i >= 2) break; 

                VisualElement target = (i % 2 == 0) ? feldOben : feldUnten;
                if (target != null)
                {
                    VisualElement docEntry = new VisualElement();
                    string icon = (kategorieDocs[i].type == "Diagramm") ? "📊" : ((kategorieDocs[i].type == "Checklist") ? "☑️" : "📄");
                    
                    string firstLine = kategorieDocs[i].title.Split('\n')[0];
                    if (firstLine.Length > 20) firstLine = firstLine.Substring(0, 17) + "...";

                    Label docLabel = new Label($"{icon} {firstLine}");
                    docLabel.style.fontSize = 12;
                    docEntry.Add(docLabel);
                    target.Add(docEntry);
                }
            }
            gridContainer.Add(cardInstance);
        }
    }

    // --- ZWEISPALTYGES LISTEN POPUP ---
    private void OpenDetailPopup(string kategorie)
    {
        activeCategoryForList = kategorie; 
        if (detailPopupOverlay != null) detailPopupOverlay.style.display = DisplayStyle.Flex;
        if (detailPopupTitle != null) detailPopupTitle.text = kategorie; 

        RefreshDetailList();
    }

    private void RefreshDetailList()
    {
        if (detailListContainer == null || globalListContainer == null) return;
        detailListContainer.Clear();
        globalListContainer.Clear();

        // --- SPALTE 1: LINKE LISTE (Kategorie Dokumente)
        List<DocumentData> kategorieDocs = speicherDaten.savedDocs.FindAll(d => d.category == activeCategoryForList);
        BuildListColumn(kategorieDocs, detailListContainer, isGlobal: false);

        // --- SPALTE 2: RECHTE LISTE (Globale Liste / Sonstige)
        List<DocumentData> alleDocs = speicherDaten.savedDocs;
        BuildListColumn(alleDocs, globalListContainer, isGlobal: true);
    }

    private void BuildListColumn(List<DocumentData> docs, VisualElement container, bool isGlobal)
    {
        if (docs.Count == 0)
        {
            Label emptyLabel = new Label(isGlobal ? "Keine Dokumente vorhanden." : "Kategorie ist leer.");
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyLabel.style.marginTop = 15;
            emptyLabel.style.color = Color.gray;
            if (poppinsBoldFont != null) emptyLabel.style.unityFont = poppinsBoldFont;
            container.Add(emptyLabel);
            return;
        }

        foreach (var doc in docs)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("list-row-item");

            string icon = (doc.type == "Diagramm") ? "📊" : ((doc.type == "Checklist") ? "☑️" : "📄");
            
            string displayTitle = doc.title.Split('\n')[0];
            if (displayTitle.Length > 30) displayTitle = displayTitle.Substring(0, 27) + "...";
            
            string textToShow = isGlobal ? $"[{doc.category}] {icon} {displayTitle}" : $"{icon} {displayTitle}";

            Label nameLabel = new Label(textToShow);
            nameLabel.style.fontSize = 14;
            nameLabel.style.color = Color.white;
            if (poppinsBoldFont != null) { nameLabel.style.unityFont = poppinsBoldFont; nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold; }
            row.Add(nameLabel);

            VisualElement btnGroup = new VisualElement();
            btnGroup.AddToClassList("row-btn-group");

            if (isGlobal)
            {
                if (doc.category != activeCategoryForList)
                {
                    Button moveBtn = new Button { text = "Hinzufügen" }; 
                    moveBtn.AddToClassList("btn-add-global");
                    if (poppinsBoldFont != null) moveBtn.style.unityFont = poppinsBoldFont;
                    moveBtn.clicked += () => MoveDocumentToActiveCategory(doc);
                    btnGroup.Add(moveBtn);
                }
            }
            else
            {
                Button editBtn = new Button();
                editBtn.AddToClassList("btn-action-icon");
                editBtn.AddToClassList("btn-edit-pen");
                editBtn.tooltip = "Dokument bearbeiten";
                editBtn.clicked += () => OpenEditPopup(doc);
                btnGroup.Add(editBtn);

                Button deleteBtn = new Button();
                deleteBtn.AddToClassList("btn-action-icon");
                deleteBtn.AddToClassList("btn-minus-delete");
                deleteBtn.tooltip = "Dokument löschen";
                deleteBtn.clicked += () => DeleteSingleDocument(doc.id);
                btnGroup.Add(deleteBtn);
            }

            row.Add(btnGroup);
            container.Add(row);
        }
    }

    private void MoveDocumentToActiveCategory(DocumentData doc)
    {
        DocumentData docInStorage = speicherDaten.savedDocs.Find(d => d.id == doc.id);
        if (docInStorage != null)
        {
            docInStorage.category = activeCategoryForList; 
            SaveDataLocally();
            RefreshDetailList();
            SpawnAllCardsAtStart();
        }
    }

    private void DeleteSingleDocument(string docId)
    {
        speicherDaten.savedDocs.RemoveAll(d => d.id == docId);
        SaveDataLocally();
        RefreshDetailList();    
        SpawnAllCardsAtStart(); 
    }

    private void CloseDetailPopup() => detailPopupOverlay.style.display = DisplayStyle.None;

    // --- BEARBEITEN (EDIT) LOGIK ---
    private void OpenEditPopup(DocumentData doc)
    {
        activeDocForEditing = doc;
        selectedEditType = doc.type;
        if (editPopupOverlay != null) editPopupOverlay.style.display = DisplayStyle.Flex;
        if (editDocNameInput != null) editDocNameInput.value = doc.title; 
    }

    private void CloseEditPopup()
    {
        editPopupOverlay.style.display = DisplayStyle.None;
        activeDocForEditing = null;
    }

    private void SelectEditType(string typeName) => selectedEditType = typeName;

    private void SaveEditedDocumentEntry()
    {
        if (activeDocForEditing == null) return;

        string updatedTitle = (editDocNameInput != null && !string.IsNullOrEmpty(editDocNameInput.value)) ? editDocNameInput.value : "Unbenannt";

        DocumentData docInList = speicherDaten.savedDocs.Find(d => d.id == activeDocForEditing.id);
        if (docInList != null)
        {
            docInList.title = updatedTitle;
            docInList.type = selectedEditType;
        }

        SaveDataLocally();
        RefreshDetailList();    
        SpawnAllCardsAtStart(); 
        CloseEditPopup();
    }

    // --- SPEICHERVERWALTUNG (JSON) ---
    private void SaveDataLocally()
    {
        string json = JsonUtility.ToJson(speicherDaten, true);
        File.WriteAllText(saveFilePath, json);
    }

    private void deleteAllDocuments()
    {
        speicherDaten.savedDocs.Clear();
        SaveDataLocally();
        SpawnAllCardsAtStart();
    }

    private void LoadDataLocally()
    {
        if (File.Exists(saveFilePath))
            speicherDaten = JsonUtility.FromJson<DocumentSaveData>(File.ReadAllText(saveFilePath));
    }
}