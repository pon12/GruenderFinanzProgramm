using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SidebarController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    public static event System.Action<bool> OnToggled;

    private const float WIDTH_EXPANDED  = 390f;
    private const float WIDTH_COLLAPSED = 100f;
    private const float ANIM_DURATION   = 0.2f;

    private const string PREF_COLLAPSED        = "sidebar_collapsed";
    private const string PREF_FORTSCHRITT_OPEN = "sidebar_fortschritt_open";
    private const string PREF_BUCHHALTUNG_OPEN = "sidebar_buchhaltung_open";
    private const string PREF_FINANZEN_OPEN    = "sidebar_finanzen_open";

    private VisualElement _root;
    private VisualElement _sidebar;
    private Button _toggleButton;
    private bool _isCollapsed = false;

    private VisualElement _fortschrittSubmenu;
    private Button _fortschrittToggleBtn;
    private bool _fortschrittOpen = false;

    private VisualElement _buchhaltungSubmenu;
    private Button _buchhaltungToggleBtn;
    private bool _buchhaltungOpen = false;

    private VisualElement _finanzenSubmenu;
    private Button _finanzenToggleBtn;
    private bool _finanzenOpen = false;

    private static readonly Dictionary<string, string> NavToScene = new()
    {
        { "nav-item-dashboard",        "Dashboard"        },
        { "nav-item-fortschritt",      "Fortschritt"      },
        { "nav-item-buchhaltung",      "Buchhaltung"      },
        { "nav-item-finanzen",         "Finanzdashboard"         },
        { "nav-item-dokumente",        "Dokument-Screen"  },
        { "nav-item-gr\u00fcndungspfad", "Gr\u00fcndungspfad"    },
        { "nav-item-wissensdatenbank", "Wissensdatenbank" },
        { "nav-item-erfolge",          "Erfolge"          },
        { "nav-item-angebot",          "Angebot"          },
        { "nav-item-rechnung",         "Rechnung"         },
        { "nav-item-kunden",           "KundenDB"         },
        { "nav-item-dienstleistungen", "Dienstleistungen" },
        { "nav-item-kassenbuch",       "Kassenbuch"       },
        { "nav-item-export",           "Export-Screen"    },
        { "nav-item-liquidit\u00e4t",  "Finanzen1"        },
        { "nav-item-rentabilit\u00e4t","Finanzen2"        },
        { "nav-item-kennzahlen",       "Finanzen3"        },
        { "nav-item-einstellungen",    "Einstellungen"    },
    };

    private static readonly HashSet<string> FortschrittSubItems = new()
    {
        "nav-item-gr\u00fcnderpfad",
        "nav-item-wissensdatenbank",
        "nav-item-erfolge",
    };

    private static readonly HashSet<string> BuchhaltungSubItems = new()
    {
        "nav-item-angebot",
        "nav-item-rechnung",
        "nav-item-kunden",
        "nav-item-dienstleistungen",
    };

    private static readonly HashSet<string> FinanzenSubItems = new()
    {
        "nav-item-kassenbuch",
        "nav-item-export",
        "nav-item-liquidit\u00e4t",
        "nav-item-rentabilit\u00e4t",
        "nav-item-kennzahlen",
    };

    // ─────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        _root    = uiDocument.rootVisualElement;
        _sidebar = _root.Q<VisualElement>("sidebar");

        _toggleButton         = _root.Q<Button>("toggle-button");
        _fortschrittSubmenu   = _root.Q<VisualElement>("fortschritt-submenu");
        _fortschrittToggleBtn = _root.Q<Button>("btn-fortschritt-toggle");
        _buchhaltungSubmenu   = _root.Q<VisualElement>("buchhaltung-submenu");
        _buchhaltungToggleBtn = _root.Q<Button>("btn-buchhaltung-toggle");
        _finanzenSubmenu      = _root.Q<VisualElement>("finanzen-submenu");
        _finanzenToggleBtn    = _root.Q<Button>("btn-finanzen-toggle");

        if (_toggleButton != null) _toggleButton.clicked += ToggleSidebar;

        RegisterNavigation();
        RegisterLogout();
        RestoreState();
        SetActiveNavItem();
    }

    void OnDisable()
    {
        if (_toggleButton != null) _toggleButton.clicked -= ToggleSidebar;

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

        _fortschrittOpen = PlayerPrefs.GetInt(PREF_FORTSCHRITT_OPEN, 0) == 1;
        ApplySubmenuState(_fortschrittSubmenu, _fortschrittToggleBtn, _fortschrittOpen);

        _buchhaltungOpen = PlayerPrefs.GetInt(PREF_BUCHHALTUNG_OPEN, 0) == 1;
        ApplySubmenuState(_buchhaltungSubmenu, _buchhaltungToggleBtn, _buchhaltungOpen);

        _finanzenOpen = PlayerPrefs.GetInt(PREF_FINANZEN_OPEN, 0) == 1;
        ApplySubmenuState(_finanzenSubmenu, _finanzenToggleBtn, _finanzenOpen);

        OnToggled?.Invoke(_isCollapsed);
    }

    private void ApplySubmenuState(VisualElement submenu, Button toggleBtn, bool isOpen)
    {
        if (submenu != null)
            submenu.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (toggleBtn != null)
            toggleBtn.style.rotate = new StyleRotate(new Rotate(isOpen ? 90f : 0f));
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(PREF_COLLAPSED,        _isCollapsed     ? 1 : 0);
        PlayerPrefs.SetInt(PREF_FORTSCHRITT_OPEN, _fortschrittOpen ? 1 : 0);
        PlayerPrefs.SetInt(PREF_BUCHHALTUNG_OPEN, _buchhaltungOpen ? 1 : 0);
        PlayerPrefs.SetInt(PREF_FINANZEN_OPEN,    _finanzenOpen    ? 1 : 0);
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
    // SUBMENU TOGGLES
    // ─────────────────────────────────────────────────

    private void ToggleFortschrittSubmenu()
    {
        _fortschrittOpen = !_fortschrittOpen;
        ApplySubmenuState(_fortschrittSubmenu, _fortschrittToggleBtn, _fortschrittOpen);
        SaveState();
    }

    private void ToggleBuchhaltungSubmenu()
    {
        _buchhaltungOpen = !_buchhaltungOpen;
        ApplySubmenuState(_buchhaltungSubmenu, _buchhaltungToggleBtn, _buchhaltungOpen);
        SaveState();
    }

    private void ToggleFinanzenSubmenu()
    {
        _finanzenOpen = !_finanzenOpen;
        ApplySubmenuState(_finanzenSubmenu, _finanzenToggleBtn, _finanzenOpen);
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

            if (FortschrittSubItems.Contains(kvp.Key) && !_fortschrittOpen)
            {
                _fortschrittOpen = true;
                ApplySubmenuState(_fortschrittSubmenu, _fortschrittToggleBtn, true);
            }
            if (BuchhaltungSubItems.Contains(kvp.Key) && !_buchhaltungOpen)
            {
                _buchhaltungOpen = true;
                ApplySubmenuState(_buchhaltungSubmenu, _buchhaltungToggleBtn, true);
            }
            if (FinanzenSubItems.Contains(kvp.Key) && !_finanzenOpen)
            {
                _finanzenOpen = true;
                ApplySubmenuState(_finanzenSubmenu, _finanzenToggleBtn, true);
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
            string itemName  = kvp.Key;
            var item = _root.Q<VisualElement>(itemName);

            if (item == null)
            {
                Debug.LogWarning($"[Sidebar] Nav-Item '{itemName}' nicht gefunden.");
                continue;
            }

            // Dropdown-Parent-Items: Chevron stoppt Propagation (TrickleDown),
            // damit nur das Submenü getoggelt wird ohne Szenennavigation.
            // Klick auf das Parent-Item selbst toggelt Submenü UND lädt Szene.
            if (itemName == "nav-item-fortschritt")
            {
                _fortschrittToggleBtn?.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    ToggleFortschrittSubmenu();
                }, TrickleDown.TrickleDown);

                item.RegisterCallback<ClickEvent>(evt =>
                {
                    if (!_fortschrittOpen)
                        ToggleFortschrittSubmenu();
                    NavigateToScene(sceneName);
                });
                continue;
            }

            if (itemName == "nav-item-buchhaltung")
            {
                _buchhaltungToggleBtn?.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    ToggleBuchhaltungSubmenu();
                }, TrickleDown.TrickleDown);

                item.RegisterCallback<ClickEvent>(evt =>
                {
                    if (!_buchhaltungOpen)
                        ToggleBuchhaltungSubmenu();
                    NavigateToScene(sceneName);
                });
                continue;
            }

            if (itemName == "nav-item-finanzen")
            {
                _finanzenToggleBtn?.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    ToggleFinanzenSubmenu();
                }, TrickleDown.TrickleDown);

                item.RegisterCallback<ClickEvent>(evt =>
                {
                    if (!_finanzenOpen)
                        ToggleFinanzenSubmenu();
                    NavigateToScene(sceneName);
                });
                continue;
            }

            // Normale Nav-Items: nur Szene laden
            item.RegisterCallback<ClickEvent>(evt => NavigateToScene(sceneName));
        }
    }

    private void NavigateToScene(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (path.Contains(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
        }
        Debug.LogWarning($"[Sidebar] Szene '{sceneName}' nicht in Build Settings gefunden.");
    }

    private void OnNavItemClicked(ClickEvent evt)
    {
        var item = evt.currentTarget as VisualElement;
        if (item == null) return;

        foreach (var kvp in NavToScene)
        {
            if (kvp.Key == item.name)
            {
                NavigateToScene(kvp.Value);
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

        PlayerPrefs.DeleteKey(PREF_COLLAPSED);
        PlayerPrefs.DeleteKey(PREF_FORTSCHRITT_OPEN);
        PlayerPrefs.DeleteKey(PREF_BUCHHALTUNG_OPEN);
        PlayerPrefs.DeleteKey(PREF_FINANZEN_OPEN);
        PlayerPrefs.Save();

        SceneManager.LoadScene(0);
    }
}