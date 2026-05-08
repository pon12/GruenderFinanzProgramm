using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
 
public class SidebarController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
 
    private const float WIDTH_EXPANDED  = 390f;
    private const float WIDTH_COLLAPSED = 80f;  // genug für 52px RadioButton + 8px margin x2
    private const float ANIM_DURATION   = 0.2f;
 
    private VisualElement _sidebar;
    private Button        _toggleButton;
    private bool          _isCollapsed  = false;
    private bool          _isAnimating  = false;
    private float         _animTime     = 0f;
    private float         _animFrom     = WIDTH_EXPANDED;
    private float         _animTarget   = WIDTH_COLLAPSED;
 
    private readonly string[] _navItemNames = new[]
    {
        "nav-item-dashboard", "nav-item-guide",         "nav-item-finanz",
        "nav-item-kosten",    "nav-item-statistik",     "nav-item-rechnung",
        "nav-item-angebot",   "nav-item-einstellungen", "nav-item-export"
    };
 
    private List<VisualElement> _navItems   = new();
    private VisualElement       _activeItem;
 
    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;
 
        _sidebar      = root.Q<VisualElement>("sidebar");
        _toggleButton = root.Q<Button>("toggle-button");
 
        if (_toggleButton != null)
            _toggleButton.RegisterCallback<ClickEvent>(evt => ToggleSidebar());
 
        foreach (var itemName in _navItemNames)
        {
            var item = root.Q<VisualElement>(itemName);
            if (item == null) continue;
 
            _navItems.Add(item);
            item.RegisterCallback<ClickEvent>(evt => SelectNavItem(item));
 
            if (item.ClassListContains("nav-item--active"))
                _activeItem = item;
        }
    }
 
    // ── Animation ──────────────────────────────
    void Update()
    {
        if (!_isAnimating) return;
 
        _animTime += Time.deltaTime;
        float t    = Mathf.Clamp01(_animTime / ANIM_DURATION);
        float ease = 1f - Mathf.Pow(1f - t, 3f);
        float w    = Mathf.Lerp(_animFrom, _animTarget, ease);
 
        // Nur die Sidebar-Breite ändern – alles andere ergibt sich durch
        // width:100% und overflow:hidden auf den Kindern automatisch
        _sidebar.style.width = w;
 
        if (t >= 1f)
        {
            _isAnimating         = false;
            _sidebar.style.width = _animTarget;
        }
    }
 
    // ── Toggle ──────────────────────────────────
    void ToggleSidebar()
    {
        _isCollapsed  = !_isCollapsed;
        _animFrom     = _isCollapsed ? WIDTH_EXPANDED  : WIDTH_COLLAPSED;
        _animTarget   = _isCollapsed ? WIDTH_COLLAPSED : WIDTH_EXPANDED;
        _animTime     = 0f;
        _isAnimating  = true;
 
        _toggleButton.text = _isCollapsed ? "+" : "−";
    }
 
    // ── Nav-Item auswählen ───────────────────────
    void SelectNavItem(VisualElement selected)
    {
        if (_activeItem != null)
        {
            _activeItem.RemoveFromClassList("nav-item--active");
            var prevRadio = _activeItem.Q<RadioButton>();
            if (prevRadio != null) prevRadio.value = false;
        }
 
        selected.AddToClassList("nav-item--active");
        var radio = selected.Q<RadioButton>();
        if (radio != null) radio.value = true;
 
        _activeItem = selected;
    }
}