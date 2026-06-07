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
    [Tooltip("Zieh hier dein Poppins-Bold Font Asset rein!")]
    [SerializeField] private Font poppinsBoldFont;

    // UI Elemente
    private VisualElement root;
    private VisualElement gridContainer;

    // Erstell-Pop-up Elemente
    private VisualElement popupOverlay;
    private Button popupCancelButton;
    private Button popupBackButton; 
    private Button popupSubmitButton; 
    private DropdownField categoryDropdown; 
    private TextField docNameInput; 

    // Kategorie-Listen-Pop-up Elemente
    private VisualElement detailPopupOverlay;
    private VisualElement detailListContainer;
    private Button detailCloseButton;
    private Label detailPopupTitle;
    private Button listCreateNewButton; 

    // Typ-Buttons im Erstell-Popup
    private Button btnTypeStandard;
    private Button btnTypeDiagramm;
    private Button btnTypeChecklist;

    private string selectedType = "Standard";
    private string activeCategoryForList = ""; 
    
    private List<string> dropdownKategorien = new List<string> { 
        "Gründung", "Finanzen", "Marketing", "Steuern", "Personal", "Recht" 
    };

    // Lokale Datenstruktur
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

        // Haupt-UI holen
        gridContainer = root.Q<VisualElement>("Grid-Container");

        // Erstell-Popup Komponenten holen
        popupOverlay = root.Q<VisualElement>("Popup-Overlay");
        popupCancelButton = root.Q<Button>("Btn-Cancel"); 
        popupBackButton = root.Q<Button>("Btn-Back");     
        popupSubmitButton = root.Q<Button>("Btn-Submit"); 
        categoryDropdown = root.Q<DropdownField>("dropKategorie"); 
        docNameInput = root.Q<TextField>("Doc-Name-Input"); 

        // Kategorie-Listen-Popup Komponenten holen
        detailPopupOverlay = root.Q<VisualElement>("Detail-Popup-Overlay");
        detailListContainer = root.Q<VisualElement>("Detail-List-Container");
        detailCloseButton = root.Q<Button>("Btn-Detail-Close");
        detailPopupTitle = root.Q<Label>("Detail-Popup-Title");
        listCreateNewButton = root.Q<Button>("Btn-List-Create-New"); 

        // Typ-Auswahl-Buttons holen
        btnTypeStandard = root.Q<Button>("Btn-Type-Standard");
        btnTypeDiagramm = root.Q<Button>("Btn-Type-Diagramm");
        btnTypeChecklist = root.Q<Button>("Btn-Type-Checklist");

        // Dropdown initialisieren
        if (categoryDropdown != null)
        {
            categoryDropdown.choices = dropdownKategorien;
            if (dropdownKategorien.Count > 0) categoryDropdown.value = dropdownKategorien[0]; 
        }

        // Popup-Events verdrahten
        if (popupCancelButton != null) popupCancelButton.clicked += ClosePopup;
        if (popupBackButton != null) popupBackButton.clicked += ClosePopup;
        if (detailCloseButton != null) detailCloseButton.clicked += CloseDetailPopup;

        if (listCreateNewButton != null)
        {
            listCreateNewButton.clicked += () => {
                CloseDetailPopup();
                OpenPopup(activeCategoryForList); 
            };
        }

        // Typen-Auswahl Logik
        if (btnTypeStandard != null) btnTypeStandard.clicked += () => SelectType("Standard");
        if (btnTypeDiagramm != null) btnTypeDiagramm.clicked += () => SelectType("Diagramm");
        if (btnTypeChecklist != null) btnTypeChecklist.clicked += () => SelectType("Checklist");

        if (popupSubmitButton != null) popupSubmitButton.clicked += CreateNewDocumentEntry;

        LoadDataLocally();
        SpawnAllCardsAtStart();
    }

    private void OpenPopup(string preselectedCategory = "") 
    { 
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.Flex; 
        if (docNameInput != null) docNameInput.value = ""; 
        
        if (!string.IsNullOrEmpty(preselectedCategory) && categoryDropdown != null)
        {
            categoryDropdown.value = preselectedCategory;
        }
    }

    private void ClosePopup() 
    { 
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.None; 
    }

    private void SelectType(string typeName)
    {
        selectedType = typeName;
    }

    private void CreateNewDocumentEntry()
    {
        string selectedCategory = categoryDropdown != null ? categoryDropdown.value : "";
        string docText = (docNameInput != null && !string.IsNullOrEmpty(docNameInput.value)) ? docNameInput.value : "Unbenannt";

        if (string.IsNullOrEmpty(selectedCategory)) return;

        // GELÖSCHT: Die "while (kategorieDocs.Count >= 2)"-Schleife ist komplett raus!
        // Dokumente werden ab jetzt unbegrenzt in der Liste gespeichert.

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
            if (imgShowList != null)
            {
                imgShowList.RegisterCallback<ClickEvent>(evt => OpenDetailPopup(kategorieName));
            }

            Button plusBtn = cardInstance.Q<Button>("btnPlus");
            if (plusBtn != null)
            {
                plusBtn.clicked += () => OpenPopup(kategorieName);
            }

            VisualElement feldOben = cardInstance.Q<VisualElement>("Datenfeld-Oben");
            VisualElement feldUnten = cardInstance.Q<VisualElement>("Datenfeld-Unten");

            if (feldOben != null) feldOben.Clear();
            if (feldUnten != null) feldUnten.Clear();

            List<DocumentData> kategorieDocs = speicherDaten.savedDocs.FindAll(d => d.category == kategorieName);

            // GEÄNDERT: Das Limit von 2 gilt jetzt NUR NOCH hier beim Zeichnen der Kachel-Vorschau!
            for (int i = 0; i < kategorieDocs.Count; i++)
            {
                if (i >= 2) break; // Bricht das Zeichnen auf der Kachel ab, löscht aber nichts aus dem Speicher!

                VisualElement target = (i % 2 == 0) ? feldOben : feldUnten;
                if (target != null)
                {
                    VisualElement docEntry = new VisualElement();
                    string icon = (kategorieDocs[i].type == "Diagramm") ? "📊" : ((kategorieDocs[i].type == "Checklist") ? "☑️" : "📄");
                    
                    Label docLabel = new Label($"{icon} {kategorieDocs[i].title}");
                    docLabel.style.fontSize = 12;
                    docEntry.Add(docLabel);
                    
                    target.Add(docEntry);
                }
            }
            gridContainer.Add(cardInstance);
        }
    }

    private void OpenDetailPopup(string kategorie)
    {
        activeCategoryForList = kategorie; 
        if (detailPopupOverlay != null) detailPopupOverlay.style.display = DisplayStyle.Flex;
        if (detailPopupTitle != null) detailPopupTitle.text = $"Dokumente: {kategorie}";

        RefreshDetailList();
    }

    private void RefreshDetailList()
    {
        if (detailListContainer == null) return;
        detailListContainer.Clear();

        List<DocumentData> kategorieDocs = speicherDaten.savedDocs.FindAll(d => d.category == activeCategoryForList);

        if (kategorieDocs.Count == 0)
        {
            Label emptyLabel = new Label("Noch keine Dokumente in dieser Kategorie.");
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyLabel.style.marginTop = 20;
            emptyLabel.style.color = Color.white; 
            
            if (poppinsBoldFont != null) emptyLabel.style.unityFont = poppinsBoldFont;
            
            detailListContainer.Add(emptyLabel);
            return;
        }

        // Hier wird nun absolut JEDES Dokument gelistet, egal wie viele es sind!
        foreach (var doc in kategorieDocs)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("list-row-item"); 

            string icon = (doc.type == "Diagramm") ? "📊" : ((doc.type == "Checklist") ? "☑️" : "📄");
            
            Label nameLabel = new Label($"{icon} {doc.title}"); 
            nameLabel.style.fontSize = 13;
            nameLabel.style.color = Color.white; 
            
            if (poppinsBoldFont != null)
            {
                nameLabel.style.unityFont = poppinsBoldFont;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            Button deleteSingleBtn = new Button();
            deleteSingleBtn.text = " ➖ "; 
            deleteSingleBtn.AddToClassList("btn-minus-delete"); 
            deleteSingleBtn.clicked += () => DeleteSingleDocument(doc.id);

            row.Add(nameLabel);
            row.Add(deleteSingleBtn);
            detailListContainer.Add(row);
        }
    }

    private void DeleteSingleDocument(string docId)
    {
        speicherDaten.savedDocs.RemoveAll(d => d.id == docId);
        SaveDataLocally();
        RefreshDetailList();    
        SpawnAllCardsAtStart(); 
    }

    private void CloseDetailPopup()
    {
        if (detailPopupOverlay != null) detailPopupOverlay.style.display = DisplayStyle.None;
    }

    private void SaveDataLocally()
    {
        string json = JsonUtility.ToJson(speicherDaten, true);
        File.WriteAllText(saveFilePath, json);
    }

    private void LoadDataLocally()
    {
        if (File.Exists(saveFilePath))
            speicherDaten = JsonUtility.FromJson<DocumentSaveData>(File.ReadAllText(saveFilePath));
    }
}