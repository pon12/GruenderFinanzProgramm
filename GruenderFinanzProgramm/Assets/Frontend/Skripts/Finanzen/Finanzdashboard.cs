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
        VisualElement diagramm =
            Root.Q<VisualElement>("diagramm");

        VisualElement yAxis =
            Root.Q<VisualElement>("chart-y-axis");

        VisualElement xAxis =
            Root.Q<VisualElement>("chart-x-axis");

        if (diagramm == null)
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

        diagramm.Add(_chart);
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
    private class LineChartElement : VisualElement
{
    private float[] _values;

    private readonly VisualElement _yAxis;
    private readonly VisualElement _xAxis;

    public LineChartElement(
        VisualElement yAxis,
        VisualElement xAxis)
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

        if (width <= 0 || height <= 0)
            return;

        // Hilfslinien

        p.strokeColor = new Color(1f, 1f, 1f, 0.15f);
        p.lineWidth = 1f;

        if (_yAxis != null)
        {
            foreach (var label in _yAxis.Children())
            {
                float y =
                    label.worldBound.center.y -
                    worldBound.yMin;

                p.BeginPath();
                p.MoveTo(new Vector2(0, y));
                p.LineTo(new Vector2(width, y));
                p.Stroke();
            }
        }

        if (_xAxis != null)
        {
            foreach (var label in _xAxis.Children())
            {
                float x =
                    label.worldBound.center.x -
                    worldBound.xMin;

                p.BeginPath();
                p.MoveTo(new Vector2(x, 0));
                p.LineTo(new Vector2(x, height));
                p.Stroke();
            }
        }

        // Keine Daten

        if (_values == null || _values.Length < 2)
        {
            _values = new float[]
            {
                0,0,0,0,0,0,
                0,0,0,0,0,0
            };
        }

        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (float value in _values)
        {
            if (value < min) min = value;
            if (value > max) max = value;
        }

        float range = Mathf.Max(max - min, 1f);

        Vector2[] points = new Vector2[_values.Length];

        for (int i = 0; i < _values.Length; i++)
        {
            float x = width * i / (_values.Length - 1);

            float normalized =
                (_values[i] - min) / range;

            float y =
                height -
                (normalized * height);

            points[i] = new Vector2(x, y);
        }

        // Grüne Linie

        p.strokeColor = Color.green;
        p.lineWidth = 3f;

        p.BeginPath();
        p.MoveTo(points[0]);

        for (int i = 1; i < points.Length; i++)
        {
            p.LineTo(points[i]);
        }

        p.Stroke();

        // Punkte

        p.fillColor = Color.green;

        foreach (Vector2 point in points)
        {
            p.BeginPath();
            p.Arc(point, 4f, 0f, 360f);
            p.Fill();
        }
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