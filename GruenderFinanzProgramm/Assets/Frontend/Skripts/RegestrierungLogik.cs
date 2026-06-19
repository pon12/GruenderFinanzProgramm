using UnityEngine;
using UnityEngine.UIElements;
using System;

public class RegestrierungLogik : MonoBehaviour
{
    private VisualElement root;

    private VisualElement screenLogin;
    private VisualElement screenMain;

    private Button btnKeyGenerieren;
    private Toggle toggle;
    private VisualElement popuppasskey;
    private Button btnclose;
    private Button btnpopupclose;
    private TextField inputProfileingabe;
    private Label lblPasskey;

    private Button btnEntschluesseln;

    private Label lblError;

    private VisualElement popUpAGB;
    private Label text2;
    private Button btnAGBclose;

    private VisualElement popUpDatenschutz;
    private Label textDatenschutz;
    private Button btnDatenschutzclose;

    private Button btnLoginSubmit;

    private readonly string farbeNormal = "#80CF95";
    private readonly string farbeHover = "#80CF95";

    public event Action OnBackToLoginRequested;
    public event Action<string> OnRegistrationSuccessful;

    private AuthService authService;
    private string aktuellerPasskey = "";
    private bool istEntschluesselt = false;
    
    // Leerzeichen alle 4 zahlen für power passkey
    
    private string FormatiereMitLeerzeichen(string input, int gruppenGroesse = 4)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            if (i > 0 && i % gruppenGroesse == 0)
                sb.Append(' ');
            sb.Append(input[i]);
        }
        return sb.ToString();
    }

    private void OnEnable()
    {
        authService = new AuthService();

        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        screenLogin = root.Q<VisualElement>("LoginScreen");
        screenMain = root.Q<VisualElement>("MainScreen");

        btnKeyGenerieren = root.Q<Button>("btnKeyGenerieren");
        toggle = root.Q<Toggle>("Toggle");
        popuppasskey = root.Q<VisualElement>("popuppasskey");
        btnclose = root.Q<Button>("btnclose");
        btnpopupclose = root.Q<Button>("btnpopupclose");
        inputProfileingabe = root.Q<TextField>("Profileingabe");
        lblPasskey = root.Q<Label>("lblPasskey");

        btnEntschluesseln = root.Q<Button>("btnEntschluesseln");
        lblError = root.Q<Label>("lblError");

        popUpAGB = root.Q<VisualElement>("PopUpAGB");
        text2 = root.Q<Label>("text2");
        btnAGBclose = root.Q<Button>("btnAGBclose");

        popUpDatenschutz = root.Q<VisualElement>("PopUpDatenschutz");
        textDatenschutz = root.Q<Label>("textDatenschutz");
        btnDatenschutzclose = root.Q<Button>("btnDatenschutzclose");

        btnLoginSubmit = root.Q<Button>("btnLoginSubmit");

        ValidateUIElements();

        ResetAGBText();
        ResetDatenschutzText();

        if (btnKeyGenerieren != null) btnKeyGenerieren.clicked += OnKeyGenerierenClicked;
        if (btnclose != null) btnclose.clicked += OnBackToLoginClicked;
        if (btnpopupclose != null) btnpopupclose.clicked += OnPopupCloseClicked;
        if (btnEntschluesseln != null) btnEntschluesseln.clicked += OnEntschluesselnClicked;
        if (btnLoginSubmit != null) btnLoginSubmit.clicked += OnLoginSubmitted;

        if (text2 != null)
        {
            text2.RegisterCallback<PointerDownEvent>(OnLabelClicked);
            text2.RegisterCallback<PointerOverEvent>(OnAGBHoverIn);
            text2.RegisterCallback<PointerOutEvent>(OnAGBHoverOut);
        }

        if (btnAGBclose != null) btnAGBclose.clicked += OnAGBcloseClicked;

        if (textDatenschutz != null)
        {
            textDatenschutz.RegisterCallback<PointerDownEvent>(OnDatenschutzLabelClicked);
            textDatenschutz.RegisterCallback<PointerOverEvent>(OnDatenschutzHoverIn);
            textDatenschutz.RegisterCallback<PointerOutEvent>(OnDatenschutzHoverOut);
        }

        if (btnDatenschutzclose != null) btnDatenschutzclose.clicked += OnDatenschutzCloseClicked;

        ShowLoginScreen();
        
    }

    private void OnKeyGenerierenClicked()
    {
        bool isToggleActive = toggle != null && toggle.value;
        bool hasText = inputProfileingabe != null && !string.IsNullOrWhiteSpace(inputProfileingabe.value);

        if (isToggleActive && hasText)
        {
            PassKeyRecord record = authService.registerUser(inputProfileingabe.value);

            if (record == null)
            {
                if (lblError != null)
                {
                    lblError.style.display = DisplayStyle.Flex;
                    lblError.style.color = Color.red;
                    lblError.text = "Registrierung fehlgeschlagen: Dieser Name existiert bereits!";
                }
                return;
            }

            if (lblError != null) lblError.style.display = DisplayStyle.None;

            if (popuppasskey != null)
            {
                popuppasskey.style.display = DisplayStyle.Flex;

                aktuellerPasskey = authService.passkeyGlobal;
                istEntschluesselt = false;

                AktualisierePasskeyAnzeige();
            }

            OnRegistrationSuccessful?.Invoke(inputProfileingabe.value);
        }
        else
        {
            ZeigeEingabeFehler(hasText, isToggleActive);
        }
    }

    private void OnEntschluesselnClicked()
    {
        if (string.IsNullOrEmpty(aktuellerPasskey)) return;

        istEntschluesselt = !istEntschluesselt;
        AktualisierePasskeyAnzeige();
    }

    private void AktualisierePasskeyAnzeige()
{
    if (lblPasskey == null) return;
    
    string recoveryKey = authService.recoveryPassKeyGlobal;
    string passkeyAnzeige;
    string recoveryAnzeige;

    if (istEntschluesselt)
    {
        // Hier wendest du die Formatierung auf BEIDE Keys an
        passkeyAnzeige = FormatiereMitLeerzeichen(aktuellerPasskey);
        recoveryAnzeige = FormatiereMitLeerzeichen(recoveryKey); // <-- Hier musst du die Funktion aufrufen
        
        if (btnEntschluesseln != null) btnEntschluesseln.text = "Verbergen";
    }
    else
    {
        // Maskiert – hier willst du vermutlich keine Leerzeichen, 
        // da Sternchen eh nur Platzhalter sind
        passkeyAnzeige = new string('*', aktuellerPasskey.Length);
        recoveryAnzeige = new string('*', recoveryKey.Length);
        
        if (btnEntschluesseln != null) btnEntschluesseln.text = "Entschlüsseln";
    }

    lblPasskey.text =
        "PassKey: " + passkeyAnzeige +
        "\nRecoveryKey: " + recoveryAnzeige;
}

    private void ZeigeEingabeFehler(bool hasText, bool isToggleActive)
    {
        if (lblError != null)
        {
            lblError.style.display = DisplayStyle.Flex;
            lblError.style.color = Color.red;

            if (!hasText && !isToggleActive)
                lblError.text = "Bitte gib einen Namen ein und akzeptiere die Bedingungen.";
            else if (!hasText)
                lblError.text = "Bitte gib einen Profilnamen ein.";
            else if (!isToggleActive)
                lblError.text = "Bitte akzeptiere die AGB und Datenschutzrichtlinie.";
        }
    }

    private void OnPopupCloseClicked()
    {
        if (popuppasskey != null) popuppasskey.style.display = DisplayStyle.None;
        aktuellerPasskey = "";
    }

    private void OnLabelClicked(PointerDownEvent evt) { if (popUpAGB != null) popUpAGB.style.display = DisplayStyle.Flex; }
    private void OnAGBcloseClicked() { if (popUpAGB != null) popUpAGB.style.display = DisplayStyle.None; }
    private void OnAGBHoverIn(PointerOverEvent evt) { if (text2 != null) text2.text = $"<color={farbeHover}><u>AGB</u></color>"; }
    private void OnAGBHoverOut(PointerOutEvent evt) => ResetAGBText();
    private void ResetAGBText() { if (text2 != null) text2.text = $"<color={farbeNormal}><u>AGB</u></color>"; }

    private void OnDatenschutzLabelClicked(PointerDownEvent evt) { if (popUpDatenschutz != null) popUpDatenschutz.style.display = DisplayStyle.Flex; }
    private void OnDatenschutzCloseClicked() { if (popUpDatenschutz != null) popUpDatenschutz.style.display = DisplayStyle.None; }
    private void OnDatenschutzHoverIn(PointerOverEvent evt) { if (textDatenschutz != null) textDatenschutz.text = $"<color={farbeHover}><u>Datenschutzrichtlinie</u></color>"; }
    private void OnDatenschutzHoverOut(PointerOutEvent evt) => ResetDatenschutzText();
    private void ResetDatenschutzText() { if (textDatenschutz != null) textDatenschutz.text = $"<color={farbeNormal}><u>Datenschutzrichtlinie</u></color>"; }

    private void OnBackToLoginClicked()
    {
        ShowLoginScreen();
        OnBackToLoginRequested?.Invoke();
    }

    private void OnLoginSubmitted() => ShowMainScreen();

    // HIER LAG DER FEHLER: Die null-Checks haben gefehlt, wodurch Unity den Wechsel abgebrochen hat, falls ein UI-Element gefehlt hat.
    private void ShowLoginScreen()
    {
        if (screenLogin != null) screenLogin.style.display = DisplayStyle.Flex;
        if (screenMain != null) screenMain.style.display = DisplayStyle.None;
        if (popuppasskey != null) popuppasskey.style.display = DisplayStyle.None;
        if (popUpAGB != null) popUpAGB.style.display = DisplayStyle.None;
        if (popUpDatenschutz != null) popUpDatenschutz.style.display = DisplayStyle.None;
        if (lblError != null) lblError.style.display = DisplayStyle.None;
    }

    private void ShowMainScreen()
    {
        if (screenLogin != null) screenLogin.style.display = DisplayStyle.None;
        if (screenMain != null) screenMain.style.display = DisplayStyle.Flex;
    }

    private void ValidateUIElements()
    {
        if (btnKeyGenerieren == null) Debug.LogError("btnKeyGenerieren fehlt");
        if (btnEntschluesseln == null) Debug.LogError("btnEntschluesseln fehlt");
        if (lblPasskey == null) Debug.LogError("lblPasskey fehlt");
    }

    // HIER LAG DER FEHLER: Die Events müssen zwingend alle wieder abgemeldet werden!
    private void OnDisable()
    {
        if (btnKeyGenerieren != null) btnKeyGenerieren.clicked -= OnKeyGenerierenClicked;
        if (btnEntschluesseln != null) btnEntschluesseln.clicked -= OnEntschluesselnClicked;
        if (btnclose != null) btnclose.clicked -= OnBackToLoginClicked;
        if (btnpopupclose != null) btnpopupclose.clicked -= OnPopupCloseClicked;
        if (btnLoginSubmit != null) btnLoginSubmit.clicked -= OnLoginSubmitted;
        if (btnAGBclose != null) btnAGBclose.clicked -= OnAGBcloseClicked;
        if (btnDatenschutzclose != null) btnDatenschutzclose.clicked -= OnDatenschutzCloseClicked;

        if (text2 != null)
        {
            text2.UnregisterCallback<PointerDownEvent>(OnLabelClicked);
            text2.UnregisterCallback<PointerOverEvent>(OnAGBHoverIn);
            text2.UnregisterCallback<PointerOutEvent>(OnAGBHoverOut);
        }

        if (textDatenschutz != null)
        {
            textDatenschutz.UnregisterCallback<PointerDownEvent>(OnDatenschutzLabelClicked);
            textDatenschutz.UnregisterCallback<PointerOverEvent>(OnDatenschutzHoverIn);
            textDatenschutz.UnregisterCallback<PointerOutEvent>(OnDatenschutzHoverOut);
        }
    }
}