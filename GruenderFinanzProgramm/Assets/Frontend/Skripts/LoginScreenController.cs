// LoginFlowController.cs
// Auf UIManager in der Login-Scene
// Nutzt die echten Button-Namen aus dem UXML

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class LoginFlowController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private const string REGISTER_SCENE  = "Registry";
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

        _root = uiDocument.rootVisualElement;
        _popupOverlay        = _root.Q("popup-overlay");
        _popupPasskeyLogin   = _root.Q("popup-passkey-login");
        _loginPasskeyDisplay = _root.Q<Label>("login-passkey-display");

        RegisterAllButtons();
    }

    private void RegisterAllButtons()
    {
        // Welcome Screen
        Bind("Anmelden",       () => ShowPopup(_popupPasskeyLogin));
        Bind("KontoErstellen", () => SceneManager.LoadScene(REGISTER_SCENE));

        // Passkey-Login Popup
        Bind("btn-close-login-popup", CloseAllPopups);
        Bind("login-btn-delete",      OnPasskeyDelete);
        Bind("btn-login-submit",      OnLoginSubmit);

        for (int i = 0; i <= 9; i++)
        {
            int digit = i;
            var btn = _root.Q<Button>($"login-btn-{digit}");
            if (btn != null)
                btn.clicked += () => OnPasskeyDigit(digit.ToString());
        }
    }

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

    private void OnLoginSubmit()
    {
        if (_loginPasskeyInput.Length < 4) { Debug.Log("[Login] Passkey unvollstaendig"); return; }
        Debug.Log("[Login] Weiter zum Dashboard");
        SceneManager.LoadScene(DASHBOARD_SCENE);
    }

    private void ShowPopup(VisualElement popup)
    {
        if (_popupOverlay    != null) _popupOverlay.style.display    = DisplayStyle.Flex;
        if (popup            != null) popup.style.display             = DisplayStyle.Flex;
    }

    private void CloseAllPopups()
    {
        if (_popupOverlay      != null) _popupOverlay.style.display      = DisplayStyle.None;
        if (_popupPasskeyLogin != null) _popupPasskeyLogin.style.display = DisplayStyle.None;
        _loginPasskeyInput = "";
        UpdateDisplay();
    }

    private void Bind(string name, System.Action action)
    {
        var btn = _root.Q<Button>(name);
        if (btn != null) btn.clicked += action;
        else Debug.LogWarning($"[LoginFlow] '{name}' nicht gefunden.");
    }
}
