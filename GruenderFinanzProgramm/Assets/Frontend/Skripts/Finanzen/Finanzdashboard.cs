using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Finanzdashboard : MonoBehaviour
{
    private VisualElement Root;
    private LineChartElement _chart;
    private BarChartElement _barchart;
    private PieChartElement _pieChart;

    private VisualElement _diagramm;
    private VisualElement _Diagramm;

    private void Awake()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("[Dashboard] Kein UIDocument gefunden!");
            return;
        }

        Root = uiDocument.rootVisualElement;
    }

 private void Start()
{
    SetupChart();
    SetupDiagramm();
    SetupDiagramm2();
    AktualisiereStatusTabelle();
    LadeKassenbuchDiagramm();

    LadeGesamtumsatz();
    LadeGewinn();        
    LadeDienstleistungsRanking();

    LadeOffeneRechnungen();

    RegistriereButtons();
}

    private void RegistriereButtons()
    {
        Button Finanzen1 = Root.Q<Button>("Finanzen1");

        if (Finanzen1 == null)
        {
            Debug.LogWarning("[Finanzdashboard] btn-finanzen1 nicht gefunden");
            return;
        }

        Finanzen1.clicked += OeffneFinanzen1;
    }

private void OeffneFinanzen1()
{
    SceneManager.LoadScene("Finanzen1");
}

    // =========================================================
    // 💰 KASSENBUCH DIAGRAMM (JAHRESSTATISTIK)
    // =========================================================
    private void LadeKassenbuchDiagramm()
    {
        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();
        if (db == null) return;

        int jahr = DateTime.Today.Year;
        float[] einnahmen = new float[12];
        float[] ausgaben  = new float[12];

        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
            foreach (var e in einkommen)
                if (DateTime.TryParse(e.getDatum(), out DateTime d) && d.Year == jahr)
                    einnahmen[d.Month - 1] += e.Amount;

        var ausgabenList = db.getAllAusgabenEntries();
        if (ausgabenList != null)
            foreach (var a in ausgabenList)
                if (DateTime.TryParse(a.getDatum(), out DateTime d) && d.Year == jahr)
                    ausgaben[d.Month - 1] += a.Amount;

        float[] netto = new float[12];
        for (int i = 0; i < 12; i++) netto[i] = einnahmen[i] - ausgaben[i];

        _chart?.SetValues(netto);

        // Y-Achse für Finanzen im Überblick updaten
        UpdateYAxisLabelsFor(Root.Q<VisualElement>("chart-y-axis"), netto);
    }

    private void LadeGesamtumsatz()
    {
        Label umsatzLabel = Root.Q<Label>("Umsatzwert");

        if (umsatzLabel == null)
        {
            Debug.LogWarning("[Finanzdashboard] Umsatzwert nicht gefunden");
            return;
        }

        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            umsatzLabel.text = "0,00 €";
            return;
        }

        float gesamtumsatz = 0f;

        var einkommen = db.getAllEinkommenEntries();

        if (einkommen != null)
        {
            foreach (var eintrag in einkommen)
            {
                gesamtumsatz += (float)eintrag.Amount;
            }
        }

        umsatzLabel.text = gesamtumsatz.ToString("N2") + " €";

        Debug.Log("[Finanzdashboard] Gesamtumsatz: " + gesamtumsatz);
    }

    private void LadeGewinn()
    {
        Label gewinnLabel = Root.Q<Label>("Gewinnwert");

        if (gewinnLabel == null)
        {
            Debug.LogWarning("[Finanzdashboard] Gewinnwert nicht gefunden");
            return;
        }

        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            gewinnLabel.text = "0,00 €";
            return;
        }

        float einnahmen = 0f;
        float ausgaben = 0f;

        // Einnahmen summieren
        var einkommen = db.getAllEinkommenEntries();

        if (einkommen != null)
        {
            foreach (var e in einkommen)
            {
                einnahmen += (float)e.Amount;
            }
        }

        // Ausgaben summieren
        var ausgabenListe = db.getAllAusgabenEntries();

        if (ausgabenListe != null)
        {
            foreach (var a in ausgabenListe)
            {
                ausgaben += (float)a.Amount;
            }
        }

        float gewinn = einnahmen - ausgaben;

        gewinnLabel.text = gewinn.ToString("N2") + " €";

        Debug.Log(
            $"[Finanzdashboard] Gewinn: {gewinn} € | Einnahmen: {einnahmen} € | Ausgaben: {ausgaben} €"
        );
    }

    private void LadeDienstleistungsRanking()
    {
        Debug.Log("=== LadeDienstleistungsRanking gestartet ===");

        VisualElement ranking = Root.Q<VisualElement>("Ranking");

        if (ranking == null)
        {
            Debug.LogError("Ranking nicht gefunden!");
            return;
        }

        ranking.Clear();

        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            Debug.LogError("Keine Datenbank aktiv");
            return;
        }

        List<Service> services = db.getAllServices();

        if (services == null)
        {
            Debug.LogError("Keine Services gefunden");
            return;
        }

        Debug.Log("Services gefunden: " + services.Count);

        foreach (Service service in services)
        {
            Label row = new Label("• " + service.name);

            row.style.color = Color.white;
            row.style.fontSize = 18;
            row.style.marginBottom = 8;

            ranking.Add(row);
        }
    }

    // =========================================================
    // STATUS TABELLE (DEIN ORIGINAL + UNVERÄNDERT)
    // =========================================================
    public void AktualisiereStatusTabelle()
    {
        VisualElement container = Root.Q<VisualElement>("StatusAngebot");

        if (container == null)
        {
            Debug.LogWarning("[Dashboard] StatusAngebot nicht gefunden");
            return;
        }

        container.Clear();

        container.Add(CreateHeader());

        Dictionary<string, int> statusCount = new Dictionary<string, int>
        {
            { "Entwurf", 0 },
            { "Versendet", 0 },
            { "Angenommen", 0 },
            { "Abgelehnt", 0 }
        };

        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db != null)
        {
            List<Offer> offers = db.getAllOffers();

            if (offers != null)
            {
                foreach (Offer offer in offers)
                {
                    if (offer == null || string.IsNullOrEmpty(offer.status))
                        continue;

                    if (statusCount.ContainsKey(offer.status))
                        statusCount[offer.status]++;
                }
            }
        }

        foreach (var entry in statusCount)
        {
            container.Add(CreateRow(entry.Key, entry.Value));
        }
    }

    // =========================================================
    // CHART SETUP
    // =========================================================
    private void SetupChart()
    {
        VisualElement diagramm1 =
            Root.Q<VisualElement>("diagramm1");

        VisualElement yAxis =
            Root.Q<VisualElement>("chart-y-axis");

        VisualElement xAxis =
            Root.Q<VisualElement>("chart-x-axis");

        if (diagramm1 == null)
        {
            Debug.LogWarning("[Dashboard] diagramm nicht gefunden");
            return;
        }

        _chart = new LineChartElement(yAxis, xAxis);

        _chart.style.position = Position.Absolute;
        _chart.style.left = 0;
        _chart.style.right = 0;
        _chart.style.top = 0;
        _chart.style.bottom = 0;

        diagramm1.Add(_chart);



      
    }

   private void SetupDiagramm()
    {
        VisualElement diagramm = Root.Q<VisualElement>("diagramm4");
        VisualElement yAxis    = Root.Q<VisualElement>("rentab-y-axis");
        VisualElement xAxis    = Root.Q<VisualElement>("rentab-x-axis");

        if (diagramm == null)
        {
            Debug.LogWarning("[Finanzdashboard] diagramm4 nicht gefunden.");
            return;
        }

        _barchart = new BarChartElement(yAxis, xAxis);
        _barchart.style.position = Position.Absolute;
        _barchart.style.left = 0; _barchart.style.right  = 0;
        _barchart.style.top  = 0; _barchart.style.bottom = 0;
        diagramm.Add(_barchart);

        // Echte Daten aus DB laden
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        int letzJahr = DateTime.Today.Year - 1;
        int diesJahr = DateTime.Today.Year;
        float ein1 = 0, aus1 = 0, ein2 = 0, aus2 = 0;
        if (db != null)
        {
            var eink = db.getAllEinkommenEntries();
            var ausg = db.getAllAusgabenEntries();
            if (eink != null) foreach (var e in eink)
                if (DateTime.TryParse(e.getDatum(), out DateTime d))
                { if (d.Year == letzJahr) ein1 += e.Amount; else if (d.Year == diesJahr) ein2 += e.Amount; }
            if (ausg != null) foreach (var a in ausg)
                if (DateTime.TryParse(a.getDatum(), out DateTime d))
                { if (d.Year == letzJahr) aus1 += a.Amount; else if (d.Year == diesJahr) aus2 += a.Amount; }
        }
        SetDaten(new float[] { ein1, aus1, ein1 - aus1, ein2, aus2, ein2 - aus2 });
    }

    public void SetDaten(float[] daten)
    {
        if (_barchart != null)
            _barchart.SetValues(daten);
    }

    private void SetupDiagramm2()
    {
        // ROHGEWINN (diagramm3) - LineChart
        VisualElement diagramm3 = Root.Q<VisualElement>("diagramm3");
        VisualElement yAxis3    = Root.Q<VisualElement>("rohgewinn-y-axis");
        VisualElement xAxis3    = Root.Q<VisualElement>("rohgewinn-x-axis");

        // KAPITALBEDARF (Diagramm2) - PieChart
        VisualElement diagramm2 = Root.Q<VisualElement>("Diagramm2");
        VisualElement pieLegende = Root.Q<VisualElement>("pie-legende");

        // Rohgewinn: Einnahmen - Betriebsausgaben pro Monat
        var db = UserDatabaseAccess.getCurrentUserDatabase();
        float[] rohgewinn = new float[12];
        if (db != null)
        {
            int jahr = DateTime.Today.Year;
            float[] einnahmen = new float[12];
            float[] ausgaben  = new float[12];
            var eink = db.getAllEinkommenEntries();
            var ausg = db.getAllAusgabenEntries();
            if (eink != null) foreach (var e in eink)
                if (DateTime.TryParse(e.getDatum(), out DateTime d) && d.Year == jahr)
                    einnahmen[d.Month - 1] += e.Amount;
            if (ausg != null) foreach (var a in ausg)
                if (DateTime.TryParse(a.getDatum(), out DateTime d) && d.Year == jahr)
                    ausgaben[d.Month - 1] += a.Amount;
            for (int i = 0; i < 12; i++) rohgewinn[i] = einnahmen[i] - ausgaben[i];
        }

        // LineChart Rohgewinn
        if (diagramm3 != null)
        {
            var rohChart = new LineChartElement(yAxis3, xAxis3);
            rohChart.style.position = Position.Absolute;
            rohChart.style.left = 0; rohChart.style.right  = 0;
            rohChart.style.top  = 0; rohChart.style.bottom = 0;
            diagramm3.Add(rohChart);
            rohChart.SetValues(rohgewinn);
            UpdateYAxisLabelsFor(yAxis3, rohgewinn);
        }

        // PieChart Kapitalbedarf - Ausgaben nach Art
        if (diagramm2 != null)
        {
            float personal = 0, betrieb = 0, steuern = 0, tilgung = 0, sonstiges = 0;
            if (db != null)
            {
                int jahr = DateTime.Today.Year;
                var ausg = db.getAllAusgabenEntries();
                if (ausg != null) foreach (var a in ausg)
                    if (DateTime.TryParse(a.getDatum(), out DateTime d) && d.Year == jahr)
                    {
                        string art = a.getArt() ?? "";
                        if (art == "Gehälter")                           personal  += a.Amount;
                        else if (art == "Steuern" || art == "Finanzamt") steuern   += a.Amount;
                        else if (art == "Tilgungsraten")                 tilgung   += a.Amount;
                        else if (art == "Marketing" || art == "Reisekosten") betrieb += a.Amount;
                        else                                             sonstiges += a.Amount;
                    }
            }
            // Fallback wenn keine Daten
            if (personal + betrieb + steuern + tilgung + sonstiges == 0)
            { personal = 30; betrieb = 25; steuern = 15; tilgung = 10; sonstiges = 20; }

            _pieChart = new PieChartElement();
            _pieChart.SetValues(new float[] { personal, betrieb, steuern, tilgung, sonstiges });
            _pieChart.style.position = Position.Absolute;
            _pieChart.style.left = 0; _pieChart.style.right  = 0;
            _pieChart.style.top  = 0; _pieChart.style.bottom = 0;
            diagramm2.Add(_pieChart);

            // Legende
            if (pieLegende != null)
            {
                var kategorien = new (string name, Color farbe)[]
                {
                    ("Gehälter",    new Color(0.502f, 0.812f, 0.584f)),
                    ("Betrieb",     new Color(0.902f, 0.224f, 0.275f)),
                    ("Steuern",     new Color(0.996f, 0.663f, 0.220f)),
                    ("Tilgung",     new Color(0.302f, 0.596f, 1.000f)),
                    ("Sonstiges",   new Color(0.627f, 0.627f, 0.627f)),
                };
                foreach (var k in kategorien)
                {
                    var zeile = new VisualElement();
                    zeile.style.flexDirection = FlexDirection.Row;
                    zeile.style.alignItems    = Align.Center;
                    zeile.style.marginBottom  = 4;
                    var dot = new VisualElement();
                    dot.style.width = 10; dot.style.height = 10;
                    dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius =
                    dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 5;
                    dot.style.backgroundColor = k.farbe;
                    dot.style.marginRight = 6; dot.style.flexShrink = 0;
                    var lbl = new Label(k.name);
                    lbl.style.color = new Color(0.8f, 0.8f, 0.8f);
                    lbl.style.fontSize = 11;
                    zeile.Add(dot); zeile.Add(lbl);
                    pieLegende.Add(zeile);
                }
            }
        }
    }

    public void SetDaten2(float[] daten)
    {
        _chart?.SetValues(daten);
    }

    public void SetPieDaten(float[] daten)
    {
        _pieChart?.SetValues(daten);
    }

    // =========================================================
    // PARSE HELPER
    // =========================================================
    private bool TryParse(string text, out DateTime result)
    {
        return DateTime.TryParse(text, out result);
    }

    // =========================================================
    // HEADER
    // =========================================================
    private VisualElement CreateHeader()
    {
        VisualElement header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.paddingBottom = 10;
        header.style.marginBottom = 10;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = new Color(1, 1, 1, 0.25f);

        Label left = new Label("Status");
        Label right = new Label("Anzahl");

        ApplyTextStyle(left, true);
        ApplyTextStyle(right, true);

        header.Add(left);
        header.Add(right);

        return header;
    }

    // =========================================================
    // ROWS
    // =========================================================
    private VisualElement CreateRow(string status, int count)
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.paddingTop = 7;
        row.style.paddingBottom = 7;

        Label statusLabel = new Label(status);
        Label countLabel = new Label(count.ToString());

        ApplyTextStyle(statusLabel, false);
        ApplyTextStyle(countLabel, false);

        row.Add(statusLabel);
        row.Add(countLabel);

        return row;
    }

    // =========================================================
    // STYLE
    // =========================================================
    private void ApplyTextStyle(Label label, bool isHeader)
    {
        label.style.color = Color.white;
        label.style.fontSize = isHeader ? 18 : 16;

        label.style.unityFontStyleAndWeight =
            isHeader ? FontStyle.Bold : FontStyle.Normal;

        if (!isHeader)
            label.style.opacity = 0.92f;
    }

    // =========================================================
    // LINE CHART (EINFACHER NETTO-CHART)
    // =========================================================
    // Identisch mit DashboardController.LineChartElement — Füllfläche + Ventoriq-Farben
    private class LineChartElement : VisualElement
    {
        private float[] _values;
        private readonly VisualElement _yAxis;
        private readonly VisualElement _xAxis;
        private static readonly Color LineColor  = new Color(0.502f, 0.812f, 0.584f, 1f);
        private static readonly Color GridColor  = new Color(0.25f,  0.25f,  0.25f,  1f);
        private static readonly Color FillColor  = new Color(0.502f, 0.812f, 0.584f, 0.12f);
        private static readonly Color PointColor = new Color(0.502f, 0.812f, 0.584f, 1f);

        public LineChartElement(VisualElement yAxis, VisualElement xAxis)
        {
            _yAxis = yAxis;
            _xAxis = xAxis;
            generateVisualContent += Draw;
        }

        public void SetValues(float[] v) { _values = v; MarkDirtyRepaint(); }

        private void Draw(MeshGenerationContext ctx)
        {
            if (_values == null || _values.Length < 2) return;
            float w = contentRect.width, h = contentRect.height, padX = 12f, padY = 14f;

            float maxV = float.MinValue, minV = float.MaxValue;
            foreach (var v in _values) { if (v > maxV) maxV = v; if (v < minV) minV = v; }
            float range = Mathf.Max(maxV - minV, 1f);
            float vMin  = minV - range * 0.08f;
            float vMax  = maxV + range * 0.08f;
            float vRange = vMax - vMin;

            var p = ctx.painter2D;
            p.strokeColor = GridColor; p.lineWidth = 0.5f;
            for (int g = 0; g <= 4; g++)
            {
                float yg = padY + (h - 2 * padY) * g / 4f;
                p.BeginPath(); p.MoveTo(new Vector2(padX, yg)); p.LineTo(new Vector2(w - padX, yg)); p.Stroke();
            }

            var pts = new Vector2[_values.Length];
            for (int i = 0; i < _values.Length; i++)
                pts[i] = new Vector2(
                    padX + (w - 2*padX) * i / (_values.Length - 1),
                    padY + (h - 2*padY) * (1f - (_values[i] - vMin) / vRange));

            p.fillColor = FillColor;
            p.BeginPath(); p.MoveTo(new Vector2(pts[0].x, h - padY));
            foreach (var pt in pts) p.LineTo(pt);
            p.LineTo(new Vector2(pts[pts.Length-1].x, h - padY));
            p.ClosePath(); p.Fill();

            p.strokeColor = LineColor; p.lineWidth = 2f;
            p.BeginPath(); p.MoveTo(pts[0]);
            for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]);
            p.Stroke();

            p.fillColor = PointColor;
            foreach (var pt in pts) { p.BeginPath(); p.Arc(pt, 3.5f, 0f, 360f); p.Fill(); }
        }
    }
    // Legacy - für Finanzen im Überblick
    private void UpdateYAxisLabels(float[] values)
    {
        UpdateYAxisLabelsFor(Root.Q<VisualElement>("chart-y-axis"), values);
    }

    private void UpdateYAxisLabelsFor(VisualElement yAxis, float[] values)
    {
        if (yAxis == null || values == null) return;

        float max = float.MinValue, min = float.MaxValue;
        foreach (float v in values) { if (v > max) max = v; if (v < min) min = v; }

        float range    = Mathf.Max(max - min, 1f);
        float dispMin  = min - range * 0.1f;
        float dispMax  = max + range * 0.1f;

        var labels = yAxis.Query<Label>().ToList();
        for (int i = 0; i < labels.Count; i++)
        {
            float t     = labels.Count > 1 ? (float)i / (labels.Count - 1) : 0f;
            float value = Mathf.Lerp(dispMax, dispMin, t);
            labels[i].text = FormatEuro(value);
        }
    }
    private string FormatEuro(float value)
    {
        string sign = value < 0 ? "-" : "";
        float abs = Mathf.Abs(value);

        if (abs >= 1000000f)
            return $"{sign}€{(abs / 1000000f):0.#}M";

        if (abs >= 1000f)
            return $"{sign}€{(abs / 1000f):0.#}k";

        return $"{sign}€{abs:0}";
    }

    private void LadeOffeneRechnungen()
    {
        Label label = Root.Q<Label>("OffeneRechnungen");

        if (label == null)
        {
            Debug.LogWarning("[Finanzdashboard] OffeneRechnungen Label nicht gefunden");
            return;
        }

        var db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            label.text = "0";
            return;
        }

        var invoices = db.getAllInvoices();

        if (invoices == null)
        {
            label.text = "0";
            return;
        }

        int count = 0;

        foreach (var r in invoices)
        {
            if (r != null && r.status == "Angenommen")
            {
                count++;
            }
        }

        label.text = count.ToString();
    }

    // Bar Chart
    private class BarChartElement : VisualElement
    {
        private float[] _values;
        private readonly VisualElement _yAxis;
        private readonly VisualElement _xAxis;

        public BarChartElement(VisualElement yAxis, VisualElement xAxis)
        {
            _yAxis = yAxis;
            _xAxis = xAxis;
            generateVisualContent += Draw;
        }

        public void SetValues(float[] values)
        {
            _values = values;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;

            float width = contentRect.width;
            float height = contentRect.height;

            if (width <= 0 || height <= 0 || _values == null)
                return;

            float max = 1f;
            foreach (var v in _values)
                if (v > max) max = v;

            // =====================================================
            // NUR HORIZONTALE GRID-LINIEN
            // =====================================================
            p.strokeColor = new Color(1f, 1f, 1f, 0.12f);
            p.lineWidth = 1f;

            if (_yAxis != null)
            {
                foreach (var label in _yAxis.Children())
                {
                    float y = label.worldBound.center.y - worldBound.yMin;

                    p.BeginPath();
                    p.MoveTo(new Vector2(0, y));
                    p.LineTo(new Vector2(width, y));
                    p.Stroke();
                }
            }

            // ❌ KEINE X-AXIS GRID LINES (VERTIKAL ENTFERNT)

            int years = 2;
            float groupWidth = width / years;

            float barWidth = 40f;
            float spacing = 10f;

            for (int year = 0; year < years; year++)
            {
                float startX = year * groupWidth + groupWidth / 2f - 70f;

                for (int type = 0; type < 3; type++)
                {
                    int index = year * 3 + type;
                    if (index >= _values.Length) continue;

                    float value = _values[index];
                    float h = (value / max) * height;

                    float x = startX + type * (barWidth + spacing);
                    float y = height - h;

                    // Ventoriq: Einnahmen=Grün, Ausgaben=Rot, Gewinn=Blau
                    Color barColor =
                        type == 0 ? new Color(0.502f, 0.812f, 0.584f, 1f) :
                        type == 1 ? new Color(0.902f, 0.224f, 0.275f, 1f) :
                                    new Color(0.302f, 0.596f, 1.000f, 1f);

                    p.fillColor = barColor;

                    p.BeginPath();
                    p.MoveTo(new Vector2(x, height));
                    p.LineTo(new Vector2(x, y));
                    p.LineTo(new Vector2(x + barWidth, y));
                    p.LineTo(new Vector2(x + barWidth, height));
                    p.ClosePath();
                    p.Fill();
                }
            }
        }
    }

    private VisualElement CreateCell(string text, bool isHeader)
    {
        var label = new Label(text);

        label.style.color = Color.white;
        label.style.unityTextAlign = TextAnchor.MiddleLeft;

        label.style.flexGrow = 1;
        label.style.width = Length.Percent(33);

        label.style.fontSize = isHeader ? 16 : 14;

        if (isHeader)
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

        return label;
    }



        private class PieChartElement : VisualElement
    {
        private float[] _values;

        private readonly Color[] _colors =
        {
            new Color(0.502f, 0.812f, 0.584f), // Grün   - Gehälter
            new Color(0.902f, 0.224f, 0.275f), // Rot    - Betrieb
            new Color(0.996f, 0.663f, 0.220f), // Orange - Steuern
            new Color(0.302f, 0.596f, 1.000f), // Blau   - Tilgung
            new Color(0.627f, 0.627f, 0.627f), // Grau   - Sonstiges
        };

        public PieChartElement()
        {
            generateVisualContent += Draw;
        }

        public void SetValues(float[] values)
        {
            _values = values;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;

            float width = contentRect.width;
            float height = contentRect.height;

            if (width <= 0 || height <= 0)
                return;

            if (_values == null || _values.Length == 0)
                return;

            float total = 0f;

            foreach (var v in _values)
                total += Mathf.Max(0, v);

            if (total <= 0)
                return;

            Vector2 center = new Vector2(width / 2f, height / 2f);
            float radius = Mathf.Min(width, height) * 0.4f;

            float start = 0f;

            for (int i = 0; i < _values.Length; i++)
            {
                float v = Mathf.Max(0, _values[i]);
                float slice = v / total;

                float end = start + slice * 360f;

                p.fillColor = _colors[i % _colors.Length];

                p.BeginPath();
                p.MoveTo(center);
                p.Arc(center, radius, start, end);
                p.ClosePath();
                p.Fill();

                start = end;
            }
        }
    }

    
}


