// EinstellungenController.cs
// Auf UIManager in der Einstellungen-Scene platzieren

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class EinstellungenController : MonoBehaviour
{
    private AuthService authService;

    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PassKeyAuthController passKeyAuthController;
    [SerializeField] private MainLogoutController mainLogoutController;

    // ═══════════════════════════════════════════════════════════
    // PLAYERPREFS KEYS
    // ═══════════════════════════════════════════════════════════
    private const string PREF_STEUERSATZ = "settings_steuersatz";
    private const string PREF_STEUER_CUSTOM_AKTIV = "settings_steuer_custom_aktiv";
    private const string PREF_STEUER_CUSTOM_WERT = "settings_steuer_custom_wert";
    private const string PREF_DARK_MODE = "settings_dark_mode";
    private const string PREF_BEGLEITER = "settings_begleiter";
    private const string PREF_RECHNR_PRAEFIX = "settings_rechnr_praefix";
    private const string PREF_STARTNUMMER = "settings_startnummer";
    private const string PREF_ZAHLUNGSZIEL = "settings_zahlungsziel";
    private const string PREF_WAEHRUNG = "settings_waehrung";
    private const string PREF_DATUMSFORMAT = "settings_datumsformat";
    private const string PREF_ZAHLUNGSHINWEIS = "settings_zahlungshinweis";
    private const string PREF_IBAN = "settings_iban";
    private const string PREF_BIC = "settings_bic";
    private const string PREF_KONTOINHABER = "settings_kontoinhaber";
    private const string PREF_KREDITINSTITUT = "settings_kreditinstitut";
    private const string PREF_LOGO_RECHNUNG = "settings_logo_rechnung";
    private const string PREF_SEITENZAHL = "settings_seitenzahl";
    private const string PREF_EXPORTPFAD = "settings_exportpfad";
    private const string PREF_UST_RECHNUNG = "settings_ust_rechnung";
    private const string PREF_AUTO_NUMMER = "settings_auto_nummer";
    private const string PREF_IBAN_RECHNUNG = "settings_iban_rechnung";
    private const string PREF_STEUERNUMMER = "settings_steuernummer";
    private const string PREF_USTIDNR = "settings_ustidnr";
    private const string PREF_HANDELSREG = "settings_handelsreg";
    private const string PREF_GRUENDUNGSJAHR = "settings_gruendungsjahr";
    private const string PREF_AGB = "settings_agb";
    private const string PREF_DISCLAIMER = "settings_disclaimer";
    private const string PREF_BARZAHLUNG = "settings_barzahlung";
    private const string PREF_UEBERWEISUNG = "settings_ueberweisung";

    // ═══════════════════════════════════════════════════════════
    // UI ELEMENTE
    // ═══════════════════════════════════════════════════════════
    private VisualElement _root;

    private Toggle _toggleLogo;
    private Toggle _toggleSeitenzahl;
    private Toggle _toggleExportpfad;

    private Button _btnSteuer7;
    private Button _btnSteuer10;
    private Button _btnSteuer19;
    private Button _btnSteuerCustom;
    private VisualElement _containerSteuerCustom;
    private TextField _inputSteuerCustom;
    private int _selectedSteuersatz = 19;
    private bool _customSteuersatz = false;

    private Button _btnLightMode;
    private Button _btnDarkMode;
    private Toggle _toggleBegleiter;

    private TextField _inputSuperkeyReset;
    private Button _btnResetPasskey;
    private Button _btnDeleteProfile;

    private Button _btnSave;
    private Button _btnReset;

    private Label _labelVersion;
    private Button _btnUpdate;
    private Button _btnOpenCredits;
    private Button _btnOpenMitwirkende;

    private Button _btnOpenUnternehmen;
    private Button _btnOpenBank;
    private Button _btnOpenRechnung;
    private Button _btnOpenBezahlweise;

    private VisualElement _popupUnternehmen;
    private TextField _inputFirmenname;
    private DropdownField _dropdownRechtsform;
    private TextField _inputGruendungsjahr;
    private TextField _inputSteuernummer;
    private TextField _inputUstidnr;
    private TextField _inputHandelsreg;
    private TextField _inputStrasse;
    private TextField _inputPlz;
    private TextField _inputStadt;
    private Button _btnCloseUnternehmen;
    private Button _btnCancelUnternehmen;
    private Button _btnSaveUnternehmen;

    private VisualElement _popupBank;
    private TextField _inputKontoinhaber;
    private TextField _inputIban;
    private TextField _inputBic;
    private TextField _inputKreditinstitut;
    private Toggle _toggleIbanRechnung;
    private Button _btnCloseBank;
    private Button _btnCancelBank;
    private Button _btnSaveBank;

    private VisualElement _popupRechnung;
    private TextField _inputRechnrPraefix;
    private TextField _inputStartnummer;
    private TextField _inputZahlungsziel;
    private DropdownField _dropdownWaehrung;
    private DropdownField _dropdownDatumsformat;
    private Toggle _toggleUstRechnung;
    private Toggle _toggleAutoNummer;
    private TextField _inputZahlungshinweis;
    private Button _btnCloseRechnung;
    private Button _btnCancelRechnung;
    private Button _btnSaveRechnung;

    private VisualElement _popupBezahlweise;
    private TextField _inputAgb;
    private TextField _inputDisclaimer;
    private TextField _inputBarzahlung;
    private TextField _inputUeberweisung;
    private Label _labelStatusAgb;
    private Label _labelStatusDisclaimer;
    private Label _labelStatusBar;
    private Label _labelStatusUeberweisung;
    private Button _btnCloseBezahlweise;
    private Button _btnCancelBezahlweise;
    private Button _btnSaveBezahlweise;

    private VisualElement _popupCredits;
    private Button _btnCloseCredits;
    private VisualElement _popupMitwirkende;
    private Button _btnCloseMitwirkende;

    private VisualElement _dialogOverlay;
    private TextField _inputSuperkey1;
    private TextField _inputSuperkey2;
    private Button _btnDialogCancel;
    private Button _btnDialogConfirm;

    private VisualElement _popupNewPasskey;
    private Label _labelNewPasskey;
    private Label _labelNewPasskeyPlain;
    private Button _btnClosePasskeyPopup;

    private VisualElement _popupGespeichert;
    private Label _labelGespeichertText;
    private Button _btnCloseGespeichert;

    private Company _currentCompany = null;

    private readonly List<string> _rechtsformOptions = new List<string>
    {
        "GmbH", "KG", "AG", "OHG", "GbR",
        "UG (haftungsbeschränkt)", "Einzelunternehmen", "GmbH & Co. KG", "eG"
    };

    private static readonly Color COLOR_GREEN = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color COLOR_INACTIVE = new Color(50f / 255f, 50f / 255f, 50f / 255f);

    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    void OnEnable()
    {
        authService = new AuthService();

        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (passKeyAuthController == null) passKeyAuthController = FindAnyObjectByType<PassKeyAuthController>();
        if (mainLogoutController == null) mainLogoutController = FindAnyObjectByType<MainLogoutController>();

        _root = uiDocument.rootVisualElement;

        QueryAllElements();
        SetupDropdowns();
        RegisterButtons();
        LoadSettings();

        _root.schedule.Execute(() =>
        {
            if (_popupUnternehmen == null || _popupGespeichert == null)
            {
                QueryAllElements();
                SetupDropdowns();
                RegisterButtons();
            }
        }).ExecuteLater(100);
    }

    // ═══════════════════════════════════════════════════════════
    // ELEMENT QUERIES
    // ═══════════════════════════════════════════════════════════

    private void QueryAllElements()
    {
        _btnSave = _root.Q<Button>("btn-save");
        _btnReset = _root.Q<Button>("btn-reset");
        _toggleLogo = _root.Q<Toggle>("toggle-logo");
        _toggleSeitenzahl = _root.Q<Toggle>("toggle-seitenzahl");
        _toggleExportpfad = _root.Q<Toggle>("toggle-exportpfad");
        _btnSteuer7 = _root.Q<Button>("btn-steuer-7");
        _btnSteuer10 = _root.Q<Button>("btn-steuer-10");
        _btnSteuer19 = _root.Q<Button>("btn-steuer-19");
        _btnSteuerCustom = _root.Q<Button>("btn-steuer-custom");
        _containerSteuerCustom = _root.Q<VisualElement>("container-steuer-custom");
        _inputSteuerCustom = _root.Q<TextField>("input-steuer-custom");
        _btnLightMode = _root.Q<Button>("btn-light-mode");
        _btnDarkMode = _root.Q<Button>("btn-dark-mode");
        _toggleBegleiter = _root.Q<Toggle>("toggle-begleiter");
        _inputSuperkeyReset = _root.Q<TextField>("input-superkey-reset");
        _btnResetPasskey = _root.Q<Button>("btn-reset-passkey");
        _btnDeleteProfile = _root.Q<Button>("btn-delete-profile");
        _labelVersion = _root.Q<Label>("label-version");
        _btnUpdate = _root.Q<Button>("btn-update");
        _btnOpenCredits = _root.Q<Button>("btn-open-credits");
        _btnOpenMitwirkende = _root.Q<Button>("btn-open-mitwirkende");
        _btnOpenUnternehmen = _root.Q<Button>("btn-open-unternehmen");
        _btnOpenBank = _root.Q<Button>("btn-open-bank");
        _btnOpenRechnung = _root.Q<Button>("btn-open-rechnung");
        _btnOpenBezahlweise = _root.Q<Button>("btn-open-bezahlweise");

        _popupUnternehmen = _root.Q<VisualElement>("popup-unternehmen");
        _inputFirmenname = _root.Q<TextField>("input-firmenname");
        _dropdownRechtsform = _root.Q<DropdownField>("dropdown-rechtsform");
        _inputGruendungsjahr = _root.Q<TextField>("input-gruendungsjahr");
        _inputSteuernummer = _root.Q<TextField>("input-steuernummer");
        _inputUstidnr = _root.Q<TextField>("input-ustidnr");
        _inputHandelsreg = _root.Q<TextField>("input-handelsreg");
        _inputStrasse = _root.Q<TextField>("input-strasse");
        _inputPlz = _root.Q<TextField>("input-plz");
        _inputStadt = _root.Q<TextField>("input-stadt");
        _btnCloseUnternehmen = _root.Q<Button>("btn-close-unternehmen");
        _btnCancelUnternehmen = _root.Q<Button>("btn-cancel-unternehmen");
        _btnSaveUnternehmen = _root.Q<Button>("btn-save-unternehmen");

        _popupBank = _root.Q<VisualElement>("popup-bank");
        _inputKontoinhaber = _root.Q<TextField>("input-kontoinhaber");
        _inputIban = _root.Q<TextField>("input-iban");
        _inputBic = _root.Q<TextField>("input-bic");
        _inputKreditinstitut = _root.Q<TextField>("input-kreditinstitut");
        _toggleIbanRechnung = _root.Q<Toggle>("toggle-iban-rechnung");
        _btnCloseBank = _root.Q<Button>("btn-close-bank");
        _btnCancelBank = _root.Q<Button>("btn-cancel-bank");
        _btnSaveBank = _root.Q<Button>("btn-save-bank");

        _popupRechnung = _root.Q<VisualElement>("popup-rechnung");
        _inputRechnrPraefix = _root.Q<TextField>("input-rechnr-praefix");
        _inputStartnummer = _root.Q<TextField>("input-startnummer");
        _inputZahlungsziel = _root.Q<TextField>("input-zahlungsziel");
        _dropdownWaehrung = _root.Q<DropdownField>("dropdown-waehrung");
        _dropdownDatumsformat = _root.Q<DropdownField>("dropdown-datumsformat");
        _toggleUstRechnung = _root.Q<Toggle>("toggle-ust-rechnung");
        _toggleAutoNummer = _root.Q<Toggle>("toggle-auto-nummer");
        _inputZahlungshinweis = _root.Q<TextField>("input-zahlungshinweis");
        _btnCloseRechnung = _root.Q<Button>("btn-close-rechnung");
        _btnCancelRechnung = _root.Q<Button>("btn-cancel-rechnung");
        _btnSaveRechnung = _root.Q<Button>("btn-save-rechnung");

        _popupBezahlweise = _root.Q<VisualElement>("popup-bezahlweise");
        _inputAgb = _root.Q<TextField>("input-agb");
        _inputDisclaimer = _root.Q<TextField>("input-disclaimer");
        _inputBarzahlung = _root.Q<TextField>("input-barzahlung");
        _inputUeberweisung = _root.Q<TextField>("input-ueberweisung");
        _labelStatusAgb = _root.Q<Label>("label-status-agb");
        _labelStatusDisclaimer = _root.Q<Label>("label-status-disclaimer");
        _labelStatusBar = _root.Q<Label>("label-status-bar");
        _labelStatusUeberweisung = _root.Q<Label>("label-status-ueberweisung");
        _btnCloseBezahlweise = _root.Q<Button>("btn-close-bezahlweise");
        _btnCancelBezahlweise = _root.Q<Button>("btn-cancel-bezahlweise");
        _btnSaveBezahlweise = _root.Q<Button>("btn-save-bezahlweise");

        _popupCredits = _root.Q<VisualElement>("popup-credits");
        _btnCloseCredits = _root.Q<Button>("btn-close-credits");
        _popupMitwirkende = _root.Q<VisualElement>("popup-mitwirkende");
        _btnCloseMitwirkende = _root.Q<Button>("btn-close-mitwirkende");

        _dialogOverlay = _root.Q<VisualElement>("dialog-overlay");
        _inputSuperkey1 = _root.Q<TextField>("input-superkey-1");
        _inputSuperkey2 = _root.Q<TextField>("input-superkey-2");
        _btnDialogCancel = _root.Q<Button>("btn-dialog-cancel");
        _btnDialogConfirm = _root.Q<Button>("btn-dialog-confirm");

        _popupNewPasskey = _root.Q<VisualElement>("popup-new-passkey");
        _labelNewPasskey = _root.Q<Label>("label-new-passkey");
        _labelNewPasskeyPlain = _root.Q<Label>("label-new-passkey-plain");
        _btnClosePasskeyPopup = _root.Q<Button>("btn-close-passkey-popup");

        _popupGespeichert = _root.Q<VisualElement>("popup-gespeichert");
        _labelGespeichertText = _root.Q<Label>("label-gespeichert-text");
        _btnCloseGespeichert = _root.Q<Button>("btn-close-gespeichert");
    }

    // ═══════════════════════════════════════════════════════════
    // DROPDOWN SETUP
    // ═══════════════════════════════════════════════════════════

    private void SetupDropdowns()
    {
        if (_dropdownRechtsform != null) _dropdownRechtsform.choices = _rechtsformOptions;
        if (_dropdownWaehrung != null) _dropdownWaehrung.choices = new List<string> { "Euro €", "Dollar $", "Pfund £", "Franken CHF" };
        if (_dropdownDatumsformat != null) _dropdownDatumsformat.choices = new List<string> { "DD.MM.YYYY", "MM/DD/YYYY", "YYYY-MM-DD" };
    }

    // ═══════════════════════════════════════════════════════════
    // BUTTON REGISTRIERUNG
    // ═══════════════════════════════════════════════════════════

    private void RegisterButtons()
    {
        if (_btnSave != null) _btnSave.clicked += () => { SaveSettings(); ShowGespeichertPopup(); };
        if (_btnReset != null) _btnReset.clicked += LoadSettings;

        if (_btnOpenUnternehmen != null) _btnOpenUnternehmen.clicked += () => ShowPopup(_popupUnternehmen);
        if (_btnOpenBank != null) _btnOpenBank.clicked += () => ShowPopup(_popupBank);
        if (_btnOpenRechnung != null) _btnOpenRechnung.clicked += () => ShowPopup(_popupRechnung);
        if (_btnOpenBezahlweise != null) _btnOpenBezahlweise.clicked += () => { LoadBezahlweiseStatus(); ShowPopup(_popupBezahlweise); };
        if (_btnOpenCredits != null) _btnOpenCredits.clicked += () => ShowPopup(_popupCredits);
        if (_btnOpenMitwirkende != null) _btnOpenMitwirkende.clicked += () => ShowPopup(_popupMitwirkende);

        if (_btnCloseUnternehmen != null) _btnCloseUnternehmen.clicked += () => HidePopup(_popupUnternehmen);
        if (_btnCancelUnternehmen != null) _btnCancelUnternehmen.clicked += () => HidePopup(_popupUnternehmen);
        if (_btnSaveUnternehmen != null) _btnSaveUnternehmen.clicked += SaveUnternehmenPopup;

        if (_btnCloseBank != null) _btnCloseBank.clicked += () => HidePopup(_popupBank);
        if (_btnCancelBank != null) _btnCancelBank.clicked += () => HidePopup(_popupBank);
        if (_btnSaveBank != null) _btnSaveBank.clicked += SaveBankPopup;

        if (_btnCloseRechnung != null) _btnCloseRechnung.clicked += () => HidePopup(_popupRechnung);
        if (_btnCancelRechnung != null) _btnCancelRechnung.clicked += () => HidePopup(_popupRechnung);
        if (_btnSaveRechnung != null) _btnSaveRechnung.clicked += SaveRechnungPopup;

        if (_btnCloseBezahlweise != null) _btnCloseBezahlweise.clicked += () => HidePopup(_popupBezahlweise);
        if (_btnCancelBezahlweise != null) _btnCancelBezahlweise.clicked += () => HidePopup(_popupBezahlweise);
        if (_btnSaveBezahlweise != null) _btnSaveBezahlweise.clicked += SaveBezahlweisePopup;

        if (_btnCloseCredits != null) _btnCloseCredits.clicked += () => HidePopup(_popupCredits);
        if (_btnCloseMitwirkende != null) _btnCloseMitwirkende.clicked += () => HidePopup(_popupMitwirkende);

        if (_btnSteuer7 != null) _btnSteuer7.clicked += () => SelectSteuersatz(7);
        if (_btnSteuer10 != null) _btnSteuer10.clicked += () => SelectSteuersatz(10);
        if (_btnSteuer19 != null) _btnSteuer19.clicked += () => SelectSteuersatz(19);
        if (_btnSteuerCustom != null) _btnSteuerCustom.clicked += SelectSteuersatzCustom;

        if (_btnLightMode != null) _btnLightMode.clicked += () => SelectMode(false);
        if (_btnDarkMode != null) _btnDarkMode.clicked += () => SelectMode(true);

        if (_btnResetPasskey != null)
            _btnResetPasskey.clicked += () =>
            {
                string recoveryKey = _inputSuperkeyReset?.value?.Trim() ?? "";
                if (string.IsNullOrEmpty(recoveryKey)) { Debug.LogWarning("[Einstellungen] Recovery Key fehlt."); return; }
                if (recoveryKey.Length != 16) { Debug.LogWarning("[Einstellungen] Recovery Key muss 16-stellig sein."); return; }

                string newPasskey = authService.resetPassKeyWithRecoveryKey(recoveryKey);
                if (!string.IsNullOrEmpty(newPasskey))
                {
                    ShowNewPasskeyPopup(newPasskey);
                    if (_inputSuperkeyReset != null) _inputSuperkeyReset.value = "";
                }
                else
                    Debug.LogWarning("[Einstellungen] Kein neuer Passkey zurückgegeben.");
            };

        if (_btnDeleteProfile != null) _btnDeleteProfile.clicked += ShowDeleteDialog;
        if (_btnDialogCancel != null) _btnDialogCancel.clicked += HideDeleteDialog;
        if (_btnDialogConfirm != null) _btnDialogConfirm.clicked += ConfirmDeleteProfile;

        if (_btnClosePasskeyPopup != null) _btnClosePasskeyPopup.clicked += () => HidePopup(_popupNewPasskey);
        if (_btnCloseGespeichert != null) _btnCloseGespeichert.clicked += () => HidePopup(_popupGespeichert);
    }

    // ═══════════════════════════════════════════════════════════
    // POPUP HILFSMETHODEN
    // ═══════════════════════════════════════════════════════════

    private void ShowPopup(VisualElement popup)
    {
        if (popup == null) { Debug.LogWarning("[Einstellungen] ShowPopup: Element ist null."); return; }
        popup.style.display = DisplayStyle.Flex;
    }

    private void HidePopup(VisualElement popup)
    {
        if (popup == null) return;
        popup.style.display = DisplayStyle.None;
    }

    // ═══════════════════════════════════════════════════════════
    // LADEN
    // ═══════════════════════════════════════════════════════════

    private void LoadSettings()
    {
        LoadVersionInfo();
        LoadCompanyData();
        LoadLocalSettings();
        LoadBezahlweiseStatus();
    }

    private void LoadVersionInfo()
    {
        if (_labelVersion != null) _labelVersion.text = "Version 1.0.0";
    }

    private void LoadCompanyData()
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) { Debug.LogWarning("[Einstellungen] Keine aktive NutzerDB."); return; }

        var companies = db.getAllCompanies();
        if (companies == null || companies.Count == 0) return;

        _currentCompany = companies[0];
        if (_inputFirmenname != null) _inputFirmenname.value = _currentCompany.name ?? "";
        if (_dropdownRechtsform != null) _dropdownRechtsform.index = _currentCompany.legalForm;
        if (_inputStadt != null) _inputStadt.value = _currentCompany.location ?? "";
    }

    private void LoadLocalSettings()
    {
        SetField(_inputRechnrPraefix, PREF_RECHNR_PRAEFIX, "RE-");
        SetField(_inputStartnummer, PREF_STARTNUMMER, "1");
        SetField(_inputZahlungsziel, PREF_ZAHLUNGSZIEL, "14");
        SetField(_inputZahlungshinweis, PREF_ZAHLUNGSHINWEIS, "Bitte überweisen Sie den Betrag auf ...");

        if (_dropdownWaehrung != null) _dropdownWaehrung.index = PlayerPrefs.GetInt(PREF_WAEHRUNG, 0);
        if (_dropdownDatumsformat != null) _dropdownDatumsformat.index = PlayerPrefs.GetInt(PREF_DATUMSFORMAT, 0);
        if (_toggleUstRechnung != null) _toggleUstRechnung.value = PlayerPrefs.GetInt(PREF_UST_RECHNUNG, 1) == 1;
        if (_toggleAutoNummer != null) _toggleAutoNummer.value = PlayerPrefs.GetInt(PREF_AUTO_NUMMER, 0) == 1;

        SetField(_inputSteuernummer, PREF_STEUERNUMMER, "");
        SetField(_inputUstidnr, PREF_USTIDNR, "");
        SetField(_inputHandelsreg, PREF_HANDELSREG, "");
        SetField(_inputGruendungsjahr, PREF_GRUENDUNGSJAHR, "");
        SetField(_inputStrasse, "settings_strasse", "");
        SetField(_inputPlz, "settings_plz", "");

        SetField(_inputKontoinhaber, PREF_KONTOINHABER, "");
        SetField(_inputIban, PREF_IBAN, "");
        SetField(_inputBic, PREF_BIC, "");
        SetField(_inputKreditinstitut, PREF_KREDITINSTITUT, "");
        if (_toggleIbanRechnung != null) _toggleIbanRechnung.value = PlayerPrefs.GetInt(PREF_IBAN_RECHNUNG, 0) == 1;

        if (_toggleLogo != null) _toggleLogo.value = PlayerPrefs.GetInt(PREF_LOGO_RECHNUNG, 1) == 1;
        if (_toggleSeitenzahl != null) _toggleSeitenzahl.value = PlayerPrefs.GetInt(PREF_SEITENZAHL, 1) == 1;
        if (_toggleExportpfad != null) _toggleExportpfad.value = PlayerPrefs.GetInt(PREF_EXPORTPFAD, 1) == 1;

        _selectedSteuersatz = PlayerPrefs.GetInt(PREF_STEUERSATZ, 19);
        _customSteuersatz = PlayerPrefs.GetInt(PREF_STEUER_CUSTOM_AKTIV, 0) == 1;
        if (_inputSteuerCustom != null)
            _inputSteuerCustom.value = PlayerPrefs.GetString(PREF_STEUER_CUSTOM_WERT, "0");
        UpdateSteuersatzButtons();

        bool isDark = PlayerPrefs.GetInt(PREF_DARK_MODE, 1) == 1;
        UpdateModeButtons(isDark);

        if (_toggleBegleiter != null) _toggleBegleiter.value = PlayerPrefs.GetInt(PREF_BEGLEITER, 1) == 1;

        SetField(_inputAgb, PREF_AGB, "");
        SetField(_inputDisclaimer, PREF_DISCLAIMER, "");
        SetField(_inputBarzahlung, PREF_BARZAHLUNG, "");
        SetField(_inputUeberweisung, PREF_UEBERWEISUNG, "");
    }

    private void LoadBezahlweiseStatus()
    {
        SetStatusLabel(_labelStatusAgb, PlayerPrefs.GetString(PREF_AGB, ""));
        SetStatusLabel(_labelStatusDisclaimer, PlayerPrefs.GetString(PREF_DISCLAIMER, ""));
        SetStatusLabel(_labelStatusBar, PlayerPrefs.GetString(PREF_BARZAHLUNG, ""));
        SetStatusLabel(_labelStatusUeberweisung, PlayerPrefs.GetString(PREF_UEBERWEISUNG, ""));
    }

    private void SetStatusLabel(Label label, string inhalt)
    {
        if (label == null) return;
        bool hat = !string.IsNullOrWhiteSpace(inhalt);
        label.text = hat ? "hinterlegt" : "nicht hinterlegt";
        label.style.color = hat
            ? new StyleColor(COLOR_GREEN)
            : new StyleColor(new Color(160f / 255f, 160f / 255f, 160f / 255f));
    }

    // ═══════════════════════════════════════════════════════════
    // SPEICHERN – GESAMT
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

        string name = _inputFirmenname?.value ?? "";
        int legalForm = _dropdownRechtsform?.index ?? 0;
        string location = _inputStadt?.value ?? "";
        int industry = 3;

        if (_currentCompany == null)
        {
            db.createCompany(name, legalForm, industry, location);
            var all = db.getAllCompanies();
            if (all != null && all.Count > 0) _currentCompany = all[all.Count - 1];
        }
        else
        {
            _currentCompany.name = name;
            _currentCompany.legalForm = legalForm;
            _currentCompany.location = location;
            db.updateCompany(_currentCompany);
        }
    }

    private void SaveLocalSettings()
    {
        SaveField(PREF_RECHNR_PRAEFIX, _inputRechnrPraefix);
        SaveField(PREF_STARTNUMMER, _inputStartnummer);
        SaveField(PREF_ZAHLUNGSZIEL, _inputZahlungsziel);
        SaveField(PREF_ZAHLUNGSHINWEIS, _inputZahlungshinweis);

        if (_dropdownWaehrung != null) PlayerPrefs.SetInt(PREF_WAEHRUNG, _dropdownWaehrung.index);
        if (_dropdownDatumsformat != null) PlayerPrefs.SetInt(PREF_DATUMSFORMAT, _dropdownDatumsformat.index);
        if (_toggleUstRechnung != null) PlayerPrefs.SetInt(PREF_UST_RECHNUNG, _toggleUstRechnung.value ? 1 : 0);
        if (_toggleAutoNummer != null) PlayerPrefs.SetInt(PREF_AUTO_NUMMER, _toggleAutoNummer.value ? 1 : 0);

        SaveField(PREF_STEUERNUMMER, _inputSteuernummer);
        SaveField(PREF_USTIDNR, _inputUstidnr);
        SaveField(PREF_HANDELSREG, _inputHandelsreg);
        SaveField(PREF_GRUENDUNGSJAHR, _inputGruendungsjahr);
        SaveField("settings_strasse", _inputStrasse);
        SaveField("settings_plz", _inputPlz);

        SaveField(PREF_KONTOINHABER, _inputKontoinhaber);
        SaveField(PREF_IBAN, _inputIban);
        SaveField(PREF_BIC, _inputBic);
        SaveField(PREF_KREDITINSTITUT, _inputKreditinstitut);
        if (_toggleIbanRechnung != null) PlayerPrefs.SetInt(PREF_IBAN_RECHNUNG, _toggleIbanRechnung.value ? 1 : 0);

        if (_toggleLogo != null) PlayerPrefs.SetInt(PREF_LOGO_RECHNUNG, _toggleLogo.value ? 1 : 0);
        if (_toggleSeitenzahl != null) PlayerPrefs.SetInt(PREF_SEITENZAHL, _toggleSeitenzahl.value ? 1 : 0);
        if (_toggleExportpfad != null) PlayerPrefs.SetInt(PREF_EXPORTPFAD, _toggleExportpfad.value ? 1 : 0);

        PlayerPrefs.SetInt(PREF_STEUERSATZ, _selectedSteuersatz);
        PlayerPrefs.SetInt(PREF_STEUER_CUSTOM_AKTIV, _customSteuersatz ? 1 : 0);
        if (_inputSteuerCustom != null)
            PlayerPrefs.SetString(PREF_STEUER_CUSTOM_WERT, _inputSteuerCustom.value);

        if (_toggleBegleiter != null) PlayerPrefs.SetInt(PREF_BEGLEITER, _toggleBegleiter.value ? 1 : 0);

        SaveField(PREF_AGB, _inputAgb);
        SaveField(PREF_DISCLAIMER, _inputDisclaimer);
        SaveField(PREF_BARZAHLUNG, _inputBarzahlung);
        SaveField(PREF_UEBERWEISUNG, _inputUeberweisung);

        PlayerPrefs.Save();
    }

    // ═══════════════════════════════════════════════════════════
    // SPEICHERN – EINZELNE POPUPS
    // ═══════════════════════════════════════════════════════════

    private void SaveUnternehmenPopup()
    {
        SaveCompanyData();
        SaveField(PREF_STEUERNUMMER, _inputSteuernummer);
        SaveField(PREF_USTIDNR, _inputUstidnr);
        SaveField(PREF_HANDELSREG, _inputHandelsreg);
        SaveField(PREF_GRUENDUNGSJAHR, _inputGruendungsjahr);
        SaveField("settings_strasse", _inputStrasse);
        SaveField("settings_plz", _inputPlz);
        PlayerPrefs.Save();
        HidePopup(_popupUnternehmen);
        ShowGespeichertPopup();
    }

    private void SaveBankPopup()
    {
        SaveField(PREF_KONTOINHABER, _inputKontoinhaber);
        SaveField(PREF_IBAN, _inputIban);
        SaveField(PREF_BIC, _inputBic);
        SaveField(PREF_KREDITINSTITUT, _inputKreditinstitut);
        if (_toggleIbanRechnung != null) PlayerPrefs.SetInt(PREF_IBAN_RECHNUNG, _toggleIbanRechnung.value ? 1 : 0);
        PlayerPrefs.Save();
        HidePopup(_popupBank);
        ShowGespeichertPopup();
    }

    private void SaveRechnungPopup()
    {
        SaveField(PREF_RECHNR_PRAEFIX, _inputRechnrPraefix);
        SaveField(PREF_STARTNUMMER, _inputStartnummer);
        SaveField(PREF_ZAHLUNGSZIEL, _inputZahlungsziel);
        SaveField(PREF_ZAHLUNGSHINWEIS, _inputZahlungshinweis);
        if (_dropdownWaehrung != null) PlayerPrefs.SetInt(PREF_WAEHRUNG, _dropdownWaehrung.index);
        if (_dropdownDatumsformat != null) PlayerPrefs.SetInt(PREF_DATUMSFORMAT, _dropdownDatumsformat.index);
        if (_toggleUstRechnung != null) PlayerPrefs.SetInt(PREF_UST_RECHNUNG, _toggleUstRechnung.value ? 1 : 0);
        if (_toggleAutoNummer != null) PlayerPrefs.SetInt(PREF_AUTO_NUMMER, _toggleAutoNummer.value ? 1 : 0);
        PlayerPrefs.Save();
        HidePopup(_popupRechnung);
        ShowGespeichertPopup();
    }

    private void SaveBezahlweisePopup()
    {
        SaveField(PREF_AGB, _inputAgb);
        SaveField(PREF_DISCLAIMER, _inputDisclaimer);
        SaveField(PREF_BARZAHLUNG, _inputBarzahlung);
        SaveField(PREF_UEBERWEISUNG, _inputUeberweisung);
        PlayerPrefs.Save();
        LoadBezahlweiseStatus();
        SyncBezahlweiseToDokumente();
        HidePopup(_popupBezahlweise);
        ShowGespeichertPopup();
    }

    private void SyncBezahlweiseToDokumente()
    {
        string path = DocumentDashboard.GetSaveFilePath();
        if (!System.IO.File.Exists(path)) return;

        var saveData = DocumentDashboard.GetSavedDocuments();
        if (saveData == null || saveData.savedDocs == null) return;

        foreach (var doc in saveData.savedDocs)
        {
            if (doc.category != "Bezahlweise") continue;

            if (doc.title == "Kontodaten (IBAN/BIC)" && doc.strukturFelder != null)
            {
                SetStrukturFeld(doc, "iban", _inputIban?.value ?? "");
                SetStrukturFeld(doc, "bic", _inputBic?.value ?? "");
                SetStrukturFeld(doc, "bank", _inputKreditinstitut?.value ?? "");
                SetStrukturFeld(doc, "kontoinhaber", _inputKontoinhaber?.value ?? "");
            }
            else if (doc.title == "Zahlungsbedingungen" && doc.strukturFelder != null)
            {
                SetStrukturFeld(doc, "zahlungsziel", _inputZahlungsziel?.value ?? "");
            }
        }

        string json = JsonUtility.ToJson(saveData, true);
        System.IO.File.WriteAllText(path, json);
    }

    private void SetStrukturFeld(DocumentDashboard.DocumentData doc, string key, string wert)
    {
        if (doc.strukturFelder == null) return;
        var feld = doc.strukturFelder.Find(f => f.key == key);
        if (feld != null) feld.wert = wert;
    }

    // ═══════════════════════════════════════════════════════════
    // STEUERSATZ
    // ═══════════════════════════════════════════════════════════

    private void SelectSteuersatz(int satz)
    {
        _selectedSteuersatz = satz;
        _customSteuersatz = false;
        UpdateSteuersatzButtons();
    }

    private void SelectSteuersatzCustom()
    {
        _customSteuersatz = true;
        UpdateSteuersatzButtons();
    }

    private void UpdateSteuersatzButtons()
    {
        AktualisiereSteuerButton(_btnSteuer7, !_customSteuersatz && _selectedSteuersatz == 7);
        AktualisiereSteuerButton(_btnSteuer10, !_customSteuersatz && _selectedSteuersatz == 10);
        AktualisiereSteuerButton(_btnSteuer19, !_customSteuersatz && _selectedSteuersatz == 19);
        AktualisiereSteuerButton(_btnSteuerCustom, _customSteuersatz);

        if (_containerSteuerCustom != null)
            _containerSteuerCustom.style.display = _customSteuersatz ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void AktualisiereSteuerButton(Button btn, bool aktiv)
    {
        if (btn == null) return;
        btn.style.backgroundColor = aktiv ? COLOR_GREEN : COLOR_INACTIVE;
        btn.style.color = aktiv
            ? new StyleColor(new Color(0f, 0f, 0f))
            : new StyleColor(new Color(1f, 1f, 1f));
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
        if (_btnDarkMode != null) _btnDarkMode.style.backgroundColor = isDark ? COLOR_GREEN : COLOR_INACTIVE;
    }

    // ═══════════════════════════════════════════════════════════
    // POPUP: NEUER PASSKEY
    // ═══════════════════════════════════════════════════════════

    private void ShowNewPasskeyPopup(string passkey)
    {
        if (_labelNewPasskey != null)
        {
            string display = "";
            foreach (char c in passkey) display += c + "  ";
            _labelNewPasskey.text = display.TrimEnd();
        }
        if (_labelNewPasskeyPlain != null)
            _labelNewPasskeyPlain.text = $"Dein neuer Passkey: {passkey}";
        ShowPopup(_popupNewPasskey);
    }

    // ═══════════════════════════════════════════════════════════
    // POPUP: LOKALPROFIL LÖSCHEN
    // ═══════════════════════════════════════════════════════════

    private void ShowDeleteDialog()
    {
        if (_inputSuperkey1 != null) _inputSuperkey1.value = "";
        if (_inputSuperkey2 != null) _inputSuperkey2.value = "";
        ShowPopup(_dialogOverlay);
    }

    private void HideDeleteDialog() => HidePopup(_dialogOverlay);

    private void ConfirmDeleteProfile()
    {
        string key1 = _inputSuperkey1?.value?.Trim() ?? "";
        string key2 = _inputSuperkey2?.value?.Trim() ?? "";

        if (string.IsNullOrEmpty(key1) || string.IsNullOrEmpty(key2))
        { Debug.LogWarning("[Einstellungen] Super-Passkey fehlt."); return; }

        if (key1 != key2)
        { Debug.LogWarning("[Einstellungen] Super-Passkeys stimmen nicht überein."); return; }

        if (mainLogoutController != null) mainLogoutController.logout();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene(0);
    }

    // ═══════════════════════════════════════════════════════════
    // POPUP: GESPEICHERT
    // ═══════════════════════════════════════════════════════════

    private void ShowGespeichertPopup()
    {
        if (_labelGespeichertText != null)
            _labelGespeichertText.text = "Einstellungen wurden gespeichert";
        ShowPopup(_popupGespeichert);
    }

    // ═══════════════════════════════════════════════════════════
    // HILFSMETHODEN
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
