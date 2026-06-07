using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class DienstleistungenScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset popupAsset; // DienstleistungPopup.uxml zuweisen!

    private VisualElement root;
    private VisualElement popupRoot;
    private VisualElement backdrop;


    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;
        popupRoot = root.Q("popup-root");

        // + Button
        root.Q<Button>("btn-neue-dienstleistung").clicked += () => OeffnePopup("Neue Dienstleistung", "", "", "Festpreis", "", "");

        // Bearbeiten-Buttons (1-5)
        for (int i = 1; i <= 5; i++)
        {
            int index = i; // Closure-fix
            var editBtn = root.Q<Button>($"btn-edit-{index}");
            if (editBtn == null) continue;

            editBtn.clicked += () =>
            {
                // Bestehende Daten aus der Karte auslesen
                string titel  = root.Q<Label>($"title-{index}").text;
                string desc   = root.Q<Label>($"desc-{index}").text;
                string preis  = root.Q<Label>($"price-{index}").text;

                OeffnePopup(titel, desc, "", "Festpreis", preis, "");
            };
        }
        // Löschen-Buttons (1-5) (Neu)
        for (int i = 1; i <= 5; i++)
        {
            int index = i; // Closure-fix
            var deleteBtn = root.Q<Button>($"btn-delete-{index}");
            if (deleteBtn == null) continue;

            deleteBtn.clicked += () =>
            {
                Debug.Log($"Lösche Dienstleistung {index}");

                // Dienstleistung löschen
                string titel = root.Q<Label>($"title-{index}").text;
                string beschreibung = root.Q<Label>($"desc-{index}").text;
                string detail = root.Q<Label>($"detail-{index}").text;
                string betrag = root.Q<Label>($"price-{index}").text;
                string anzahl = root.Q<Label>($"anzahl-{index}").text;
                string preismodell = root.Q<Label>($"preismodell-{index}").text;
                //Daten aus datenbank löschen
                UserDatabaseAccess.getCurrentUserDatabase().DeleteDienstleistung(titel, beschreibung, detail, betrag, anzahl, preismodell);
                // UI aktualisieren 
                root.Q<Label>($"title-{index}").text = "";
                root.Q<Label>($"desc-{index}").text = "";
                root.Q<Label>($"price-{index}").text = "";
                root.Q<Label>($"anzahl-{index}").text = "";
                root.Q<Label>($"beschre-{index}").text = "";
            };
        }
    }

    private void OeffnePopup(string titel, string beschreibung,
                          string detail, string preismodell,
                          string betrag, string anzahl)
{
    popupRoot.Clear();

    var popup = popupAsset.Instantiate();

    // Felder befüllen
    popup.Q<Label>("popup-titel").text                  = titel;
    popup.Q<TextField>("feld-beschreibung").value       = beschreibung;
    popup.Q<TextField>("feld-detailbeschreibung").value = detail;
    popup.Q<TextField>("feld-betrag").value             = betrag;
    popup.Q<TextField>("feld-anzahl").value             = anzahl;
    popup.Q<DropdownField>("feld-preismodell").value    = preismodell;

    // Fertig-Button
    popup.Q<Button>("btn-fertig").clicked += SchliessPopup;
    // Speichern (Neu)
    popup.Q<Button>("btn-fertig").clicked += () =>   {
        SchliessPopup();
        SavetoDienstleistung(
            popup.Q<Label>("popup-titel").text,
            popup.Q<TextField>("feld-beschreibung").value,
            popup.Q<TextField>("feld-detailbeschreibung").value,
            popup.Q<TextField>("feld-betrag").value,
            popup.Q<TextField>("feld-anzahl").value,
            popup.Q<DropdownField>("feld-preismodell").value
        );
    };

    // Klick auf Backdrop (außerhalb der Box) schließt Popup
    popupRoot.RegisterCallback<ClickEvent>(evt =>
    {
        if (evt.target == popupRoot) SchliessPopup();
    });

    popupRoot.Add(popup);
    popupRoot.style.display = DisplayStyle.Flex;
}
// Neu
private void Start()
{
    UserDatabaseAccess.getCurrentUserDatabase().setupDienstleistungenTable();    
}
// Neu speichert die Dienstleistung in der Datenbank, wenn auf "Fertig" geklickt wird
private void SavetoDienstleistung(string titel, string beschreibung, string detail, string betrag, string anzahl, string preismodell)
{
    
    Debug.Log($"Speichere Dienstleistung: {titel}, {beschreibung}, {detail}, {betrag}, {anzahl}, {preismodell}");
    UserDatabaseAccess.getCurrentUserDatabase().createDienstleistung(titel, beschreibung, detail,  betrag, anzahl, preismodell);

    

}


private void SchliessPopup()
{
    popupRoot.style.display = DisplayStyle.None;
    popupRoot.Clear();
}
}