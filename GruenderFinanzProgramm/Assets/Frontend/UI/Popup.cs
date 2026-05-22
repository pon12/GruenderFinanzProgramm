using UnityEngine;
using UnityEngine.UIElements;

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
    }

    private void OeffnePopup(string titel, string beschreibung,
                              string detail, string preismodell,
                              string betrag, string anzahl)
    {
        // Alten Inhalt leeren und neu laden
        popupRoot.Clear();

        var popup = popupAsset.Instantiate();
        var backdrop = popup.Q("popup-backdrop");

        // Felder befüllen
        popup.Q<Label>("popup-titel").text                    = titel;
        popup.Q<TextField>("feld-beschreibung").value         = beschreibung;
        popup.Q<TextField>("feld-detailbeschreibung").value   = detail;
        popup.Q<TextField>("feld-betrag").value               = betrag;
        popup.Q<TextField>("feld-anzahl").value               = anzahl;

        var dropdown = popup.Q<DropdownField>("feld-preismodell");
        dropdown.value = preismodell;

        // Fertig-Button
        popup.Q<Button>("btn-fertig").clicked += () => SchliessPopup();

        // Backdrop-Klick schließt Popup
        backdrop.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == backdrop) SchliessPopup();
        });

        popupRoot.Add(popup);

        // Anzeigen
        popupRoot.style.display  = DisplayStyle.Flex;
        backdrop.style.display   = DisplayStyle.Flex;
    }

    private void SchliessPopup()
    {
        popupRoot.style.display = DisplayStyle.None;
        popupRoot.Clear();
    }
}