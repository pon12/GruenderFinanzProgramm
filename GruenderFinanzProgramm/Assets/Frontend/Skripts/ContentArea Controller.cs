// ContentAreaController.cs
// Liegt auf demselben GameObject wie das UIDocument des Screens
// (z.B. "Dienstleistungen UI" oder "UIManager")
//
// Reagiert auf SidebarController.OnToggled und verschiebt
// den Hauptinhalt synchron zur Sidebar-Animation.
//
// VORAUSSETZUNG: SidebarController.cs braucht diese zwei Zeilen:
//
//   // Direkt unter "public class SidebarController : MonoBehaviour {"
//   public static event System.Action<bool> OnToggled;
//
//   // Am Ende von ToggleSidebar(), nach _isAnimating = true:
//   OnToggled?.Invoke(_isCollapsed);

using UnityEngine;
using UnityEngine.UIElements;

public class ContentAreaController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    // Padding-Werte: sidebar-breite + 40px content-abstand
    // Expanded:  390 + 40 = 430
    // Collapsed:  80 + 40 = 120
    private const float PADDING_EXPANDED  = 430f;
    private const float PADDING_COLLAPSED = 120f;
    private const float ANIM_DURATION     = 0.2f;  // Gleich wie in SidebarController

    private VisualElement _mainContent;
    private bool  _isAnimating  = false;
    private float _animTime     = 0f;
    private float _animFrom     = PADDING_EXPANDED;
    private float _animTarget   = PADDING_COLLAPSED;

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        _mainContent = uiDocument?.rootVisualElement.Q<VisualElement>("main-content");

        if (_mainContent == null)
            Debug.LogWarning("[ContentAreaController] 'main-content' nicht gefunden. " +
                             "Pruefen ob name=\"main-content\" im UXML gesetzt ist.");

        // Auf Sidebar-Toggle reagieren
        SidebarController.OnToggled += OnSidebarToggled;
    }

    void OnDisable()
    {
        SidebarController.OnToggled -= OnSidebarToggled;
    }

    void Update()
    {
        if (!_isAnimating || _mainContent == null) return;

        _animTime += Time.deltaTime;
        float t    = Mathf.Clamp01(_animTime / ANIM_DURATION);
        float ease = 1f - Mathf.Pow(1f - t, 3f); // Gleiche Kurve wie SidebarController
        _mainContent.style.paddingLeft = Mathf.Lerp(_animFrom, _animTarget, ease);

        if (t >= 1f)
        {
            _isAnimating = false;
            _mainContent.style.paddingLeft = _animTarget;
        }
    }

    private void OnSidebarToggled(bool isCollapsed)
    {
        if (_mainContent == null) return;

        _animFrom   = isCollapsed ? PADDING_EXPANDED  : PADDING_COLLAPSED;
        _animTarget = isCollapsed ? PADDING_COLLAPSED : PADDING_EXPANDED;
        _animTime   = 0f;
        _isAnimating = true;
    }
}
