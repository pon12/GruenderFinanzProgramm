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
    private VisualElement gridContainer;
    
    // Pop-up Elemente
    private VisualElement popupOverlay;
    private Button popupCancelButton;
    private Button popupSubmitButton;
    private TextField documentNameInput;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Dashboard Elemente greifen
        createButton = root.Q<Button>("Create-Button");
        gridContainer = root.Q<VisualElement>("Grid-Container");

        // Pop-up Elemente greifen
        popupOverlay = root.Q<VisualElement>("Popup-Overlay");
        popupCancelButton = root.Q<Button>("Btn-Cancel"); // Heißt so dein Abbrechen-Button?
        popupSubmitButton = root.Q<Button>("Btn-Submit"); // Heißt so dein Erstellen-Button?
        documentNameInput = root.Q<TextField>("Doc-Name-Input"); // Falls du ein Eingabefeld hast

        // Events zuweisen
        if (createButton != null) createButton.clicked += OpenPopup;
        if (popupCancelButton != null) popupCancelButton.clicked += ClosePopup;
        if (popupSubmitButton != null) popupSubmitButton.clicked += CreateNewDocument;

        SpawnTestCards();
    }

    // Pop-up anzeigen
    private void OpenPopup()
    {
        if (popupOverlay != null)
        {
            popupOverlay.style.display = DisplayStyle.Flex;
        }
    }

    // Pop-up verstecken
    private void ClosePopup()
    {
        if (popupOverlay != null)
        {
            popupOverlay.style.display = DisplayStyle.None;
            if (documentNameInput != null) documentNameInput.value = ""; // Textfeld leeren
        }
    }

    // Logik, wenn man im Pop-up auf "Erstellen" drückt
    private void CreateNewDocument()
    {
        string newDocName = documentNameInput != null ? documentNameInput.value : "Neues Dokument";
        
        if (string.IsNullOrEmpty(newDocName)) return;

        Debug.Log($"Erstelle neues Dokument mit Name: {newDocName}");

        // Hier kannst du jetzt sogar dynamisch eine NEUE Karte mit deinem Wunschnamen spawnen!
        if (gridContainer != null && categoryCardTemplate != null)
        {
            VisualElement cardInstance = categoryCardTemplate.Instantiate();
            Label titleLabel = cardInstance.Q<Label>("CardTitle");
            if (titleLabel != null) titleLabel.text = newDocName;
            
            gridContainer.Add(cardInstance);
        }

        // Pop-up danach wieder schließen
        ClosePopup();
    }

    void OnDisable()
    {
        if (createButton != null) createButton.clicked -= OpenPopup;
        if (popupCancelButton != null) popupCancelButton.clicked -= ClosePopup;
        if (popupSubmitButton != null) popupSubmitButton.clicked -= CreateNewDocument;
    }

    private void SpawnTestCards()
    {
        if (gridContainer == null || categoryCardTemplate == null) return;
        string[] testKategorien = { "Gründung", "Finanzen", "Marketing" };
        foreach (string name in testKategorien)
        {
            VisualElement cardInstance = categoryCardTemplate.Instantiate();
            Label titleLabel = cardInstance.Q<Label>("CardTitle");
            if (titleLabel != null) titleLabel.text = name;
            gridContainer.Add(cardInstance);
        }
    }
}