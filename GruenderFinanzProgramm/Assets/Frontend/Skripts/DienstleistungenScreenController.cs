using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class DienstleistungenScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset popupAsset;

    private List<Service> daten = new List<Service>();
    private VisualElement rootElement;

    private ScrollView tabelleBody;
    private VisualElement popupOverlay;
    private int editIndex = -1;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        rootElement  = root;
        tabelleBody  = root.Query<ScrollView>("tabelle-body").First();
        popupOverlay = root.Query<VisualElement>("popup-overlay").First();

        var btnNeu = root.Q<Button>("btn-neu");
        if (btnNeu == null) { Debug.LogError("btn-neu nicht gefunden!"); return; }

        btnNeu.clicked += () => OeffnePopup(-1);
        popupOverlay.RegisterCallback<ClickEvent>(evt => { if (evt.target == popupOverlay) SchliessPopup(); });

        LadeDienstleistungenAusDatenbank();
        TabelleAktualisieren();
        RegistriereHelpTooltips(root);
    }

    private void TabelleAktualisieren()
    {
        tabelleBody.Clear();

        for (int i = 0; i < daten.Count; i++)
        {
            int index = i;
            var d = daten[i];

            var zeile = new VisualElement();
            zeile.AddToClassList("tabelle-zeile");

            zeile.Add(Zelle(d.name,                        "col-titel"));
            zeile.Add(Zelle(d.description,                 "col-beschreibung"));
            zeile.Add(Zelle(d.priceModel,                  "col-preismodell"));
            zeile.Add(Zelle(d.price.ToString("F2") + " \u20ac", "col-betrag"));

            var aktionen = new VisualElement();
            aktionen.AddToClassList("col-aktionen");

            var btnEdit = new Button(() => OeffnePopup(index));
            btnEdit.text = "Bearbeiten";
            btnEdit.AddToClassList("zeile-btn");

            var btnDel = new Button(() =>
            {
                DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
                if (db == null)
                {
                    Debug.LogError("[Dienstleistungen] Keine aktive UserDB gefunden.");
                    return;
                }
                db.deleteService(daten[index].id);
                LadeDienstleistungenAusDatenbank();
                TabelleAktualisieren();
            });

            btnDel.text = "L\u00f6schen";
            btnDel.AddToClassList("zeile-btn");
            btnDel.AddToClassList("zeile-btn-loeschen");

            aktionen.Add(btnEdit);
            aktionen.Add(btnDel);
            zeile.Add(aktionen);

            tabelleBody.Add(zeile);
        }
    }

    private VisualElement Zelle(string text, string spaltenKlasse)
    {
        var label = new Label(text);
        label.AddToClassList("tabelle-zelle");
        label.AddToClassList(spaltenKlasse);
        return label;
    }

    private void OeffnePopup(int index)
    {
        editIndex = index;
        popupOverlay.Clear();

        var popup = popupAsset.Instantiate();

        var d = index >= 0 ? daten[index] : new Service { name = "", description = "", priceModel = "Festpreis", price = 0.0 };

        popup.Q<TextField>("feld-titel").value           = d.name;
        popup.Q<TextField>("feld-beschreibung").value    = d.description;
        popup.Q<DropdownField>("feld-preismodell").value = d.priceModel;
        popup.Q<TextField>("feld-betrag").value          = d.price.ToString("F2");

        popup.Q<Button>("btn-fertig").clicked += () =>
        {
            double.TryParse(
                popup.Q<TextField>("feld-betrag").value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double betrag
            );

            var neu = new Service
            {
                name        = popup.Q<TextField>("feld-titel").value,
                description = popup.Q<TextField>("feld-beschreibung").value,
                priceModel  = popup.Q<DropdownField>("feld-preismodell").value,
                price       = betrag
            };

            DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null)
            {
                Debug.LogError("[Dienstleistungen] Keine aktive UserDB gefunden.");
                return;
            }

            if (editIndex >= 0)
            {
                neu.id = daten[editIndex].id;
                db.updateService(neu);
            }
            else
            {
                db.createService(neu);
            }

            LadeDienstleistungenAusDatenbank();
            SchliessPopup();
            TabelleAktualisieren();
        };

        popupOverlay.Add(popup);
        popupOverlay.style.display = DisplayStyle.Flex;
        RegistrierePopupTooltips(popup, rootElement);
    }

    private void SchliessPopup()
    {
        popupOverlay.style.display = DisplayStyle.None;
        popupOverlay.Clear();
        editIndex = -1;
    }

    private void LadeDienstleistungenAusDatenbank()
    {
        try
        {
            DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null)
            {
                Debug.LogError("[Dienstleistungen] Keine aktive UserDB gefunden.");
                daten = new List<Service>();
                return;
            }
            daten = db.getAllServices() ?? new List<Service>();
            Debug.Log("[Dienstleistungen] Geladen: " + daten.Count);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Dienstleistungen] Laden fehlgeschlagen: " + e);
            daten = new List<Service>();
        }
    }

    // =========================================================
    // HELP TOOLTIPS
    // =========================================================

    private void RegistriereHelpTooltips(VisualElement root)
    {
        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Hier verwaltest du alle deine Dienstleistungen und Produkte. " +
            "Angelegte Eintr\u00e4ge stehen beim Erstellen von Angeboten und Rechnungen zur Auswahl. " +
            "Du kannst Eintr\u00e4ge jederzeit bearbeiten oder l\u00f6schen.");

        HelpTooltip.Registriere(root, "btn-help-neu",
            "Legt eine neue Dienstleistung oder ein neues Produkt an. " +
            "F\u00fclle Name, Beschreibung, Preismodell und Betrag aus. " +
            "Der Eintrag ist danach sofort in Angeboten und Rechnungen verf\u00fcgbar.");

        HelpTooltip.Registriere(root, "btn-help-name",
            "Der Name der Dienstleistung oder des Produkts. " +
            "Er erscheint als Artikelbezeichnung in Angeboten und Rechnungen.");

        HelpTooltip.Registriere(root, "btn-help-beschreibung",
            "Eine kurze Beschreibung der Leistung. " +
            "Sie wird in der Positionsbeschreibung auf Angeboten und Rechnungen angezeigt.");

        HelpTooltip.Registriere(root, "btn-help-preismodell",
            "Festpreis: fixer Gesamtpreis f\u00fcr die Leistung. " +
            "Stundensatz: Preis pro geleistete Arbeitsstunde. " +
            "Pauschal: Pauschalpreis unabh\u00e4ngig vom Aufwand.");

        HelpTooltip.Registriere(root, "btn-help-betrag",
            "Der Preis der Dienstleistung in Euro. " +
            "Bei Stundensatz gilt der Wert pro Stunde, " +
            "bei Festpreis und Pauschal als Gesamtbetrag.");
    }

    private void RegistrierePopupTooltips(VisualElement popup, VisualElement root)
    {
        var popupHelp = popup.Q<VisualElement>("btn-help-popup");
        if (popupHelp != null)
            HelpTooltip.RegistriereInKarte(root, popupHelp,
                "F\u00fclle hier die Daten der Dienstleistung aus und best\u00e4tige mit Fertig. " +
                "Der Eintrag wird gespeichert und ist danach in Angeboten und Rechnungen verf\u00fcgbar.");

        var preisHelp = popup.Q<VisualElement>("btn-help-preismodell-popup");
        if (preisHelp != null)
            HelpTooltip.RegistriereInKarte(root, preisHelp,
                "Festpreis: fixer Gesamtpreis. " +
                "Stundensatz: Preis pro Stunde. " +
                "Pauschal: Pauschalpreis unabh\u00e4ngig vom Aufwand.");
    }
}
