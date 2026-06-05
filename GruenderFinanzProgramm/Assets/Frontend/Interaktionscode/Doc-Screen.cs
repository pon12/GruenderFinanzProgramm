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

    // UI Elemente
    private VisualElement root;
    private Button createButton;
    private Button deleteButton;
    private VisualElement gridContainer;

    
    // Pop-up Elemente
    private VisualElement popupOverlay;
    private Button popupCancelButton;
    private Button popupSubmitButton; 
    
    // Eingabefelder
    private DropdownField categoryDropdown; 
    private TextField docNameInput; 

    // Typ-Buttons
    private Button btnTypeStandard;
    private Button btnTypeDiagramm;
    private Button btnTypeChecklist;

    private string selectedType = "Standard";
    private List<string> dropdownKategorien = new List<string> { 
        "Gründung", "Finanzen", "Marketing", "Steuern", "Personal", "Recht" 
    };

    // Datenstruktur
    [System.Serializable]
    public class DocumentData
    {
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

        createButton = root.Q<Button>("Create-Button");
        deleteButton = root.Q<Button>("Delete-Button");
        gridContainer = root.Q<VisualElement>("Grid-Container");

        popupOverlay = root.Q<VisualElement>("Popup-Overlay");
        popupCancelButton = root.Q<Button>("Btn-Cancel"); 
        popupSubmitButton = root.Q<Button>("Btn-Submit"); 
        
        categoryDropdown = root.Q<DropdownField>("dropKategorie"); 
        docNameInput = root.Q<TextField>("Doc-Name-Input"); 

        btnTypeStandard = root.Q<Button>("Btn-Type-Standard");
        btnTypeDiagramm = root.Q<Button>("Btn-Type-Diagramm");
        btnTypeChecklist = root.Q<Button>("Btn-Type-Checklist");

        if (categoryDropdown != null)
        {
            categoryDropdown.choices = dropdownKategorien;
            if (dropdownKategorien.Count > 0) categoryDropdown.value = dropdownKategorien[0]; 
        }

        if (createButton != null) createButton.clicked += OpenPopup;
        if (deleteButton != null) deleteButton.clicked += deleteAllDocuments;
        if (popupCancelButton != null) popupCancelButton.clicked += ClosePopup;

        if (btnTypeStandard != null) btnTypeStandard.clicked += () => ApplyTemplate("Standard");
        if (btnTypeDiagramm != null) btnTypeDiagramm.clicked += () => ApplyTemplate("Diagramm");
        if (btnTypeChecklist != null) btnTypeChecklist.clicked += () => ApplyTemplate("Checklist");

        if (popupSubmitButton != null) popupSubmitButton.clicked += CreateNewDocumentEntry;

        LoadDataLocally();
        SpawnAllCardsAtStart();
        getAllData();
    }

    private void OpenPopup() { if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.Flex; }
    private void ClosePopup() { if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.None; }

    private void ApplyTemplate(string typeName)
    {
        selectedType = typeName;
        string templateName = (typeName == "Standard") ? "Businessplan" : (typeName == "Diagramm") ? "Diagramm" : "Checkliste";
        if (docNameInput != null) docNameInput.value = templateName;
    }

    private void CreateNewDocumentEntry()
    {
        string selectedCategory = categoryDropdown != null ? categoryDropdown.value : "";
        string docText = docNameInput != null ? docNameInput.value : "Unbenannt";

        if (string.IsNullOrEmpty(selectedCategory)) return;

        // 1. Liste filtern
        List<DocumentData> kategorieDocs = speicherDaten.savedDocs.FindAll(d => d.category == selectedCategory);

        // 2. Älteste löschen, wenn wir schon 2 haben (immer nur max 2 behalten)
        while (kategorieDocs.Count >= 2)
        {
            DocumentData altesDoc = kategorieDocs[0];
            speicherDaten.savedDocs.Remove(altesDoc);
            kategorieDocs.Remove(altesDoc);
        }

        // 3. Neues Dokument hinzufügen
        DocumentData newDoc = new DocumentData { category = selectedCategory, title = docText, type = selectedType };
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

            VisualElement feldOben = cardInstance.Q<VisualElement>("Datenfeld-Oben");
            VisualElement feldUnten = cardInstance.Q<VisualElement>("Datenfeld-Unten");

            if (feldOben != null) feldOben.Clear();
            if (feldUnten != null) feldUnten.Clear();

            List<DocumentData> kategorieDocs = speicherDaten.savedDocs.FindAll(d => d.category == kategorieName);

            for (int i = 0; i < kategorieDocs.Count; i++)
            {
                // Gerade = Oben, Ungerade = Unten
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

    private void SaveDataLocally()
    {
        string json = JsonUtility.ToJson(speicherDaten, true);
        File.WriteAllText(saveFilePath, json);
    }

    //Lösche alle Dokumente.
    private void deleteAllDocuments()
    {
        speicherDaten.savedDocs.Clear();
        SaveDataLocally();
        SpawnAllCardsAtStart();
    }

    //Lösche ein bestimmtes Dokument anhand von Kategorie und Titel.
    private void clearOneDataEntry(string category, string title)
    {
        DocumentData entryToRemove = speicherDaten.savedDocs.Find(d => d.category == category && d.title == title);
        if (entryToRemove != null)
        {
            speicherDaten.savedDocs.Remove(entryToRemove);
            SaveDataLocally();
            SpawnAllCardsAtStart();
        }
    }


    // Gibt alle Dokumente
    private List<DocumentData> getAllData()
    {
    //Test um zu sehen ob funktion gecall wird.
    Debug.Log("GetAllData function called");
    List<DocumentData> allData = speicherDaten.savedDocs;
    //Debug nur zum test kannst du wens geht rausnehmen
    foreach (var doc in allData)
    {
        Debug.Log($"Category: {doc.category}, Title: {doc.title}, Type: {doc.type}");
    }
    return allData;
    }



    private void LoadDataLocally()
    {
        if (File.Exists(saveFilePath))
            speicherDaten = JsonUtility.FromJson<DocumentSaveData>(File.ReadAllText(saveFilePath));
    }
}