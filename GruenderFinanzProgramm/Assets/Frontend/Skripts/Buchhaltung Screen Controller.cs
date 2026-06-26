using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private float _currentPadding = PADDING_EXPANDED;

    // Sortierung
    private List<Offer> _angebote     = new();
    private string      _sortColumn   = "Bezeichnung";
    private bool        _sortAscending = true;

    // Header-Labels (für Pfeil-Update)
    private Label _hBezeichnung, _hErstellt, _hFaellig, _hStatus;

    // ─────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────

    private void OnEnable()  => SidebarController.OnToggled += OnSidebarToggled;
    private void OnDisable() => SidebarController.OnToggled -= OnSidebarToggled;

    private void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        _root        = uiDocument.rootVisualElement;
        _mainContent = _root.Q<VisualElement>("main-content");
        _liste       = _root.Q<ScrollView>("buchhaltung-list-container");

        bool collapsed = PlayerPrefs.GetInt("sidebar_collapsed", 0) == 1;
        _currentPadding = collapsed ? PADDING_COLLAPSED : PADDING_EXPANDED;
        if (_mainContent != null)
            _mainContent.style.paddingLeft = _currentPadding;

        // Header als Geschwister-Element vor der ScrollView einfügen
        if (_liste?.parent != null)
        {
            var header = ErstelleHeader();
            int idx = _liste.parent.IndexOf(_liste);
            _liste.parent.Insert(idx, header);
        }

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
        float start   = _currentPadding;
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

        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null)
            {
                Debug.LogError("[Buchhaltung] db ist null");
                ZeigeLeermeldung("Keine Datenbankverbindung.");
                return;
            }

            _angebote = db.getAllOffers() ?? new List<Offer>();

            if (_angebote.Count == 0)
            {
                ZeigeLeermeldung();
                return;
            }

            RenderListe();
        }
        catch (Exception e)
        {
            Debug.LogError("[Buchhaltung] " + e.Message);
            ZeigeLeermeldung("Fehler beim Laden der Einträge.");
        }
    }

    // ─────────────────────────────────────────────────
    // SORTIERUNG
    // ─────────────────────────────────────────────────

    private void SetSortColumn(string column)
    {
        if (_sortColumn == column)
            _sortAscending = !_sortAscending;   // gleiche Spalte → Richtung umkehren
        else
        {
            _sortColumn    = column;
            _sortAscending = true;              // neue Spalte → aufsteigend starten
        }

        AktualisiereHeaderPfeile();
        RenderListe();
    }

    private IEnumerable<Offer> SortiereAngebote()
    {
        IEnumerable<Offer> sorted = _sortColumn switch
        {
            "Bezeichnung" => _angebote.OrderBy(a => $"Angebot {a.offerNumber} – {a.customerName}",
                                StringComparer.OrdinalIgnoreCase),
            "Erstellt"    => _angebote.OrderBy(a => DatumZuTicks(a.date)),
            "Faellig"     => _angebote.OrderBy(a => DatumZuTicks(a.validUntil)),
            "Status"      => _angebote.OrderBy(a => a.status, StringComparer.OrdinalIgnoreCase),
            _             => _angebote.OrderBy(a => a.offerNumber)
        };

        return _sortAscending ? sorted : sorted.Reverse();
    }

    /// Datum-String → Ticks für korrektes chronologisches Sortieren.
    /// Gibt 0 zurück wenn das Format nicht parsebar ist (Strings landen dann vorne).
    private static long DatumZuTicks(string datum)
    {
        if (DateTime.TryParse(datum, out var dt))
            return dt.Ticks;
        return 0;
    }

    // ─────────────────────────────────────────────────
    // UI AUFBAU
    // ─────────────────────────────────────────────────

    private void RenderListe()
{
    if (_liste == null) return;
    _liste.Clear();

    foreach (var a in SortiereAngebote())
    {
        // Prüfen, ob der Kundenname vorhanden oder leer ist
        string kundenName = string.IsNullOrWhiteSpace(a.customerName) 
            ? "Unbekannter Kunde" 
            : a.customerName;

        _liste.Add(ErstelleZeile(
            bezeichnung: $"Angebot {a.offerNumber} – {kundenName}", // <-- Nutzt den geprüften Namen
            erstellt:    a.date,
            faellig:     a.validUntil,
            status:      a.status
        ));
    }
}

    private VisualElement ErstelleHeader()
{
    var header = new VisualElement();
    header.style.flexDirection = FlexDirection.Row;
    header.style.alignItems    = Align.Center;
    header.style.width         = Length.Percent(100); // <-- WICHTIG für Prozent-Layouts
    header.style.paddingTop    = 4;
    header.style.paddingBottom = 4;
    header.style.paddingLeft   = 12;
    header.style.paddingRight  = 12;
    header.style.marginBottom  = 2;

    // Kein hartes flexGrow im Code, das regelt nun die USS!
    _hBezeichnung = ErstelleHeaderSpalte("Bezeichnung", "Bezeichnung", "col-bezeichnung");
    _hErstellt    = ErstelleHeaderSpalte("Erstellt",    "Erstellt",    "col-erstellt");
    _hFaellig     = ErstelleHeaderSpalte("Fällig",      "Faellig",     "col-faellig");
    _hStatus      = ErstelleHeaderSpalte("Status",      "Status",      "col-status");

    header.Add(_hBezeichnung);
    header.Add(_hErstellt);
    header.Add(_hFaellig);
    header.Add(_hStatus);

    AktualisiereHeaderPfeile();
    return header;
}

private Label ErstelleHeaderSpalte(string text, string columnKey, string ussClass, float flexGrow = 0)
{
    var lbl = new Label(text);
    lbl.AddToClassList(ussClass); // <-- Zuweisung der USS-Klasse für die Breite
    lbl.style.color                   = Grau;
    lbl.style.fontSize                 = 16;
    lbl.style.unityFontStyleAndWeight  = FontStyle.Bold;

    if (flexGrow > 0) lbl.style.flexGrow = flexGrow;

    lbl.RegisterCallback<MouseEnterEvent>(_ => lbl.style.color = Color.white);
    lbl.RegisterCallback<MouseLeaveEvent>(_ =>
        lbl.style.color = (_sortColumn == columnKey) ? Color.white : Grau);
    lbl.RegisterCallback<ClickEvent>(_ => SetSortColumn(columnKey));

    return lbl;
}

    private void AktualisiereHeaderPfeile()
    {
        string pfeil = _sortAscending ? " ↑" : " ↓";

        void Aktualisiere(Label lbl, string key, string basisText)
        {
            bool aktiv  = _sortColumn == key;
            lbl.text    = aktiv ? basisText + pfeil : basisText;
            lbl.style.color = aktiv ? Color.white : Grau;
        }

        Aktualisiere(_hBezeichnung, "Bezeichnung", "Bezeichnung");
        Aktualisiere(_hErstellt,    "Erstellt",    "Erstellt");
        Aktualisiere(_hFaellig,     "Faellig",     "Fällig");
        Aktualisiere(_hStatus,      "Status",      "Status");
    }

    private VisualElement ErstelleZeile(string bezeichnung, string erstellt,
                                    string faellig, string status)
{
    var zeile = new VisualElement();
    zeile.style.flexDirection   = FlexDirection.Row;
    zeile.style.alignItems      = Align.Center;
    zeile.style.width           = Length.Percent(100);
    
    // Paddings auf 0 setzen, da die Spalten-Prozente (50+20+15+15 = 100%) 
    // bereits die volle Breite der Zeile ausnutzen.
    zeile.style.paddingTop      = 10;
    zeile.style.paddingBottom   = 10;
    zeile.style.paddingLeft     = 0; // <-- Geändert von 12
    zeile.style.paddingRight    = 0; // <-- Geändert von 12
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

    // 1. Bezeichnung (Inline-flexGrow entfernen, damit USS-Prozente exakt greifen)
    var lblBezeichnung = new Label(bezeichnung);
    lblBezeichnung.AddToClassList("col-bezeichnung");
    lblBezeichnung.style.color      = Color.white;
    lblBezeichnung.style.fontSize   = 14;
    lblBezeichnung.style.overflow   = Overflow.Hidden;
    lblBezeichnung.style.whiteSpace = WhiteSpace.NoWrap;

    // 2. Erstellt
    var lblErstellt = new Label(erstellt);
    lblErstellt.AddToClassList("col-erstellt");
    lblErstellt.style.color    = Grau;
    lblErstellt.style.fontSize = 14;

    // 3. Fällig
    var lblFaellig = new Label(faellig);
    lblFaellig.AddToClassList("col-faellig");
    lblFaellig.style.color    = Grau;
    lblFaellig.style.fontSize = 14;

    // 4. Status-Spalte als unsichtbarer Container (nimmt die 15% ein)
    var statusContainer = new VisualElement();
    statusContainer.AddToClassList("col-status");
    statusContainer.style.justifyContent = Justify.FlexStart; // Zentriert das Badge horizontal

    // Das eigentliche kompakte Badge im Container
    var statusBadge = new Label(status);
    statusBadge.style.fontSize       = 11;
    statusBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
    statusBadge.style.whiteSpace     = WhiteSpace.NoWrap;
    
    // Festes Padding für den "Kapsel-Look" um den Text herum
    statusBadge.style.paddingTop     = 4;
    statusBadge.style.paddingBottom  = 4;
    statusBadge.style.paddingLeft    = 16;
    statusBadge.style.paddingRight   = 16;
    
    // Runde Ecken für den Pillen-Look
    statusBadge.style.borderTopLeftRadius     = 8;
    statusBadge.style.borderTopRightRadius    = 8;
    statusBadge.style.borderBottomLeftRadius  = 8;
    statusBadge.style.borderBottomRightRadius = 8;
    
    statusBadge.style.backgroundColor         = HoleStatusFarbe(status);
    statusBadge.style.color                   = new Color(0.1f, 0.1f, 0.1f);

    // Badge in den Container packen
    statusContainer.Add(statusBadge);

    // Alles zur Zeile hinzufügen
    zeile.Add(lblBezeichnung);
    zeile.Add(lblErstellt);
    zeile.Add(lblFaellig);
    zeile.Add(statusContainer); // <-- Container statt nacktes Label

    return zeile;
}

    private static Color HoleStatusFarbe(string status)
    {
        return status switch
        {
            "Angenommen" or "Bezahlt"    => Gruen,
            "Abgelehnt"  or "Überfällig" => Rot,
            "Entwurf"    or "Offen"      => Grau,
            "Versendet"                  => new Color(255f / 255f, 195f / 255f, 0f / 255f),
            _                            => new Color(180f / 255f, 180f / 255f, 180f / 255f)
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