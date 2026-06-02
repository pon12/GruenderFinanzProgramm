using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SidebarController : MonoBehaviour
{

    private VisualElement _finanzSubmenu;
    private Button        _finanzToggleBtn;
    private bool          _finanzOpen = false;
    public static event System.Action<bool> OnToggled;
    
    [System.Serializable]
    public struct SceneMapping
    {
        public string buttonName; 
        public string sceneName;  
    }

    [Header("Zuweisungen")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private List<SceneMapping> navigationConfig;

    private const float WIDTH_EXPANDED  = 390f;
    private const float WIDTH_COLLAPSED = 80f;
    private const float ANIM_DURATION   = 0.2f;

    private VisualElement _sidebar;
    private Button        _toggleButton;

    private bool          _isCollapsed  = false;
    private bool          _isAnimating  = false;
    private float         _animTime     = 0f;
    private float         _animFrom     = WIDTH_EXPANDED;
    private float         _animTarget   = WIDTH_COLLAPSED;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        _sidebar = root.Q<VisualElement>("sidebar");
        _toggleButton = root.Q<Button>("toggle-button");

        // SICHERHEITS-CHECK: Sidebar sichtbar machen
        if (_sidebar != null)
        {
            _sidebar.style.width = WIDTH_EXPANDED;
            _sidebar.style.display = DisplayStyle.Flex;
            _sidebar.style.opacity = 1f;
        }

        if (_toggleButton != null)
            _toggleButton.RegisterCallback<ClickEvent>(evt => ToggleSidebar());

        // Navigation zuweisen
        foreach (var mapping in navigationConfig)
        {
            if (string.IsNullOrEmpty(mapping.buttonName)) continue;
            var btn = root.Q<VisualElement>(mapping.buttonName);
            if (btn != null)
            {
                btn.RegisterCallback<ClickEvent>(evt => SceneManager.LoadScene(mapping.sceneName));
            }
        }
    
    {
    _finanzSubmenu   = root.Q<VisualElement>("finanz-submenu");
    _finanzToggleBtn = root.Q<Button>("btn-finanz-toggle");

    if (_finanzToggleBtn != null)
        _finanzToggleBtn.clicked += ToggleFinanzSubmenu;
    }
    }

    private void ToggleFinanzSubmenu()
{
    _finanzOpen = !_finanzOpen;

    if (_finanzSubmenu != null)
        _finanzSubmenu.style.display = _finanzOpen
            ? DisplayStyle.Flex
            : DisplayStyle.None;

    if (_finanzToggleBtn != null)
        _finanzToggleBtn.text = _finanzOpen ? "▼" : "▶";
}

    void OnDisable()
{
    if (_toggleButton != null)
        _toggleButton.clicked -= ToggleSidebar;
    
    // Statisches Event komplett leeren beim Deaktivieren
    OnToggled = null;
}

    void Update()
    {
        if (!_isAnimating || _sidebar == null) return;

        _animTime += Time.deltaTime;
        float t = Mathf.Clamp01(_animTime / ANIM_DURATION);
        float ease = 1f - Mathf.Pow(1f - t, 3f);
        _sidebar.style.width = Mathf.Lerp(_animFrom, _animTarget, ease);

        if (t >= 1f)
        {
            _isAnimating = false;
            _sidebar.style.width = _animTarget;
        }
    }

    void ToggleSidebar()
    {
        _isCollapsed = !_isCollapsed;
        _animFrom = _isCollapsed ? WIDTH_EXPANDED : WIDTH_COLLAPSED;
        _animTarget = _isCollapsed ? WIDTH_COLLAPSED : WIDTH_EXPANDED;
        _animTime = 0f;
        _isAnimating = true;
        OnToggled?.Invoke(_isCollapsed);
    }

    
}

