// LoginFlowController.cs
// Auf UIManager in der Login-Scene

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class LoginFlowController : MonoBehaviour
{
    [SerializeField] private UIDocument          uiDocument;
    [SerializeField] private PassKeyAuthController passKeyAuthController;
    [SerializeField] private string              DASHBOARD_SCENE = "Dashboard";

    private VisualElement _root;
    private VisualElement _popupOverlay;
    private VisualElement _popupPasskeyLogin;
    private VisualElement _popupErrorLogin;
    private Label         _loginPasskeyDisplay;
    private Button        _btnAppClose;
    private string        _loginPasskeyInput = "";

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null) { Debug.LogError("[LoginFlow] UIDocument fehlt."); return; }

        if (passKeyAuthController == null)
            passKeyAuthController = FindAnyObjectByType<PassKeyAuthController>();

        if (passKeyAuthController == null)
            Debug.LogError("[LoginFlow] PassKeyAuthController nicht gefunden.");

        _root = uiDocument.rootVisualElement;
        if (_root == null) { Debug.LogError("[LoginFlow] Root nicht gefunden."); return; }

        _popupOverlay      = _root.Q("popup-overlay");
        _popupPasskeyLogin = _root.Q("popup-passkey-login");
        _popupErrorLogin   = _root.Q("popup-error-login");
        _loginPasskeyDisplay = _root.Q<Label>("login-passkey-display");
        _btnAppClose = _root.Q<Button>("btn-app-close");
        if (_btnAppClose != null)
            _btnAppClose.clicked += CloseApplication;
        else
            Debug.LogWarning("[LoginFlow] Button 'btn-app-close' nicht gefunden.");

        RegisterAllButtons();
        UpdateDisplay();
    }

    // ─────────────────────────────────────────────────
    // BUTTON REGISTRIERUNG
    // ─────────────────────────────────────────────────

    private void RegisterAllButtons()
    {
        // Anmelden → Passkey-Popup oeffnen
        Bind("Anmelden", () => ShowPopup(_popupPasskeyLogin));

        // X Button → alles schliessen
        Bind("btn-close-login-popup", CloseAllPopups);

        // Zahlenpad 0–9
        for (int i = 0; i <= 9; i++)
        {
            int digit = i;
            var btn = _root.Q<Button>($"login-btn-{digit}");
            if (btn != null)
                btn.clicked += () => OnPasskeyDigit(digit.ToString());
            else
                Debug.LogWarning($"[LoginFlow] login-btn-{digit} nicht gefunden.");
        }

        // Loeschen
        Bind("login-btn-delete", OnPasskeyDelete);

        // Submit
        Bind("btn-login-submit", OnLoginSubmit);

        // Error Popup – Erneut versuchen
        Bind("btn-error-retry", OnErrorRetry);

        // Error Popup – Abbrechen
        Bind("btn-error-cancel", CloseAllPopups);
    }

    // ─────────────────────────────────────────────────
    // PASSKEY EINGABE
    // ─────────────────────────────────────────────────

    private void OnPasskeyDigit(string digit)
    {
        if (_loginPasskeyInput.Length >= 4) return;
        _loginPasskeyInput += digit;
        UpdateDisplay();
    }

    private void OnPasskeyDelete()
    {
        if (_loginPasskeyInput.Length == 0) return;
        _loginPasskeyInput = _loginPasskeyInput.Substring(0, _loginPasskeyInput.Length - 1);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_loginPasskeyDisplay == null) return;

        string displayText = "";
        for (int i = 0; i < 4; i++)
            displayText += i < _loginPasskeyInput.Length ? "● " : "_ ";

        _loginPasskeyDisplay.text = displayText.TrimEnd();
    }

    // ─────────────────────────────────────────────────
    // LOGIN – BACKEND
    // ─────────────────────────────────────────────────

    private void OnLoginSubmit()
{
    if (_loginPasskeyInput.Length < 4)
    {
        Debug.LogWarning("[Login] Passkey unvollstaendig.");
        return;
    }

    if (passKeyAuthController == null)
    {
        Debug.LogError("[Login] PassKeyAuthController fehlt.");
        return;
    }

    bool loginSuccessful = passKeyAuthController.loginWithPassKeyValue(_loginPasskeyInput);

    if (!loginSuccessful)
    {
        Debug.LogWarning("[Login] Login fehlgeschlagen – zeige Fehler-Popup.");
        ShowErrorPopup();
        return;
    }

    // ── Tutorial-Check ──────────────────────────────
    if (TutorialManager.Instance != null)
    {
        string nutzername = StateManager.Instance?.getCurrentUser()?.username ?? "default";
        TutorialManager.Instance.PruefeErstenStart(nutzername);
    }
    // ────────────────────────────────────────────────

    Debug.Log("[Login] Login erfolgreich – lade Dashboard.");
    SceneManager.LoadScene(DASHBOARD_SCENE);
}

    // ─────────────────────────────────────────────────
    // POPUP STEUERUNG
    // ─────────────────────────────────────────────────

    private void ShowPopup(VisualElement popup)
    {
        if (_popupOverlay != null) _popupOverlay.style.display = DisplayStyle.Flex;
        if (popup         != null) popup.style.display         = DisplayStyle.Flex;

        SetCloseButtonEnabled(false);
    }

    private void ShowErrorPopup()
    {
        // Passkey-Popup ausblenden
        if (_popupPasskeyLogin != null)
            _popupPasskeyLogin.style.display = DisplayStyle.None;

        // Eingabe zuruecksetzen
        _loginPasskeyInput = "";
        UpdateDisplay();

        // Error-Popup anzeigen
        if (_popupOverlay    != null) _popupOverlay.style.display    = DisplayStyle.Flex;
        if (_popupErrorLogin != null) _popupErrorLogin.style.display = DisplayStyle.Flex;

        SetCloseButtonEnabled(false);
    }

    private void OnErrorRetry()
    {
        // Error-Popup ausblenden
        if (_popupErrorLogin != null)
            _popupErrorLogin.style.display = DisplayStyle.None;

        // Passkey-Popup wieder anzeigen
        if (_popupPasskeyLogin != null)
            _popupPasskeyLogin.style.display = DisplayStyle.Flex;
    }

    private void CloseAllPopups()
    {
        if (_popupOverlay      != null) _popupOverlay.style.display      = DisplayStyle.None;
        if (_popupPasskeyLogin != null) _popupPasskeyLogin.style.display = DisplayStyle.None;
        if (_popupErrorLogin   != null) _popupErrorLogin.style.display   = DisplayStyle.None;

        _loginPasskeyInput = "";
        UpdateDisplay();

        SetCloseButtonEnabled(true);
    }

    // ─────────────────────────────────────────────────
    // HILFSFUNKTION
    // ─────────────────────────────────────────────────

    private void Bind(string name, System.Action action)
    {
        var btn = _root.Q<Button>(name);
        if (btn != null)
            btn.clicked += action;
        else
            Debug.LogWarning($"[LoginFlow] Button '{name}' nicht gefunden.");
    }
    // ─────────────────────────────────────────────────
    // APP SCHLIESSEN
    // ─────────────────────────────────────────────────
    public void CloseApplication()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }


    private void SetCloseButtonEnabled(bool enabled)
    {
    if (_btnAppClose == null) return;
    _btnAppClose.SetEnabled(enabled);
    }
}