using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Finanzdashboard : MonoBehaviour
{
    private VisualElement Root;
    private LineChartElement _chart;

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

        float[] einnahmen = new float[12];
        float[] ausgaben  = new float[12];

        // EINNAHMEN
        var einkommen = db.getAllEinkommenEntries();
        if (einkommen != null)
        {
            foreach (var e in einkommen)
            {
                if (TryParse(e.Datum, out DateTime d))
                {
                    einnahmen[d.Month - 1] += e.Amount;
                }
            }
        }

        // AUSGABEN
        var ausgabenList = db.getAllAusgabenEntries();
        if (ausgabenList != null)
        {
            foreach (var a in ausgabenList)
            {
                if (TryParse(a.Datum, out DateTime d))
                {
                    ausgaben[d.Month - 1] += a.Amount;
                }
            }
        }

        // NETTO (Einnahmen - Ausgaben)
        float[] netto = new float[12];

        for (int i = 0; i < 12; i++)
        {
            netto[i] = einnahmen[i] - ausgaben[i];
        }

        _chart?.SetValues(netto);

        UpdateYAxisLabels(netto);
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
        gewinnLabel.style.color = gewinn < 0
            ? new StyleColor(new UnityEngine.Color(230f/255f, 57f/255f, 70f/255f))   // Rot
            : new StyleColor(new UnityEngine.Color(128f/255f, 207f/255f, 149f/255f)); // Grün

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
        var canvas = Root.Q<VisualElement>("diagramm");
        if (canvas == null) { Debug.LogWarning("[Finanzdashboard] diagramm nicht gefunden"); return; }

        _chart = new LineChartElement(new float[12]);
        canvas.Add(_chart);
        _chart.style.position = Position.Absolute;
        _chart.style.left = 0; _chart.style.top = 0;
        _chart.style.right = 0; _chart.style.bottom = 0;
    }

    // Identisch mit DashboardController.LineChartElement
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
    private void UpdateYAxisLabels(float[] values)
    {
        VisualElement yAxis = Root.Q<VisualElement>("chart-y-axis");

        if (yAxis == null)
        {
            Debug.LogWarning("[Finanzdashboard] chart-y-axis nicht gefunden");
            return;
        }

        float max = float.MinValue;
        float min = float.MaxValue;

        foreach (float value in values)
        {
            if (value > max) max = value;
            if (value < min) min = value;
        }

        float range = Mathf.Max(max - min, 1f);

        float displayMin = min - range * 0.1f;
        float displayMax = max + range * 0.1f;

        var labels = yAxis.Query<Label>().ToList();

        for (int i = 0; i < labels.Count; i++)
        {
            float t = labels.Count > 1
                ? (float)i / (labels.Count - 1)
                : 0f;

            float value = Mathf.Lerp(displayMax, displayMin, t);

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

    private bool TryParse(string text, out DateTime result)
    {
        return DateTime.TryParse(text, out result);
    }

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

    private void ApplyTextStyle(Label label, bool isHeader)
    {
        label.style.color = Color.white;
        label.style.fontSize = isHeader ? 18 : 16;
        label.style.unityFontStyleAndWeight = isHeader ? FontStyle.Bold : FontStyle.Normal;
        if (!isHeader) label.style.opacity = 0.92f;
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
}