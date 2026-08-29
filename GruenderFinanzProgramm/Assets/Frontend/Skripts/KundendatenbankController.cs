using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class KundendatenbankController : MonoBehaviour
{
    private UIDocument uiDocument;

    [Header("UI Templates & Container")]
    [SerializeField] private VisualTreeAsset kundenZeileTemplate;

    private VisualElement root;
    private ScrollView kundenContainer;
    private VisualElement kundenListeHolder;
    private Button btnKundeHinzufuegen;
    private Label lblCounter;

    [Header("Lokaler Nutzer")]
    private Button btnEditLocal;

    [Header("Popups")]
    private VisualElement popupErstellen;
    private VisualElement popupBearbeiten;
    private VisualElement popupGeloescht;
    private Label lblGeloeschtText;
    private Button btnCloseGeloescht;
    private VisualElement popupGespeichert;
    private Label lblGespeichertText;
    private Button btnCloseGespeichert;
    private VisualElement popupLoeschenBestaetigen;
    private Button btnLoeschenAbbrechen, btnLoeschenBestaetigen, btnLoeschenClose;
    private KundeData kundeZumLoeschen;

    // Inputs Erstellen-Popup
    private TextField inputCreateVorname, inputCreateNachname, inputCreateFirma;
    private TextField inputCreateStrasse, inputCreatePlz, inputCreateOrt;
    private TextField inputCreateEmail, inputCreateTelefon;
    private DropdownField dropdownCreateVorwahl;
    private Button btnCreateSpeichern, btnCreateAbbrechen, btnCreateClose;

    // Inputs Bearbeiten-Popup
    private TextField inputEditVorname, inputEditNachname, inputEditFirma;
    private TextField inputEditStrasse, inputEditPlz, inputEditOrt;
    private TextField inputEditEmail, inputEditTelefon;
    private DropdownField dropdownEditVorwahl;
    private Button btnEditSpeichern, btnEditAbbrechen, btnEditClose;

    // Verfügbare Ländervorwahlen für das Telefon-Dropdown, Standard ist Deutschland.
    private static readonly List<string> Vorwahlen = new List<string> { "+49", "+43", "+41", "+33", "+1" };

    private List<KundeData> kundenListe = new List<KundeData>();
    private KundeData aktuellBearbeiteterKunde;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        kundenContainer = root.Q<ScrollView>("kunden-container");
        kundenListeHolder = root.Q<VisualElement>("kunden-liste-holder");
        btnKundeHinzufuegen = root.Q<Button>("btn-add-coustomer");
        lblCounter = root.Q<Label>("lbl-counter");
        btnEditLocal = root.Q<Button>("btn-edit-local");

        popupErstellen = root.Q<VisualElement>("PopUpKundeerstellen");
        popupBearbeiten = root.Q<VisualElement>("PopUpKundenbearbeiten");
        popupGeloescht = root.Q<VisualElement>("popup-kunde-geloescht");
        lblGeloeschtText = popupGeloescht?.Q<Label>("label-kunde-geloescht-text");
        btnCloseGeloescht = popupGeloescht?.Q<Button>("btn-close-kunde-geloescht");
        popupGespeichert = root.Q<VisualElement>("popup-kunde-gespeichert");
        lblGespeichertText = popupGespeichert?.Q<Label>("label-kunde-gespeichert-text");
        btnCloseGespeichert = popupGespeichert?.Q<Button>("btn-close-kunde-gespeichert");
        popupLoeschenBestaetigen = root.Q<VisualElement>("PopUpKundeLoeschenBestaetigen");
        btnLoeschenAbbrechen = popupLoeschenBestaetigen?.Q<Button>("loeschen-btn-abbrechen");
        btnLoeschenBestaetigen = popupLoeschenBestaetigen?.Q<Button>("loeschen-btn-bestaetigen");
        btnLoeschenClose = popupLoeschenBestaetigen?.Q<Button>("loeschen-btn-close");

        AssignPopupElements();

        InitialisiereVorwahlDropdown(dropdownCreateVorwahl);
        InitialisiereVorwahlDropdown(dropdownEditVorwahl);
        NurZiffern(inputCreatePlz, 5);
        NurZiffern(inputEditPlz, 5);
        NurZiffern(inputCreateTelefon, 15);
        NurZiffern(inputEditTelefon, 15);

        SetElementVisible(popupErstellen, false);
        SetElementVisible(popupBearbeiten, false);
        SetElementVisible(popupGeloescht, false);
        SetElementVisible(popupGespeichert, false);
        SetElementVisible(popupLoeschenBestaetigen, false);

        RegisterEvents();
        LadeKundenAusDatenbank();
        LadeLokalenNutzer();

        RegistriereHelpTooltips();
        ButtonHoverController.RegistriereAlle(root);
    }

    // ============================================================
    // LOKALER NUTZER: Karte oben in der KDB mit den echten Firmen-/
    // Kontaktdaten aus den Einstellungen befüllen.
    // FIX: Die 5 Labels (Name/Firma/Adresse/Email/Telefon) waren vorher
    // reiner Platzhaltertext im UXML ("Max Mustermann" usw.) - es gab im
    // ganzen Controller keine einzige Zeile, die diese Labels überhaupt
    // per Q<Label>(...) angefasst hat. Einstellungen und KDB waren also
    // schlicht nie verbunden.
    // ============================================================
    private void LadeLokalenNutzer()
    {
        var lblName = root.Q<Label>("label-lokaler-nutzer-name");
        var lblFirma = root.Q<Label>("label-lokaler-nutzer-firma");
        var lblAdresse = root.Q<Label>("label-lokaler-nutzer-adresse");
        var lblEmail = root.Q<Label>("label-lokaler-nutzer-email");
        var lblTelefon = root.Q<Label>("label-lokaler-nutzer-telefon");

        try
        {
            // Profilname kommt aus dem Auth-System (bei der Registrierung
            // vergeben), nicht aus der Firmen-DB.
            string profilname = StateManager.Instance?.getCurrentUser()?.username;
            if (lblName != null)
                lblName.text = string.IsNullOrWhiteSpace(profilname) ? "Kein Profilname hinterlegt" : profilname;

            var db = UserDatabaseAccess.getCurrentUserDatabase();
            var company = db?.getAllCompanies()?.FirstOrDefault();

            if (lblFirma != null)
                lblFirma.text = string.IsNullOrWhiteSpace(company?.name) ? "Keine Firma hinterlegt" : company.name;

            if (lblAdresse != null)
            {
                string strasse = company?.strasseuHausNr;
                string plzOrt = company != null && company.plz > 0
                    ? $"{company.plz} {company.location}".Trim()
                    : company?.location;
                string adresse = string.Join(" ", new[] { strasse, plzOrt }.Where(s => !string.IsNullOrWhiteSpace(s)));
                lblAdresse.text = string.IsNullOrWhiteSpace(adresse) ? "Keine Adresse hinterlegt" : adresse;
            }

            if (lblEmail != null)
            {
                string email = company?.email;
                email = string.IsNullOrWhiteSpace(email) || email == "Null" ? null : email;
                lblEmail.text = email ?? "Keine E-Mail hinterlegt";
            }

            if (lblTelefon != null)
            {
                string telefon = company?.handyNr;
                telefon = string.IsNullOrWhiteSpace(telefon) || telefon == "Null" ? null : telefon;
                lblTelefon.text = telefon ?? "Keine Telefonnummer hinterlegt";
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[KDB] LadeLokalenNutzer: " + e.Message);
        }
    }

    // ============================================================
    // HELP TOOLTIPS
    // ============================================================

    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(root, "btn-help-seitentitel",
            "Die Kundendatenbank verwaltet alle deine Kunden und Kontakte. " +
            "Lege neue Kunden an, bearbeite bestehende Eintr\u00e4ge oder l\u00f6sche sie. " +
            "Kunden k\u00f6nnen direkt in Angeboten und Rechnungen ausgew\u00e4hlt werden.");

        HelpTooltip.Registriere(root, "btn-help-lokaler-nutzer",
            "Dies sind deine eigenen Firmen-/Kontaktdaten aus den Einstellungen. " +
            "Klicke auf \u201e\u00c4ndern\u201c um direkt zu den Einstellungen zu springen und sie anzupassen.");

        HelpTooltip.Registriere(root, "btn-help-kunden",
            "Klicke auf \u201eKunde Hinzuf\u00fcgen\u201c um einen neuen Kunden anzulegen. " +
            "Alle gespeicherten Kunden erscheinen in der Liste darunter.");
    }

    // ============================================================
    // POPUP-ELEMENTE ZUWEISEN
    // ============================================================

    private void AssignPopupElements()
    {
        if (popupErstellen != null)
        {
            inputCreateVorname = popupErstellen.Q<TextField>("create-vorname");
            inputCreateNachname = popupErstellen.Q<TextField>("create-nachname");
            inputCreateFirma = popupErstellen.Q<TextField>("create-firma");
            inputCreateStrasse = popupErstellen.Q<TextField>("create-strasse");
            inputCreatePlz = popupErstellen.Q<TextField>("create-plz");
            inputCreateOrt = popupErstellen.Q<TextField>("create-ort");
            inputCreateEmail = popupErstellen.Q<TextField>("create-email");
            inputCreateTelefon = popupErstellen.Q<TextField>("create-telefon");
            dropdownCreateVorwahl = popupErstellen.Q<DropdownField>("create-vorwahl");
            btnCreateSpeichern = popupErstellen.Q<Button>("create-btn-speichern");
            btnCreateAbbrechen = popupErstellen.Q<Button>("create-btn-abbrechen");
            btnCreateClose = popupErstellen.Q<Button>("create-btn-close");
        }

        if (popupBearbeiten != null)
        {
            inputEditVorname = popupBearbeiten.Q<TextField>("edit-vorname");
            inputEditNachname = popupBearbeiten.Q<TextField>("edit-nachname");
            inputEditFirma = popupBearbeiten.Q<TextField>("edit-firma");
            inputEditStrasse = popupBearbeiten.Q<TextField>("edit-strasse");
            inputEditPlz = popupBearbeiten.Q<TextField>("edit-plz");
            inputEditOrt = popupBearbeiten.Q<TextField>("edit-ort");
            inputEditEmail = popupBearbeiten.Q<TextField>("edit-email");
            inputEditTelefon = popupBearbeiten.Q<TextField>("edit-telefon");
            dropdownEditVorwahl = popupBearbeiten.Q<DropdownField>("edit-vorwahl");
            btnEditSpeichern = popupBearbeiten.Q<Button>("edit-btn-speichern");
            btnEditAbbrechen = popupBearbeiten.Q<Button>("edit-btn-abbrechen");
            btnEditClose = popupBearbeiten.Q<Button>("edit-btn-close");
        }
    }

    // Beschränkt ein Textfeld auf Ziffern - z. B. für PLZ und Telefonnummer.
    private static void NurZiffern(TextField feld, int maxLength = -1)
    {
        if (feld == null) return;
        feld.RegisterValueChangedCallback(evt =>
        {
            string gefiltert = new string(evt.newValue.Where(char.IsDigit).ToArray());
            if (maxLength > 0 && gefiltert.Length > maxLength)
                gefiltert = gefiltert.Substring(0, maxLength);
            if (gefiltert != evt.newValue)
                feld.SetValueWithoutNotify(gefiltert);
        });
    }

    private void InitialisiereVorwahlDropdown(DropdownField dropdown)
    {
        if (dropdown == null) return;
        dropdown.choices = new List<string>(Vorwahlen);
        dropdown.SetValueWithoutNotify("+49");
    }

    // Trennt eine gespeicherte Telefonnummer wieder in Vorwahl und Rest auf,
    // damit das Bearbeiten-Popup beide Felder korrekt vorbefüllt.
    private void SetzeTelefonFelder(DropdownField vorwahlDropdown, TextField telefonFeld, string kompletteNummer)
    {
        if (vorwahlDropdown == null || telefonFeld == null) return;

        string passendeVorwahl = "+49";
        string rest = kompletteNummer ?? "";

        foreach (var v in Vorwahlen)
        {
            if (rest.StartsWith(v))
            {
                passendeVorwahl = v;
                rest = rest.Substring(v.Length).TrimStart();
                break;
            }
        }

        vorwahlDropdown.SetValueWithoutNotify(passendeVorwahl);
        telefonFeld.SetValueWithoutNotify(rest);
    }

    private static string ZusammengesetzteTelefonnummer(DropdownField vorwahlDropdown, TextField telefonFeld)
    {
        string vorwahl = vorwahlDropdown != null ? vorwahlDropdown.value : "+49";
        string ziffern = telefonFeld != null ? telefonFeld.value : "";
        return string.IsNullOrEmpty(ziffern) ? "" : vorwahl + " " + ziffern;
    }

    // ============================================================
    // EVENTS
    // ============================================================

    private void RegisterEvents()
    {
        if (btnKundeHinzufuegen != null)
            btnKundeHinzufuegen.clicked += () => SetElementVisible(popupErstellen, true);

        if (btnCreateAbbrechen != null)
            btnCreateAbbrechen.clicked += () => SetElementVisible(popupErstellen, false);
        if (btnCreateClose != null)
            btnCreateClose.clicked += () => SetElementVisible(popupErstellen, false);
        if (btnCreateSpeichern != null)
            btnCreateSpeichern.clicked += SpeichereNeuenKunden;

        if (btnEditAbbrechen != null)
            btnEditAbbrechen.clicked += () => SetElementVisible(popupBearbeiten, false);
        if (btnEditClose != null)
            btnEditClose.clicked += () => SetElementVisible(popupBearbeiten, false);
        if (btnEditSpeichern != null)
            btnEditSpeichern.clicked += SpeichereBearbeitetenKunden;

        if (btnCloseGeloescht != null)
            btnCloseGeloescht.clicked += () => SetElementVisible(popupGeloescht, false);
        if (btnCloseGespeichert != null)
            btnCloseGespeichert.clicked += () => SetElementVisible(popupGespeichert, false);

        if (btnLoeschenAbbrechen != null)
            btnLoeschenAbbrechen.clicked += () =>
            {
                kundeZumLoeschen = null;
                SetElementVisible(popupLoeschenBestaetigen, false);
            };
        if (btnLoeschenClose != null)
            btnLoeschenClose.clicked += () =>
            {
                kundeZumLoeschen = null;
                SetElementVisible(popupLoeschenBestaetigen, false);
            };
        if (btnLoeschenBestaetigen != null)
            btnLoeschenBestaetigen.clicked += () =>
            {
                SetElementVisible(popupLoeschenBestaetigen, false);
                if (kundeZumLoeschen != null) LoescheKunde(kundeZumLoeschen);
                kundeZumLoeschen = null;
            };

        // FIX: Öffnete vorher das "Kunde bearbeiten"-Popup mit leeren
        // Feldern (aktuellBearbeiteterKunde = null) - beim Speichern wäre
        // das als NEUER Kunde in der KDB gelandet, statt die eigenen
        // Firmendaten zu ändern. Die echten Firmendaten liegen in den
        // Einstellungen, also verlinkt "Ändern" jetzt dorthin - und öffnet
        // das Unternehmensdaten-Popup dort gleich automatisch mit.
        if (btnEditLocal != null)
            btnEditLocal.clicked += () =>
            {
                EinstellungenController.OeffneUnternehmenPopupBeimStart = true;
                SceneManager.LoadScene("Einstellungen");
            };
    }

    private void SetElementVisible(VisualElement element, bool visible)
    {
        if (element == null) return;
        element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        element.style.position = visible ? Position.Absolute : Position.Relative;
    }

    // ============================================================
    // DATEN LADEN
    // ============================================================

    private void LadeKundenAusDatenbank()
    {
        kundenListe.Clear();
        bool datenbankErfolgreich = false;

        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db != null)
            {
                var backendKunden = db.getAllCustomers();
                if (backendKunden != null)
                {
                    foreach (var bKunde in backendKunden)
                    {
                        var kData = new KundeData();
                        kData.backendObjekt = bKunde;
                        kData.id = bKunde.id.ToString();

                        string vollerName = bKunde.name ?? "";
                        string[] teile = vollerName.Split(' ');
                        if (teile.Length > 0) kData.vorname = teile[0];
                        if (teile.Length > 1)
                            kData.nachname = string.Join(" ", teile, 1, teile.Length - 1);

                        kData.firma = "Kunde";
                        kData.strasse = bKunde.street ?? "";
                        kData.plz = bKunde.postalCode ?? "";
                        kData.ort = bKunde.city ?? "";
                        kData.email = bKunde.email ?? "";
                        kData.telefon = bKunde.phone ?? "";

                        kundenListe.Add(kData);
                    }
                    datenbankErfolgreich = true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[KDB] DB nicht erreichbar: " + e.Message);
        }

        if (!datenbankErfolgreich)
            GeneriereTestDaten();

        RefreshKundenListe();
    }

    private void GeneriereTestDaten()
    {
        if (kundenListe.Count == 0)
        {
            kundenListe.Add(new KundeData { id = "1", vorname = "Max", nachname = "Mustermann", firma = "Mustert GmbH", strasse = "Musterstr. 8", plz = "12345", ort = "Musterstadt", email = "muster@email.de", telefon = "+49 016212345" });
            kundenListe.Add(new KundeData { id = "2", vorname = "Erika", nachname = "Musterfrau", firma = "Fraunhofer AG", strasse = "Technikweg 4", plz = "54321", ort = "Techcity", email = "erika@fraunhofer.de", telefon = "+49 017598765" });
        }
    }

    // ============================================================
    // LISTE AUFBAUEN
    // ============================================================

    private void RefreshKundenListe()
    {
        if (lblCounter != null)
            lblCounter.text = kundenListe.Count == 1 ? "1 Eintrag" : $"{kundenListe.Count} Eintr\u00e4ge";

        VisualElement ziel = kundenListeHolder ?? (VisualElement)kundenContainer;
        if (ziel == null || kundenZeileTemplate == null) return;

        ziel.Clear();

        // FIX: Bisher blieb die Liste bei 0 echten Kunden komplett leer,
        // ohne jeden Hinweis, was dort einmal auftauchen wird. Jetzt gibt
        // es stattdessen einen Platzhalter-Hinweistext.
        if (kundenListe.Count == 0)
        {
            var leerHinweis = new Label(
                "Noch keine Kunden angelegt. Über \u201eKunde Hinzuf\u00fcgen\u201c " +
                "erscheinen deine Kundendaten hier.");
            leerHinweis.style.color = new Color(0.56f, 0.56f, 0.56f); // #8E8D8D
            leerHinweis.style.fontSize = 18;
            leerHinweis.style.unityTextAlign = TextAnchor.MiddleCenter;
            leerHinweis.style.marginTop = 24;
            leerHinweis.style.whiteSpace = WhiteSpace.Normal;
            ziel.Add(leerHinweis);
            AppEventManager.KundenAnzahlGeaendert(0);
            return;
        }

        foreach (var kunde in kundenListe)
        {
            var neueKarte = kundenZeileTemplate.Instantiate();

            var nameLabel = neueKarte.Q<Label>("lbl-name");
            var firmaLabel = neueKarte.Q<Label>("lbl-firma");
            var emailLabel = neueKarte.Q<Label>("lbl-email");
            var telefonLabel = neueKarte.Q<Label>("lbl-number");
            var adresseLabel = neueKarte.Q<Label>("lbl-adress");
            var btnAendern = neueKarte.Q<Button>("btn-edit");
            var btnLoeschen = neueKarte.Q<Button>("btn-delete");

            if (nameLabel != null) nameLabel.text = $"{kunde.vorname} {kunde.nachname}".Trim();
            if (firmaLabel != null) firmaLabel.text = kunde.firma;
            if (emailLabel != null) emailLabel.text = kunde.email;
            if (telefonLabel != null) telefonLabel.text = kunde.telefon;
            if (adresseLabel != null)
                adresseLabel.text = $"{kunde.strasse}, {kunde.plz} {kunde.ort}".Trim();

            if (btnAendern != null) btnAendern.clicked += () => OeffneBearbeitenPopup(kunde);
            if (btnLoeschen != null) btnLoeschen.clicked += () => OeffneLoeschenBestaetigenPopup(kunde);

            // Help-Icon in der Karte registrieren (Template-Icon)
            var helpIcon = neueKarte.Q<VisualElement>("btn-help-kundenkarte");
            if (helpIcon != null)
                HelpTooltip.RegistriereInKarte(root, helpIcon,
                    "Hier siehst du die Kontaktdaten dieses Kunden. " +
                    "Klicke auf \u201e\u00c4ndern\u201c um die Daten zu bearbeiten " +
                    "oder auf \u201eL\u00f6schen\u201c um den Eintrag zu entfernen.");

            ziel.Add(neueKarte);
        }

        AppEventManager.KundenAnzahlGeaendert(kundenListe.Count);
    }

    // ============================================================
    // KUNDEN SPEICHERN / BEARBEITEN / LOESCHEN
    // ============================================================

    private void SpeichereNeuenKunden()
    {
        var uiKunde = new KundeData
        {
            id = Guid.NewGuid().ToString(),
            vorname = inputCreateVorname != null ? inputCreateVorname.value : "",
            nachname = inputCreateNachname != null ? inputCreateNachname.value : "",
            firma = inputCreateFirma != null ? inputCreateFirma.value : "",
            strasse = inputCreateStrasse != null ? inputCreateStrasse.value : "",
            plz = inputCreatePlz != null ? inputCreatePlz.value : "",
            ort = inputCreateOrt != null ? inputCreateOrt.value : "",
            email = inputCreateEmail != null ? inputCreateEmail.value : "",
            telefon = ZusammengesetzteTelefonnummer(dropdownCreateVorwahl, inputCreateTelefon)
        };

        bool tatsaechlichGespeichert = false;
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db != null)
            {
                var bKunde = new Customer
                {
                    name = $"{uiKunde.vorname} {uiKunde.nachname}".Trim(),
                    street = uiKunde.strasse,
                    postalCode = uiKunde.plz,
                    city = uiKunde.ort,
                    email = uiKunde.email,
                    phone = uiKunde.telefon,
                    lastUpdated = DateTime.Now
                };
                db.createCustomer(bKunde);

                // Nach insertAndGetId() traegt bKunde.id die echte, vom Backend
                // vergebene ID - 0/unveraendert wuerde auf einen fehlgeschlagenen
                // Insert hindeuten. Zusaetzlich per getCustomerById gegenpruefen,
                // dass der Datensatz auch wirklich in der DB angekommen ist.
                tatsaechlichGespeichert = bKunde.id > 0 && db.getCustomerById(bKunde.id) != null;
            }
        }
        catch (Exception e) { Debug.LogWarning("[KDB] Speichern fehlgeschlagen: " + e.Message); }

        if (!tatsaechlichGespeichert) kundenListe.Add(uiKunde);

        if (tatsaechlichGespeichert)
        {
            if (lblGespeichertText != null) lblGespeichertText.text = "Kunde wurde hinzugefügt";
            SetElementVisible(popupGespeichert, true);
        }

        SetElementVisible(popupErstellen, false);
        ClearCreateInputs();

        if (tatsaechlichGespeichert) LadeKundenAusDatenbank();
        else RefreshKundenListe();
    }

    private void OeffneBearbeitenPopup(KundeData kunde)
    {
        aktuellBearbeiteterKunde = kunde;

        if (inputEditVorname != null) inputEditVorname.value = kunde.vorname;
        if (inputEditNachname != null) inputEditNachname.value = kunde.nachname;
        if (inputEditFirma != null) inputEditFirma.value = kunde.firma;
        if (inputEditStrasse != null) inputEditStrasse.value = kunde.strasse;
        if (inputEditPlz != null) inputEditPlz.value = kunde.plz;
        if (inputEditOrt != null) inputEditOrt.value = kunde.ort;
        if (inputEditEmail != null) inputEditEmail.value = kunde.email;
        SetzeTelefonFelder(dropdownEditVorwahl, inputEditTelefon, kunde.telefon);

        SetElementVisible(popupBearbeiten, true);
    }

    private void SpeichereBearbeitetenKunden()
    {
        if (aktuellBearbeiteterKunde == null)
        {
            SetElementVisible(popupBearbeiten, false);
            return;
        }

        aktuellBearbeiteterKunde.vorname = inputEditVorname != null ? inputEditVorname.value : aktuellBearbeiteterKunde.vorname;
        aktuellBearbeiteterKunde.nachname = inputEditNachname != null ? inputEditNachname.value : aktuellBearbeiteterKunde.nachname;
        aktuellBearbeiteterKunde.firma = inputEditFirma != null ? inputEditFirma.value : aktuellBearbeiteterKunde.firma;
        aktuellBearbeiteterKunde.strasse = inputEditStrasse != null ? inputEditStrasse.value : aktuellBearbeiteterKunde.strasse;
        aktuellBearbeiteterKunde.plz = inputEditPlz != null ? inputEditPlz.value : aktuellBearbeiteterKunde.plz;
        aktuellBearbeiteterKunde.ort = inputEditOrt != null ? inputEditOrt.value : aktuellBearbeiteterKunde.ort;
        aktuellBearbeiteterKunde.email = inputEditEmail != null ? inputEditEmail.value : aktuellBearbeiteterKunde.email;
        aktuellBearbeiteterKunde.telefon = ZusammengesetzteTelefonnummer(dropdownEditVorwahl, inputEditTelefon);

        bool tatsaechlichAktualisiert = false;
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db != null && aktuellBearbeiteterKunde.backendObjekt != null)
            {
                var bKunde = aktuellBearbeiteterKunde.backendObjekt;
                bKunde.name = $"{aktuellBearbeiteterKunde.vorname} {aktuellBearbeiteterKunde.nachname}".Trim();
                bKunde.street = aktuellBearbeiteterKunde.strasse;
                bKunde.postalCode = aktuellBearbeiteterKunde.plz;
                bKunde.city = aktuellBearbeiteterKunde.ort;
                bKunde.email = aktuellBearbeiteterKunde.email;
                bKunde.phone = aktuellBearbeiteterKunde.telefon;
                bKunde.lastUpdated = DateTime.Now;
                db.updateCustomer(bKunde);

                // Nach dem Update erneut aus der DB laden und gegenpruefen,
                // ob die neuen Werte auch wirklich dort angekommen sind.
                var geprueft = db.getCustomerById(bKunde.id);
                tatsaechlichAktualisiert = geprueft != null
                    && geprueft.name == bKunde.name
                    && geprueft.phone == bKunde.phone;
            }
        }
        catch (Exception e) { Debug.LogWarning("[KDB] Update fehlgeschlagen: " + e.Message); }

        if (tatsaechlichAktualisiert)
        {
            if (lblGespeichertText != null) lblGespeichertText.text = "Kunde wurde aktualisiert";
            SetElementVisible(popupGespeichert, true);
        }

        SetElementVisible(popupBearbeiten, false);

        if (tatsaechlichAktualisiert) LadeKundenAusDatenbank();
        else RefreshKundenListe();
    }

    private void OeffneLoeschenBestaetigenPopup(KundeData kunde)
    {
        kundeZumLoeschen = kunde;
        SetElementVisible(popupLoeschenBestaetigen, true);
    }

    private void LoescheKunde(KundeData kunde)
    {
        bool tatsaechlichGeloescht = false;
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db != null && int.TryParse(kunde.id, out int id))
            {
                bool vorherVorhanden = db.getCustomerById(id) != null;
                if (vorherVorhanden)
                {
                    db.deleteCustomer(id);
                    tatsaechlichGeloescht = db.getCustomerById(id) == null;
                }
            }
        }
        catch (Exception e) { Debug.LogWarning("[KDB] L\u00f6schen fehlgeschlagen: " + e.Message); }

        kundenListe.Remove(kunde);

        if (tatsaechlichGeloescht)
        {
            if (lblGeloeschtText != null) lblGeloeschtText.text = "Kunde wurde gelöscht";
            SetElementVisible(popupGeloescht, true);
            LadeKundenAusDatenbank();
        }
        else
        {
            RefreshKundenListe();
        }
    }

    // ============================================================
    // HILFSMETHODEN
    // ============================================================

    private void ClearCreateInputs()
    {
        if (inputCreateVorname != null) inputCreateVorname.value = "";
        if (inputCreateNachname != null) inputCreateNachname.value = "";
        if (inputCreateFirma != null) inputCreateFirma.value = "";
        if (inputCreateStrasse != null) inputCreateStrasse.value = "";
        if (inputCreatePlz != null) inputCreatePlz.value = "";
        if (inputCreateOrt != null) inputCreateOrt.value = "";
        if (inputCreateEmail != null) inputCreateEmail.value = "";
        if (inputCreateTelefon != null) inputCreateTelefon.value = "";
        if (dropdownCreateVorwahl != null) dropdownCreateVorwahl.SetValueWithoutNotify("+49");
    }

    private void ClearEditInputs()
    {
        if (inputEditVorname != null) inputEditVorname.value = "";
        if (inputEditNachname != null) inputEditNachname.value = "";
        if (inputEditFirma != null) inputEditFirma.value = "";
        if (inputEditStrasse != null) inputEditStrasse.value = "";
        if (inputEditPlz != null) inputEditPlz.value = "";
        if (inputEditOrt != null) inputEditOrt.value = "";
        if (inputEditEmail != null) inputEditEmail.value = "";
        if (inputEditTelefon != null) inputEditTelefon.value = "";
        if (dropdownEditVorwahl != null) dropdownEditVorwahl.SetValueWithoutNotify("+49");
    }
}

[System.Serializable]
public class KundeData
{
    public Customer backendObjekt;
    public string id;
    public string vorname;
    public string nachname;
    public string firma;
    public string strasse;
    public string plz;
    public string ort;
    public string email;
    public string telefon;
}