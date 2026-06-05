// SidebarController.cs
// Auf UI_Sidebar_Root Prefab

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SidebarController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    public static event System.Action<bool> OnToggled;

    [System.Serializable]
    public struct SceneMapping
    {
        public string buttonName;
        public string sceneName;
    }

    [Header("Zuweisungen")]
    [SerializeField] private List<SceneMapping> navigationConfig;

    private const float WIDTH_EXPANDED  = 390f;
    private const float WIDTH_COLLAPSED = 80f;
    private const float ANIM_DURATION   = 0.2f;

    // PlayerPrefs Keys fuer State-Persistenz
    private const string PREF_COLLAPSED   = "sidebar_collapsed";
    private const string PREF_FINANZ_OPEN = "sidebar_finanz_open";

    private VisualElement _root;
    private VisualElement _sidebar;
    private Button        _toggleButton;
    private bool          _isCollapsed = false;

    // Finanzboard Dropdown
    private VisualElement _finanzSubmenu;
    private Button        _finanzToggleBtn;
    private bool          _finanzOpen = false;

    // Nav-Item Name → Scene Name
    private static readonly Dictionary<string, string> NavToScene = new()
    {
        { "nav-item-dashboard",        "Dashboard"        },
        { "nav-item-guide",            "Fortschritt"      },
        { "nav-item-finanz",           "Finanzboard"      },
        { "nav-item-angebot",          "Angebot"          },
        { "nav-item-rechnung",         "Rechnung"         },
        { "nav-item-kunden",           "KundenDB"  },
        { "nav-item-dienstleistungen", "Dienstleistungen" },
        { "nav-item-kassenbuch",       "Kassenbuch"       },
        { "nav-item-einstellungen",    "Einstellungen"    },
    };

    // Sub-Items die unter Finanzboard liegen
    private static readonly HashSet<string> FinanzSubItems = new()
    {
        "nav-item-angebot",
        "nav-item-rechnung",
        "nav-item-kunden",
        "nav-item-dienstleistungen",
        "nav-item-kassenbuch",
    };

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        _root         = uiDocument.rootVisualElement;
        _sidebar      = _root.Q<VisualElement>("sidebar");
        _toggleButton = _root.Q<Button>("toggle-button");

        _finanzSubmenu   = _root.Q<VisualElement>("finanz-submenu");
        _finanzToggleBtn = _root.Q<Button>("btn-finanz-toggle");

        if (_toggleButton    != null) _toggleButton.clicked    += ToggleSidebar;
        if (_finanzToggleBtn != null) _finanzToggleBtn.clicked += ToggleFinanzSubmenu;

        RegisterNavigation();
        RegisterLogout();

        // Gespeicherten State wiederherstellen
        RestoreState();

        // Aktives Nav-Item anhand aktueller Scene setzen
        SetActiveNavItem();
    }

    void OnDisable()
    {
        if (_toggleButton    != null) _toggleButton.clicked    -= ToggleSidebar;
        if (_finanzToggleBtn != null) _finanzToggleBtn.clicked -= ToggleFinanzSubmenu;
    }

    // ─────────────────────────────────────────────────
    // STATE PERSISTENZ
    // ─────────────────────────────────────────────────

    private void RestoreState()
    {
        // Sidebar Collapsed/Expanded State
        _isCollapsed = PlayerPrefs.GetInt(PREF_COLLAPSED, 0) == 1;

        if (_sidebar != null)
            _sidebar.style.width = _isCollapsed ? WIDTH_COLLAPSED : WIDTH_EXPANDED;

        if (_toggleButton != null)
            _toggleButton.text = _isCollapsed ? "+" : "−";

        UpdateNavLabelsVisibility();

        // Finanzboard Dropdown State
        _finanzOpen = PlayerPrefs.GetInt(PREF_FINANZ_OPEN, 0) == 1;

        if (_finanzSubmenu   != null) _finanzSubmenu.style.display  = _finanzOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (_finanzToggleBtn != null) _finanzToggleBtn.text          = _finanzOpen ? "▼" : "▶";

        // ContentAreaController informieren
        OnToggled?.Invoke(_isCollapsed);
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(PREF_COLLAPSED,   _isCollapsed ? 1 : 0);
        PlayerPrefs.SetInt(PREF_FINANZ_OPEN, _finanzOpen  ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ─────────────────────────────────────────────────
    // SIDEBAR TOGGLE
    // ─────────────────────────────────────────────────

    private void ToggleSidebar()
    {
        _isCollapsed = !_isCollapsed;

        StartCoroutine(AnimateWidth(_isCollapsed ? WIDTH_COLLAPSED : WIDTH_EXPANDED));

        if (_toggleButton != null)
            _toggleButton.text = _isCollapsed ? "+" : "−";

        UpdateNavLabelsVisibility();
        OnToggled?.Invoke(_isCollapsed);
        SaveState();
    }

    private void UpdateNavLabelsVisibility()
    {
        var labels = _root.Query<Label>(className: "nav-label").ToList();
        foreach (var label in labels)
            label.style.display = _isCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private IEnumerator AnimateWidth(float targetWidth)
    {
        if (_sidebar == null) yield break;

        float startWidth = _sidebar.resolvedStyle.width;
        float elapsed    = 0f;

        while (elapsed < ANIM_DURATION)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / ANIM_DURATION);
            float eased = t * t * (3f - 2f * t);
            _sidebar.style.width = Mathf.Lerp(startWidth, targetWidth, eased);
            yield return null;
        }

        _sidebar.style.width = targetWidth;
    }

    // ─────────────────────────────────────────────────
    // FINANZBOARD DROPDOWN
    // ─────────────────────────────────────────────────

    private void ToggleFinanzSubmenu()
    {
        _finanzOpen = !_finanzOpen;

        if (_finanzSubmenu   != null) _finanzSubmenu.style.display  = _finanzOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (_finanzToggleBtn != null) _finanzToggleBtn.text          = _finanzOpen ? "▼" : "▶";

        SaveState();
    }

    // ─────────────────────────────────────────────────
    // ACTIVE NAV ITEM HIGHLIGHTING
    // ─────────────────────────────────────────────────

    private void SetActiveNavItem()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // Alle Highlights zuruecksetzen
        foreach (var navName in NavToScene.Keys)
        {
            var item = _root.Q<VisualElement>(navName);
            if (item != null)
                item.RemoveFromClassList("nav-item--active");
        }

        // Passendes Item highlighten
        foreach (var kvp in NavToScene)
        {
            if (kvp.Value != currentScene) continue;

            var item = _root.Q<VisualElement>(kvp.Key);
            if (item != null)
                item.AddToClassList("nav-item--active");

            // Wenn Sub-Item aktiv → Finanzboard automatisch aufklappen
            if (FinanzSubItems.Contains(kvp.Key) && !_finanzOpen)
            {
                _finanzOpen = true;
                if (_finanzSubmenu   != null) _finanzSubmenu.style.display  = DisplayStyle.Flex;
                if (_finanzToggleBtn != null) _finanzToggleBtn.text          = "▼";
            }

            break;
        }
    }

    // ─────────────────────────────────────────────────
    // NAVIGATION
    // ─────────────────────────────────────────────────

    private void RegisterNavigation()
    {
        foreach (var kvp in NavToScene)
        {
            string sceneName = kvp.Value;
            var item = _root.Q<VisualElement>(kvp.Key);
            if (item != null)
                item.RegisterCallback<ClickEvent>(_ => SceneManager.LoadScene(sceneName));
            else
                Debug.LogWarning($"[Sidebar] Nav-Item '{kvp.Key}' nicht gefunden.");
        }
    }

    // ─────────────────────────────────────────────────
    // LOGOUT – BACKEND VERKNUEPFUNG
    // ─────────────────────────────────────────────────

    private void RegisterLogout()
    {
        var logoutItem = _root.Q<VisualElement>("nav-item-logout");
        if (logoutItem == null) return;

        logoutItem.RegisterCallback<ClickEvent>(_ =>
        {
            // Backend: Nutzer ausloggen – NutzerDB wird deaktiviert
            var logoutController = FindFirstObjectByType<MainLogoutController>();
            if (logoutController != null)
                logoutController.logout();
            else
                Debug.LogWarning("[Sidebar] MainLogoutController nicht gefunden.");

            // State beim Logout komplett zuruecksetzen
            PlayerPrefs.DeleteKey(PREF_COLLAPSED);
            PlayerPrefs.DeleteKey(PREF_FINANZ_OPEN);
            PlayerPrefs.Save();

            SceneManager.LoadScene("Login");
        });
    }
}