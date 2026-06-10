// EinstellungenController.cs
// Auf UIManager in der Einstellungen-Scene

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class EinstellungenController : MonoBehaviour
{
    AuthService authService;
    [SerializeField] private UIDocument            uiDocument;
    [SerializeField] private PassKeyAuthController passKeyAuthController;
    [SerializeField] private MainLogoutController  mainLogoutController;

    // ═══════════════════════════════════════════════════════════
    // PLAYERPREFS KEYS
    // ═══════════════════════════════════════════════════════════
    private const string PREF_STEUERSATZ     = "settings_steuersatz";
    private const string PREF_DARK_MODE      = "settings_dark_mode";
    private const string PREF_BEGLEITER      = "settings_begleiter";
    private const string PREF_RECHNR_PRAEFIX = "settings_rechnr_praefix";
    private const string PREF_STARTNUMMER    = "settings_startnummer";
    private const string PREF_ZAHLUNGSZIEL   = "settings_zahlungsziel";
    private const string PREF_WAEHRUNG       = "settings_waehrung";
    private const string PREF_DATUMSFORMAT   = "settings_datumsformat";
    private const string PREF_ZAHLUNGSHINWEIS= "settings_zahlungshinweis";
    private const string PREF_IBAN           = "settings_iban";
    private const string PREF_BIC            = "settings_bic";
    private const string PREF_KONTOINHABER   = "settings_kontoinhaber";
    private const string PREF_KREDITINSTITUT = "settings_kreditinstitut";
    private const string PREF_LOGO_RECHNUNG  = "settings_logo_rechnung";
    private const string PREF_SEITENZAHL     = "settings_seitenzahl";
    private const string PREF_EXPORTPFAD     = "settings_exportpfad";
    private const string PREF_UST_RECHNUNG   = "settings_ust_rechnung";
    private const string PREF_AUTO_NUMMER    = "settings_auto_nummer";
    private const string PREF_IBAN_RECHNUNG  = "settings_iban_rechnung";
    private const string PREF_STEUERNUMMER   = "settings_steuernummer";
    private const string PREF_USTIDNR        = "settings_ustidnr";
    private const string PREF_HANDELSREG     = "settings_handelsreg";
    private const string PREF_GRUENDUNGSJAHR = "settings_gruendungsjahr";

    // ═══════════════════════════════════════════════════════════
    // UI ELEMENTE
    // ═══════════════════════════════════════════════════════════

    private VisualElement _root;

    // Unternehmen
    private TextField     _inputFirmenname;
    private DropdownField _dropdownRechtsform;
    private TextField     _inputGruendungsjahr;
    private TextField     _inputSteuernummer;
    private TextField     _inputUstidnr;
    private TextField     _inputHandelsreg;
    private TextField     _inputStrasse;
    private TextField     _inputPlz;
    private TextField     _inputStadt;

    // Rechnungsformat
    private TextField     _inputRechnrPraefix;
    private TextField     _inputStartnummer;
    private TextField     _inputZahlungsziel;
    private DropdownField _dropdownWaehrung;
    private DropdownField _dropdownDatumsformat;
    private Toggle        _toggleUstRechnung;
    private Toggle        _toggleAutoNummer;
    private TextField     _inputZahlungshinweis;

    // Bank
    private TextField     _inputKontoinhaber;
    private TextField     _inputIban;
    private TextField     _inputBic;
    private TextField     _inputKreditinstitut;
    private Toggle        _toggleIbanRechnung;

    // PDF Export
    private Toggle        _toggleLogo;
    private Toggle        _toggleSeitenzahl;
    private Toggle        _toggleExportpfad;

    // Steuersatz
    private Button        _btnSteuer7;
    private Button        _btnSteuer10;
    private Button        _btnSteuer19;
    private int           _selectedSteuersatz = 19;

    // Layout
    private Button        _btnLightMode;
    private Button        _btnDarkMode;
    private Toggle        _toggleBegleiter;

    // Aktions-Buttons
    private Button        _btnSave;
    private Button        _btnReset;
    private Button        _btnResetPasskey;
    private TextField     _inputSuperkeyReset;
    private Button        _btnDeleteProfile;

    // Bestaetigungsdialog Profil loeschen
    private VisualElement _dialogOverlay;
    private TextField     _inputSuperkey1;
    private TextField     _inputSuperkey2;
    private Button        _btnDialogCancel;
    private Button        _btnDialogConfirm;

    // Popup: Neuer Passkey
    private VisualElement _popupNewPasskey;
    private Label         _labelNewPasskey;
    private Label         _labelNewPasskeyPlain;
    private Button        _btnClosePasskeyPopup;

    private VisualElement _popupGespeichert;
    private Label         _labelGespeichertText;
    private Button        _btnCloseGespeichert;

    // Aktuelle Firma aus Backend
    private Company _currentCompany = null;

    // Rechtsform Magic Numbers laut Dokumentation
    // 0=GmbH, 1=KG, 2=AG, 3=OHG, 4=GbR, 5=UG, 6=Einzelunternehmen, 7=GmbH & Co. KG, 8=eG
    private readonly List<string> _rechtsformOptions = new List<string>
    {
        "GmbH", "KG", "AG", "OHG", "GbR",
        "UG (haftungsbeschränkt)", "Einzelunternehmen", "GmbH & Co. KG", "eG"
    };

    // Farben
    private static readonly Color COLOR_GREEN    = new Color(128f/255f, 207f/255f, 149f/255f);
    private static readonly Color COLOR_INACTIVE = new Color(50f/255f,  50f/255f,  50f/255f);

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    void OnEnable()
    {

        // Popup auf Root-Ebene verschieben damit position: absolute korrekt wirkt
        if (_popupGespeichert != null)
{
        _popupGespeichert.RemoveFromHierarchy();
        _root.Add(_popupGespeichert);
        _popupGespeichert.style.display = DisplayStyle.None;
}
        authService = new AuthService();

    if (passKeyAuthController == null)
        passKeyAuthController = FindAnyObjectByType<PassKeyAuthController>();

        if (uiDocument           == null) uiDocument           = GetComponent<UIDocument>();
        if (passKeyAuthController == null) passKeyAuthController = FindAnyObjectByType<PassKeyAuthController>();
        if (mainLogoutController  == null) mainLogoutController  = FindAnyObjectByType<MainLogoutController>();

        _root = uiDocument.rootVisualElement;

        QueryAllElements();
        SetupDropdowns();
        RegisterButtons();
        LoadSettings();
    }

    // ═══════════════════════════════════════════════════════════
    // ELEMENT QUERIES
    // ═══════════════════════════════════════════════════════════

    private void QueryAllElements()
    {
        _inputFirmenname      = _root.Q<TextField>("input-firmenname");
        _dropdownRechtsform   = _root.Q<DropdownField>("dropdown-rechtsform");
        _inputGruendungsjahr  = _root.Q<TextField>("input-gruendungsjahr");
        _inputSteuernummer    = _root.Q<TextField>("input-steuernummer");
        _inputUstidnr         = _root.Q<TextField>("input-ustidnr");
        _inputHandelsreg      = _root.Q<TextField>("input-handelsreg");
        _inputStrasse         = _root.Q<TextField>("input-strasse");
        _inputPlz             = _root.Q<TextField>("input-plz");
        _inputStadt           = _root.Q<TextField>("input-stadt");

        _inputRechnrPraefix   = _root.Q<TextField>("input-rechnr-praefix");
        _inputStartnummer     = _root.Q<TextField>("input-startnummer");
        _inputZahlungsziel    = _root.Q<TextField>("input-zahlungsziel");
        _dropdownWaehrung     = _root.Q<DropdownField>("dropdown-waehrung");
        _dropdownDatumsformat = _root.Q<DropdownField>("dropdown-datumsformat");
        _toggleUstRechnung    = _root.Q<Toggle>("toggle-ust-rechnung");
        _toggleAutoNummer     = _root.Q<Toggle>("toggle-auto-nummer");
        _inputZahlungshinweis = _root.Q<TextField>("input-zahlungshinweis");

        _inputKontoinhaber    = _root.Q<TextField>("input-kontoinhaber");
        _inputIban            = _root.Q<TextField>("input-iban");
        _inputBic             = _root.Q<TextField>("input-bic");
        _inputKreditinstitut  = _root.Q<TextField>("input-kreditinstitut");
        _toggleIbanRechnung   = _root.Q<Toggle>("toggle-iban-rechnung");

        _toggleLogo           = _root.Q<Toggle>("toggle-logo");
        _toggleSeitenzahl     = _root.Q<Toggle>("toggle-seitenzahl");
        _toggleExportpfad     = _root.Q<Toggle>("toggle-exportpfad");

        _btnSteuer7           = _root.Q<Button>("btn-steuer-7");
        _btnSteuer10          = _root.Q<Button>("btn-steuer-10");
        _btnSteuer19          = _root.Q<Button>("btn-steuer-19");

        _btnLightMode         = _root.Q<Button>("btn-light-mode");
        _btnDarkMode          = _root.Q<Button>("btn-dark-mode");
        _toggleBegleiter      = _root.Q<Toggle>("toggle-begleiter");

        _btnSave              = _root.Q<Button>("btn-save");
        _btnReset             = _root.Q<Button>("btn-reset");
        _btnResetPasskey      = _root.Q<Button>("btn-reset-passkey");
        _inputSuperkeyReset   = _root.Q<TextField>("input-superkey-reset");
        _btnDeleteProfile     = _root.Q<Button>("btn-delete-profile");

        _dialogOverlay        = _root.Q<VisualElement>("dialog-overlay");
        _inputSuperkey1       = _root.Q<TextField>("input-superkey-1");
        _inputSuperkey2       = _root.Q<TextField>("input-superkey-2");
        _btnDialogCancel      = _root.Q<Button>("btn-dialog-cancel");
        _btnDialogConfirm     = _root.Q<Button>("btn-dialog-confirm");

        // Popup: Neuer Passkey
        _popupNewPasskey      = _root.Q<VisualElement>("popup-new-passkey");
        _labelNewPasskey      = _root.Q<Label>("label-new-passkey");
        _labelNewPasskeyPlain = _root.Q<Label>("label-new-passkey-plain");
        _btnClosePasskeyPopup = _root.Q<Button>("btn-close-passkey-popup");

        _popupGespeichert     = _root.Q<VisualElement>("popup-gespeichert");
        _labelGespeichertText = _root.Q<Label>("label-gespeichert-text");
        _btnCloseGespeichert  = _root.Q<Button>("btn-close-gespeichert");

        Debug.Log($"[Einstellungen] popup-new-passkey: {(_popupNewPasskey != null ? "gefunden" : "NULL – in UXML pruefen")}");
    }

    // ═══════════════════════════════════════════════════════════
    // DROPDOWN SETUP
    // ═══════════════════════════════════════════════════════════

    private void SetupDropdowns()
    {
        if (_dropdownRechtsform   != null) _dropdownRechtsform.choices   = _rechtsformOptions;
        if (_dropdownWaehrung     != null) _dropdownWaehrung.choices     = new List<string> { "Euro €", "Dollar $", "Pfund £", "Franken CHF" };
        if (_dropdownDatumsformat != null) _dropdownDatumsformat.choices = new List<string> { "DD.MM.YYYY", "MM/DD/YYYY", "YYYY-MM-DD" };
    }

    // ═══════════════════════════════════════════════════════════
    // BUTTON REGISTRIERUNG
    // ═══════════════════════════════════════════════════════════

    private void RegisterButtons()
    {
     if (_btnSave != null)
    _btnSave.clicked += () =>
    {
        SaveSettings();
        ShowGespeichertPopup();
    };

        if (_btnCloseGespeichert != null)
        _btnCloseGespeichert.clicked += () =>
    {
        if (_popupGespeichert != null)
            _popupGespeichert.style.display = DisplayStyle.None;
    };
        if (_btnReset != null) _btnReset.clicked += LoadSettings;

        // Popup: Neuer Passkey schliessen
        if (_btnClosePasskeyPopup != null)
        _btnClosePasskeyPopup.clicked += () =>
        {
        if (_popupNewPasskey != null)
            _popupNewPasskey.style.display = DisplayStyle.None;
    };
        // Steuersatz Radio-Buttons
        if (_btnSteuer7  != null) _btnSteuer7.clicked  += () => SelectSteuersatz(7);
        if (_btnSteuer10 != null) _btnSteuer10.clicked += () => SelectSteuersatz(10);
        if (_btnSteuer19 != null) _btnSteuer19.clicked += () => SelectSteuersatz(19);

        // Dark/Light Mode
        if (_btnLightMode != null) _btnLightMode.clicked += () => SelectMode(false);
        if (_btnDarkMode  != null) _btnDarkMode.clicked  += () => SelectMode(true);

        // Passkey zuruecksetzen – Backend: PassKeyAuthController.resetPassKey()
        // Laut Doku: RecoveryKey wird eingegeben, neuer PassKey wird erzeugt
        // Passkey zuruecksetzen
// Backend: resetPassKeyWithRecoveryKey(string enteredRecoveryKey) gibt neuen Passkey zurueck
if (_btnResetPasskey != null)
    _btnResetPasskey.clicked += () =>
    {
        string recoveryKey = _inputSuperkeyReset?.value?.Trim() ?? "";

        if (string.IsNullOrEmpty(recoveryKey))
        {
            Debug.LogWarning("[Einstellungen] Recovery Key fehlt.");
            return;
        }

        if (recoveryKey.Length != 16)
        {
            Debug.LogWarning("[Einstellungen] Recovery Key muss 16-stellig sein.");
            return;
        }

        if (passKeyAuthController == null)
        {
            Debug.LogWarning("[Einstellungen] PassKeyAuthController nicht gefunden.");
            return;
        }

        // Backend: neuen Passkey per Recovery Key abrufen
        string newPasskey = authService.resetPassKeyWithRecoveryKey(recoveryKey);

        if (!string.IsNullOrEmpty(newPasskey))
        {
            ShowNewPasskeyPopup(newPasskey);
            if (_inputSuperkeyReset != null) _inputSuperkeyReset.value = "";
            Debug.Log("[Einstellungen] Passkey erfolgreich zurueckgesetzt.");
        }
        else
            Debug.LogWarning("[Einstellungen] Kein neuer Passkey zurueckgegeben – Recovery Key korrekt?");
    };

        // Lokalprofil loeschen
        if (_btnDeleteProfile  != null) _btnDeleteProfile.clicked  += ShowDeleteDialog;
        if (_btnDialogCancel   != null) _btnDialogCancel.clicked   += HideDeleteDialog;
        if (_btnDialogConfirm  != null) _btnDialogConfirm.clicked  += ConfirmDeleteProfile;
    }

    // ═══════════════════════════════════════════════════════════
    // LADEN
    // ═══════════════════════════════════════════════════════════

    private void LoadSettings()
    {
        LoadVersionInfo();
        LoadCompanyData();
        LoadLocalSettings();
    }

    private void LoadVersionInfo()
    {
        var labelVersion = _root.Q<Label>("label-version");
        if (labelVersion != null)
        {
            try
            {
                string v = "1.0.0";
                labelVersion.text = $"Version {v}";
            }
            catch
            {
                labelVersion.text = "Version –";
            }
        }
    }

    private void LoadCompanyData()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) { Debug.LogWarning("[Einstellungen] Keine aktive NutzerDB."); return; }

        var companies = db.getAllCompanies();
        if (companies == null || companies.Count == 0) { Debug.Log("[Einstellungen] Keine Firma gefunden."); return; }

        _currentCompany = companies[0];

        if (_inputFirmenname    != null) _inputFirmenname.value    = _currentCompany.name     ?? "";
        if (_dropdownRechtsform != null) _dropdownRechtsform.index = _currentCompany.legalForm;
        if (_inputStadt         != null) _inputStadt.value         = _currentCompany.location ?? "";

        Debug.Log($"[Einstellungen] Firma geladen: {_currentCompany.name}");
    }

    private void LoadLocalSettings()
    {
        SetField(_inputRechnrPraefix,   PREF_RECHNR_PRAEFIX,  "RE-");
        SetField(_inputStartnummer,     PREF_STARTNUMMER,     "1");
        SetField(_inputZahlungsziel,    PREF_ZAHLUNGSZIEL,    "14");
        SetField(_inputZahlungshinweis, PREF_ZAHLUNGSHINWEIS, "Bitte überweisen Sie den Betrag auf ...");

        if (_dropdownWaehrung     != null) _dropdownWaehrung.index     = PlayerPrefs.GetInt(PREF_WAEHRUNG,     0);
        if (_dropdownDatumsformat != null) _dropdownDatumsformat.index = PlayerPrefs.GetInt(PREF_DATUMSFORMAT, 0);
        if (_toggleUstRechnung    != null) _toggleUstRechnung.value    = PlayerPrefs.GetInt(PREF_UST_RECHNUNG, 1) == 1;
        if (_toggleAutoNummer     != null) _toggleAutoNummer.value     = PlayerPrefs.GetInt(PREF_AUTO_NUMMER,  0) == 1;

        SetField(_inputSteuernummer,   PREF_STEUERNUMMER,   "");
        SetField(_inputUstidnr,        PREF_USTIDNR,        "");
        SetField(_inputHandelsreg,     PREF_HANDELSREG,     "");
        SetField(_inputGruendungsjahr, PREF_GRUENDUNGSJAHR, "");
        SetField(_inputStrasse,        "settings_strasse",  "");
        SetField(_inputPlz,            "settings_plz",      "");

        SetField(_inputKontoinhaber,   PREF_KONTOINHABER,   "");
        SetField(_inputIban,           PREF_IBAN,           "");
        SetField(_inputBic,            PREF_BIC,            "");
        SetField(_inputKreditinstitut, PREF_KREDITINSTITUT, "");
        if (_toggleIbanRechnung != null) _toggleIbanRechnung.value = PlayerPrefs.GetInt(PREF_IBAN_RECHNUNG, 0) == 1;

        if (_toggleLogo       != null) _toggleLogo.value       = PlayerPrefs.GetInt(PREF_LOGO_RECHNUNG, 1) == 1;
        if (_toggleSeitenzahl != null) _toggleSeitenzahl.value = PlayerPrefs.GetInt(PREF_SEITENZAHL,    1) == 1;
        if (_toggleExportpfad != null) _toggleExportpfad.value = PlayerPrefs.GetInt(PREF_EXPORTPFAD,    1) == 1;

        _selectedSteuersatz = PlayerPrefs.GetInt(PREF_STEUERSATZ, 19);
        UpdateSteuersatzButtons();

        bool isDark = PlayerPrefs.GetInt(PREF_DARK_MODE, 1) == 1;
        UpdateModeButtons(isDark);

        if (_toggleBegleiter != null) _toggleBegleiter.value = PlayerPrefs.GetInt(PREF_BEGLEITER, 1) == 1;
    }

    // ═══════════════════════════════════════════════════════════
    // SPEICHERN
    // ═══════════════════════════════════════════════════════════

    private void SaveSettings()
    {
        SaveCompanyData();
        SaveLocalSettings();
        Debug.Log("[Einstellungen] Alle Einstellungen gespeichert.");
    }

    private void SaveCompanyData()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) { Debug.LogWarning("[Einstellungen] Keine aktive NutzerDB."); return; }

        string name      = _inputFirmenname?.value ?? "";
        int    legalForm = _dropdownRechtsform?.index ?? 0;
        string location  = _inputStadt?.value ?? "";
        int    industry  = 3;
        string steuerNr  = _inputSteuernummer?.value ?? "";
        string ustIdNr   = _inputUstidnr?.value ?? "";
        string handelsReg = _inputHandelsreg?.value ?? "";
        string gruendungsJahr = _inputGruendungsjahr?.value ?? "";
        string plz = _inputPlz?.value ?? "";
        string strasseuHausNr = _inputStrasse?.value ?? "";

        if (_currentCompany == null)
        {
            db.createCompany( name, legalForm, industry, location, steuerNr,gruendungsJahr ,handelsReg , strasseuHausNr , plz , ustIdNr);
            Debug.Log($"[Einstellungen] Neue Firma angelegt: {name}");
            var all = db.getAllCompanies();
            if (all != null && all.Count > 0) _currentCompany = all[all.Count - 1];
        }
        else
        {
            _currentCompany.name      = name;
            _currentCompany.legalForm = legalForm;
            _currentCompany.location  = location;
            _currentCompany.industry  = industry;
            _currentCompany.steuerNr  = steuerNr;
            _currentCompany.ustIdNr   = ustIdNr;
            _currentCompany.handelsReg = handelsReg;
            _currentCompany.gruendungsJahr = gruendungsJahr;
            _currentCompany.plz = plz;
            _currentCompany.strasseuHausNr = strasseuHausNr;
            db.updateCompany(_currentCompany);
            Debug.Log($"[Einstellungen] Firma aktualisiert: {name}");
        }
    }

    private void SaveLocalSettings()
    {
        SaveField(PREF_RECHNR_PRAEFIX,  _inputRechnrPraefix);
        SaveField(PREF_STARTNUMMER,     _inputStartnummer);
        SaveField(PREF_ZAHLUNGSZIEL,    _inputZahlungsziel);
        SaveField(PREF_ZAHLUNGSHINWEIS, _inputZahlungshinweis);

        if (_dropdownWaehrung     != null) PlayerPrefs.SetInt(PREF_WAEHRUNG,     _dropdownWaehrung.index);
        if (_dropdownDatumsformat != null) PlayerPrefs.SetInt(PREF_DATUMSFORMAT, _dropdownDatumsformat.index);
        if (_toggleUstRechnung    != null) PlayerPrefs.SetInt(PREF_UST_RECHNUNG, _toggleUstRechnung.value ? 1 : 0);
        if (_toggleAutoNummer     != null) PlayerPrefs.SetInt(PREF_AUTO_NUMMER,  _toggleAutoNummer.value  ? 1 : 0);

        SaveField(PREF_STEUERNUMMER,   _inputSteuernummer);
        SaveField(PREF_USTIDNR,        _inputUstidnr);
        SaveField(PREF_HANDELSREG,     _inputHandelsreg);
        SaveField(PREF_GRUENDUNGSJAHR, _inputGruendungsjahr);
        SaveField("settings_strasse",  _inputStrasse);
        SaveField("settings_plz",      _inputPlz);

        SaveField(PREF_KONTOINHABER,   _inputKontoinhaber);
        SaveField(PREF_IBAN,           _inputIban);
        SaveField(PREF_BIC,            _inputBic);
        SaveField(PREF_KREDITINSTITUT, _inputKreditinstitut);
        if (_toggleIbanRechnung != null) PlayerPrefs.SetInt(PREF_IBAN_RECHNUNG, _toggleIbanRechnung.value ? 1 : 0);

        if (_toggleLogo       != null) PlayerPrefs.SetInt(PREF_LOGO_RECHNUNG, _toggleLogo.value       ? 1 : 0);
        if (_toggleSeitenzahl != null) PlayerPrefs.SetInt(PREF_SEITENZAHL,    _toggleSeitenzahl.value ? 1 : 0);
        if (_toggleExportpfad != null) PlayerPrefs.SetInt(PREF_EXPORTPFAD,    _toggleExportpfad.value ? 1 : 0);

        PlayerPrefs.SetInt(PREF_STEUERSATZ, _selectedSteuersatz);
        if (_toggleBegleiter != null) PlayerPrefs.SetInt(PREF_BEGLEITER, _toggleBegleiter.value ? 1 : 0);

        PlayerPrefs.Save();
    }

    // ═══════════════════════════════════════════════════════════
    // STEUERSATZ RADIO-BUTTONS
    // ═══════════════════════════════════════════════════════════

    private void SelectSteuersatz(int satz)
    {
        _selectedSteuersatz = satz;
        UpdateSteuersatzButtons();
    }

    private void UpdateSteuersatzButtons()
    {
        if (_btnSteuer7  != null) _btnSteuer7.style.backgroundColor  = _selectedSteuersatz ==  7 ? COLOR_GREEN : COLOR_INACTIVE;
        if (_btnSteuer10 != null) _btnSteuer10.style.backgroundColor = _selectedSteuersatz == 10 ? COLOR_GREEN : COLOR_INACTIVE;
        if (_btnSteuer19 != null) _btnSteuer19.style.backgroundColor = _selectedSteuersatz == 19 ? COLOR_GREEN : COLOR_INACTIVE;
    }

    // ═══════════════════════════════════════════════════════════
    // DARK / LIGHT MODE
    // ═══════════════════════════════════════════════════════════

    private void SelectMode(bool isDark)
    {
        PlayerPrefs.SetInt(PREF_DARK_MODE, isDark ? 1 : 0);
        PlayerPrefs.Save();
        UpdateModeButtons(isDark);
    }

    private void UpdateModeButtons(bool isDark)
    {
        if (_btnLightMode != null) _btnLightMode.style.backgroundColor = isDark ? COLOR_INACTIVE : COLOR_GREEN;
        if (_btnDarkMode  != null) _btnDarkMode.style.backgroundColor  = isDark ? COLOR_GREEN    : COLOR_INACTIVE;
    }

    // ═══════════════════════════════════════════════════════════
    // POPUP: NEUER PASSKEY
    // ═══════════════════════════════════════════════════════════

    private void ShowNewPasskeyPopup(string passkey)
    {
        if (_labelNewPasskey != null)
        {
            string display = "";
            foreach (char c in passkey)
                display += c + "  ";
            _labelNewPasskey.text = display.TrimEnd();
        }

        if (_labelNewPasskeyPlain != null)
            _labelNewPasskeyPlain.text = $"Dein neuer Passkey: {passkey}";

        if (_popupNewPasskey != null)
            _popupNewPasskey.style.display = DisplayStyle.Flex;
        else
            Debug.LogWarning("[Einstellungen] popup-new-passkey nicht gefunden – in UXML pruefen.");
    }

    // ═══════════════════════════════════════════════════════════
    // LOKALPROFIL LOESCHEN
    // ═══════════════════════════════════════════════════════════

    private void ShowDeleteDialog()
    {
        if (_inputSuperkey1 != null) _inputSuperkey1.value = "";
        if (_inputSuperkey2 != null) _inputSuperkey2.value = "";
        if (_dialogOverlay  != null) _dialogOverlay.style.display = DisplayStyle.Flex;
    }

    private void HideDeleteDialog()
    {
        if (_dialogOverlay != null) _dialogOverlay.style.display = DisplayStyle.None;
    }

    private void ConfirmDeleteProfile()
    {
        string key1 = _inputSuperkey1?.value?.Trim() ?? "";
        string key2 = _inputSuperkey2?.value?.Trim() ?? "";

        if (string.IsNullOrEmpty(key1) || string.IsNullOrEmpty(key2))
        {
            Debug.LogWarning("[Einstellungen] Super-Passkey fehlt.");
            return;
        }

        if (key1 != key2)
        {
            Debug.LogWarning("[Einstellungen] Super-Passkeys stimmen nicht ueberein.");
            return;
        }

        if (mainLogoutController != null)
            mainLogoutController.logout();
        else
            Debug.LogWarning("[Einstellungen] MainLogoutController nicht gefunden.");

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("[Einstellungen] Lokalprofil geloescht – lade Login-Scene.");
        SceneManager.LoadScene(0);
    }


        private void ShowGespeichertPopup()
{
    // Szenenname erkennen und Nachricht anpassen
    string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

    // Szenenname lesbar machen – z.B. "Einstellungen" bleibt "Einstellungen"
    if (_labelGespeichertText != null)
        _labelGespeichertText.text = $"{sceneName} wurden gespeichert";

    if (_popupGespeichert != null)
        _popupGespeichert.style.display = DisplayStyle.Flex;
}
    // ═══════════════════════════════════════════════════════════
    // HILFSFUNKTIONEN
    // ═══════════════════════════════════════════════════════════

    private void SetField(TextField field, string key, string fallback)
    {
        if (field != null) field.value = PlayerPrefs.GetString(key, fallback);
    }

    private void SaveField(string key, TextField field)
    {
        if (field != null) PlayerPrefs.SetString(key, field.value);
    }
}