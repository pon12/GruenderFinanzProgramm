using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BuchhaltungScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private static readonly Color Gruen = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color Rot   = new Color(230f / 255f,  57f / 255f,  70f / 255f);
    private static readonly Color Grau  = new Color(150f / 255f, 150f / 255f, 150f / 255f);

    private const float PADDING_EXPANDED  = 430f;
    private const float PADDING_COLLAPSED = 140f;
    private const float ANIM_DURATION     = 0.2f;

    private VisualElement _root;
    private VisualElement _mainContent;
    private ScrollView    _liste;

    // Aktuelles Padding tracken – kein resolvedStyle nötig
    private float _currentPadding = PADDING_EXPANDED;

    // ─────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────

    private void OnEnable()
    {
        SidebarController.OnToggled += OnSidebarToggled;
    }

    private void OnDisable()
    {
        SidebarController.OnToggled -= OnSidebarToggled;
    }

    private void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        _root        = uiDocument.rootVisualElement;
        _mainContent = _root.Q<VisualElement>("main-content");
        _liste       = _root.Q<ScrollView>("buchhaltung-list-container");

        // Initialzustand sofort setzen – nicht auf Event warten
        bool collapsed = PlayerPrefs.GetInt("sidebar_collapsed", 0) == 1;
        _currentPadding = collapsed ? PADDING_COLLAPSED : PADDING_EXPANDED;
        if (_mainContent != null)
            _mainContent.style.paddingLeft = _currentPadding;

        LadeEintraege();
    }

    // ─────────────────────────────────────────────────
    // SIDEBAR REAKTION
    // ─────────────────────────────────────────────────

    private void OnSidebarToggled(bool isCollapsed)
    {
        if (_mainContent == null) return;
        float target = isCollapsed ? PADDING_COLLAPSED : PADDING_EXPANDED;
        StopAllCoroutines();
        StartCoroutine(AnimatePadding(target));
    }

    private IEnumerator AnimatePadding(float targetPadding)
    {
        float start   = _currentPadding; // resolvedStyle vermeiden
        float elapsed = 0f;

        while (elapsed < ANIM_DURATION)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / ANIM_DURATION);
            float eased = t * t * (3f - 2f * t);
            _currentPadding = Mathf.Lerp(start, targetPadding, eased);
            if (_mainContent != null)
                _mainContent.style.paddingLeft = _currentPadding;
            yield return null;
        }

        _currentPadding = targetPadding;
        if (_mainContent != null)
            _mainContent.style.paddingLeft = _currentPadding;
    }

    // ─────────────────────────────────────────────────
    // DATEN LADEN
    // ─────────────────────────────────────────────────

    private void LadeEintraege()
    {
        if (_liste == null) { Debug.LogError("[Buchhaltung] ScrollView nicht gefunden!"); return; }
        _liste.Clear();

        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();

            if (db == null)
            {
                Debug.LogError("[Buchhaltung] db ist null");
                ZeigeLeermeldung("Keine Datenbankverbindung.");
                return;
            }

            var angebote = db.getAllOffers() ?? new List<Offer>();

            if (angebote.Count == 0)
            {
                ZeigeLeermeldung();
                return;
            }

            foreach (var angebot in angebote)
                _liste.Add(ErstelleZeile(
                    bezeichnung: "Angebot " + angebot.offerNumber + " – " + angebot.customerName,
                    erstellt:    angebot.date,
                    faellig:     angebot.validUntil,
                    status:      angebot.status
                ));
        }
        catch (Exception e)
        {
            Debug.LogError("[Buchhaltung] " + e.Message);
            ZeigeLeermeldung("Fehler beim Laden der Einträge.");
        }
    }

    // ─────────────────────────────────────────────────
    // UI AUFBAU
    // ─────────────────────────────────────────────────

    private VisualElement ErstelleZeile(string bezeichnung, string erstellt, string faellig, string status)
    {
        var zeile = new VisualElement();
        zeile.style.flexDirection   = FlexDirection.Row;
        zeile.style.alignItems      = Align.Center;
        zeile.style.paddingTop      = 10;
        zeile.style.paddingBottom   = 10;
        zeile.style.paddingLeft     = 12;
        zeile.style.paddingRight    = 12;
        zeile.style.marginBottom    = 4;
        zeile.style.backgroundColor = new Color(55f / 255f, 55f / 255f, 55f / 255f);
        zeile.style.borderTopLeftRadius     = 8;
        zeile.style.borderTopRightRadius    = 8;
        zeile.style.borderBottomLeftRadius  = 8;
        zeile.style.borderBottomRightRadius = 8;

        zeile.RegisterCallback<MouseEnterEvent>(_ =>
            zeile.style.backgroundColor = new Color(65f / 255f, 65f / 255f, 65f / 255f));
        zeile.RegisterCallback<MouseLeaveEvent>(_ =>
            zeile.style.backgroundColor = new Color(55f / 255f, 55f / 255f, 55f / 255f));

        var lblBezeichnung = new Label(bezeichnung);
        lblBezeichnung.AddToClassList("col-bezeichnung");
        lblBezeichnung.style.color      = Color.white;
        lblBezeichnung.style.fontSize   = 13;
        lblBezeichnung.style.flexGrow   = 1;
        lblBezeichnung.style.overflow   = Overflow.Hidden;
        lblBezeichnung.style.whiteSpace = WhiteSpace.NoWrap;

        var lblErstellt = new Label(erstellt);
        lblErstellt.AddToClassList("col-erstellt");
        lblErstellt.style.color    = Grau;
        lblErstellt.style.fontSize = 12;
        lblErstellt.style.width    = 110;

        var lblFaellig = new Label(faellig);
        lblFaellig.AddToClassList("col-faellig");
        lblFaellig.style.color    = Grau;
        lblFaellig.style.fontSize = 12;
        lblFaellig.style.width    = 110;

        var statusBadge = new Label(status);
        statusBadge.AddToClassList("col-status");
        statusBadge.style.fontSize      = 11;
        statusBadge.style.width         = 100;
        statusBadge.style.paddingTop    = 3;
        statusBadge.style.paddingBottom = 3;
        statusBadge.style.paddingLeft   = 8;
        statusBadge.style.paddingRight  = 8;
        statusBadge.style.borderTopLeftRadius     = 20;
        statusBadge.style.borderTopRightRadius    = 20;
        statusBadge.style.borderBottomLeftRadius  = 20;
        statusBadge.style.borderBottomRightRadius = 20;
        statusBadge.style.unityTextAlign          = TextAnchor.MiddleCenter;
        statusBadge.style.backgroundColor         = HoleStatusFarbe(status);
        statusBadge.style.color                   = new Color(0.1f, 0.1f, 0.1f);

        zeile.Add(lblBezeichnung);
        zeile.Add(lblErstellt);
        zeile.Add(lblFaellig);
        zeile.Add(statusBadge);

        return zeile;
    }

    private static Color HoleStatusFarbe(string status)
    {
        return status switch
        {
            "Angenommen" or "Bezahlt"    => Gruen,
            "Abgelehnt"  or "Überfällig" => Rot,
            _ => new Color(180f / 255f, 180f / 255f, 180f / 255f)
        };
    }

    private void ZeigeLeermeldung(string text = "Noch keine Angebote oder Rechnungen vorhanden.")
    {
        var hinweis = new Label(text);
        hinweis.style.color          = Grau;
        hinweis.style.fontSize       = 14;
        hinweis.style.unityTextAlign = TextAnchor.MiddleCenter;
        hinweis.style.marginTop      = 40;
        hinweis.style.flexGrow       = 1;
        _liste.Add(hinweis);
    }
}