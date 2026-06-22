// ================================================================
// DashboardController.cs  – Frontend-Meeting-Fixes
//
// Änderungen:
//  1. Y-Achse Chart: dynamische Eurowerte statt 1.0/0.5/0.0
//  2. Kalender: keine Tage aus Vor-/Folgemonat mehr, nur leere Felder
//  3. 5 Navigations-Buttons unter den 5 Stat-Kacheln statt 3 unter Chart
// ================================================================
using System;
using System.Collections.Generic;
using System.Linq;
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
        AppEventManager.OnKundenAnzahlGeaendert         += OnKunden;
        AppEventManager.OnAngeboteAnzahlGeaendert        += OnAngebote;
        AppEventManager.OnRechnungenAnzahlGeaendert      += OnRechnungen;
        AppEventManager.OnKassenbuchGeaendert            += OnKassenbuch;
        AppEventManager.OnDokumenteFortschrittGeaendert  += OnDokumenteFortschritt;
    }

    void OnDisable()
    {
        AppEventManager.OnKundenAnzahlGeaendert         -= OnKunden;
        AppEventManager.OnAngeboteAnzahlGeaendert        -= OnAngebote;
        AppEventManager.OnRechnungenAnzahlGeaendert      -= OnRechnungen;
        AppEventManager.OnKassenbuchGeaendert            -= OnKassenbuch;
        AppEventManager.OnDokumenteFortschrittGeaendert  -= OnDokumenteFortschritt;
    }

    // ============================================================
    // INITIALER DB-LOAD (beim ersten Öffnen UND bei Jahreswechsel
    // im Kalender – lädt alle 5 Kacheln + Chart für ein bestimmtes Jahr)
    // ============================================================
    private void LadeInitialDaten()
    {
        LadeJahresDaten(_currentYear);
        LadeGruendungspfadFortschritt();
    }

    // ============================================================
    // GRÜNDUNGSPFAD-FORTSCHRITT LADEN
    // Liest den gespeicherten Fortschritt direkt aus der DB,
    // damit der Dashboard-Balken auch beim ersten Öffnen stimmt
    // (ohne dass der Gründungspfad-Screen zuerst besucht werden muss).
    // ============================================================
    private void LadeGruendungspfadFortschritt()
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null) return;

            var docs = db.getAllUserDocuments();
            var save = docs?.FirstOrDefault(d => d.documentType == 9001);
            if (save == null) return;

            var data = JsonUtility.FromJson<GruendungspfadSpeicher>(save.text);
            if (data == null) return;

            // Gleiche Segment-Zuordnung wie im GruendungspfadController
            float[] seg = data.segmentFortschritt ?? new float[5];
            if (seg.Length >= 5)
            {
                SetzeSegmente(seg[0], seg[1], seg[2], seg[3], seg[4]);
            }
        }
        catch { /* kein Gründungspfad-Save vorhanden, Balken bleibt bei 0 */ }
    }

    // Minimale Datenklasse zum Deserialisieren des Gründungspfad-Speichers
    [System.Serializable]
    private class GruendungspfadSpeicher
    {
        public float[] segmentFortschritt;
    }

    private void LadeJahresDaten(int jahr)
    {
        try
        {
            var db = UserDatabaseAccess.getCurrentUserDatabase();
            if (db == null) return;

            // --- Kunden: gefiltert nach lastUpdated-Jahr ---
            var alleKunden = db.getAllCustomers();
            int kundenImJahr = 0;
            if (alleKunden != null)
            {
                foreach (var k in alleKunden)
                    if (k.lastUpdated.Year == jahr) kundenImJahr++;
            }
            SetLabel("lbl-kunden", kundenImJahr.ToString());

            // --- Angebote: gefiltert nach date-Jahr ---
            var alleAngebote = db.getAllOffers();
            int angeboteImJahr = 0;
            if (alleAngebote != null)
            {
                foreach (var a in alleAngebote)
                    if (TryParseDatum(a.date, out DateTime d) && d.Year == jahr) angeboteImJahr++;
            }
            SetLabel("lbl-angebote", angeboteImJahr.ToString());

            // --- Rechnungen: gefiltert nach date-Jahr ---
            var alleRechnungen = db.getAllInvoices();
            int rechnungenImJahr = 0;
            if (alleRechnungen != null)
            {
                foreach (var r in alleRechnungen)
                    if (TryParseDatum(r.date, out DateTime d) && d.Year == jahr) rechnungenImJahr++;
            }
            SetLabel("lbl-rechnungen", rechnungenImJahr.ToString());

            // --- Kassenbuch: Bilanz + Monatswerte + Kontostand, alles für 'jahr' ---
            float umsatzJahr  = 0f;
            float[] monate    = BerechneMonatsBilanz(db, jahr, out umsatzJahr);
            float kontostandJahr = BerechneKontostandBisJahresende(db, jahr);

            SetLabel("lbl-kassenbuch", "€ " + umsatzJahr.ToString("N0"));
            SetKontostand(kontostandJahr);

            _chart?.SetValues(monate);
            UpdateYAxisLabels(monate);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Dashboard] JahresDatenLaden: " + e.Message);
        }
    }

    // Kontostand "bis zum gewählten Jahr" = alle Einkommen/Ausgaben mit
    // Datum <= 31.12.<jahr>. So bleibt der Kontostand bei Jahreswechsel
    // konsistent mit dem, was bis dahin tatsächlich passiert ist.
    float BerechneKontostandBisJahresende(DataBase db, int jahr)
    {
        var stichtag = new DateTime(jahr, 12, 31);
        float summe = 0f;

        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
            foreach (var e in einkommen)
            {
                bool ok = TryParseDatum(e.Datum, out DateTime d);
                bool zaehlt = ok && d <= stichtag;
                Debug.Log($"[DEBUG-Kontostand] EINKOMMEN '{e.Datum}' Amount={e.Amount} parsed={ok}->{(ok?d.ToString("yyyy-MM-dd"):"FEHLER")} zaehlt={zaehlt}");
                if (zaehlt) summe += e.Amount;
            }

        var ausgaben = db.getAllAusgabenEntries();
        if (ausgaben != null)
            foreach (var a in ausgaben)
            {
                bool ok = TryParseDatum(a.Datum, out DateTime d);
                bool zaehlt = ok && d <= stichtag;
                Debug.Log($"[DEBUG-Kontostand] AUSGABE '{a.Datum}' Amount={a.Amount} parsed={ok}->{(ok?d.ToString("yyyy-MM-dd"):"FEHLER")} zaehlt={zaehlt}");
                if (zaehlt) summe -= a.Amount;
            }

        Debug.Log($"[DEBUG-Kontostand] ENDERGEBNIS = {summe}");
        return summe;
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
        if (einkommen != null)
        {
            foreach (var e in einkommen)
            {
                if (TryParseDatum(e.Datum, out DateTime d) && d.Year == jahr)
                {
                    bilanzProMonat[d.Month - 1] += e.Amount;
                    summeJahr += e.Amount;
                }
            }
        }

        var ausgaben = db.getAllAusgabenEntries();
        if (ausgaben != null)
        {
            foreach (var a in ausgaben)
            {
                if (TryParseDatum(a.Datum, out DateTime d) && d.Year == jahr)
                {
                    bilanzProMonat[d.Month - 1] -= a.Amount;
                    summeJahr -= a.Amount;
                }
            }
        }

        return bilanzProMonat;
    }

    // ============================================================
    // Y-ACHSE  – dynamische Eurowert-Labels statt fixer 1.0/0.5/0.0
    // ============================================================
    void UpdateYAxisLabels(float[] monatswerte)
    {
        var yAxisContainer = _root.Q<VisualElement>("chart-y-axis");
        if (yAxisContainer == null || monatswerte == null || monatswerte.Length == 0) return;

        float maxV = float.MinValue, minV = float.MaxValue;
        foreach (var v in monatswerte) { if (v > maxV) maxV = v; if (v < minV) minV = v; }

        // Etwas Puffer wie im Chart selbst, damit die Linie nicht exakt am Rand klebt
        float range = Mathf.Max(maxV - minV, 1f);
        float vMin  = minV - range * 0.08f;
        float vMax  = maxV + range * 0.08f;

        // 5 Labels von oben (vMax) nach unten (vMin), gleichmäßig verteilt
        var labels = yAxisContainer.Query<Label>().ToList();
        for (int i = 0; i < labels.Count; i++)
        {
            float t      = labels.Count > 1 ? (float)i / (labels.Count - 1) : 0f;
            float wert   = Mathf.Lerp(vMax, vMin, t);
            labels[i].text = FormatiereEuroKompakt(wert);
        }
    }

    // Formatiert z.B. 15000 -> "€15k", -500 -> "-€500", 1234567 -> "€1,2M"
    string FormatiereEuroKompakt(float wert)
    {
        string vorzeichen = wert < 0 ? "-" : "";
        float abs = Mathf.Abs(wert);

        if (abs >= 1_000_000f)
            return $"{vorzeichen}€{(abs / 1_000_000f).ToString("0.#")}M";
        if (abs >= 1_000f)
            return $"{vorzeichen}€{(abs / 1_000f).ToString("0.#")}k";
        return $"{vorzeichen}€{abs.ToString("0")}";
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
        {
            _chart?.SetValues(monate);
            UpdateYAxisLabels(monate);
        }
    }

    void OnDokumenteFortschritt(float stammdaten, float vertraege, float steuer, float rechnungen, float sonstiges)
        => SetzeSegmente(stammdaten, vertraege, steuer, rechnungen, sonstiges);

    void SetzeSegmente(float stammdaten, float vertraege, float steuer, float rechnungen, float sonstiges)
    {
        SetSegment("seg-fill-stammdaten", stammdaten);
        SetSegment("seg-fill-vertraege",  vertraege);
        SetSegment("seg-fill-steuer",     steuer);
        SetSegment("seg-fill-rechnungen", rechnungen);
        SetSegment("seg-fill-sonstiges",  sonstiges);

        float gesamt = (stammdaten + vertraege + steuer + rechnungen + sonstiges) / 5f;
        var pctLabel = _root.Q<Label>("lbl-fortschritt-gesamt");
        if (pctLabel != null) pctLabel.text = Mathf.RoundToInt(gesamt * 100f) + "%";
    }

    void SetSegment(string name, float wert)
    {
        var fill = _root.Q<VisualElement>(name);
        if (fill == null) return;
        fill.style.width = new StyleLength(new Length(Mathf.Clamp01(wert) * 100f, LengthUnit.Percent));
    }

    // ============================================================
    // BUTTONS  – 5 Stück, je einer unter den 5 Stat-Kacheln
    //
    // Layout:
    //   unter "Kunden hinterlegt"      -> Kundendatenbank
    //   unter "Angebote erstellt"      -> Angebot
    //   unter "Rechnungen gestellt"    -> Rechnung
    //   unter "Kassenbuch-Umsatz Jahr" + "Kontostand" (1 breiter Button) -> Kassenbuch
    // ============================================================
    void SetupButtons()
    {
        BindScene("btn-nav-kunden",     "KundenDB");
        BindScene("btn-nav-angebot",    "Angebot");
        BindScene("btn-nav-rechnung",   "Rechnung");
        BindScene("btn-nav-kassenbuch", "Kassenbuch");
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
            {
                if (int.TryParse(evt.newValue, out int y))
                {
                    _currentYear = y;
                    RenderKalender();
                    LadeJahresDaten(_currentYear);
                    LadeDeadlines();
                }
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
        int vorherigesJahr = _currentYear;

        _currentMonth += delta;
        if (_currentMonth < 1)  { _currentMonth = 12; _currentYear--; }
        if (_currentMonth > 12) { _currentMonth = 1;  _currentYear++; }

        var dropMonat = _root.Q<DropdownField>("dropdown-monat");
        var dropJahr  = _root.Q<DropdownField>("dropdown-jahr");
        if (dropMonat != null) dropMonat.index = _currentMonth - 1;
        if (dropJahr  != null) dropJahr.value  = _currentYear.ToString();
        RenderKalender();

        // Nur neu laden wenn sich das Jahr durch den Monatswechsel tatsächlich geändert hat
        if (_currentYear != vorherigesJahr)
        {
            LadeJahresDaten(_currentYear);
            LadeDeadlines();
        }
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

    // FIX: keine Tage aus Vor-/Folgemonat mehr anzeigen.
    // Slots vor dem 1. des Monats und nach dem letzten Tag bleiben leer (kein Text, deaktiviert).
    void RenderKalender()
    {
        var today          = DateTime.Today;
        var ersterTag      = new DateTime(_currentYear, _currentMonth, 1);
        int tageImMonat    = DateTime.DaysInMonth(_currentYear, _currentMonth);
        int startWochentag = (int)ersterTag.DayOfWeek;

        for (int i = 0; i < 42; i++)
        {
            var btn = _root.Q<Button>($"cal-day-{i}");
            if (btn == null) continue;

            btn.RemoveFromClassList("cal-day-today");
            btn.RemoveFromClassList("cal-day-other-month");
            btn.RemoveFromClassList("cal-day-deadline");
            btn.RemoveFromClassList("cal-day-empty");
            btn.tooltip = "";

            bool istImAktuellenMonat = i >= startWochentag && i - startWochentag < tageImMonat;

            if (!istImAktuellenMonat)
            {
                // Leeres Feld statt Tag aus Vor-/Folgemonat
                btn.text = "";
                btn.AddToClassList("cal-day-empty");
                btn.SetEnabled(false);
                continue;
            }

            btn.SetEnabled(true);
            int tag = i - startWochentag + 1;
            btn.text = tag.ToString();

            if (tag == today.Day && _currentMonth == today.Month && _currentYear == today.Year)
                btn.AddToClassList("cal-day-today");

            var tagDatum = new DateTime(_currentYear, _currentMonth, tag);
            string key = tagDatum.ToString("yyyy-MM-dd");

            if (_deadlines.TryGetValue(key, out var eintraege))
            {
                btn.AddToClassList("cal-day-deadline");
                btn.tooltip = string.Join("\n", eintraege);
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
