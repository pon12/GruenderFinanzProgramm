using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class KundendatenbankController : MonoBehaviour
{
    private UIDocument uiDocument;

    [Header("UI Templates & Container")]
    [SerializeField] private VisualTreeAsset kundenZeileTemplate; 

    private ScrollView kundenContainer;
    private VisualElement kundenListeHolder; 
    private Button btnKundeHinzufuegen;
    private Label lblCounter; 

    [Header("Lokaler Nutzer")]
    private Button btnEditLocal; // Der Button für den lokalen Nutzer oben

    [Header("Popups")]
    private VisualElement popupErstellen;
    private VisualElement popupBearbeiten;

    // Inputs Erstellen-Popup
    private TextField inputCreateVorname, inputCreateNachname, inputCreateFirma, inputCreateStrasse, inputCreatePlz, inputCreateOrt, inputCreateEmail, inputCreateTelefon;
    private Button btnCreateSpeichern, btnCreateAbbrechen;

    // Inputs Bearbeiten-Popup
    private TextField inputEditVorname, inputEditNachname, inputEditFirma, inputEditStrasse, inputEditPlz, inputEditOrt, inputEditEmail, inputEditTelefon;
    private Button btnEditSpeichern, btnEditAbbrechen;

    private List<KundeData> kundenListe = new List<KundeData>();
    private KundeData aktuellBearbeiteterKunde;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // 1. Haupt-UI Elemente greifen
        kundenContainer = root.Q<ScrollView>("kunden-container");
        kundenListeHolder = root.Q<VisualElement>("kunden-liste-holder"); 
        btnKundeHinzufuegen = root.Q<Button>("btn-add-coustomer"); 
        lblCounter = root.Q<Label>("lbl-counter"); 

        // 2. Lokalen Nutzer-Button greifen
        btnEditLocal = root.Q<Button>("btn-edit-local");

        // 3. Popups greifen (Mit dem korrekten "n" im Namen)
        popupErstellen = root.Q<VisualElement>("PopUpKundeerstellen");
        popupBearbeiten = root.Q<VisualElement>("PopUpKundenbearbeiten");

        // 4. Popup-Inhalte zuweisen
        AssignPopupElements(root);

        // Standardmäßig verstecken
        SetElementVisible(popupErstellen, false);
        SetElementVisible(popupBearbeiten, false);

        // 5. Events binden
        RegisterEvents();

        // 6. Testdaten generieren & Liste anzeigen
        GeneriereTestDaten();
        RefreshKundenListe();
    }

    private void AssignPopupElements(VisualElement root)
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
            btnCreateSpeichern = popupErstellen.Q<Button>("create-btn-speichern");
            btnCreateAbbrechen = popupErstellen.Q<Button>("create-btn-abbrechen");
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
            btnEditSpeichern = popupBearbeiten.Q<Button>("edit-btn-speichern");
            btnEditAbbrechen = popupBearbeiten.Q<Button>("edit-btn-abbrechen");
        }
    }

    private void RegisterEvents()
    {
        if (btnKundeHinzufuegen != null) btnKundeHinzufuegen.clicked += () => SetElementVisible(popupErstellen, true);
        
        if (btnCreateAbbrechen != null) btnCreateAbbrechen.clicked += () => SetElementVisible(popupErstellen, false);
        if (btnCreateSpeichern != null) btnCreateSpeichern.clicked += SpeichereNeuenKunden;

        if (btnEditAbbrechen != null) btnEditAbbrechen.clicked += () => SetElementVisible(popupBearbeiten, false);
        if (btnEditSpeichern != null) btnEditSpeichern.clicked += SpeichereBearbeitetenKunden;

        // Event für den lokalen Nutzer-Button
        if (btnEditLocal != null)
        {
            btnEditLocal.clicked += () =>
            {
                // Da der lokale Nutzer keine echten Listendaten hat, öffnen wir das Popup 
                // testweise mit einem leeren Dummy-Kunden oder blenden einfach die Maske ein.
                aktuellBearbeiteterKunde = null; 
                ClearEditInputs();
                SetElementVisible(popupBearbeiten, true);
            };
        }
    }

    private void SetElementVisible(VisualElement element, bool visible)
    {
        if (element != null)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.position = visible ? Position.Absolute : Position.Relative;
        }
    }

    private void GeneriereTestDaten()
    {
        kundenListe.Add(new KundeData { id = Guid.NewGuid().ToString(), vorname = "Max", nachname = "Mustermann", firma = "Mustert GmbH", strasse = "Musterstr. 8", plz = "12345", ort = "Musterstadt", email = "muster@email.de", telefon = "0162-12345" });
        kundenListe.Add(new KundeData { id = Guid.NewGuid().ToString(), vorname = "Erika", nachname = "Musterfrau", firma = "Fraunhofer AG", strasse = "Technikweg 4", plz = "54321", ort = "Techcity", email = "erika@fraunhofer.de", telefon = "0175-98765" });
    }

    private void RefreshKundenListe()
    {
        if (lblCounter != null)
        {
            if (kundenListe.Count == 1)
                lblCounter.text = "1 Eintrag";
            else
                lblCounter.text = $"{kundenListe.Count} Einträge";
        }

        VisualElement zielContainer = kundenListeHolder != null ? kundenListeHolder : (VisualElement)kundenContainer;
        if (zielContainer == null || kundenZeileTemplate == null) return;

        zielContainer.Clear();

        foreach (var kunde in kundenListe)
        {
            VisualElement neueKarte = kundenZeileTemplate.Instantiate();

            var nameLabel = neueKarte.Q<Label>("lbl-name");
            var firmaLabel = neueKarte.Q<Label>("lbl-firma");
            var emailLabel = neueKarte.Q<Label>("lbl-email");
            var telefonLabel = neueKarte.Q<Label>("lbl-number");
            var adresseLabel = neueKarte.Q<Label>("lbl-adress");

            var btnAendern = neueKarte.Q<Button>("btn-edit");
            var btnLoeschen = neueKarte.Q<Button>("btn-delete");

            if (nameLabel != null) nameLabel.text = $"{kunde.vorname} {kunde.nachname}";
            if (firmaLabel != null) firmaLabel.text = kunde.firma;
            if (emailLabel != null) emailLabel.text = kunde.email;
            if (telefonLabel != null) telefonLabel.text = kunde.telefon;
            if (adresseLabel != null) adresseLabel.text = $"{kunde.strasse}, {kunde.plz} {kunde.ort}";

            if (btnAendern != null) btnAendern.clicked += () => OeffneBearbeitenPopup(kunde);
            if (btnLoeschen != null) btnLoeschen.clicked += () => LoescheKunde(kunde);

            zielContainer.Add(neueKarte);
        }
    }

    private void SpeichereNeuenKunden()
    {
        KundeData neuerKunde = new KundeData
        {
            id = Guid.NewGuid().ToString(),
            vorname = inputCreateVorname != null ? inputCreateVorname.value : "",
            nachname = inputCreateNachname != null ? inputCreateNachname.value : "",
            firma = inputCreateFirma != null ? inputCreateFirma.value : "",
            strasse = inputCreateStrasse != null ? inputCreateStrasse.value : "",
            plz = inputCreatePlz != null ? inputCreatePlz.value : "",
            ort = inputCreateOrt != null ? inputCreateOrt.value : "",
            email = inputCreateEmail != null ? inputCreateEmail.value : "",
            telefon = inputCreateTelefon != null ? inputCreateTelefon.value : ""
        };

        kundenListe.Add(neuerKunde);
        SetElementVisible(popupErstellen, false);
        
        RefreshKundenListe(); 
        ClearCreateInputs();
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
        if (inputEditTelefon != null) inputEditTelefon.value = kunde.telefon;

        SetElementVisible(popupBearbeiten, true);
    }

    private void SpeichereBearbeitetenKunden()
    {
        // Falls aktuellBearbeiteterKunde null ist (weil wir über den lokalen Nutzer kamen), 
        // schließen wir einfach nur das Popup ohne die Liste zu verändern.
        if (aktuellBearbeiteterKunde == null)
        {
            SetElementVisible(popupBearbeiten, false);
            return;
        }

        if (inputEditVorname != null) aktuellBearbeiteterKunde.vorname = inputEditVorname.value;
        if (inputEditNachname != null) aktuellBearbeiteterKunde.nachname = inputEditNachname.value;
        if (inputEditFirma != null) aktuellBearbeiteterKunde.firma = inputEditFirma.value;
        if (inputEditStrasse != null) aktuellBearbeiteterKunde.strasse = inputEditStrasse.value;
        if (inputEditPlz != null) aktuellBearbeiteterKunde.plz = inputEditPlz.value;
        if (inputEditOrt != null) aktuellBearbeiteterKunde.ort = inputEditOrt.value;
        if (inputEditEmail != null) aktuellBearbeiteterKunde.email = inputEditEmail.value;
        if (inputEditTelefon != null) aktuellBearbeiteterKunde.telefon = inputEditTelefon.value;

        SetElementVisible(popupBearbeiten, false);
        RefreshKundenListe();
    }

    private void LoescheKunde(KundeData kunde)
    {
        kundenListe.Remove(kunde);
        RefreshKundenListe();
    }

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
    }
}

[System.Serializable]
public class KundeData
{
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