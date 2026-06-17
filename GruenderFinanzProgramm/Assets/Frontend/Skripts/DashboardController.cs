// ================================================================
// DashboardController.cs  – OHNE EventManager
//
// Lädt beim Start einfach alles direkt aus der DB.
// Genau dasselbe Prinzip wie KundendatenbankController.
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

    private readonly string[] _monthNames =
    {
        "Januar","Februar","März","April","Mai","Juni",
        "Juli","August","September","Oktober","November","Dezember"
    };

    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        var today     = DateTime.Today;
        _currentYear  = today.Year;
        _currentMonth = today.Month;

        SetupButtons();
        SetupCalendar();
        SetupChart();

        // Alles direkt aus DB laden – fertig.
        LadeDaten();
    }

    // ============================================================
    // DATEN LADEN  (direkt aus DB, kein EventManager)
    // ============================================================
    private void LadeDaten()
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null) { Debug.LogWarning("[Dashboard] Keine aktive DB."); return; }

            string aktuellesJahr = DateTime.Today.Year.ToString();

            // --- Kachel 1: Kunden ---
            var kunden = db.getAllCustomers();
            SetLabel("lbl-kunden", (kunden?.Count ?? 0).ToString());

            // --- Kachel 2: Angebote ---
            var angebote = db.getAllOffers();
            SetLabel("lbl-angebote", (angebote?.Count ?? 0).ToString());

            // --- Kachel 3: Rechnungen ---
            var rechnungen = db.getAllInvoices();
            SetLabel("lbl-rechnungen", (rechnungen?.Count ?? 0).ToString());

            // --- Kachel 4: Kassenbuch-Umsatz (nur aktuelles Jahr) ---
            // Datum ist ein string im Format "dd.MM.yyyy" laut createEinkommen()
            var alleEinkommen = db.getAllEinkommenEntries();
            float umsatzJahr = 0f;
            float[] monate   = new float[12];

            if (alleEinkommen != null)
            {
                foreach (var e in alleEinkommen)
                {
                    // Datum parsen: "dd.MM.yyyy" oder "yyyy-MM-dd" – beide abfangen
                    if (DateTime.TryParse(e.Datum, out DateTime datum))
                    {
                        if (datum.Year == DateTime.Today.Year)
                        {
                            umsatzJahr += e.Amount;
                            monate[datum.Month - 1] += e.Amount;
                        }
                    }
                }
            }
            SetLabel("lbl-kassenbuch", "€" + umsatzJahr.ToString("N0"));
            _chart?.SetValues(monate);

            // --- Kachel 5: Kontostand (Einkommen - Ausgaben, gesamt) ---
            float differenz = db.getDifferenz();
            SetLabel("lbl-kontostand", "€" + differenz.ToString("N0"));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Dashboard] Fehler beim Laden: " + e.Message);
        }
    }

    // ============================================================
    // BUTTONS  – SceneManager wie SidebarController
    // ============================================================
    void SetupButtons()
    {
        BindScene("btn-nav-finanzen", "Kassenbuch");   // Scene-Namen prüfen!
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
    // HILFS-METHODEN
    // ============================================================
    void SetLabel(string name, string text)
    {
        var lbl = _root.Q<Label>(name);
        if (lbl != null) lbl.text = text;
    }

    void SetSegment(string fillName, float value)
    {
        var fill = _root.Q<VisualElement>(fillName);
        if (fill == null) return;
        fill.RemoveFromClassList("seg-full");
        fill.RemoveFromClassList("seg-partial");
        fill.RemoveFromClassList("seg-empty");
        fill.style.width = new StyleLength(
            new Length(Mathf.Clamp01(value) * 100f, LengthUnit.Percent));
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
            {
                _currentMonth = dropMonat.index + 1;
                RenderKalender();
            });
        }

        if (dropJahr != null)
        {
            var jahre = new List<string>();
            for (int y = _currentYear - 3; y <= _currentYear + 3; y++)
                jahre.Add(y.ToString());
            dropJahr.choices = jahre;
            dropJahr.value   = _currentYear.ToString();
            dropJahr.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int y))
                { _currentYear = y; RenderKalender(); }
            });
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

            int tag; bool anderMonat;

            if (i < startWochentag)
            { tag = vormonatTage - (startWochentag - 1 - i); anderMonat = true; }
            else if (i - startWochentag < tageImMonat)
            { tag = i - startWochentag + 1; anderMonat = false; }
            else
            { tag = i - startWochentag - tageImMonat + 1; anderMonat = true; }

            btn.text = tag.ToString();

            if (anderMonat)
                btn.AddToClassList("cal-day-other-month");
            else if (tag == today.Day && _currentMonth == today.Month && _currentYear == today.Year)
                btn.AddToClassList("cal-day-today");
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

        public LineChartElement(float[] values) { _values = values; generateVisualContent += Draw; }

        public void SetValues(float[] v) { _values = v; MarkDirtyRepaint(); }

        private void Draw(MeshGenerationContext ctx)
        {
            if (_values == null || _values.Length < 2) return;

            float w = contentRect.width, h = contentRect.height;
            float padX = 12f, padY = 14f;

            float maxVal = float.MinValue, minVal = float.MaxValue;
            foreach (var v in _values) { if (v > maxVal) maxVal = v; if (v < minVal) minVal = v; }

            float range = Mathf.Max(maxVal - minVal, 1f);
            float vMin = minVal - range * 0.08f, vMax = maxVal + range * 0.08f;
            float vRange = vMax - vMin;

            var p = ctx.painter2D;

            p.strokeColor = GridColor; p.lineWidth = 0.5f;
            for (int g = 0; g <= 4; g++)
            {
                float yg = padY + (h - 2 * padY) * g / 4f;
                p.BeginPath(); p.MoveTo(new Vector2(padX, yg));
                p.LineTo(new Vector2(w - padX, yg)); p.Stroke();
            }

            var pts = new Vector2[_values.Length];
            for (int i = 0; i < _values.Length; i++)
                pts[i] = new Vector2(
                    padX + (w - 2 * padX) * i / (_values.Length - 1),
                    padY + (h - 2 * padY) * (1f - (_values[i] - vMin) / vRange));

            p.fillColor = FillColor;
            p.BeginPath(); p.MoveTo(new Vector2(pts[0].x, h - padY));
            foreach (var pt in pts) p.LineTo(pt);
            p.LineTo(new Vector2(pts[pts.Length - 1].x, h - padY));
            p.ClosePath(); p.Fill();

            p.strokeColor = LineColor; p.lineWidth = 2f;
            p.BeginPath(); p.MoveTo(pts[0]);
            for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]);
            p.Stroke();

            p.fillColor = PointColor;
            foreach (var pt in pts) { p.BeginPath(); p.Arc(pt, 3.5f, 0f, 360f); p.Fill(); }
        }
    }
}
