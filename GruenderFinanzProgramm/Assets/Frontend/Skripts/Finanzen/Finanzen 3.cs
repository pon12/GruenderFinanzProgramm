using UnityEngine;
using UnityEngine.UIElements;

public class Finanzen3 : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _diagramm;
    private VisualElement _Diagramm;

    private LineChartElement _chart;
    private PieChartElement _pieChart;

    private void Awake()
    {
        var document = GetComponent<UIDocument>();

        if (document == null)
        {
            Debug.LogError("[Finanzen3] UIDocument fehlt.");
            return;
        }

        _root = document.rootVisualElement;
    }

    private void Start()
    {
        SetupDiagramm();
    }

    private void SetupDiagramm()
    {
        _diagramm = _root.Q<VisualElement>("diagramm");
        _Diagramm = _root.Q<VisualElement>("Diagramm");

        if (_diagramm == null || _Diagramm == null)
        {
            Debug.LogError("[Finanzen3] Diagramm fehlt.");
            return;
        }

        // Layout FIX
        _diagramm.style.flexGrow = 1;
        _Diagramm.style.flexGrow = 1;

        _diagramm.style.width = Length.Percent(100);
        _diagramm.style.height = Length.Percent(100);

        _Diagramm.style.width = Length.Percent(100);
        _Diagramm.style.height = Length.Percent(100);

        VisualElement yAxis = _root.Q<VisualElement>("chart-y-axis");
        VisualElement xAxis = _root.Q<VisualElement>("chart-x-axis");

        // =========================
        // LINE CHART
        // =========================
        _chart = new LineChartElement(yAxis, xAxis);

        _chart.style.position = Position.Absolute;
        _chart.style.left = 0;
        _chart.style.right = 0;
        _chart.style.top = 0;
        _chart.style.bottom = 0;

        _diagramm.Add(_chart);

        _chart.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            _chart.MarkDirtyRepaint();
        });

        // =========================
        // PIE CHART
        // =========================
        _pieChart = new PieChartElement();

        _pieChart.SetValues(new float[]
        {
            30f, 25f, 6f, 10f, 29f
        });

        _pieChart.style.position = Position.Absolute;
        _pieChart.style.left = 0;
        _pieChart.style.right = 0;
        _pieChart.style.top = 0;
        _pieChart.style.bottom = 0;

        _Diagramm.Add(_pieChart);

        // TEST
        SetDaten(null); // zeigt jetzt eine 0-Linie
    }

    public void SetDaten(float[] daten)
    {
        _chart?.SetValues(daten);
    }

    public void SetPieDaten(float[] daten)
    {
        _pieChart?.SetValues(daten);
    }

    // =========================
    // LINE CHART (0-LINIE + DYNAMISCH)
    // =========================
    private class LineChartElement : VisualElement
    {
        private float[] _values;
        private readonly VisualElement _yAxis;
        private readonly VisualElement _xAxis;

        public LineChartElement(VisualElement yAxis, VisualElement xAxis)
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

        private float GetMax(float[] values)
        {
            float max = 0f;

            foreach (var v in values)
                if (v > max) max = v;

            return max;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;

            float width = contentRect.width;
            float height = contentRect.height;

            if (width <= 0 || height <= 0)
                return;

            // =========================
            // GRID
            // =========================
            p.strokeColor = new Color(1f, 1f, 1f, 0.15f);
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

            if (_xAxis != null)
            {
                foreach (var label in _xAxis.Children())
                {
                    float x = label.worldBound.center.x - worldBound.xMin;

                    p.BeginPath();
                    p.MoveTo(new Vector2(x, 0));
                    p.LineTo(new Vector2(x, height));
                    p.Stroke();
                }
            }

            // =========================
            // FALLBACK -> 0-LINIE
            // =========================
            if (_values == null || _values.Length < 2)
            {
                _values = new float[] { 0f, 0f };
            }

            // =========================
            // SCALE (0 → MAX)
            // =========================
            float max = Mathf.Max(1f, GetMax(_values));
            float range = max;

            Vector2[] points = new Vector2[_values.Length];

            for (int i = 0; i < _values.Length; i++)
            {
                float x = width * i / (_values.Length - 1);

                float normalized = _values[i] / range;
                float y = height - (normalized * height);

                points[i] = new Vector2(x, y);
            }

            // =========================
            // LINE
            // =========================
            p.strokeColor = Color.green;
            p.lineWidth = 3f;

            p.BeginPath();
            p.MoveTo(points[0]);

            for (int i = 1; i < points.Length; i++)
                p.LineTo(points[i]);

            p.Stroke();

            // =========================
            // POINTS
            // =========================
            p.fillColor = Color.green;

            foreach (var pt in points)
            {
                p.BeginPath();
                p.Arc(pt, 4f, 0f, 360f);
                p.Fill();
            }
        }
    }

    // =========================
    // PIE CHART
    // =========================
    private class PieChartElement : VisualElement
    {
        private float[] _values;

        private readonly Color[] _colors =
        {
            new Color(0.55f, 0.75f, 0.00f),
            new Color(0.32f, 0.35f, 0.24f),
            new Color(0.56f, 0.58f, 0.47f),
            new Color(0.67f, 0.69f, 0.58f),
            new Color(0.84f, 0.84f, 0.80f)
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