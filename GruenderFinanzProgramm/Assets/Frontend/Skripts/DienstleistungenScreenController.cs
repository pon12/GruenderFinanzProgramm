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

    // Referenz auf den aktuell offenen Popup (für Confirm-Dialog benötigt)
    private VisualElement currentPopup;

    // Dirty-Flag: true sobald der Nutzer etwas in ein Feld getippt / geändert hat
    private bool _isDirty = false;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
        rootElement  = root;
        tabelleBody  = root.Query<ScrollView>("tabelle-body").First();
        popupOverlay = root.Query<VisualElement>("popup-overlay").First();

        var btnNeu = root.Q<Button>("btn-neu");
        if (btnNeu == null) { Debug.LogError("btn-neu nicht gefunden!"); return; }

        btnNeu.clicked += () => OeffnePopup(-1);
        popupOverlay.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == popupOverlay) VersuchSchliessen();
        });

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

            zeile.Add(Zelle(d.name,                            "col-titel"));
            zeile.Add(Zelle(d.description,                     "col-beschreibung"));
            zeile.Add(Zelle(d.priceModel,                      "col-preismodell"));
            zeile.Add(Zelle(d.price.ToString("F2") + " \u20ac","col-betrag"));

            var aktionen = new VisualElement();
            aktionen.AddToClassList("col-aktionen");

            var btnEdit = new Button(() => OeffnePopup(index));
            btnEdit.text = "Bearbeiten";
            btnEdit.AddToClassList("zeile-btn-bearbeiten");

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
            btnDel.AddToClassList("zeile-btn-bearbeiten");
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
        _isDirty  = false; // Dirty-Flag zurücksetzen
        popupOverlay.Clear();

        var popup = popupAsset.Instantiate();
        currentPopup = popup; // Referenz für spätere Nutzung im Confirm-Dialog merken

        var d = index >= 0
            ? daten[index]
            : new Service { name = "", description = "", priceModel = "Festpreis", price = 0.0 };

        // ---- Felder befüllen (VOR dem Registrieren der Callbacks, damit setValue kein dirty auslöst) ----
        var feldTitel        = popup.Q<TextField>("feld-titel");
        var feldBeschreibung = popup.Q<TextField>("feld-beschreibung");
        var feldPreismodell  = popup.Q<DropdownField>("feld-preismodell");
        var feldBetrag       = popup.Q<TextField>("feld-betrag");

        feldTitel.value        = d.name;
        feldBeschreibung.value = d.description;
        feldPreismodell.value  = d.priceModel;
        feldBetrag.value       = d.price.ToString("F2");

        // ---- Dirty-Tracking: erst NACH dem Setzen der Initialwerte registrieren ----
        feldTitel.RegisterValueChangedCallback(_        => _isDirty = true);
        feldBeschreibung.RegisterValueChangedCallback(_ => _isDirty = true);
        feldPreismodell.RegisterValueChangedCallback(_  => _isDirty = true);
        feldBetrag.RegisterValueChangedCallback(_       => _isDirty = true);

        // ---- X-Button (Schließen oben rechts) ----
        var btnClose = popup.Q<Button>("btn-close-popup");
        if (btnClose != null)
            btnClose.clicked += VersuchSchliessen;

        // ---- Abbrechen-Button ----
        var btnAbbrechen = popup.Q<Button>("btn-abbrechen");
        if (btnAbbrechen != null)
            btnAbbrechen.clicked += VersuchSchliessen;

        // ---- Fertig-Button ----
        popup.Q<Button>("btn-fertig").clicked += () => SpeichernUndSchliessen(popup);

        popupOverlay.Add(popup);
        popupOverlay.style.display = DisplayStyle.Flex;
        RegistrierePopupTooltips(popup, rootElement);
    }

    // Entscheidet, ob direkt geschlossen oder der Confirm-Dialog gezeigt wird
    private void VersuchSchliessen()
    {
        if (_isDirty)
            OeffneConfirmDialog();
        else
            SchliessPopup();
    }

    // =========================================================
    // CONFIRM-DIALOG  (alle Layout-Styles inline – kein CSS-Klassen-Bug)
    // =========================================================

    private void OeffneConfirmDialog()
    {
        // --- Overlay: liegt über allem, verdunkelt den Hintergrund ---
        var confirmOverlay = new VisualElement();
        confirmOverlay.name = "confirm-overlay";
        confirmOverlay.style.position        = Position.Absolute;
        confirmOverlay.style.left            = 0;
        confirmOverlay.style.top             = 0;
        confirmOverlay.style.right           = 0;
        confirmOverlay.style.bottom          = 0;
        confirmOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
        confirmOverlay.style.alignItems      = Align.Center;
        confirmOverlay.style.justifyContent  = Justify.Center;

        // --- Card: dunkle Box im Stil von gespeichert.uxml ---
        var box = new VisualElement();
        box.style.width                  = 340;
        box.style.backgroundColor        = new Color(56f/255f, 56f/255f, 56f/255f, 1f);
        box.style.borderTopLeftRadius    = 20;
        box.style.borderTopRightRadius   = 20;
        box.style.borderBottomLeftRadius = 20;
        box.style.borderBottomRightRadius= 20;
        box.style.paddingTop             = 32;
        box.style.paddingBottom          = 28;
        box.style.paddingLeft            = 28;
        box.style.paddingRight           = 28;
        box.style.position               = Position.Relative;

        // --- X-Button oben rechts: schließt nur Confirm, bleibt im Hauptpopup ---
        var btnX = new Button(() => rootElement.Remove(confirmOverlay));
        btnX.text                            = "✕";
        btnX.style.position                  = Position.Absolute;
        btnX.style.top                       = 8;
        btnX.style.right                     = 8;
        btnX.style.width                     = 28;
        btnX.style.height                    = 28;
        btnX.style.fontSize                  = 16;
        btnX.style.color                     = new Color(160f/255f, 160f/255f, 160f/255f);
        btnX.style.backgroundColor           = Color.clear;
        btnX.style.borderTopWidth            = 0;
        btnX.style.borderRightWidth          = 0;
        btnX.style.borderBottomWidth         = 0;
        btnX.style.borderLeftWidth           = 0;
        btnX.style.unityTextAlign            = TextAnchor.MiddleCenter;
        box.Add(btnX);

        // --- Titel ---
        var titel = new Label("Speichern?");
        titel.style.fontSize    = 20;
        titel.style.color       = Color.white;
        titel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titel.style.marginBottom= 8;
        titel.style.marginTop   = 4;
        box.Add(titel);

        // --- Beschreibungstext ---
        var text = new Label("M\u00f6chtest du die eingetragenen Daten speichern?");
        text.style.fontSize    = 14;
        text.style.color       = new Color(200f/255f, 200f/255f, 200f/255f);
        text.style.whiteSpace  = WhiteSpace.Normal;
        box.Add(text);

        // --- Button-Zeile ---
        var btnZeile = new VisualElement();
        btnZeile.style.flexDirection = FlexDirection.Row;
        btnZeile.style.marginTop     = 20;

        // Nein – kein Speichern, alles schließen
        var btnNein = new Button(() =>
        {
            rootElement.Remove(confirmOverlay);
            SchliessPopup();
        });
        btnNein.text                            = "Nein";
        btnNein.style.flexGrow                  = 1;
        btnNein.style.height                    = 44;
        btnNein.style.borderTopLeftRadius       = 10;
        btnNein.style.borderTopRightRadius      = 10;
        btnNein.style.borderBottomLeftRadius    = 10;
        btnNein.style.borderBottomRightRadius   = 10;
        btnNein.style.borderTopWidth            = 0;
        btnNein.style.borderRightWidth          = 0;
        btnNein.style.borderBottomWidth         = 0;
        btnNein.style.borderLeftWidth           = 0;
        btnNein.style.backgroundColor           = new Color(100f/255f, 100f/255f, 100f/255f);
        btnNein.style.color                     = Color.white;
        btnNein.style.fontSize                  = 16;
        btnNein.style.unityFontStyleAndWeight   = FontStyle.Bold;
        btnNein.style.marginRight               = 8;

        // Ja – speichern und schließen
        var btnJa = new Button(() =>
        {
            rootElement.Remove(confirmOverlay);
            if (currentPopup != null)
                SpeichernUndSchliessen(currentPopup);
        });
        btnJa.text                              = "Ja";
        btnJa.style.flexGrow                    = 1;
        btnJa.style.height                      = 44;
        btnJa.style.borderTopLeftRadius         = 10;
        btnJa.style.borderTopRightRadius        = 10;
        btnJa.style.borderBottomLeftRadius      = 10;
        btnJa.style.borderBottomRightRadius     = 10;
        btnJa.style.borderTopWidth              = 0;
        btnJa.style.borderRightWidth            = 0;
        btnJa.style.borderBottomWidth           = 0;
        btnJa.style.borderLeftWidth             = 0;
        btnJa.style.backgroundColor             = new Color(128f/255f, 207f/255f, 149f/255f);
        btnJa.style.color                       = Color.black;
        btnJa.style.fontSize                    = 16;
        btnJa.style.unityFontStyleAndWeight     = FontStyle.Bold;

        btnZeile.Add(btnNein);
        btnZeile.Add(btnJa);
        box.Add(btnZeile);

        confirmOverlay.Add(box);
        rootElement.Add(confirmOverlay);
    }

    // =========================================================
    // SPEICHERN-LOGIK (ausgelagert, wird von Fertig + Confirm-Ja genutzt)
    // =========================================================

    private void SpeichernUndSchliessen(VisualElement popup)
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
    }

    private void SchliessPopup()
    {
        popupOverlay.style.display = DisplayStyle.None;
        popupOverlay.Clear();
        currentPopup = null;
        editIndex    = -1;
        _isDirty     = false;
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