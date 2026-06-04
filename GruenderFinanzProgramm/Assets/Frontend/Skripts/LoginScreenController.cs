// LoginFlowController.cs
// Auf UIManager in der Login-Scene

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class LoginFlowController : MonoBehaviour
{
    [SerializeField] private UIDocument          uiDocument;
    [SerializeField] private PassKeyAuthController passKeyAuthController;

    private const string DASHBOARD_SCENE = "Dashboard";

    private VisualElement _root;
    private VisualElement _popupOverlay;
    private VisualElement _popupPasskeyLogin;
    private Label         _loginPasskeyDisplay;
    private string        _loginPasskeyInput = "";

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        // Falls nicht im Inspector zugewiesen, in der Scene suchen
        if (passKeyAuthController == null)
            passKeyAuthController = FindFirstObjectByType<PassKeyAuthController>();

        if (passKeyAuthController == null)
            Debug.LogError("[LoginFlow] PassKeyAuthController nicht gefunden. " +
                           "Bitte Prefab in die Scene ziehen oder im Inspector zuweisen.");

        _root                = uiDocument.rootVisualElement;
        _popupOverlay        = _root.Q("popup-overlay");
        _popupPasskeyLogin   = _root.Q("popup-passkey-login");
        _loginPasskeyDisplay = _root.Q<Label>("login-passkey-display");

        RegisterAllButtons();
    }

    // ─────────────────────────────────────────────────
    // BUTTON REGISTRIERUNG
    // ─────────────────────────────────────────────────

    private void RegisterAllButtons()
    {
        // "Anmelden" → Passkey-Popup öffnen
        Bind("Anmelden", () => ShowPopup(_popupPasskeyLogin));

        // X Button → Popup schließen und Input zurücksetzen
        Bind("btn-close-login-popup", CloseAllPopups);

        // Zahlenpad 0–9 (Passkey: 4-stellig, nur Zahlen laut Doku)
        for (int i = 0; i <= 9; i++)
        {
            int digit = i;
            var btn = _root.Q<Button>($"login-btn-{digit}");
            if (btn != null)
                btn.clicked += () => OnPasskeyDigit(digit.ToString());
            else
                Debug.LogWarning($"[LoginFlow] login-btn-{digit} nicht gefunden.");
        }

        // Löschen
        Bind("login-btn-delete", OnPasskeyDelete);

        // Anmelden absenden → Backend
        Bind("btn-login-submit", OnLoginSubmit);
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
        string s = "";
        for (int i = 0; i < 4; i++)
            s += i < _loginPasskeyInput.Length ? "● " : "_ ";
        _loginPasskeyDisplay.text = s.TrimEnd();
    }

    // ─────────────────────────────────────────────────
    // LOGIN – BACKEND VERKNÜPFUNG
    // ─────────────────────────────────────────────────

    private void OnLoginSubmit()
    {
        if (_loginPasskeyInput.Length < 4)
        {
            Debug.LogWarning("[Login] Passkey unvollständig – bitte alle 4 Ziffern eingeben.");
            return;
        }

        if (passKeyAuthController == null)
        {
            Debug.LogError("[Login] PassKeyAuthController fehlt – Login nicht möglich.");
            return;
        }

        // Backend: Nutzer mit PassKey einloggen
        // Nach erfolgreichem Aufruf ist die passende NutzerDB automatisch aktiv
        // Alle weiteren Screens nutzen dann UserDatabaseAccess.getCurrentUserDatabase()
        passKeyAuthController.loginWithPassKey();

        Debug.Log("[Login] loginWithPassKey aufgerufen – lade Dashboard.");
        SceneManager.LoadScene(DASHBOARD_SCENE);
    }

    // ─────────────────────────────────────────────────
    // POPUP STEUERUNG
    // ─────────────────────────────────────────────────

    private void ShowPopup(VisualElement popup)
    {
        if (_popupOverlay != null) _popupOverlay.style.display  = DisplayStyle.Flex;
        if (popup         != null) popup.style.display           = DisplayStyle.Flex;
    }

    private void CloseAllPopups()
    {
        if (_popupOverlay      != null) _popupOverlay.style.display      = DisplayStyle.None;
        if (_popupPasskeyLogin != null) _popupPasskeyLogin.style.display = DisplayStyle.None;
        _loginPasskeyInput = "";
        UpdateDisplay();
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
            Debug.LogWarning($"[LoginFlow] Button '{name}' nicht gefunden im UXML.");
    }
}