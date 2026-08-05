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
    // Header-Grundfarbe = Marken-Grün (wie Dienstleistungen/Export), damit
    // alle Tabellen-Header in der App einheitlich aussehen. Aktiv sortierte
    // Spalte bleibt weiß, damit man weiterhin sieht wonach sortiert ist.
    private static readonly Color HeaderGruen = new Color(128f / 255f, 207f / 255f, 149f / 255f);

    private const float PADDING_EXPANDED  = 430f;
    private const float PADDING_COLLAPSED = 140f;
    private const float ANIM_DURATION     = 0.2f;

    private VisualElement _root;
    private VisualElement _mainContent;
    private ScrollView    _liste;
    private float _currentPadding = PADDING_EXPANDED;

    // Struktur für eine einheitliche Anzeige von Angeboten und Rechnungen
    private class BuchhaltungsEintrag
    {
        public string Typ;
        public string Nummer;
        public string Bezeichnung;
        public string Erstellt;
        public string Faellig;
        public string Status;
        public long TicksErstellt;
        public long TicksFaellig;
    }

    private List<BuchhaltungsEintrag> _eintraege = new();
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
        RegistriereHelpTooltips();
        ButtonHoverController.RegistriereAlle(_root);
    }

    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(_root, "btn-help-seitentitel",
            "Das Buchhaltungs-Dashboard zeigt alle Angebote und Rechnungen in einer Übersicht. " +
            "Du kannst nach Bezeichnung, Erstelldatum, Fälligkeit und Status sortieren. " +
            "Klicke auf eine Spaltenüberschrift, um die Sortierung zu ändern.");
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
        if (_liste == null) 
        { 
            Debug.LogError("[Buchhaltung] ScrollView nicht gefunden!");
            return; 
        }

        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null)
            {
                Debug.LogError("[Buchhaltung] db ist null");
                ZeigeLeermeldung("Keine Datenbankverbindung.");
                return;
            }

            _eintraege.Clear();

            // 1. Angebote (Offers) laden
            var angebote = db.getAllOffers() ?? new List<Offer>();
            foreach (var a in angebote)
            {
                string kundenName = string.IsNullOrWhiteSpace(a.customerName) ? "Unbekannter Kunde" : a.customerName;
                _eintraege.Add(new BuchhaltungsEintrag
                {
                    Typ = "Angebot",
                    Nummer = a.offerNumber,
                    Bezeichnung = $"Angebot {a.offerNumber} – {kundenName}",
                    Erstellt = a.date,
                    Faellig = a.validUntil,
                    Status = a.status,
                    TicksErstellt = DatumZuTicks(a.date),
                    TicksFaellig = DatumZuTicks(a.validUntil)
                });
            }

            // 2. Rechnungen (Invoices) laden
            var rechnungen = db.getAllInvoices() ?? new List<Invoice>();
            foreach (var r in rechnungen)
            {
                string kundenName = string.IsNullOrWhiteSpace(r.customerName) ? "Unbekannter Kunde" : r.customerName;
                
                // Hinweis: Falls die Variablen in deiner Invoice-Klasse anders heißen (z.B. 'validUntil' statt 'dueDate'),
                // passe 'r.invoiceNumber' oder 'r.dueDate' hier einfach entsprechend an.
                _eintraege.Add(new BuchhaltungsEintrag
                {
                    Typ = "Rechnung",
                    Nummer = r.invoiceNumber,
                    Bezeichnung = $"Rechnung {r.invoiceNumber} – {kundenName}",
                    Erstellt = r.date,
                    Faellig = r.dueDate, 
                    Status = r.status,
                    TicksErstellt = DatumZuTicks(r.date),
                    TicksFaellig = DatumZuTicks(r.dueDate)
                });
            }

            if (_eintraege.Count == 0)
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
            _sortAscending = !_sortAscending; // Gleiche Spalte → Richtung umkehren
        else
        {
            _sortColumn    = column;
            _sortAscending = true; // Neue Spalte → aufsteigend starten
        }

        AktualisiereHeaderPfeile();
        RenderListe();
    }

    private IEnumerable<BuchhaltungsEintrag> SortiereEintraege()
    {
        IEnumerable<BuchhaltungsEintrag> sorted = _sortColumn switch
        {
            "Bezeichnung" => _eintraege.OrderBy(e => e.Bezeichnung, StringComparer.OrdinalIgnoreCase),
            "Erstellt"    => _eintraege.OrderBy(e => e.TicksErstellt),
            "Faellig"     => _eintraege.OrderBy(e => e.TicksFaellig),
            "Status"      => _eintraege.OrderBy(e => e.Status, StringComparer.OrdinalIgnoreCase),
            _             => _eintraege.OrderBy(e => e.TicksErstellt)
        };

        return _sortAscending ? sorted : sorted.Reverse();
    }

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

    foreach (var e in SortiereEintraege())
    {
        _liste.Add(ErstelleZeile(e)); // Übergibt jetzt das ganze Objekt
    }
}

    private VisualElement ErstelleHeader()
    {
        var header = new VisualElement();
        header.AddToClassList("tabelle-header");
        header.style.alignItems    = Align.Center;
        header.style.width         = Length.Percent(100);
        header.style.paddingLeft   = 20;
        header.style.paddingRight  = 20;

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
        lbl.AddToClassList(ussClass);
        lbl.AddToClassList("tabelle-header-zelle");
        lbl.style.color                   = HeaderGruen;
        lbl.style.fontSize                 = 16;
        lbl.style.unityFontStyleAndWeight  = FontStyle.Bold;

        if (flexGrow > 0) lbl.style.flexGrow = flexGrow;

        lbl.RegisterCallback<MouseEnterEvent>(_ => lbl.style.color = Color.white);
        lbl.RegisterCallback<MouseLeaveEvent>(_ =>
            lbl.style.color = (_sortColumn == columnKey) ? Color.white : HeaderGruen);
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
            lbl.style.color = aktiv ? Color.white : HeaderGruen;
        }

        Aktualisiere(_hBezeichnung, "Bezeichnung", "Bezeichnung");
        Aktualisiere(_hErstellt,    "Erstellt",    "Erstellt");
        Aktualisiere(_hFaellig,     "Faellig",     "Fällig");
        Aktualisiere(_hStatus,      "Status",      "Status");
    }

    private VisualElement ErstelleZeile(BuchhaltungsEintrag eintrag)
{
    var zeile = new VisualElement();
    zeile.AddToClassList("tabelle-zeile");
    zeile.style.marginBottom = 4;
    zeile.style.borderTopLeftRadius = 8;
    zeile.style.borderTopRightRadius = 8;
    zeile.style.borderBottomLeftRadius = 8;
    zeile.style.borderBottomRightRadius = 8;

    // 1. Bezeichnung
    var lblBezeichnung = new Label(eintrag.Bezeichnung);
    lblBezeichnung.AddToClassList("col-bezeichnung");
    lblBezeichnung.AddToClassList("tabelle-zelle");
    // ... (bisherige Styles)

    // 2. Erstellt
    var lblErstellt = new Label(eintrag.Erstellt);
    lblErstellt.AddToClassList("col-erstellt");
    lblErstellt.AddToClassList("tabelle-zelle");

    // 3. Fällig
    var lblFaellig = new Label(eintrag.Faellig);
    lblFaellig.AddToClassList("col-faellig");
    lblFaellig.AddToClassList("tabelle-zelle");

    // 4. Status
    var statusContainer = new VisualElement();
    statusContainer.AddToClassList("col-status");
    var statusBadge = new Label(eintrag.Status);
    Color statusFarbe = HoleStatusFarbe(eintrag.Status);
    statusBadge.style.color                    = statusFarbe;
    statusBadge.style.backgroundColor          = new Color(statusFarbe.r, statusFarbe.g, statusFarbe.b, 0.16f);
    statusBadge.style.borderTopColor           = statusFarbe;
    statusBadge.style.borderBottomColor        = statusFarbe;
    statusBadge.style.borderLeftColor          = statusFarbe;
    statusBadge.style.borderRightColor         = statusFarbe;
    statusBadge.style.borderTopWidth           = 1;
    statusBadge.style.borderBottomWidth        = 1;
    statusBadge.style.borderLeftWidth          = 1;
    statusBadge.style.borderRightWidth         = 1;
    statusBadge.style.borderTopLeftRadius      = 12;
    statusBadge.style.borderTopRightRadius     = 12;
    statusBadge.style.borderBottomLeftRadius   = 12;
    statusBadge.style.borderBottomRightRadius  = 12;
    statusBadge.style.paddingLeft              = 10;
    statusBadge.style.paddingRight             = 10;
    statusBadge.style.paddingTop               = 3;
    statusBadge.style.paddingBottom            = 3;
    statusBadge.style.fontSize                 = 12;
    statusBadge.style.unityFontStyleAndWeight   = FontStyle.Bold;
    statusBadge.style.unityTextAlign            = TextAnchor.MiddleCenter;
    statusContainer.Add(statusBadge);

    // 5. NEU: Bearbeiten-Button am Ende der Zeile
    var btnBearbeiten = new Button();
    btnBearbeiten.text = "Bearbeiten";
    btnBearbeiten.AddToClassList("zeile-btn-bearbeiten");
    btnBearbeiten.style.marginLeft = StyleKeyword.Auto; // Schiebt den Button ganz nach rechts
    btnBearbeiten.style.marginRight = 12;

    // Klick-Event: Ruft die Weiterleitung auf
    btnBearbeiten.RegisterCallback<ClickEvent>(_ => OnBearbeitenGeklickt(eintrag.Typ, eintrag.Nummer));

    // Alle Elemente der Zeile hinzufügen
    zeile.Add(lblBezeichnung);
    zeile.Add(lblErstellt);
    zeile.Add(lblFaellig);
    zeile.Add(statusContainer);
    zeile.Add(btnBearbeiten); // <-- Button anhängen

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

    private void OnBearbeitenGeklickt(string typ, string nummer)
{
    Debug.Log($"[Buchhaltung] Öffne Bearbeiten-Popup für {typ}: {nummer}");

    if (_root == null)
    {
        Debug.LogError("[Buchhaltung] _root ist null! Popup kann nicht geöffnet werden.");
        return;
    }

    var overlay = _root.Q<VisualElement>("popup-modal-hintergrund");
    var titelLabel = _root.Q<Label>("popup-titel");
    var btnSchliessen = _root.Q<Button>("btn-popup-schliessen");
    
    var inputKunde = _root.Q<TextField>("popup-input-kunde");
    var inputRef = _root.Q<TextField>("popup-input-referenz");
    var dropdownStatus = _root.Q<DropdownField>("popup-dropdown-status");
    var btnSpeichern = _root.Q<Button>("btn-popup-speichern");

    if (overlay == null || inputKunde == null || inputRef == null || dropdownStatus == null || btnSpeichern == null)
    {
        Debug.LogError("[Buchhaltung] Eines der Popup-UI-Elemente wurde nicht in der UXML gefunden!");
        return;
    }

    titelLabel.text = $"{typ} bearbeiten – {nummer}";
    overlay.RemoveFromClassList("popup-hidden"); 

    // Schließen-Button sicher zuweisen
    btnSchliessen.clickable.clicked += () => overlay.AddToClassList("popup-hidden");

    var db = UserDatabaseAccess.getCurrentUserDatabase();
    if (db == null) return;

    List<string> statusOptionen = (typ == "Rechnung") 
        ? new List<string> { "Entwurf", "Versendet", "Bezahlt", "Abgelehnt" }
        : new List<string> { "Entwurf", "Versendet", "Angenommen", "Abgelehnt" };
    
    dropdownStatus.choices = statusOptionen;

    // Neues Clickable-Objekt zur fehlerfreien Event-Isolierung erstellen
    btnSpeichern.clickable = new Clickable(() => 
    {
        if (typ == "Rechnung")
        {
            var rechnungen = db.getAllInvoices();
            Invoice rechnung = rechnungen?.Find(r => r.invoiceNumber == nummer);

            if (rechnung != null)
            {
                rechnung.customerName = inputKunde.value;
                rechnung.status = dropdownStatus.value;
                
                db.update(rechnung);
                Debug.Log($"[Buchhaltung] Rechnung {nummer} erfolgreich aktualisiert.");
                
                overlay.AddToClassList("popup-hidden"); 
                LadeEintraege(); 
            }
        }
        else if (typ == "Angebot")
        {
            var angebote = db.getAllOffers();
            Offer angebot = angebote?.Find(o => o.offerNumber == nummer);

            if (angebot != null)
            {
                angebot.customerName = inputKunde.value;
                angebot.status = dropdownStatus.value;
                
                db.update(angebot);
                Debug.Log($"[Buchhaltung] Angebot {nummer} erfolgreich aktualisiert.");
                
                overlay.AddToClassList("popup-hidden"); 
                LadeEintraege();
            }
        }
    });

    // --- POPUP-FELDER VORAB BEFÜLLEN ---
    // Da kein 'reference'-Feld im Code existiert, leeren wir das Infofeld standardmäßig 
    // oder nutzen es als reine UI-Zusatzinfo.
    inputRef.value = ""; 

    if (typ == "Rechnung")
    {
        var rechnungen = db.getAllInvoices();
        Invoice rechnung = rechnungen?.Find(r => r.invoiceNumber == nummer);
        if (rechnung != null)
        {
            inputKunde.value = rechnung.customerName;
            dropdownStatus.value = rechnung.status;
        }
    }
    else if (typ == "Angebot")
    {
        var angebote = db.getAllOffers();
        Offer angebot = angebote?.Find(o => o.offerNumber == nummer);
        if (angebot != null)
        {
            inputKunde.value = angebot.customerName;
            dropdownStatus.value = angebot.status;
        }
    }
}
}