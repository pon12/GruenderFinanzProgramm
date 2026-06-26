using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class FinanzChartElement : VisualElement
{
    private float[] _einnahmen;
    private float[] _ausgaben;

    private readonly List<Label> _labels = new();

    private readonly string[] monate =
    {
        "Jan","Feb","Mrz","Apr",
        "Mai","Jun","Jul","Aug",
        "Sep","Okt","Nov","Dez"
    };

    public FinanzChartElement()
    {
        generateVisualContent += Draw;
    }

    public void SetData(float[] einnahmen, float[] ausgaben)
    {
        _einnahmen = einnahmen;
        _ausgaben = ausgaben;

        MarkDirtyRepaint();
    }

    void ClearLabels()
    {
        foreach (var l in _labels)
            l.RemoveFromHierarchy();

        _labels.Clear();
    }

    private void Draw(MeshGenerationContext ctx)
    {
        if (_einnahmen == null || _ausgaben == null)
            return;

        ClearLabels();

        float width = contentRect.width;
        float height = contentRect.height;

        float leftPad = 70;
        float bottomPad = 50;
        float topPad = 30;
        float rightPad = 20;

        var painter = ctx.painter2D;

        float maxValue = 0;

        for (int i = 0; i < 12; i++)
        {
            maxValue = Mathf.Max(maxValue, _einnahmen[i]);
            maxValue = Mathf.Max(maxValue, _ausgaben[i]);
        }

        if (maxValue <= 0)
            maxValue = 100;

        maxValue *= 1.1f;

        // =====================
        // ACHSEN
        // =====================

        painter.strokeColor = Color.white;
        painter.lineWidth = 2;

        painter.BeginPath();
        painter.MoveTo(new Vector2(leftPad, topPad));
        painter.LineTo(new Vector2(leftPad, height - bottomPad));
        painter.Stroke();

        painter.BeginPath();
        painter.MoveTo(new Vector2(leftPad, height - bottomPad));
        painter.LineTo(new Vector2(width - rightPad, height - bottomPad));
        painter.Stroke();

        // =====================
        // Y SKALA
        // =====================

        int gridLines = 5;

        for (int i = 0; i <= gridLines; i++)
        {
            float t = i / (float)gridLines;

            float value = maxValue * (1f - t);

            float y =
                topPad +
                (height - topPad - bottomPad) * t;

            painter.strokeColor =
                new Color(1, 1, 1, 0.15f);

            painter.lineWidth = 1;

            painter.BeginPath();
            painter.MoveTo(new Vector2(leftPad, y));
            painter.LineTo(new Vector2(width - rightPad, y));
            painter.Stroke();

            Label label = new Label(value.ToString("0") + " €");

            label.style.position = Position.Absolute;
            label.style.left = 5;
            label.style.top = y - 10;

            Add(label);
            _labels.Add(label);
        }

        // =====================
// SÄULEN
// =====================

float groupWidth =
    (width - leftPad - rightPad) / 12f;

float barWidth = groupWidth * 0.30f;

for (int i = 0; i < 12; i++)
{
    float centerX =
        leftPad + groupWidth * i + groupWidth * 0.5f;

    float einnahmenHeight =
        (_einnahmen[i] / maxValue)
        * (height - topPad - bottomPad);

    float ausgabenHeight =
        (_ausgaben[i] / maxValue)
        * (height - topPad - bottomPad);

    // ==================================
    // EINNAHMEN (GRÜN)
    // ==================================

    float ex = centerX - barWidth - 2;
    float ey = height - bottomPad - einnahmenHeight;

    painter.fillColor = Color.green;

    painter.BeginPath();
    painter.MoveTo(new Vector2(ex, ey));
    painter.LineTo(new Vector2(ex + barWidth, ey));
    painter.LineTo(new Vector2(ex + barWidth, height - bottomPad));
    painter.LineTo(new Vector2(ex, height - bottomPad));
    painter.LineTo(new Vector2(ex, ey));
    painter.Fill();

    // ==================================
    // AUSGABEN (ROT)
    // ==================================

    float ax = centerX + 2;
    float ay = height - bottomPad - ausgabenHeight;

    painter.fillColor = Color.red;

    painter.BeginPath();
    painter.MoveTo(new Vector2(ax, ay));
    painter.LineTo(new Vector2(ax + barWidth, ay));
    painter.LineTo(new Vector2(ax + barWidth, height - bottomPad));
    painter.LineTo(new Vector2(ax, height - bottomPad));
    painter.LineTo(new Vector2(ax, ay));
    painter.Fill();

    // ==================================
    // MONATSBESCHRIFTUNG
    // ==================================

    Label monat = new Label(monate[i]);

    monat.style.position = Position.Absolute;
    monat.style.left = centerX - 15;
    monat.style.top = height - bottomPad + 5;
    monat.style.color = Color.white;
    monat.style.fontSize = 10;

    Add(monat);
    _labels.Add(monat);
}
    }
}
