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
    private Button popupBackButton; // NEU: Für den Pfeil oben rechts/links im Pop-up
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

    // Lokale Datenstruktur (Sichert Funktionalität ohne fehlerhafte DB-Referenzen)
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

        // UI-Elemente zuweisen
        createButton = root.Q<Button>("Create-Button");
        deleteButton = root.Q<Button>("Delete-Button"); // Oben rechts "Dokumente Löschen"
        gridContainer = root.Q<VisualElement>("Grid-Container");

        // Pop-up Elemente zuweisen
        popupOverlay = root.Q<VisualElement>("Popup-Overlay");
        popupCancelButton = root.Q<Button>("Btn-Cancel"); // "Abbrechen"
        popupBackButton = root.Q<Button>("Btn-Back");     // "Zurück"-Pfeil oben rechts im Pop-up
        popupSubmitButton = root.Q<Button>("Btn-Submit"); // "erstellen"
        
        categoryDropdown = root.Q<DropdownField>("dropKategorie"); 
        docNameInput = root.Q<TextField>("Doc-Name-Input"); 

        btnTypeStandard = root.Q<Button>("Btn-Type-Standard");
        btnTypeDiagramm = root.Q<Button>("Btn-Type-Diagramm");
        btnTypeChecklist = root.Q<Button>("Btn-Type-Checklist");

        // Dropdown befüllen
        if (categoryDropdown != null)
        {
            categoryDropdown.choices = dropdownKategorien;
            if (dropdownKategorien.Count > 0) categoryDropdown.value = dropdownKategorien[0]; 
        }

        // Klick-Events verknüpfen
        if (createButton != null) createButton.clicked += OpenPopup;
        if (deleteButton != null) deleteButton.clicked += DeleteAllDocuments;
        
        // BEIDE Buttons schließen jetzt das Pop-up zuverlässig!
        if (popupCancelButton != null) popupCancelButton.clicked += ClosePopup;
        if (popupBackButton != null) popupBackButton.clicked += ClosePopup;

        // Typen auswählen (Ändert NUR den Typen-String, überschreibt NICHT mehr dein Textfeld!)
        if (btnTypeStandard != null) btnTypeStandard.clicked += () => SelectType("Standard");
        if (btnTypeDiagramm != null) btnTypeDiagramm.clicked += () => SelectType("Diagramm");
        if (btnTypeChecklist != null) btnTypeChecklist.clicked += () => SelectType("Checklist");

        if (popupSubmitButton != null) popupSubmitButton.clicked += CreateNewDocumentEntry;

        // Daten laden und UI aufbauen
        LoadDataLocally();
        SpawnAllCardsAtStart();
    }

    private void OpenPopup() 
    { 
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.Flex; 
        if (docNameInput != null) docNameInput.value = ""; // Textfeld leeren, damit du frei tippen kannst!
    }

    private void ClosePopup() 
    { 
        if (popupOverlay != null) popupOverlay.style.display = DisplayStyle.None; 
    }

    private void SelectType(string typeName)
    {
        selectedType = typeName;
        Debug.Log($"Typ ausgewählt: {selectedType}");
        // Hier kannst du optional Code einfügen, um den ausgewählten Button visuell zu markieren
    }

    private void CreateNewDocumentEntry()
    {
        string selectedCategory = categoryDropdown != null ? categoryDropdown.value : "";
        
        // Nutzt deinen eingetippten Namen. Wenn leer, dann "Unbenannt"
        string docText = (docNameInput != null && !string.IsNullOrEmpty(docNameInput.value)) ? docNameInput.value : "Unbenannt";

        if (string.IsNullOrEmpty(selectedCategory)) return;

        // 1. Liste nach aktueller Kategorie filtern
        List<DocumentData> kategorieDocs = speicherDaten.savedDocs.FindAll(d => d.category == selectedCategory);

        // 2. Ältestes Dokument kicken, wenn das Kartenlimit (max. 2 Einträge) erreicht ist
        while (kategorieDocs.Count >= 2)
        {
            DocumentData altesDoc = kategorieDocs[0];
            speicherDaten.savedDocs.Remove(altesDoc);
            kategorieDocs.Remove(altesDoc);
        }

        // 3. Neues Dokument mit freiem Namen hinzufügen
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
                // Maximal 2 Dokumente anzeigen
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

    // "Dokumente Löschen"-Button Logik (Leert die Anzeige und die Speicherdatei)
    private void DeleteAllDocuments()
    {
        speicherDaten.savedDocs.Clear();
        SaveDataLocally();
        SpawnAllCardsAtStart();
        Debug.Log("Alle lokalen Dokumente wurden gelöscht.");
    }
}