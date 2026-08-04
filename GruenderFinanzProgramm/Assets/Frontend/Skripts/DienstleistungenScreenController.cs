using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

public class DienstleistungenScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset popupAsset;

    private List<Service> daten = new List<Service>();
    private VisualElement rootElement;

    private const double MaxServicePreis = 99_999.99;
    private const int MaxServiceNameLaenge = 100;
    private const int MaxServiceBeschreibungLaenge = 150;

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
        rootElement = root;
        tabelleBody = root.Query<ScrollView>("tabelle-body").First();
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

            zeile.Add(Zelle(d.name, "col-titel"));
            zeile.Add(Zelle(d.description, "col-beschreibung"));
            zeile.Add(Zelle(d.priceModel, "col-preismodell"));
            zeile.Add(Zelle(d.price.ToString("F2") + " \u20ac", "col-betrag"));

            var aktionen = new VisualElement();
            aktionen.AddToClassList("col-aktionen");

            var btnEdit = new Button(() => OeffnePopup(index));
            btnEdit.text = "Bearbeiten";
            btnEdit.AddToClassList("zeile-btn-bearbeiten");

            var btnDel = new Button(() =>
            {
                OeffneLoeschenConfirmDialog(index);
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
    label.text = text;
    label.tooltip = text; // Tooltip aktivieren

    label.AddToClassList("tabelle-zelle");
    label.AddToClassList(spaltenKlasse);

    // WICHTIG: Erlaubt Hover-Events für Tooltips (Standard ist Position)
    label.pickingMode = PickingMode.Position;

    // Optional: Nutze dein eigenes Runtime-Tooltip-System, falls Unitys C#-Tooltip 
    // im Build nicht reagiert:
    // HelpTooltip.RegistriereInKarte(rootElement, label, text);

    return label;
}

    private void OeffnePopup(int index)
    {
        editIndex = index;
        _isDirty = false; // Dirty-Flag zurücksetzen
        popupOverlay.Clear();

        var popup = popupAsset.Instantiate();
        currentPopup = popup; // Referenz für spätere Nutzung im Confirm-Dialog merken

        var d = index >= 0
            ? daten[index]
            : new Service { name = "", description = "", priceModel = "Festpreis", price = 0.0 };

        // ---- Felder befüllen (VOR dem Registrieren der Callbacks, damit setValue kein dirty auslöst) ----
        var feldTitel = popup.Q<TextField>("feld-titel");
        var feldBeschreibung = popup.Q<TextField>("feld-beschreibung");
        var feldPreismodell = popup.Q<DropdownField>("feld-preismodell");
        var feldBetrag = popup.Q<TextField>("feld-betrag");

        feldTitel.value = d.name;
        feldBeschreibung.value = d.description;
        feldPreismodell.value = d.priceModel;
        feldBetrag.value = d.price.ToString("F2");

        // ---- Validierungs-Labels erstellen und direkt nach dem jeweiligen Feld einfügen ----
        var fehlerTitel = ErstelleFehlerLabel("fehler-titel");
        var fehlerBeschreibung = ErstelleFehlerLabel("fehler-beschreibung");
        var fehlerBetrag = ErstelleFehlerLabel("fehler-betrag");
        EinfuegenNach(feldTitel, fehlerTitel);
        EinfuegenNach(feldBeschreibung, fehlerBeschreibung);
        EinfuegenNach(feldBetrag, fehlerBetrag);

        // ---- Dirty-Tracking + Live-Validierung: erst NACH dem Setzen der Initialwerte registrieren ----
        feldTitel.RegisterValueChangedCallback(evt => { _isDirty = true; ZeigeFehler(fehlerTitel, ValidiereName(evt.newValue)); });
        feldBeschreibung.RegisterValueChangedCallback(evt => { _isDirty = true; ZeigeFehler(fehlerBeschreibung, ValidiereBeschreibung(evt.newValue)); });
        feldPreismodell.RegisterValueChangedCallback(_ => _isDirty = true);
        feldBetrag.RegisterValueChangedCallback(evt => { _isDirty = true; ZeigeFehler(fehlerBetrag, ValidiereBetrag(evt.newValue)); });

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
        confirmOverlay.style.position = Position.Absolute;
        confirmOverlay.style.left = 0;
        confirmOverlay.style.top = 0;
        confirmOverlay.style.right = 0;
        confirmOverlay.style.bottom = 0;
        confirmOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
        confirmOverlay.style.alignItems = Align.Center;
        confirmOverlay.style.justifyContent = Justify.Center;

        // --- Card: dunkle Box im Stil von gespeichert.uxml ---
        var box = new VisualElement();
        box.style.width = 340;
        box.style.backgroundColor = new Color(56f / 255f, 56f / 255f, 56f / 255f, 1f);
        box.style.borderTopLeftRadius = 20;
        box.style.borderTopRightRadius = 20;
        box.style.borderBottomLeftRadius = 20;
        box.style.borderBottomRightRadius = 20;
        box.style.paddingTop = 32;
        box.style.paddingBottom = 28;
        box.style.paddingLeft = 28;
        box.style.paddingRight = 28;
        box.style.position = Position.Relative;

        // --- X-Button oben rechts: schließt nur Confirm, bleibt im Hauptpopup ---
        var btnX = new Button(() => rootElement.Remove(confirmOverlay));
        btnX.text = "✕";
        btnX.style.position = Position.Absolute;
        btnX.style.top = 8;
        btnX.style.right = 8;
        btnX.style.width = 28;
        btnX.style.height = 28;
        btnX.style.fontSize = 16;
        btnX.style.color = new Color(160f / 255f, 160f / 255f, 160f / 255f);
        btnX.style.backgroundColor = Color.clear;
        btnX.style.borderTopWidth = 0;
        btnX.style.borderRightWidth = 0;
        btnX.style.borderBottomWidth = 0;
        btnX.style.borderLeftWidth = 0;
        btnX.style.unityTextAlign = TextAnchor.MiddleCenter;
        box.Add(btnX);

        // --- Titel ---
        var titel = new Label("Speichern?");
        titel.style.fontSize = 20;
        titel.style.color = Color.white;
        titel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titel.style.marginBottom = 8;
        titel.style.marginTop = 4;
        box.Add(titel);

        // --- Beschreibungstext ---
        var text = new Label("M\u00f6chtest du die eingetragenen Daten speichern?");
        text.style.fontSize = 14;
        text.style.color = new Color(200f / 255f, 200f / 255f, 200f / 255f);
        text.style.whiteSpace = WhiteSpace.Normal;
        box.Add(text);

        // --- Button-Zeile ---
        var btnZeile = new VisualElement();
        btnZeile.style.flexDirection = FlexDirection.Row;
        btnZeile.style.marginTop = 20;

        // Nein – kein Speichern, alles schließen
        var btnNein = new Button(() =>
        {
            rootElement.Remove(confirmOverlay);
            SchliessPopup();
        });
        btnNein.text = "Nein";
        btnNein.style.flexGrow = 1;
        btnNein.style.height = 44;
        btnNein.style.borderTopLeftRadius = 10;
        btnNein.style.borderTopRightRadius = 10;
        btnNein.style.borderBottomLeftRadius = 10;
        btnNein.style.borderBottomRightRadius = 10;
        btnNein.style.borderTopWidth = 0;
        btnNein.style.borderRightWidth = 0;
        btnNein.style.borderBottomWidth = 0;
        btnNein.style.borderLeftWidth = 0;
        btnNein.style.backgroundColor = new Color(100f / 255f, 100f / 255f, 100f / 255f);
        btnNein.style.color = Color.white;
        btnNein.style.fontSize = 16;
        btnNein.style.unityFontStyleAndWeight = FontStyle.Bold;
        btnNein.style.marginRight = 8;

        // Ja – speichern und schließen
        var btnJa = new Button(() =>
        {
            rootElement.Remove(confirmOverlay);
            if (currentPopup != null)
                SpeichernUndSchliessen(currentPopup);
        });
        btnJa.text = "Ja";
        btnJa.style.flexGrow = 1;
        btnJa.style.height = 44;
        btnJa.style.borderTopLeftRadius = 10;
        btnJa.style.borderTopRightRadius = 10;
        btnJa.style.borderBottomLeftRadius = 10;
        btnJa.style.borderBottomRightRadius = 10;
        btnJa.style.borderTopWidth = 0;
        btnJa.style.borderRightWidth = 0;
        btnJa.style.borderBottomWidth = 0;
        btnJa.style.borderLeftWidth = 0;
        btnJa.style.backgroundColor = new Color(128f / 255f, 207f / 255f, 149f / 255f);
        btnJa.style.color = Color.black;
        btnJa.style.fontSize = 16;
        btnJa.style.unityFontStyleAndWeight = FontStyle.Bold;

        btnZeile.Add(btnNein);
        btnZeile.Add(btnJa);
        box.Add(btnZeile);

        confirmOverlay.Add(box);
        rootElement.Add(confirmOverlay);
    }

    private void OeffneLoeschenConfirmDialog(int index)
{
    // 1. Overlay
    var confirmOverlay = new VisualElement();
    confirmOverlay.name = "confirm-overlay";
    confirmOverlay.AddToClassList("confirm-overlay");

    // 2. Box
    var box = new VisualElement();
    box.AddToClassList("confirm-box");

    // 3. Titel
    var titel = new Label("Eintrag löschen?");
    titel.AddToClassList("confirm-titel");
    box.Add(titel); // Wichtig für Anzeige!

    // 4. Beschreibungstext
    string serviceName = (daten != null && index >= 0 && index < daten.Count) ? daten[index].name : "diesen Eintrag";
    var text = new Label($"Möchtest du '{serviceName}' wirklich unwiderruflich löschen?");
    text.AddToClassList("confirm-text");
    box.Add(text); // Wichtig für Anzeige!

    // 5. Button-Zeile
    var btnZeile = new VisualElement();
    btnZeile.AddToClassList("confirm-btn-zeile");

    // Abbrechen-Button
    var btnAbbrechen = new Button(() => rootElement.Remove(confirmOverlay));
    btnAbbrechen.text = "Abbrechen";
    btnAbbrechen.AddToClassList("btn-dialog-abbrechen");

    // Löschen-Button
    var btnBestaetigen = new Button(() =>
    {
        rootElement.Remove(confirmOverlay);

        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db != null)
        {
            db.deleteService(daten[index].id);
            LadeDienstleistungenAusDatenbank();
            TabelleAktualisieren();
        }
    });
    btnBestaetigen.text = "Löschen";
    btnBestaetigen.AddToClassList("btn-dialog-loeschen");

    // Zusammenbauen
    btnZeile.Add(btnAbbrechen);
    btnZeile.Add(btnBestaetigen);
    box.Add(btnZeile);

    confirmOverlay.Add(box);
    rootElement.Add(confirmOverlay);
}

    // =========================================================
    // SPEICHERN-LOGIK (ausgelagert, wird von Fertig + Confirm-Ja genutzt)
    // =========================================================

    private void SpeichernUndSchliessen(VisualElement popup)
    {
        var feldTitel = popup.Q<TextField>("feld-titel");
        var feldBeschreibung = popup.Q<TextField>("feld-beschreibung");
        var feldPreismodell = popup.Q<DropdownField>("feld-preismodell");
        var feldBetrag = popup.Q<TextField>("feld-betrag");

        string name = feldTitel != null ? feldTitel.value : "";
        string description = feldBeschreibung != null ? feldBeschreibung.value : "";
        string priceModel = feldPreismodell != null ? feldPreismodell.value : "Festpreis";
        string betragText = feldBetrag != null ? feldBetrag.value : "0";

        // ---- Validierung: Felder prüfen und Fehler-Labels aktualisieren ----
        string fehlerN  = ValidiereName(name);
        string fehlerB  = ValidiereBeschreibung(description);
        string fehlerBt = ValidiereBetrag(betragText);

        ZeigeFehler(popup.Q<Label>("fehler-titel"), fehlerN);
        ZeigeFehler(popup.Q<Label>("fehler-beschreibung"), fehlerB);
        ZeigeFehler(popup.Q<Label>("fehler-betrag"), fehlerBt);

        if (fehlerN != null || fehlerB != null || fehlerBt != null) return; // Abbruch bei Fehler

        double betrag = ParseDienstleistungsPreis(betragText);

        var neu = new Service
        {
            name = BegrenzeText(name, MaxServiceNameLaenge),
            description = BegrenzeText(description, MaxServiceBeschreibungLaenge),
            priceModel = string.IsNullOrWhiteSpace(priceModel) ? "Festpreis" : priceModel,
            price = BegrenzeServicePreis(betrag)
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
        editIndex = -1;
        _isDirty = false;
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

    private static string BegrenzeText(string text, int maxLaenge)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        text = text.Trim();

        if (text.Length > maxLaenge)
        {
            return text.Substring(0, maxLaenge);
        }

        return text;
    }

    private static double BegrenzeServicePreis(double preis)
    {
        if (double.IsNaN(preis) || double.IsInfinity(preis))
        {
            return 0.0;
        }

        if (preis < 0.0)
        {
            return 0.0;
        }

        if (preis > MaxServicePreis)
        {
            return MaxServicePreis;
        }

        return Math.Round(preis, 2);
    }

    // =========================================================
    // VALIDIERUNG & FEHLER-LABELS
    // =========================================================

    /// Erstellt ein unsichtbares rotes Label mit dem angegebenen Namen.
    private static Label ErstelleFehlerLabel(string labelName)
    {
        var label = new Label();
        label.name = labelName;
        label.style.color = Color.red;
        label.style.fontSize = 11;
        label.style.marginTop = 2;
        label.style.display = DisplayStyle.None; // standardmäßig unsichtbar
        return label;
    }

    /// Zeigt eine Fehlermeldung im Label an, oder versteckt es wenn nachricht null/leer ist.
    private static void ZeigeFehler(Label label, string nachricht)
    {
        if (label == null) return;
        if (string.IsNullOrEmpty(nachricht))
        {
            label.text = "";
            label.style.display = DisplayStyle.None;
        }
        else
        {
            label.text = nachricht;
            label.style.display = DisplayStyle.Flex;
        }
    }

    /// Fügt nachElement direkt nach element in denselben Parent ein.
    private static void EinfuegenNach(VisualElement element, VisualElement nachElement)
    {
        var parent = element?.parent;
        if (parent == null) return;
        int index = parent.IndexOf(element);
        parent.Insert(index + 1, nachElement);
    }

    private static string ValidiereName(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Name darf nicht leer sein.";
        if (text.Trim().Length > MaxServiceNameLaenge)
            return $"Maximal {MaxServiceNameLaenge} Zeichen erlaubt.";
        return null;
    }

    private static string ValidiereBeschreibung(string text)
    {
        if (text != null && text.Trim().Length > MaxServiceBeschreibungLaenge)
            return $"Maximal {MaxServiceBeschreibungLaenge} Zeichen erlaubt.";
        return null;
    }

    private static string ValidiereBetrag(string text)
    {
        text = (text ?? "").Replace("\u20ac", "").Trim();
        if (string.IsNullOrWhiteSpace(text))
            return "Bitte einen Betrag eingeben.";

        var kultDe = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
        double wert;
        bool isDeFormat = false;

        // Versuch 1: Deutsches Format (z. B. "1.000,50" oder "12,50")
        if (double.TryParse(text, System.Globalization.NumberStyles.Any, kultDe, out wert))
            isDeFormat = true;
        // Versuch 2: Englisches Format (z. B. "12.50")
        else if (!double.TryParse(text.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out wert))
            return "Bitte eine g\u00fcltige Zahl eingeben (z.\u00a0B. 12,50).";

        if (wert < 0)
            return "Betrag darf nicht negativ sein.";
        if (wert > MaxServicePreis)
            return "Maximal 99.999,99\u00a0\u20ac erlaubt.";

        // Nachkommastellen prüfen: Dezimalzeichen je nach Format suchen
        char dezSep = isDeFormat ? ',' : '.';
        int sepIdx = text.LastIndexOf(dezSep);
        if (sepIdx >= 0 && text.Length - sepIdx - 1 > 2)
            return "Maximal 2 Nachkommastellen erlaubt.";

        return null;
    }

    private static double ParseDienstleistungsPreis(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0.0;
        }

        text = text.Replace("€", "").Trim();

        if (double.TryParse(
            text,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.GetCultureInfo("de-DE"),
            out double preisDe))
        {
            return preisDe;
        }

        if (double.TryParse(
            text.Replace(",", "."),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out double preisInvariant))
        {
            return preisInvariant;
        }

        return 0.0;
    }

}