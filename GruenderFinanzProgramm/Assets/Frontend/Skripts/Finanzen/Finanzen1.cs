using UnityEngine;
using UnityEngine.UIElements;

public class Finanzen1 : MonoBehaviour
{
    private VisualElement _root;
    private LineChartElement _chart;

    private void Awake()
    {
        UIDocument document = GetComponent<UIDocument>();

        if (document == null)
        {
            Debug.LogError("[Finanzen2] Kein UIDocument gefunden.");
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
        VisualElement diagramm =
            _root.Q<VisualElement>("diagramm");

        VisualElement yAxis =
            _root.Q<VisualElement>("chart-y-axis");

        VisualElement xAxis =
            _root.Q<VisualElement>("chart-x-axis");

        if (diagramm == null)
        {
            Debug.LogError("[Finanzen2] Diagramm nicht gefunden.");
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

    public void SetDaten(float[] daten)
    {
        if (_chart != null)
        {
            _chart.SetValues(daten);
        }
    }

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

            // =====================================
            // HILFSLINIEN AUF LABEL-POSITIONEN
            // =====================================

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

            // =====================================
            // KEINE DATEN
            // =====================================

            if (_values == null || _values.Length < 2)
            {
                _values = new float[]
                {
                    0,0,0,0,0,0,
                    0,0,0,0,0,0
                };
            }

            // =====================================
            // MIN / MAX
            // =====================================

            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (float value in _values)
            {
                if (value < min) min = value;
                if (value > max) max = value;
            }

            float range = Mathf.Max(max - min, 1f);

            // =====================================
            // PUNKTE BERECHNEN
            // =====================================

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

            // =====================================
            // DIAGRAMMLINIE
            // =====================================

            p.strokeColor = Color.green;
            p.lineWidth = 3f;

            p.BeginPath();
            p.MoveTo(points[0]);

            for (int i = 1; i < points.Length; i++)
            {
                p.LineTo(points[i]);
            }

            p.Stroke();

            // =====================================
            // DATENPUNKTE
            // =====================================

            p.fillColor = Color.green;

            foreach (Vector2 point in points)
            {
                p.BeginPath();
                p.Arc(point, 4f, 0f, 360f);
                p.Fill();
            }
        }
    }
}