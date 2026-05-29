using UnityEngine;
using UnityEngine.UIElements;
using System; 

public class RegestrierungLogik : MonoBehaviour
{
    private VisualElement root;

    // Die beiden Haupt-Bildschirme (Container)
    private VisualElement screenLogin;
    private VisualElement screenMain;

    // Elemente des Key-Generators (MainScreen)
    private Button btnKeyGenerieren; 
    private Toggle toggle;
    private VisualElement popuppasskey;
    private Button btnclose;      
    private Button btnpopupclose; 
    private TextField inputProfileingabe;
    private Label lblPasskey;

    // NEU: Der Button zum Sichtbar-machen / Entschlüsseln
    private Button btnEntschluesseln; 

    // Das Fehler-Label für falsche/fehlende Eingaben
    private Label lblError;

    // Das AGB-Popup und das klickbare Label
    private VisualElement popUpAGB;
    private Label text2; 
    private Button btnAGBclose;

    // Das Datenschutz-Popup und seine klickbaren Elemente
    private VisualElement popUpDatenschutz; 
    private Label textDatenschutz; 
    private Button btnDatenschutzclose; 

    // Elemente vom Login-Screen
    private Button btnLoginSubmit; 

    // Hex-Farbcodes für die Links (Hover-Effekt)
    private readonly string farbeNormal = "#00C833";
    private readonly string farbeHover = "#00FF4D";

    // --- EVENTS FÜR DIE WIEDERVERWENDBARKEIT ---
    public event Action OnBackToLoginRequested;
    public event Action<string> OnRegistrationSuccessful; 

    // --- BACKEND INSTANZ & CODESPEICHER ---
    private AuthService authService; 
    private string aktuellerPasskey = ""; // Speichert den echten Key im Hintergrund ab
    private bool istEntschluesselt = false; // Zustand, ob der Key gerade offen liegt

    private void OnEnable()
    {
        authService = new AuthService();

        UIDocument uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        // 1. Die großen Screen-Container suchen
        screenLogin = root.Q<VisualElement>("LoginScreen");
        screenMain = root.Q<VisualElement>("MainScreen");

        // 2. Elemente für den Key-Generator suchen
        btnKeyGenerieren = root.Q<Button>("btnKeyGenerieren"); 
        toggle = root.Q<Toggle>("Toggle");
        popuppasskey = root.Q<VisualElement>("popuppasskey");
        btnclose = root.Q<Button>("btnclose"); 
        btnpopupclose = root.Q<Button>("btnpopupclose"); 
        inputProfileingabe = root.Q<TextField>("Profileingabe");
        lblPasskey = root.Q<Label>("lblPasskey");

        // NEU: Button aus dem UI Builder heraussuchen
        btnEntschluesseln = root.Q<Button>("btnEntschluesseln");

        // Fehler-Label aus dem UI holen
        lblError = root.Q<Label>("lblError");

        // 3. Elemente für das AGB-Popup suchen
        popUpAGB = root.Q<VisualElement>("PopUpAGB");
        text2 = root.Q<Label>("text2");
        btnAGBclose = root.Q<Button>("btnAGBclose");

        // Elemente für das Datenschutz-Popup holen
        popUpDatenschutz = root.Q<VisualElement>("PopUpDatenschutz"); 
        textDatenschutz = root.Q<Label>("textDatenschutz");
        btnDatenschutzclose = root.Q<Button>("btnDatenschutzclose"); 

        // 4. Elemente für den Login-Screen suchen
        btnLoginSubmit = root.Q<Button>("btnLoginSubmit"); 

        // Sicherheits-Checks in der Unity-Konsole
        ValidateUIElements();

        // Styling via Rich Text initialisieren
        ResetAGBText();
        ResetDatenschutzText();

        // --- EVENTS REGISTRIEREN ---
        if (btnKeyGenerieren != null) btnKeyGenerieren.clicked += OnKeyGenerierenClicked;
        if (btnclose != null) btnclose.clicked += OnBackToLoginClicked;
        if (btnpopupclose != null) btnpopupclose.clicked += OnPopupCloseClicked;
        
        // NEU: Klick-Event für den Entschlüsselungs-Button registrieren
        if (btnEntschluesseln != null) btnEntschluesseln.clicked += OnEntschluesselnClicked;
        
        // AGB Klick & Hover
        if (text2 != null)
        {
            text2.RegisterCallback<PointerDownEvent>(OnLabelClicked);
            text2.RegisterCallback<PointerOverEvent>(OnAGBHoverIn);
            text2.RegisterCallback<PointerOutEvent>(OnAGBHoverOut);
        }
        if (btnAGBclose != null) btnAGBclose.clicked += OnAGBcloseClicked;
        
        // Datenschutz Klick, Hover & Schließen
        if (textDatenschutz != null)
        {
            textDatenschutz.RegisterCallback<PointerDownEvent>(OnDatenschutzLabelClicked);
            textDatenschutz.RegisterCallback<PointerOverEvent>(OnDatenschutzHoverIn);
            textDatenschutz.RegisterCallback<PointerOutEvent>(OnDatenschutzHoverOut);
        }
        if (btnDatenschutzclose != null) btnDatenschutzclose.clicked += OnDatenschutzCloseClicked;
        
        if (btnLoginSubmit != null) btnLoginSubmit.clicked += OnLoginSubmitted;

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

                // Wir merken uns den echten Key im Hintergrund und setzen den Sichtbarkeits-Status zurück
                aktuellerPasskey = authService.passkeyGlobal;
                istEntschluesselt = false;

                // Zeigt den Key direkt verschlüsselt/maskiert an
                AktualisierePasskeyAnzeige();
            }

            OnRegistrationSuccessful?.Invoke(inputProfileingabe.value);
        }
        else
        {
            ZeigeEingabeFehler(hasText, isToggleActive);
        }
    }

    // NEU: Die Logik für den "btnEntschluesseln"-Button
    private void OnEntschluesselnClicked()
    {
        if (string.IsNullOrEmpty(aktuellerPasskey)) return;

        // Zustand umkehren (Anzeigen <-> Verbergen)
        istEntschluesselt = !istEntschluesselt;
        
        AktualisierePasskeyAnzeige();
    }

    // NEU: Hilfsfunktion, die steuert, ob Text oder Sternchen angezeigt werden
    private void AktualisierePasskeyAnzeige()
    {
        if (lblPasskey == null) return;

        if (istEntschluesselt)
        {
            // Zeigt den echten Passkey im Label an
            lblPasskey.text = "PassKey: " + aktuellerPasskey;
            
            if (btnEntschluesseln != null) btnEntschluesseln.text = "Verbergen";
        }
        else
        {
            // Erzeugt dynamisch eine Sternchenkette passend zur Länge des Keys (z.B. "****")
            string maskiert = new string('*', aktuellerPasskey.Length);
            lblPasskey.text = "PassKey: " + maskiert;
            
            if (btnEntschluesseln != null) btnEntschluesseln.text = "Entschlüsseln";
        }
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
        aktuellerPasskey = ""; // Aus Sicherheitsgründen leeren beim Schließen
    }

    // --- AGB LOGIK ---
    private void OnLabelClicked(PointerDownEvent evt) { if (popUpAGB != null) popUpAGB.style.display = DisplayStyle.Flex; }
    private void OnAGBcloseClicked() { if (popUpAGB != null) popUpAGB.style.display = DisplayStyle.None; }
    private void OnAGBHoverIn(PointerOverEvent evt) => text2.text = $"<color={farbeHover}><u>AGB</u></color>";
    private void OnAGBHoverOut(PointerOutEvent evt) => ResetAGBText();
    private void ResetAGBText() { if (text2 != null) text2.text = $"<color={farbeNormal}><u>AGB</u></color>"; }

    // --- DATENSCHUTZ LOGIK ---
    private void OnDatenschutzLabelClicked(PointerDownEvent evt) { if (popUpDatenschutz != null) popUpDatenschutz.style.display = DisplayStyle.Flex; }
    private void OnDatenschutzCloseClicked() { if (popUpDatenschutz != null) popUpDatenschutz.style.display = DisplayStyle.None; }
    private void OnDatenschutzHoverIn(PointerOverEvent evt) => textDatenschutz.text = $"<color={farbeHover}><u>Datenschutzrichtlinie</u></color>";
    private void OnDatenschutzHoverOut(PointerOutEvent evt) => ResetDatenschutzText();
    private void ResetDatenschutzText() { if (textDatenschutz != null) textDatenschutz.text = $"<color={farbeNormal}><u>Datenschutzrichtlinie</u></color>"; }

    // --- SCREEN WECHSEL LOGIK ---
    private void OnBackToLoginClicked()
    {
        ShowLoginScreen();
        OnBackToLoginRequested?.Invoke();
    }

    private void OnLoginSubmitted() => ShowMainScreen();

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
        if (btnKeyGenerieren == null) Debug.LogError("Konnte den Button 'btnKeyGenerieren' nicht finden!");
        if (btnEntschluesseln == null) Debug.LogError("Konnte den Button 'btnEntschluesseln' nicht finden!");
        if (text2 == null) Debug.LogError("Konnte das Label 'text2' (AGB) nicht finden!");
        if (popUpAGB == null) Debug.LogError("Konnte das Popup 'PopUpAGB' nicht finden!");
        if (btnAGBclose == null) Debug.LogError("Konnte den Button 'btnAGBclose' nicht finden!");
        if (inputProfileingabe == null) Debug.LogError("Konnte das TextField 'Profileingabe' nicht finden!");
        if (lblPasskey == null) Debug.LogError("Konnte das Label 'lblPasskey' nicht finden!");
        if (popUpDatenschutz == null) Debug.LogError("Konnte das Popup 'PopUpDatenschutz' nicht finden!");
        if (textDatenschutz == null) Debug.LogError("Konnte das Label 'textDatenschutz' nicht finden!");
        if (btnDatenschutzclose == null) Debug.LogError("Konnte den Button 'btnDatenschutzclose' nicht finden!");
        if (lblError == null) Debug.LogError("Konnte das Label 'lblError' nicht finden!");
    }

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