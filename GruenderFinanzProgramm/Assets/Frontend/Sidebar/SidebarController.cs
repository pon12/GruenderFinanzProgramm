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

    private const float WIDTH_EXPANDED = 390f;
    private const float WIDTH_COLLAPSED = 100f;
    private const float ANIM_DURATION = 0.2f;

    private const string PREF_COLLAPSED = "sidebar_collapsed";
    private const string PREF_FORTSCHRITT_OPEN = "sidebar_fortschritt_open";
    private const string PREF_FINANZ_OPEN = "sidebar_finanz_open";

    private VisualElement _root;
    private VisualElement _sidebar;
    private Button _toggleButton;
    private bool _isCollapsed = false;

    private VisualElement _finanzSubmenu;
    private Button _finanzToggleBtn;
    private bool _finanzOpen = false;
    
    private VisualElement _fortschrittSubmenu;
    private Button _fortschrittToggleBtn;
    private bool _fortschrittOpen = false;

    private static readonly Dictionary<string, string> NavToScene = new()
    {
        { "nav-item-dashboard",        "Dashboard"        },
        { "nav-item-guide",            "Fortschritt"      },
        { "nav-item-wissensdatenbank", "Wissensdatenbank"  },
        { "nav-item-finanz",           "Finanzboard"      },
        { "nav-item-angebot",          "Angebot"          },
        { "nav-item-rechnung",         "Rechnung"         },
        { "nav-item-kunden",           "KundenDB"         },
        { "nav-item-dienstleistungen", "Dienstleistungen" },
        { "nav-item-kassenbuch",       "Kassenbuch"       },
        { "nav-item-export",           "Export-Screen"    },
        { "nav-item-dokumente",        "Dokument-Screen"  },
        { "nav-item-einstellungen",    "Einstellungen"    },
    };

    private static readonly HashSet<string> FinanzSubItems = new()
    {
        "nav-item-angebot",
        "nav-item-rechnung",
        "nav-item-kunden",
        "nav-item-dienstleistungen",
        "nav-item-kassenbuch",
    };
    
    private static readonly HashSet<string> FortschrittSubItems = new()
    {
        "nav-item-wissensdatenbank",
    };

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        _root = uiDocument.rootVisualElement;
        _sidebar = _root.Q<VisualElement>("sidebar");
        _toggleButton = _root.Q<Button>("toggle-button");
        
        _fortschrittSubmenu = _root.Q<VisualElement>("fortschritt-submenu");
        _fortschrittToggleBtn = _root.Q<Button>("btn-fortschritt-toggle");

        _finanzSubmenu = _root.Q<VisualElement>("finanz-submenu");
        _finanzToggleBtn = _root.Q<Button>("btn-finanz-toggle");
        
        if (_toggleButton != null) _toggleButton.clicked += ToggleSidebar;
        if (_fortschrittToggleBtn != null)
            _fortschrittToggleBtn.clicked += ToggleFortschrittSubmenu;
        if (_finanzToggleBtn != null)
        _finanzToggleBtn.clicked += ToggleFinanzSubmenu;

        RegisterNavigation();
        RegisterLogout();

        RestoreState();
        SetActiveNavItem();
    }

    void OnDisable()
    {
        if (_toggleButton != null) _toggleButton.clicked -= ToggleSidebar;
        if (_fortschrittToggleBtn != null)
            _fortschrittToggleBtn.clicked -= ToggleFortschrittSubmenu;
        if (_finanzToggleBtn != null) _finanzToggleBtn.clicked -= ToggleFinanzSubmenu;

        foreach (var kvp in NavToScene)
        {
            var item = _root?.Q<VisualElement>(kvp.Key);
            if (item != null)
                item.UnregisterCallback<ClickEvent>(OnNavItemClicked);
        }

        var logoutItem = _root?.Q<VisualElement>("nav-item-logout");
        if (logoutItem != null)
            logoutItem.UnregisterCallback<ClickEvent>(OnLogoutClicked);
    }

    // ─────────────────────────────────────────────────
    // STATE PERSISTENZ
    // ─────────────────────────────────────────────────

    private void RestoreState()
    {
        _isCollapsed = PlayerPrefs.GetInt(PREF_COLLAPSED, 0) == 1;

        if (_sidebar != null)
            _sidebar.style.width = _isCollapsed ? WIDTH_COLLAPSED : WIDTH_EXPANDED;

        if (_toggleButton != null)
            _toggleButton.text = _isCollapsed ? "+" : "-";

        UpdateNavLabelsVisibility();

        _finanzOpen = PlayerPrefs.GetInt(PREF_FINANZ_OPEN, 0) == 1;

        if (_finanzSubmenu != null) _finanzSubmenu.style.display = _finanzOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (_finanzToggleBtn != null)
        _finanzToggleBtn.style.rotate = new StyleRotate(new Rotate(_finanzOpen ? 90f : 0f));
        
        _fortschrittOpen = PlayerPrefs.GetInt(PREF_FORTSCHRITT_OPEN, 0) == 1;
        if (_fortschrittSubmenu != null)
            _fortschrittSubmenu.style.display = _fortschrittOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (_fortschrittToggleBtn != null)
            _fortschrittToggleBtn.style.rotate = new StyleRotate(new Rotate(_fortschrittOpen ? 90f : 0f));

        OnToggled?.Invoke(_isCollapsed);
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(PREF_COLLAPSED, _isCollapsed ? 1 : 0);
        PlayerPrefs.SetInt(PREF_FORTSCHRITT_OPEN, _fortschrittOpen ? 1 : 0);
        PlayerPrefs.SetInt(PREF_FINANZ_OPEN, _finanzOpen ? 1 : 0);
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
            _toggleButton.text = _isCollapsed ? "+" : "-";

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
        float elapsed = 0f;

        while (elapsed < ANIM_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ANIM_DURATION);
            float eased = t * t * (3f - 2f * t);
            _sidebar.style.width = Mathf.Lerp(startWidth, targetWidth, eased);
            yield return null;
        }

        _sidebar.style.width = targetWidth;
    }

    // ─────────────────────────────────────────────────
    // FORTSCHRITT UND FINANZBOARD DROPDOWN
    // ─────────────────────────────────────────────────

   private void ToggleFinanzSubmenu()
{
    _finanzOpen = !_finanzOpen;

    if (_finanzSubmenu != null) _finanzSubmenu.style.display = _finanzOpen ? DisplayStyle.Flex : DisplayStyle.None;

    // Icon rotieren: 0° = zugeklappt, 90° = aufgeklappt
    if (_finanzToggleBtn != null)
        _finanzToggleBtn.style.rotate = new StyleRotate(new Rotate(_finanzOpen ? 90f : 0f));

    SaveState();
}
   
private void ToggleFortschrittSubmenu()
{
    _fortschrittOpen = !_fortschrittOpen;
    if (_fortschrittSubmenu != null)
        _fortschrittSubmenu.style.display = _fortschrittOpen ? DisplayStyle.Flex : DisplayStyle.None;
    if (_fortschrittToggleBtn != null)
        _fortschrittToggleBtn.style.rotate = new StyleRotate(new Rotate(_fortschrittOpen ? 90f : 0f));
    SaveState();
}
    // ─────────────────────────────────────────────────
    // ACTIVE NAV ITEM HIGHLIGHTING
    // ─────────────────────────────────────────────────

    private void SetActiveNavItem()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        foreach (var navName in NavToScene.Keys)
        {
            var item = _root.Q<VisualElement>(navName);
            if (item != null)
                item.RemoveFromClassList("nav-item--active");
        }

        foreach (var kvp in NavToScene)
        {
            if (kvp.Value != currentScene) continue;

            var item = _root.Q<VisualElement>(kvp.Key);
            if (item != null)
                item.AddToClassList("nav-item--active");

            if (FinanzSubItems.Contains(kvp.Key) && !_finanzOpen)
            {
                _finanzOpen = true;
                if (_finanzSubmenu != null) _finanzSubmenu.style.display = DisplayStyle.Flex;
                if (_finanzToggleBtn != null)
                _finanzToggleBtn.style.rotate = new StyleRotate(new Rotate(_finanzOpen ? 90f : 0f));
            }
            
            if (FortschrittSubItems.Contains(kvp.Key) && !_fortschrittOpen)
            {
                _fortschrittOpen = true;
                if (_fortschrittSubmenu != null)
                    _fortschrittSubmenu.style.display = DisplayStyle.Flex;
                if (_fortschrittToggleBtn != null)
                    _fortschrittToggleBtn.style.rotate = new StyleRotate(new Rotate(90f));
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
            if (item == null)
            {
                Debug.LogWarning($"[Sidebar] Nav-Item '{kvp.Key}' nicht gefunden.");
                continue;
            }
            
            if (kvp.Key == "nav-item-guide")
            {
                item.RegisterCallback<ClickEvent>(evt => ToggleFortschrittSubmenu());
                if (_fortschrittToggleBtn != null)
                    _fortschrittToggleBtn.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
                continue;
            }

          if (kvp.Key == "nav-item-finanz")
{
            item.RegisterCallback<ClickEvent>(evt => ToggleFinanzSubmenu());
             // Button-Klick nicht nach oben weitergeben
            if (_finanzToggleBtn != null)
            _finanzToggleBtn.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            continue;
}

            item.RegisterCallback<ClickEvent>(evt =>
            {   
                bool sceneExists = false;
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    if (path.Contains(sceneName))
                    {
                        sceneExists = true;
                        break;
                    }
                }
                
                if (sceneExists)
                    SceneManager.LoadScene(sceneName);
                else
                    Debug.LogWarning($"[Sidebar] Scene '{sceneName}' nicht in Build Settings gefunden.");
            });
        }
    }

    private void OnNavItemClicked(ClickEvent evt)
    {
        var item = evt.currentTarget as VisualElement;
        if (item == null) return;

        foreach (var kvp in NavToScene)
        {
            if (kvp.Key == item.name)
            {
                SceneManager.LoadScene(kvp.Value);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────
    // LOGOUT
    // ─────────────────────────────────────────────────

    private void RegisterLogout()
    {
        var logoutItem = _root.Q<VisualElement>("nav-item-logout");
        if (logoutItem == null) return;
        logoutItem.RegisterCallback<ClickEvent>(OnLogoutClicked);
    }

    private void OnLogoutClicked(ClickEvent evt)
    {
        var logoutController = FindAnyObjectByType<MainLogoutController>();
        if (logoutController != null)
            logoutController.logout();
        else
            Debug.LogWarning("[Sidebar] MainLogoutController nicht gefunden.");

        PlayerPrefs.DeleteKey("sidebar_collapsed");
        PlayerPrefs.DeleteKey("sidebar_fortschritt_open");
        PlayerPrefs.DeleteKey("sidebar_finanz_open");
        PlayerPrefs.Save();

        SceneManager.LoadScene(0);
    }
}