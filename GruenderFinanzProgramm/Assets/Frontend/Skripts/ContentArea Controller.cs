// ContentAreaController.cs
// Auf UIManager jeder App-Scene

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class ContentAreaController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private const float PADDING_EXPANDED  = 430f;
    private const float PADDING_COLLAPSED = 120f;
    private const float ANIM_DURATION     = 0.2f;
    private const string PREF_COLLAPSED   = "sidebar_collapsed";

    private VisualElement _mainContent;

    void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        _mainContent = uiDocument.rootVisualElement.Q<VisualElement>("main-content");

        if (_mainContent == null)
        {
            Debug.LogWarning("[ContentArea] 'main-content' nicht gefunden im UXML.");
            return;
        }

        // Event abonnieren fuer zukuenftige Toggles in dieser Scene
        SidebarController.OnToggled += OnSidebarToggled;

        // State direkt aus PlayerPrefs lesen – kein Event abwarten noetig
        bool isCollapsed = PlayerPrefs.GetInt(PREF_COLLAPSED, 0) == 1;
        float padding    = isCollapsed ? PADDING_COLLAPSED : PADDING_EXPANDED;
        _mainContent.style.paddingLeft = padding;
    }

    void OnDisable()
    {
        SidebarController.OnToggled -= OnSidebarToggled;
    }

    private void OnSidebarToggled(bool isCollapsed)
    {
        if (_mainContent == null) return;
        float targetPadding = isCollapsed ? PADDING_COLLAPSED : PADDING_EXPANDED;
        StartCoroutine(AnimatePadding(targetPadding));
    }

    private IEnumerator AnimatePadding(float targetPadding)
    {
        float startPadding = _mainContent.resolvedStyle.paddingLeft;
        float elapsed      = 0f;

        while (elapsed < ANIM_DURATION)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / ANIM_DURATION);
            float eased = t * t * (3f - 2f * t);
            _mainContent.style.paddingLeft = Mathf.Lerp(startPadding, targetPadding, eased);
            yield return null;
        }

        _mainContent.style.paddingLeft = targetPadding;
    }
}