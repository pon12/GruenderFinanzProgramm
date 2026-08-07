using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class BuchhaltungScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset buchhaltungZeileTemplate;

    private static readonly Color Gruen = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color Rot = new Color(230f / 255f, 57f / 255f, 70f / 255f);
    private static readonly Color Grau = new Color(150f / 255f, 150f / 255f, 150f / 255f);
    // Header-Grundfarbe = Marken-Grün, damit alle Tabellen-Header einheitlich aussehen.
    private static readonly Color HeaderGruen = new Color(128f / 255f, 207f / 255f, 149f / 255f);

    private const float PADDING_EXPANDED = 430f;
    private const float PADDING_COLLAPSED = 140f;
    private const float ANIM_DURATION = 0.2f;

    private VisualElement _root;
    private VisualElement _mainContent;
    private ScrollView _liste;
    private float _currentPadding = PADDING_EXPANDED;

    // Einheitliche Struktur für die Anzeige von Angeboten und Rechnungen
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
    private string _sortColumn = "Bezeichnung";
    private bool _sortAscending = true;

    // Header-Labels aus der UXML (für Sortierung und Pfeil-Anzeige)
    private Label _hBezeichnung, _hArt, _hErstellt, _hFaellig, _hStatus;

    // ─────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────

    private void OnEnable() => SidebarController.OnToggled += OnSidebarToggled;
    private void OnDisable() => SidebarController.OnToggled -= OnSidebarToggled;

    private void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        _root = uiDocument.rootVisualElement;
        _mainContent = _root.Q<VisualElement>("main-content");
        _liste = _root.Q<ScrollView>("buchhaltung-list-container");

        bool collapsed = PlayerPrefs.GetInt("sidebar_collapsed", 0) == 1;
        _currentPadding = collapsed ? PADDING_COLLAPSED : PADDING_EXPANDED;

        if (_mainContent != null)
            _mainContent.style.paddingLeft = _currentPadding;

        RegistriereSortHeader();
        LadeEintraege();
        RegistriereHelpTooltips();
        ButtonHoverController.RegistriereAlle(_root);
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
        float start = _currentPadding;
        float elapsed = 0f;

        while (elapsed < ANIM_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ANIM_DURATION);
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
    // SORTIERUNG (Header kommt aus der UXML, nicht mehr aus C#)
    // ─────────────────────────────────────────────────

    private void RegistriereSortHeader()
    {
        _hBezeichnung = _root.Q<Label>("header-bezeichnung");
        _hArt = _root.Q<Label>("header-art");
        _hErstellt = _root.Q<Label>("header-erstellt");
        _hFaellig = _root.Q<Label>("header-faellig");
        _hStatus = _root.Q<Label>("header-status");

        RegistriereEinzelnenHeader(_hBezeichnung, "Bezeichnung");
        RegistriereEinzelnenHeader(_hArt, "Art");
        RegistriereEinzelnenHeader(_hErstellt, "Erstellt");
        RegistriereEinzelnenHeader(_hFaellig, "Faellig");
        RegistriereEinzelnenHeader(_hStatus, "Status");

        AktualisiereHeaderPfeile();
    }

    private void RegistriereEinzelnenHeader(Label lbl, string columnKey)
    {
        if (lbl == null) return;
        lbl.pickingMode = PickingMode.Position;
        lbl.RegisterCallback<ClickEvent>(_ => SetSortColumn(columnKey));
        lbl.RegisterCallback<MouseEnterEvent>(_ => lbl.style.color = Color.white);
        lbl.RegisterCallback<MouseLeaveEvent>(_ =>
            lbl.style.color = (_sortColumn == columnKey) ? Color.white : HeaderGruen);
    }

    private void SetSortColumn(string column)
    {
        if (_sortColumn == column)
            _sortAscending = !_sortAscending; // gleiche Spalte -> Richtung umkehren
        else
        {
            _sortColumn = column;
            _sortAscending = true; // neue Spalte -> aufsteigend starten
        }

        AktualisiereHeaderPfeile();
        RenderListe();
    }

    private void AktualisiereHeaderPfeile()
    {
        string pfeil = _sortAscending ? " ↑" : " ↓";

        void Aktualisiere(Label lbl, string key, string basisText)
        {
            if (lbl == null) return;
            bool aktiv = _sortColumn == key;
            lbl.text = aktiv ? basisText + pfeil : basisText;
            lbl.style.color = aktiv ? Color.white : HeaderGruen;
        }

        Aktualisiere(_hBezeichnung, "Bezeichnung", "Bezeichnung");
        Aktualisiere(_hArt, "Art", "Art");
        Aktualisiere(_hErstellt, "Erstellt", "Erstellt");
        Aktualisiere(_hFaellig, "Faellig", "Fällig");
        Aktualisiere(_hStatus, "Status", "Status");
    }

    private IEnumerable<BuchhaltungsEintrag> SortiereEintraege()
    {
        IEnumerable<BuchhaltungsEintrag> sortiert = _sortColumn switch
        {
            "Bezeichnung" => _eintraege.OrderBy(e => e.Bezeichnung, StringComparer.OrdinalIgnoreCase),
            "Art" => _eintraege.OrderBy(e => e.Typ, StringComparer.OrdinalIgnoreCase),
            "Erstellt" => _eintraege.OrderBy(e => e.TicksErstellt),
            "Faellig" => _eintraege.OrderBy(e => e.TicksFaellig),
            "Status" => _eintraege.OrderBy(e => e.Status, StringComparer.OrdinalIgnoreCase),
            _ => _eintraege.OrderBy(e => e.TicksErstellt)
        };

        return _sortAscending ? sortiert : sortiert.Reverse();
    }

    private static long DatumZuTicks(string datum)
    {
        if (DateTime.TryParse(datum, out var dt))
            return dt.Ticks;
        return 0;
    }

    // ─────────────────────────────────────────────────
    // DATEN LADEN
    // ─────────────────────────────────────────────────

    private void LadeEintraege()
    {
        if (_liste == null)
        {
            Debug.LogError("[Buchhaltung] ScrollView nicht gefunden.");
            return;
        }

        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null)
            {
                Debug.LogError("[Buchhaltung] Keine aktive Datenbank.");
                ZeigeLeermeldung("Keine Datenbankverbindung.");
                return;
            }

            _eintraege.Clear();

            // Angebote laden
            var angebote = db.getAllOffers() ?? new List<Offer>();
            foreach (var a in angebote)
            {
                _eintraege.Add(new BuchhaltungsEintrag
                {
                    Typ = "Angebot",
                    Nummer = a.offerNumber,
                    Bezeichnung = a.offerNumber,
                    Erstellt = a.date,
                    Faellig = a.validUntil,
                    Status = a.status,
                    TicksErstellt = DatumZuTicks(a.date),
                    TicksFaellig = DatumZuTicks(a.validUntil)
                });
            }

            // Rechnungen laden
            var rechnungen = db.getAllInvoices() ?? new List<Invoice>();
            foreach (var r in rechnungen)
            {
                _eintraege.Add(new BuchhaltungsEintrag
                {
                    Typ = "Rechnung",
                    Nummer = r.invoiceNumber,
                    Bezeichnung = r.invoiceNumber,
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
    // UI AUFBAU
    // ─────────────────────────────────────────────────

    private void RenderListe()
    {
        if (_liste == null) return;
        _liste.Clear();

        foreach (var eintrag in SortiereEintraege())
            _liste.Add(ErstelleZeile(eintrag));
    }

    private VisualElement ErstelleZeile(BuchhaltungsEintrag eintrag)
    {
        if (buchhaltungZeileTemplate == null)
        {
            Debug.LogError("[Buchhaltung] buchhaltungZeileTemplate ist im Inspector nicht zugewiesen.");
            return new VisualElement();
        }

        VisualElement zeile = buchhaltungZeileTemplate.Instantiate();

        Label lblBezeichnung = zeile.Q<Label>("row-bezeichnung");
        Label lblArt = zeile.Q<Label>("row-art");
        Label lblErstellt = zeile.Q<Label>("row-erstellt");
        Label lblFaellig = zeile.Q<Label>("row-faellig");
        DropdownField dropdownStatus = zeile.Q<DropdownField>("row-status-dropdown");
        Button btnBearbeiten = zeile.Q<Button>("btn-bearbeiten");
        Button btnPfad = zeile.Q<Button>("btn-open-pfad");
        Button btnExportieren = zeile.Q<Button>("btn-exportieren");

        if (lblBezeichnung != null) lblBezeichnung.text = eintrag.Bezeichnung;
        if (lblArt != null) lblArt.text = eintrag.Typ;
        if (lblErstellt != null) lblErstellt.text = eintrag.Erstellt;
        if (lblFaellig != null) lblFaellig.text = eintrag.Faellig;

        BuchhaltungsEintrag lokalerEintrag = eintrag;

        if (dropdownStatus != null)
        {
            dropdownStatus.choices = (eintrag.Typ == "Rechnung")
                ? new List<string> { "Entwurf", "Versendet", "Bezahlt", "Abgelehnt" }
                : new List<string> { "Entwurf", "Versendet", "Angenommen", "Abgelehnt" };
            dropdownStatus.SetValueWithoutNotify(eintrag.Status);
            dropdownStatus.RegisterValueChangedCallback(evt =>
                AktualisiereStatus(lokalerEintrag, evt.newValue));
        }

        if (btnBearbeiten != null) btnBearbeiten.clicked += () => OnBearbeitenGeklickt(lokalerEintrag.Typ, lokalerEintrag.Nummer);
        if (btnPfad != null) btnPfad.clicked += () => OeffnePfad(lokalerEintrag);
        if (btnExportieren != null) btnExportieren.clicked += () => ExportiereBeleg(lokalerEintrag);

        var zeilenHelp = zeile.Q<VisualElement>("btn-help-exportieren-zeile");
        if (zeilenHelp != null)
            HelpTooltip.RegistriereInKarte(_root, zeilenHelp,
                "Exportiert diesen Beleg als PDF. Mit dem Ordner-Symbol öffnest du den zuletzt gespeicherten Ordner.");

        return zeile;
    }

    private static Color HoleStatusFarbe(string status)
    {
        return status switch
        {
            "Angenommen" or "Bezahlt" => Gruen,
            "Abgelehnt" or "Überfällig" => Rot,
            "Entwurf" or "Offen" => Grau,
            "Versendet" => new Color(255f / 255f, 195f / 255f, 0f / 255f),
            _ => new Color(180f / 255f, 180f / 255f, 180f / 255f)
        };
    }

    private void ZeigeLeermeldung(string text = "Noch keine Angebote oder Rechnungen vorhanden.")
    {
        var hinweis = new Label(text);
        hinweis.style.color = Grau;
        hinweis.style.fontSize = 14;
        hinweis.style.unityTextAlign = TextAnchor.MiddleCenter;
        hinweis.style.marginTop = 40;
        hinweis.style.flexGrow = 1;
        _liste.Add(hinweis);
    }

    // ─────────────────────────────────────────────────
    // AKTIONEN
    // ─────────────────────────────────────────────────

    private void AktualisiereStatus(BuchhaltungsEintrag eintrag, string neuerStatus)
    {
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        if (eintrag.Typ == "Rechnung")
        {
            var rechnung = db.getAllInvoices()?.Find(r => r.invoiceNumber == eintrag.Nummer);
            if (rechnung == null) return;
            rechnung.status = neuerStatus;
            db.update(rechnung);
        }
        else
        {
            var angebot = db.getAllOffers()?.Find(o => o.offerNumber == eintrag.Nummer);
            if (angebot == null) return;
            angebot.status = neuerStatus;
            db.update(angebot);
        }

        eintrag.Status = neuerStatus;
        Debug.Log($"[Buchhaltung] Status von {eintrag.Typ} {eintrag.Nummer} auf {neuerStatus} geändert.");
    }

    private void OnBearbeitenGeklickt(string typ, string nummer)
    {
        // Navigation zum bestehenden Angebots-/Rechnungsscreen wird noch angebunden.
        Debug.Log($"[Buchhaltung] Bearbeiten für {typ} {nummer} angefordert – Navigation folgt.");
    }

    private void OeffnePfad(BuchhaltungsEintrag eintrag)
    {
        // Speicherpfad pro Beleg wird noch angebunden.
        Debug.Log($"[Buchhaltung] Pfad-Funktion für {eintrag.Typ} {eintrag.Nummer} angefordert – noch nicht angebunden.");
    }

    private void ExportiereBeleg(BuchhaltungsEintrag eintrag)
    {
        // PDF-Export wird noch an die bestehende Beleg-Erzeugung angebunden.
        Debug.Log($"[Buchhaltung] Export für {eintrag.Typ} {eintrag.Nummer} angefordert – noch nicht angebunden.");
    }

    // ─────────────────────────────────────────────────
    // TOOLTIPS
    // ─────────────────────────────────────────────────

    private void RegistriereHelpTooltips()
    {
        HelpTooltip.Registriere(_root, "btn-help-seitentitel",
            "Das Buchhaltungs-Dashboard zeigt alle Angebote und Rechnungen in einer Übersicht. " +
            "Du kannst nach Bezeichnung, Art, Erstelldatum, Fälligkeit und Status sortieren. " +
            "Klicke auf eine Spaltenüberschrift, um die Sortierung zu ändern.");

        HelpTooltip.Registriere(_root, "btn-help-bezeichnung",
            "Nummer des Angebots oder der Rechnung.");

        HelpTooltip.Registriere(_root, "btn-help-art",
            "Zeigt an, ob es sich um ein Angebot oder eine Rechnung handelt.");

        HelpTooltip.Registriere(_root, "btn-help-erstellt",
            "Datum, an dem der Beleg erstellt wurde.");

        HelpTooltip.Registriere(_root, "btn-help-faellig",
            "Datum, bis zu dem das Angebot gültig ist bzw. an dem die Rechnung fällig wird.");

        HelpTooltip.Registriere(_root, "btn-help-status",
            "Aktueller Status des Belegs. Kann direkt über das Dropdown in der Zeile geändert werden.");

        HelpTooltip.Registriere(_root, "btn-help-bearbeiten",
            "Öffnet den Beleg zur Bearbeitung im Angebots- bzw. Rechnungsscreen.");

        HelpTooltip.Registriere(_root, "btn-help-pfad",
            "Öffnet den Speicherort der zuletzt exportierten Datei.");

        HelpTooltip.Registriere(_root, "btn-help-exportieren",
            "Exportiert den Beleg als PDF.");
    }
}