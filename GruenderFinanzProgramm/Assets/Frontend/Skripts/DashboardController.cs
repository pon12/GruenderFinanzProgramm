// ================================================================
// DashboardController.cs  – MIT EventManager
// ================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UIDocument))]
public class DashboardController : MonoBehaviour
{
    private VisualElement    _root;
    private int              _currentYear;
    private int              _currentMonth;
    private LineChartElement _chart;

    // Deadlines: Key = "yyyy-MM-dd", Value = Liste von Beschreibungen für diesen Tag
    private Dictionary<string, List<string>> _deadlines = new Dictionary<string, List<string>>();

    private readonly string[] _monthNames =
    {
        "Januar","Februar","März","April","Mai","Juni",
        "Juli","August","September","Oktober","November","Dezember"
    };

    // ============================================================
    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        var today     = DateTime.Today;
        _currentYear  = today.Year;
        _currentMonth = today.Month;

        SetupButtons();
        SetupCalendar();
        SetupChart();

        // Beim Start einmalig direkt aus DB laden (Fallback)
        LadeInitialDaten();
        LadeDeadlines();
        RenderKalender(); // Kalender neu zeichnen damit Deadline-Marker sichtbar sind

        // Ab jetzt auf Events hören
        AppEventManager.OnKundenAnzahlGeaendert    += OnKunden;
        AppEventManager.OnAngeboteAnzahlGeaendert   += OnAngebote;
        AppEventManager.OnRechnungenAnzahlGeaendert += OnRechnungen;
        AppEventManager.OnKassenbuchGeaendert       += OnKassenbuch;
    }

    void OnDisable()
    {
        AppEventManager.OnKundenAnzahlGeaendert    -= OnKunden;
        AppEventManager.OnAngeboteAnzahlGeaendert   -= OnAngebote;
        AppEventManager.OnRechnungenAnzahlGeaendert -= OnRechnungen;
        AppEventManager.OnKassenbuchGeaendert       -= OnKassenbuch;
    }

    // ============================================================
    // INITIALER DB-LOAD (nur beim ersten Öffnen des Dashboards,
    // danach kommen Updates via Events)
    // ============================================================
    private void LadeInitialDaten()
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null) return;

            // Kacheln
            SetLabel("lbl-kunden",    (db.getAllCustomers()?.Count ?? 0).ToString());
            SetLabel("lbl-angebote",  (db.getAllOffers()?.Count    ?? 0).ToString());
            SetLabel("lbl-rechnungen",(db.getAllInvoices()?.Count  ?? 0).ToString());

            // Kontostand: Vorzeichen + Leerzeichen + Farbe (rot bei Minus, grün bei Plus)
            float kontostand = db.getDifferenz();
            SetKontostand(kontostand);

            // Kassenbuch-Jahresauswertung: BILANZ pro Monat (Einkommen - Ausgaben)
            float umsatzJahr = 0f;
            float[] monate   = BerechneMonatsBilanz(db, DateTime.Today.Year, out umsatzJahr);
            SetLabel("lbl-kassenbuch", "€ " + umsatzJahr.ToString("N0"));
            _chart?.SetValues(monate);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Dashboard] InitialLaden: " + e.Message);
        }
    }

    // ============================================================
    // KONTOSTAND  – Farbe + Vorzeichen
    // ============================================================
    void SetKontostand(float wert)
    {
        var lbl = _root.Q<Label>("lbl-kontostand");
        if (lbl == null) return;

        string vorzeichen = wert < 0 ? "- " : "";
        float betragAbs   = Mathf.Abs(wert);
        lbl.text = "€ " + vorzeichen + betragAbs.ToString("N0");

        lbl.RemoveFromClassList("stat-value-green");
        lbl.RemoveFromClassList("stat-value-red");
        lbl.AddToClassList(wert < 0 ? "stat-value-red" : "stat-value-green");
    }

    // ============================================================
    // MONATS-BILANZ  – Einkommen minus Ausgaben, pro Monat, für 1 Jahr
    // ============================================================
    static readonly string[] DatumFormate =
        { "dd.MM.yyyy","d.M.yyyy","dd.M.yyyy","d.MM.yyyy","yyyy-MM-dd","yyyy/MM/dd" };

    bool TryParseDatum(string text, out DateTime ergebnis)
    {
        var inv  = System.Globalization.CultureInfo.InvariantCulture;
        var deDe = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
        var none = System.Globalization.DateTimeStyles.None;

        return DateTime.TryParseExact(text, DatumFormate, inv, none, out ergebnis)
            || DateTime.TryParse(text, deDe, none, out ergebnis);
    }

    float[] BerechneMonatsBilanz(DataBase db, int jahr, out float summeJahr)
    {
        float[] bilanzProMonat = new float[12];
        summeJahr = 0f;

        var einkommen = db.getAllEinkommenEntries();
        Debug.Log($"[DEBUG-Dashboard] Einkommen-Einträge gefunden: {einkommen?.Count ?? -1}");
        if (einkommen != null)
        {
            foreach (var e in einkommen)
            {
                bool ok = TryParseDatum(e.Datum, out DateTime d);
                Debug.Log($"[DEBUG-Dashboard] Einkommen Datum='{e.Datum}' Amount={e.Amount} parsed={ok} -> {(ok ? d.ToString("yyyy-MM-dd") : "FEHLER")} JahrMatch={(ok && d.Year == jahr)}");
                if (ok && d.Year == jahr)
                {
                    bilanzProMonat[d.Month - 1] += e.Amount;
                    summeJahr += e.Amount;
                }
            }
        }

        var ausgaben = db.getAllAusgabenEntries();
        Debug.Log($"[DEBUG-Dashboard] Ausgaben-Einträge gefunden: {ausgaben?.Count ?? -1}");
        if (ausgaben != null)
        {
            foreach (var a in ausgaben)
            {
                bool ok = TryParseDatum(a.Datum, out DateTime d);
                Debug.Log($"[DEBUG-Dashboard] Ausgabe Datum='{a.Datum}' Amount={a.Amount} parsed={ok} -> {(ok ? d.ToString("yyyy-MM-dd") : "FEHLER")} JahrMatch={(ok && d.Year == jahr)}");
                if (ok && d.Year == jahr)
                {
                    bilanzProMonat[d.Month - 1] -= a.Amount;
                    summeJahr -= a.Amount;
                }
            }
        }

        Debug.Log($"[DEBUG-Dashboard] ENDERGEBNIS summeJahr (Bilanz) = {summeJahr}");

        return bilanzProMonat;
    }

    // ============================================================
    // EVENT-HANDLER  – werden von anderen Screens gefeuert
    // ============================================================
    void OnKunden(int anzahl)
        => SetLabel("lbl-kunden", anzahl.ToString());

    void OnAngebote(int anzahl)
    {
        SetLabel("lbl-angebote", anzahl.ToString());
        LadeDeadlines();
        RenderKalender();
    }

    void OnRechnungen(int anzahl)
    {
        SetLabel("lbl-rechnungen", anzahl.ToString());
        LadeDeadlines();
        RenderKalender();
    }

    void OnKassenbuch(float umsatzJahr, float kontostand, float[] monate)
    {
        SetLabel("lbl-kassenbuch", "€ " + umsatzJahr.ToString("N0"));
        SetKontostand(kontostand);
        if (monate != null && monate.Length == 12)
            _chart?.SetValues(monate);
    }

    // ============================================================
    // BUTTONS  – wie SidebarController per SceneManager
    // ============================================================
    void SetupButtons()
    {
        BindScene("btn-nav-finanzen", "Kassenbuch");
        BindScene("btn-nav-angebot",  "Angebot");
        BindScene("btn-nav-rechnung", "Rechnung");
    }

    void BindScene(string buttonName, string sceneName)
    {
        _root.Q<Button>(buttonName)?.RegisterCallback<ClickEvent>(_ =>
        {
            bool exists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                if (SceneUtility.GetScenePathByBuildIndex(i).Contains(sceneName))
                { exists = true; break; }
            }
            if (exists) SceneManager.LoadScene(sceneName);
            else Debug.LogWarning($"[Dashboard] Scene '{sceneName}' nicht gefunden.");
        });
    }

    // ============================================================
    // HILFS-METHODE
    // ============================================================
    void SetLabel(string name, string text)
    {
        var lbl = _root.Q<Label>(name);
        if (lbl != null) lbl.text = text;
    }

    // ============================================================
    // KALENDER
    // ============================================================
    void SetupCalendar()
    {
        var dropMonat = _root.Q<DropdownField>("dropdown-monat");
        var dropJahr  = _root.Q<DropdownField>("dropdown-jahr");

        if (dropMonat != null)
        {
            dropMonat.choices = new List<string>(_monthNames);
            dropMonat.index   = _currentMonth - 1;
            dropMonat.RegisterValueChangedCallback(_ =>
            { _currentMonth = dropMonat.index + 1; RenderKalender(); });
        }
        if (dropJahr != null)
        {
            var jahre = new List<string>();
            for (int y = _currentYear - 3; y <= _currentYear + 3; y++) jahre.Add(y.ToString());
            dropJahr.choices = jahre;
            dropJahr.value   = _currentYear.ToString();
            dropJahr.RegisterValueChangedCallback(evt =>
            { if (int.TryParse(evt.newValue, out int y)) { _currentYear = y; RenderKalender(); } });
        }

        _root.Q<Button>("btn-prev-month")?.RegisterCallback<ClickEvent>(_ => WechsleMonat(-1));
        _root.Q<Button>("btn-next-month")?.RegisterCallback<ClickEvent>(_ => WechsleMonat(+1));

        var grid = _root.Q<VisualElement>("kalender-grid");
        if (grid != null) grid.style.flexGrow = 1;

        RenderKalender();
    }

    void WechsleMonat(int delta)
    {
        _currentMonth += delta;
        if (_currentMonth < 1)  { _currentMonth = 12; _currentYear--; }
        if (_currentMonth > 12) { _currentMonth = 1;  _currentYear++; }

        var dropMonat = _root.Q<DropdownField>("dropdown-monat");
        var dropJahr  = _root.Q<DropdownField>("dropdown-jahr");
        if (dropMonat != null) dropMonat.index = _currentMonth - 1;
        if (dropJahr  != null) dropJahr.value  = _currentYear.ToString();
        RenderKalender();
    }

    // ============================================================
    // DEADLINES  – aus Angeboten (date + 14 Tage) und Rechnungen (dueDate)
    // ============================================================
    private const int ANGEBOT_GUELTIGKEIT_TAGE = 14;

    void LadeDeadlines()
    {
        _deadlines.Clear();

        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null) return;

            // --- Angebote: Gültig bis = Erstellungsdatum + 14 Tage ---
            var angebote = db.getAllOffers();
            if (angebote != null)
            {
                foreach (var a in angebote)
                {
                    if (TryParseDatum(a.date, out DateTime erstellt))
                    {
                        DateTime gueltigBis = erstellt.AddDays(ANGEBOT_GUELTIGKEIT_TAGE);
                        string key = gueltigBis.ToString("yyyy-MM-dd");

                        if (!_deadlines.ContainsKey(key))
                            _deadlines[key] = new List<string>();

                        _deadlines[key].Add($"Angebot {a.offerNumber} gültig bis");
                    }
                }
            }

            // --- Rechnungen: Fälligkeitsdatum direkt aus dueDate ---
            var rechnungen = db.getAllInvoices();
            if (rechnungen != null)
            {
                foreach (var r in rechnungen)
                {
                    if (TryParseDatum(r.dueDate, out DateTime faellig))
                    {
                        string key = faellig.ToString("yyyy-MM-dd");

                        if (!_deadlines.ContainsKey(key))
                            _deadlines[key] = new List<string>();

                        _deadlines[key].Add($"Rechnung {r.invoiceNumber} fällig");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Dashboard] Deadlines laden: " + e.Message);
        }
    }

    void RenderKalender()
    {
        var today          = DateTime.Today;
        var ersterTag      = new DateTime(_currentYear, _currentMonth, 1);
        int tageImMonat    = DateTime.DaysInMonth(_currentYear, _currentMonth);
        int startWochentag = (int)ersterTag.DayOfWeek;
        int vormonatTage   = DateTime.DaysInMonth(
            _currentMonth == 1 ? _currentYear - 1 : _currentYear,
            _currentMonth == 1 ? 12 : _currentMonth - 1);

        for (int i = 0; i < 42; i++)
        {
            var btn = _root.Q<Button>($"cal-day-{i}");
            if (btn == null) continue;

            btn.RemoveFromClassList("cal-day-today");
            btn.RemoveFromClassList("cal-day-other-month");
            btn.RemoveFromClassList("cal-day-deadline");
            btn.tooltip = "";

            int tag; bool anderMonat;
            if      (i < startWochentag)
                { tag = vormonatTage - (startWochentag - 1 - i); anderMonat = true; }
            else if (i - startWochentag < tageImMonat)
                { tag = i - startWochentag + 1; anderMonat = false; }
            else
                { tag = i - startWochentag - tageImMonat + 1; anderMonat = true; }

            btn.text = tag.ToString();
            if (anderMonat)
            {
                btn.AddToClassList("cal-day-other-month");
            }
            else
            {
                if (tag == today.Day && _currentMonth == today.Month && _currentYear == today.Year)
                    btn.AddToClassList("cal-day-today");

                // Deadline-Check für diesen Tag (nur im aktuellen Monat)
                var tagDatum = new DateTime(_currentYear, _currentMonth, tag);
                string key = tagDatum.ToString("yyyy-MM-dd");

                if (_deadlines.TryGetValue(key, out var eintraege))
                {
                    btn.AddToClassList("cal-day-deadline");
                    btn.tooltip = string.Join("\n", eintraege);
                }
            }
        }
    }

    // ============================================================
    // CHART
    // ============================================================
    void SetupChart()
    {
        var canvas = _root.Q<VisualElement>("chart-canvas");
        if (canvas == null) return;

        _chart = new LineChartElement(new float[12]);
        canvas.Add(_chart);
        _chart.style.position = Position.Absolute;
        _chart.style.left = 0; _chart.style.top    = 0;
        _chart.style.right = 0; _chart.style.bottom = 0;
    }

    // ============================================================
    // INNER CLASS: Liniendiagramm
    // ============================================================
    private class LineChartElement : VisualElement
    {
        private float[] _values;
        private static readonly Color LineColor  = new Color(0.502f, 0.812f, 0.584f, 1f);
        private static readonly Color GridColor  = new Color(0.25f,  0.25f,  0.25f,  1f);
        private static readonly Color FillColor  = new Color(0.502f, 0.812f, 0.584f, 0.12f);
        private static readonly Color PointColor = new Color(0.502f, 0.812f, 0.584f, 1f);

        public LineChartElement(float[] v) { _values = v; generateVisualContent += Draw; }
        public void SetValues(float[] v)   { _values = v; MarkDirtyRepaint(); }

        private void Draw(MeshGenerationContext ctx)
        {
            if (_values == null || _values.Length < 2) return;
            float w = contentRect.width, h = contentRect.height, padX = 12f, padY = 14f;

            float maxV = float.MinValue, minV = float.MaxValue;
            foreach (var v in _values) { if (v > maxV) maxV = v; if (v < minV) minV = v; }
            float range = Mathf.Max(maxV - minV, 1f);
            float vMin = minV - range * 0.08f, vMax = maxV + range * 0.08f, vRange = vMax - vMin;

            var p = ctx.painter2D;
            p.strokeColor = GridColor; p.lineWidth = 0.5f;
            for (int g = 0; g <= 4; g++)
            {
                float yg = padY + (h - 2 * padY) * g / 4f;
                p.BeginPath(); p.MoveTo(new Vector2(padX, yg)); p.LineTo(new Vector2(w - padX, yg)); p.Stroke();
            }

            var pts = new Vector2[_values.Length];
            for (int i = 0; i < _values.Length; i++)
                pts[i] = new Vector2(padX + (w - 2*padX)*i/(_values.Length-1),
                                     padY + (h - 2*padY)*(1f-(_values[i]-vMin)/vRange));

            p.fillColor = FillColor;
            p.BeginPath(); p.MoveTo(new Vector2(pts[0].x, h-padY));
            foreach (var pt in pts) p.LineTo(pt);
            p.LineTo(new Vector2(pts[pts.Length-1].x, h-padY)); p.ClosePath(); p.Fill();

            p.strokeColor = LineColor; p.lineWidth = 2f;
            p.BeginPath(); p.MoveTo(pts[0]);
            for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]); p.Stroke();

            p.fillColor = PointColor;
            foreach (var pt in pts) { p.BeginPath(); p.Arc(pt, 3.5f, 0f, 360f); p.Fill(); }
        }
    }
}
