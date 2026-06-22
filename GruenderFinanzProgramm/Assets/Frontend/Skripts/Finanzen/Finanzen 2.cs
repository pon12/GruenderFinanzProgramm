using UnityEngine;
using UnityEngine.UIElements;

public class Finanzen2 : MonoBehaviour
{
    private VisualElement _root;
    private BarChartElement _chart;

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

        _chart = new BarChartElement(yAxis, xAxis);

        _chart.style.position = Position.Absolute;
        _chart.style.left = 0;
        _chart.style.right = 0;
        _chart.style.top = 0;
        _chart.style.bottom = 0;

        diagramm.Add(_chart);

        // TESTDATEN
       SetDaten(new float[]
    {
        // Jahr 1
        100000f,
        40000f,
        8000f,

        // Jahr 2
        220000f,
        90000f,
        45000f
    });
    }

    public void SetDaten(float[] daten)
    {
        if (_chart != null)
        {
            _chart.SetValues(daten);
        }
    }

   private class BarChartElement : VisualElement
{
    private float[] _values;

    private readonly VisualElement _yAxis;
    private readonly VisualElement _xAxis;

    public BarChartElement(
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
        // GRID-LINIEN
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

        // =====================================
        // KEINE DATEN
        // =====================================

        if (_values == null || _values.Length == 0)
            return;

        // =====================================
        // MAX-WERT ERMITTELN
        // =====================================

        float max = 1f;

        foreach (float value in _values)
        {
            if (value > max)
                max = value;
        }

        // =====================================
        // 2 JAHRE × 3 BALKEN
        // =====================================

        int years = 2;

        float groupWidth = width / years;

        float barWidth = 40f;
        float spacing = 10f;

        for (int year = 0; year < years; year++)
        {
            float startX =
                year * groupWidth +
                groupWidth / 2f -
                70f;

            for (int type = 0; type < 3; type++)
            {
                int index = year * 3 + type;

                if (index >= _values.Length)
                    continue;

                float value = _values[index];

                float normalized =
                    value / max;

                float barHeight =
                    normalized * height;

                float x =
                    startX +
                    type * (barWidth + spacing);

                float y =
                    height - barHeight;

                // Umsatz
                if (type == 0)
                {
                    p.fillColor =
                        new Color(
                            0.55f,
                            0.75f,
                            0.0f,
                            1f);
                }
                // Rohgewinn
                else if (type == 1)
                {
                    p.fillColor =
                        new Color(
                            0.32f,
                            0.36f,
                            0.25f,
                            1f);
                }
                // Ergebnis
                else
                {
                    p.fillColor =
                        new Color(
                            0.58f,
                            0.60f,
                            0.50f,
                            1f);
                }

                p.BeginPath();

                p.MoveTo(
                    new Vector2(
                        x,
                        height));

                p.LineTo(
                    new Vector2(
                        x,
                        y));

                p.LineTo(
                    new Vector2(
                        x + barWidth,
                        y));

                p.LineTo(
                    new Vector2(
                        x + barWidth,
                        height));

                p.ClosePath();
                p.Fill();
            }
        }
    }
}
}