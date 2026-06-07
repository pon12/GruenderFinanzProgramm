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
    [Tooltip("Ziehe hier dein Poppins-Bold Font Asset (oder Font Definition Asset) aus dem Projektordner rein!")]
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

    // Gesamtlisten-Pop-up Elemente
    private VisualElement detailPopupOverlay;
    private VisualElement detailListContainer;
    private Button detailCloseButton;

    // Typ-Buttons im Erstell-Popup
    private Button btnTypeStandard;
    private Button btnTypeDiagramm;
    private Button btnTypeChecklist;

    private string selectedType = "Standard";
    private List<string> dropdownKategorien = new List<string> { 
        "Gründung", "Finanzen", "Marketing", "Steuern", "Personal", "Recht" 
    };

    // Lokale Datenstruktur
    [System.Serializable]
    public class DocumentData
    {
        public string id; // Einzigartige ID für den Minus-Button
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

        // Gesamtlisten-Popup Komponenten holen
        detailPopupOverlay = root.Q<VisualElement>("Detail-Popup-Overlay");
        detailListContainer = root.Q<VisualElement>("Detail-List-Container");
        detailCloseButton = root.Q<Button>("Btn-Detail-Close");

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

        // Standard-Popup-Events verdrahten
        if (popupCancelButton != null) popupCancelButton.clicked += ClosePopup;
        if (popupBackButton != null) popupBackButton.clicked += ClosePopup;
        if (detailCloseButton != null) detailCloseButton.clicked += CloseDetailPopup;

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

        List<DocumentData> kategorieDocs = speicherDaten.savedDocs.FindAll(d => d.category == selectedCategory);

        while (kategorieDocs.Count >= 2)
        {
            DocumentData altesDoc = kategorieDocs[0];
            speicherDaten.savedDocs.Remove(altesDoc);
            kategorieDocs.Remove(altesDoc);
        }

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

            // Holt das Bild-Icon DIREKT aus der frisch instanziierten Kachel
            VisualElement imgShowList = cardInstance.Q("Btn-Show-List");
            if (imgShowList != null)
            {
                imgShowList.RegisterCallback<ClickEvent>(evt => OpenDetailPopup());
            }

            // Holt den Plus-Button aus der Kachel
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

            for (int i = 0; i < kategorieDocs.Count; i++)
            {
                if (i >= 2) break;

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

    private void OpenDetailPopup()
    {
        if (detailPopupOverlay != null) detailPopupOverlay.style.display = DisplayStyle.Flex;
        RefreshDetailList();
    }

    private void RefreshDetailList()
    {
        if (detailListContainer == null) return;
        detailListContainer.Clear();

        if (speicherDaten.savedDocs.Count == 0)
        {
            Label emptyLabel = new Label("Noch keine Dokumente erstellt.");
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyLabel.style.marginTop = 20;
            emptyLabel.style.color = Color.white; // Weiße Schrift
            
            // Schriftart dynamisch zuweisen falls im Inspector hinterlegt
            if (poppinsBoldFont != null) emptyLabel.style.unityFont = poppinsBoldFont;
            
            detailListContainer.Add(emptyLabel);
            return;
        }

        foreach (var doc in speicherDaten.savedDocs)
        {
            // 1. Zeilen-Container erstellen
            VisualElement row = new VisualElement();
            row.AddToClassList("list-row-item"); 

            string icon = (doc.type == "Diagramm") ? "📊" : ((doc.type == "Checklist") ? "☑️" : "📄");
            
            // 2. Label erstellen und per Code stylen (Weiß + Poppins)
            Label nameLabel = new Label($"[{doc.category}] {icon} {doc.title}");
            nameLabel.style.fontSize = 13;
            nameLabel.style.color = Color.white; // Macht den Text sicher weiß!
            
            // Wenn du im Inspector deine Poppins-Schriftart reingezogen hast, nutzt er sie jetzt:
            if (poppinsBoldFont != null)
            {
                nameLabel.style.unityFont = poppinsBoldFont;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }

            // 3. Minus-Button erstellen
            Button deleteSingleBtn = new Button();
            deleteSingleBtn.text = " ➖ "; 
            deleteSingleBtn.AddToClassList("btn-minus-delete"); 
            deleteSingleBtn.clicked += () => DeleteSingleDocument(doc.id);

            // Alles zusammenbauen
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